using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyApp.Data;
using PropertyApp.Models;
using Microsoft.Extensions.Logging;

namespace PropertyApp.Pages.Apartments
{
    public class TerminateRentingModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;
        private readonly ILogger<TerminateRentingModel> _logger;

        public TerminateRentingModel(PropertyContext context, ILogger<TerminateRentingModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Do not bind the full Apartment on POST to avoid validating navigation props
        public Apartment Apartment { get; set; } = default!;

        [BindProperty]
        public int ApartmentId { get; set; }

        public Property Property { get; set; } = default!;
        public int? CurrentUserId { get; set; }
        public string? CurrentUserRole { get; set; }

        // Expose owner and tenant users for display
        public User? OwnerCurrent { get; set; }
        public User? TenantCurrent { get; set; }

        // Bind the tenant contract DTO for editing FromDate/EndDate
        [BindProperty]
        public TenantContractEdit TenantContract { get; set; } = new TenantContractEdit();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
                return NotFound();

            var apartment = await _context.Apartments.FirstOrDefaultAsync(m => m.IdApartment == id);
            if (apartment == null)
                return NotFound();

            Apartment = apartment;
            ApartmentId = apartment.IdApartment;

            var property = await _context.Properties.FirstOrDefaultAsync(p => p.IdProperty == apartment.IdProperty);
            if (property == null)
                return NotFound();

            Property = property;

            CurrentUserId = HttpContext.Session.GetInt32("IdCurrentUser");

            if (!CurrentUserId.HasValue)
            {
                _logger.LogInformation("User has no active role today. CurrentUserId={CurrentUserId}, ApartmentId={ApartmentId}", CurrentUserId, apartment.IdApartment);
                return RedirectToPage("/Apartments/Index");
            }

            var now = DateTime.Today;
            var role = await _context.ApartmentUsers
                .Where(au => au.IdApartment == apartment.IdApartment
                             && au.IdUser == CurrentUserId.Value
                             && au.FromDate <= now
                             && (au.EndDate == null || au.EndDate >= now))
                .Select(au => au.UserRole)
                .FirstOrDefaultAsync();

            CurrentUserRole = role;

            // Load current tenant ApartmentUser (active today) to populate DTO
            var now2 = DateTime.Today;
            var tenantApartmentUser = await _context.ApartmentUsers
                .Where(au => au.IdApartment == apartment.IdApartment
                             && au.UserRole == "tenant"
                             && au.FromDate <= now2
                             && (au.EndDate == null || au.EndDate >= now2))
                .FirstOrDefaultAsync();

            if (tenantApartmentUser != null)
            {
                TenantContract = new TenantContractEdit
                {
                    Id = tenantApartmentUser.Id,
                    IdApartment = tenantApartmentUser.IdApartment,
                    IdUser = tenantApartmentUser.IdUser,
                    UserRole = tenantApartmentUser.UserRole,
                    FromDate = tenantApartmentUser.FromDate,
                    EndDate = tenantApartmentUser.EndDate
                };

                // if FromDate is 01.01.0001, set it to today for better UX
                if (TenantContract.FromDate == DateTime.MinValue)
                {
                    TenantContract.FromDate = DateTime.Today;
                }

                TenantCurrent = await _context.Users.FirstOrDefaultAsync(u => u.IdUser == tenantApartmentUser.IdUser);
            }
            else
            {
                TenantCurrent = null;
                return RedirectToPage("./Details", new { id = Apartment.IdApartment });
            }

            var ownerApartmentUser = await _context.ApartmentUsers
                .Where(au => au.IdApartment == apartment.IdApartment
                             && au.UserRole == "owner"
                             && au.FromDate <= now2
                             && (au.EndDate == null || au.EndDate >= now2))
                .FirstOrDefaultAsync();

            if (ownerApartmentUser != null)
                OwnerCurrent = await _context.Users.FirstOrDefaultAsync(u => u.IdUser == ownerApartmentUser.IdUser);

            ViewData["IdProperty"] = new SelectList(_context.Properties, "IdProperty", "IdProperty");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Fallback: if ApartmentId did not bind, try TenantContract.IdApartment
            if (ApartmentId == 0 && TenantContract?.IdApartment != 0)
            {
                ApartmentId = TenantContract.IdApartment;
            }

            // Ensure Apartment is loaded for redisplay or redirect
            if (ApartmentId != 0)
            {
                Apartment = await _context.Apartments.FindAsync(ApartmentId) ?? new Apartment { IdApartment = ApartmentId };
            }

            // Validate DTO
            if (!ModelState.IsValid)
            {
                _logger.LogInformation("TenantContract DTO not valid on TerminateRenting POST. ApartmentId={ApartmentId}, TenantContractId={TenantContractId}, TenantContract.IdApartment={TenantContractIdApartment}", ApartmentId, TenantContract?.Id, TenantContract?.IdApartment);
                return Page();
            }

            if (TenantContract.Id == 0)
            {
                _logger.LogInformation("No contract id supplied, cannot update. ApartmentId={ApartmentId}, TenantContract.IdApartment={TenantContractIdApartment}", ApartmentId, TenantContract?.IdApartment);
                return RedirectToPage("./Details", new { id = ApartmentId });
            }

            var existing = await _context.ApartmentUsers.FindAsync(TenantContract.Id);
            if (existing == null)
                return NotFound();

            existing.FromDate = TenantContract.FromDate;
            existing.EndDate = TenantContract.EndDate;

            _context.ApartmentUsers.Update(existing);
            await _context.SaveChangesAsync();
            _logger.LogInformation("SAVED! Should now navigate to Details with ApartmentId={ApartmentId}, TenantContract.IdApartment={TenantContractIdApartment}", ApartmentId, TenantContract?.IdApartment);
            return RedirectToPage("./Details", new { id = ApartmentId });
        }

        private bool ApartmentExists(int id)
        {
            return _context.Apartments.Any(e => e.IdApartment == id);
        }
    }
}
