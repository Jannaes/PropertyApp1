using Humanizer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PropertyApp.Data;
using PropertyApp.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PropertyApp.Pages.Apartments
{
    public class ChangeOwnerModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;
        private readonly ILogger<ChangeOwnerModel> _logger;

        public ChangeOwnerModel(PropertyContext context, ILogger<ChangeOwnerModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public ApartmentUser ApartmentUser { get; set; } = default!;
        public Apartment Apartment { get; set; } = default!;
        public Property Property { get; set; } = default!;

        [BindProperty]
        [EmailAddress]
        public string? NewOwnerEmail { get; set; }

        // Next scheduled owner (if any)
        public ApartmentUser? ExistingNextOwner { get; set; }
        public string? ExistingNextOwnerEmail { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Check that user has access to view apartment details
            var idCurrentUser = HttpContext.Session.GetInt32("IdCurrentUser");
            if (idCurrentUser == null)
            {
                return RedirectToPage("/Users/Login");
            }

            // Does this user have UserRole == "owner" today in ApartmentUser table
            var apartmentUser = await _context.ApartmentUsers
                .FirstOrDefaultAsync(au => au.IdApartment == id &&
                                au.IdUser == idCurrentUser &&
                                au.UserRole == "owner" &&
                                au.FromDate <= DateTime.Now &&
                                (au.EndDate == null || au.EndDate >= DateTime.Now));

            if (apartmentUser == null)
            {
                // User is not owner today. Return back to Apartments/Details with IdApartment
                return RedirectToPage("/Apartments/Details", new { id });
            }

            ApartmentUser = apartmentUser;

            var apartment = await _context.Apartments
                .FirstOrDefaultAsync(a => a.IdApartment == ApartmentUser.IdApartment);
            Apartment = apartment!;

            // Get data of the Property that is linked to the Apartment
            var property = await _context.Properties
                .FirstOrDefaultAsync(p => p.IdProperty == Apartment.IdProperty);
            Property = property!;

            // Find any future owner record (next scheduled owner)
            var nextOwner = await _context.ApartmentUsers
                .Where(au => au.IdApartment == ApartmentUser.IdApartment && au.UserRole == "owner" && au.FromDate > DateTime.Now)
                .Include(au => au.IdUserNavigation)
                .OrderBy(au => au.FromDate)
                .FirstOrDefaultAsync();

            ExistingNextOwner = nextOwner;
            ExistingNextOwnerEmail = nextOwner?.IdUserNavigation?.Email;

            ViewData["IdApartment"] = new SelectList(_context.Apartments, "IdApartment", "IdApartment");
            ViewData["IdUser"] = new SelectList(_context.Users, "IdUser", "IdUser");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Ignore validation for EF navigation props that aren't posted from the form
            ModelState.Remove("ApartmentUser.IdApartmentNavigation");
            ModelState.Remove("ApartmentUser.IdUserNavigation");

            if (!ModelState.IsValid)
            {
                foreach (var entry in ModelState)
                {
                    var errors = entry.Value.Errors.Select(e => !string.IsNullOrEmpty(e.ErrorMessage) ? e.ErrorMessage : (e.Exception?.Message ?? "[exception]"));
                    _logger.LogWarning("ModelState[{Key}] has {Count} errors: {Errors}", entry.Key, entry.Value.Errors.Count, string.Join("; ", errors));
                }

                await ReloadApartmentAndPropertyAsync();
                return Page();
            }

            _logger.LogWarning("Model is valid");

            // Load existing ApartmentUser from DB
            var existing = await _context.ApartmentUsers.FindAsync(ApartmentUser.Id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.FromDate = ApartmentUser.FromDate;
            existing.EndDate = ApartmentUser.EndDate;

            _context.ApartmentUsers.Update(existing);

            // If NewOwnerEmail provided, EndDate must be set
            if (!string.IsNullOrWhiteSpace(NewOwnerEmail) && !existing.EndDate.HasValue)
            {
                ModelState.AddModelError("ApartmentUser.EndDate", "If new owner is set then EndDate has to also be set.");
                _logger.LogWarning("ApartmentUser.EndDate. If new owner is set then EndDate has to also be set.");
                await ReloadApartmentAndPropertyAsync();
                return Page();
            }

            // If an email for new owner was provided, validate and create new ApartmentUser starting day after EndDate
            if (!string.IsNullOrWhiteSpace(NewOwnerEmail))
            {
                var newOwner = await _context.Users.FirstOrDefaultAsync(u => u.Email == NewOwnerEmail);
                if (newOwner == null)
                {
                    ModelState.AddModelError(nameof(NewOwnerEmail), "No user found with this email.");
                    _logger.LogWarning("No user found with email {NewOwnerEmail}", NewOwnerEmail);
                    await ReloadApartmentAndPropertyAsync();
                    return Page();
                }

                // Determine start date for new owner: day after EndDate (EndDate is required when NewOwnerEmail is provided)
                DateTime startDate = existing.EndDate.Value.AddDays(1);

                // Avoid creating duplicate active owner records
                var duplicate = await _context.ApartmentUsers.AnyAsync(au =>
                    au.IdApartment == existing.IdApartment &&
                    au.IdUser == newOwner.IdUser &&
                    au.UserRole == "owner" &&
                    au.FromDate <= startDate &&
                    (au.EndDate == null || au.EndDate >= startDate));

                if (!duplicate)
                {
                    var newApartmentUser = new ApartmentUser
                    {
                        IdApartment = existing.IdApartment,
                        IdUser = newOwner.IdUser,
                        UserRole = "owner",
                        FromDate = startDate,
                        EndDate = null
                    };

                    _context.ApartmentUsers.Add(newApartmentUser);
                }
                else
                {
                    ModelState.AddModelError(nameof(NewOwnerEmail), "This user is already an active owner for the apartment on the specified start date.");
                    _logger.LogWarning("Duplicate active owner record for user {NewOwnerEmail}", NewOwnerEmail);
                    await ReloadApartmentAndPropertyAsync();
                    return Page();
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {   
                _logger.LogError("Concurrency error when updating ApartmentUser with Id {Id}", ApartmentUser.Id);
                if (!ApartmentUserExists(ApartmentUser.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            _logger.LogWarning("Apartment ownership changed successfully.");
            // return RedirectToPage("./Index");
            return RedirectToPage("/Apartments/Details", new { id = existing.IdApartment });
        }

        public async Task<IActionResult> OnPostDeleteNextOwnerAsync(int deleteId)
        {
            var toDelete = await _context.ApartmentUsers.FindAsync(deleteId);
            if (toDelete == null)
            {
                return NotFound();
            }

            int aptId = toDelete.IdApartment;
            _context.ApartmentUsers.Remove(toDelete);
            await _context.SaveChangesAsync();

            // redirect back to this page for same apartment
            return RedirectToPage(new { id = aptId });
        }

        private async Task ReloadApartmentAndPropertyAsync()
        {
            if (ApartmentUser != null)
            {
                Apartment = await _context.Apartments.FirstOrDefaultAsync(a => a.IdApartment == ApartmentUser.IdApartment) ?? new Apartment();
                Property = await _context.Properties.FirstOrDefaultAsync(p => p.IdProperty == Apartment.IdProperty) ?? new Property();

                var nextOwner = await _context.ApartmentUsers
                    .Where(au => au.IdApartment == ApartmentUser.IdApartment && au.UserRole == "owner" && au.FromDate > DateTime.Now)
                    .Include(au => au.IdUserNavigation)
                    .OrderBy(au => au.FromDate)
                    .FirstOrDefaultAsync();

                ExistingNextOwner = nextOwner;
                ExistingNextOwnerEmail = nextOwner?.IdUserNavigation?.Email;
            }

            ViewData["IdApartment"] = new SelectList(_context.Apartments, "IdApartment", "IdApartment");
            ViewData["IdUser"] = new SelectList(_context.Users, "IdUser", "IdUser");
        }

        private bool ApartmentUserExists(int id)
        {
            return _context.ApartmentUsers.Any(e => e.Id == id);
        }
    }
}
