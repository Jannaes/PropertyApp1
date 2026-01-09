using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.FutureTenants
{
    public class FutureTenantModel : PageModel
    {
        private readonly PropertyContext _context;

        public FutureTenantModel(PropertyContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; } = string.Empty;
        public bool HasAccess(int userId, int apartmentId)
        {
            var au = _context.ApartmentUsers
                .FirstOrDefault(au => au.IdUser == userId
                                   && au.IdApartment == apartmentId
                                   && au.UserRole == "tenant"
                                   && (au.EndDate == null || au.EndDate >= DateTime.Today));

            return au != null;
        }

        [BindProperty(SupportsGet = true)]
        public int ApartmentId { get; set; }

        [BindProperty]
        public DateTime FromDate { get; set; } = DateTime.Now;

        [BindProperty]
        public DateTime? ToDate { get; set; }


        public int? CurrentUserId { get; set; }
        public string? CurrentUserEmail { get; set; }
        // Data to display after saving
        public User? SavedUser { get; set; }
        public ApartmentUser? SavedApartmentUser { get; set; }


        public void OnGet(int apartmentId)
        {

            // Get apartmentId from query parameter
            ApartmentId = apartmentId;

            // Check if the current user is the owner of the apartment
            var currentUserId = HttpContext.Session.GetInt32("IdCurrentUser");
            var isOwner = _context.ApartmentUsers
                .Any(au => au.IdApartment == ApartmentId && au.IdUser == currentUserId && au.UserRole == "owner");

            if (!isOwner)
            {
                ModelState.AddModelError("", "You are not the owner of this apartment.");
                return;
            }

            // Look for future tenant for this apartment
            var futureTenant = _context.ApartmentUsers
                .Where(au => au.IdApartment == ApartmentId
                         && au.UserRole == "tenant"
                         && au.FromDate > DateTime.Today)
                .OrderBy(au => au.FromDate)
                .FirstOrDefault();

            if (futureTenant != null)
            {
                SavedApartmentUser = futureTenant;
                SavedUser = _context.Users.FirstOrDefault(u => u.IdUser == futureTenant.IdUser);

                // Update bind properties 
                Email = SavedUser?.Email ?? string.Empty;
                FromDate = futureTenant.FromDate;
                ToDate = futureTenant.EndDate;
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var currentUserId = HttpContext.Session.GetInt32("IdCurrentUser");

            // Check if the current user is the owner
            bool isOwner = _context.ApartmentUsers
                .Any(au => au.IdApartment == ApartmentId
                        && au.IdUser == currentUserId
                        && au.UserRole == "owner");

            if (!isOwner)
            {
                ModelState.AddModelError("", "Only the apartment owner can add a tenant.");

                return Page();
            }

            // Find existing user by email
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == Email);

            if (existingUser == null)
            {
                existingUser = new User
                {
                    Email = Email,
                    Password = "0000",
                    Firstname = "Name",
                    Lastname = "Lastname"
                };

                _context.Users.Add(existingUser);
                await _context.SaveChangesAsync();
            }

            // Find last tenant to calculate newFromDate
            var lastTenant = _context.ApartmentUsers
                .Where(au => au.IdApartment == ApartmentId && au.UserRole == "tenant")
                .OrderByDescending(au => au.EndDate ?? DateTime.MaxValue)
                .FirstOrDefault();

            DateTime newFromDate = lastTenant != null
                ? (lastTenant.EndDate ?? lastTenant.FromDate).AddDays(1)
                : DateTime.Today;

            // Check if there is already a future tenant
            var futureTenant = _context.ApartmentUsers
                .FirstOrDefault(au => au.IdApartment == ApartmentId
                                   && au.UserRole == "tenant"
                                   && au.FromDate > DateTime.Today);

            if (futureTenant != null)
            {
                // Update existing future tenant dates
                futureTenant.FromDate = FromDate;
                futureTenant.EndDate = ToDate;
                SavedApartmentUser = futureTenant;
            }
            else
            {
                // Create new future tenant
                var apartmentUser = new ApartmentUser
                {
                    IdApartment = ApartmentId,
                    IdUser = existingUser.IdUser,
                    UserRole = "tenant",
                    FromDate = newFromDate,
                    EndDate = ToDate
                };

                _context.ApartmentUsers.Add(apartmentUser);
                SavedApartmentUser = apartmentUser;

                //Add access for the tenant so they can see the apartment after login
                var userAccess = new UserAccess
                {
                    IdApartment = ApartmentId,
                    IdUser = existingUser.IdUser,
                    FromDate = apartmentUser.FromDate,
                    EndDate = ToDate
                };
                _context.UserAccesses.Add(userAccess);
            }

            await _context.SaveChangesAsync();

            // Save user for display
            SavedUser = existingUser;

            // Update bind properties for UI
            FromDate = SavedApartmentUser.FromDate;
            ToDate = SavedApartmentUser.EndDate;

            ModelState.Clear();

            // return Page();
            return RedirectToPage("/Apartments/Details", new { id = ApartmentId });
        }


        public async Task<IActionResult> OnPostUpdateAsync()
        {
            // Find FUTURE tenant for this apartment
            var futureTenant = await _context.ApartmentUsers
                .Where(au => au.IdApartment == ApartmentId
                          && au.UserRole == "tenant"
                          && au.FromDate > DateTime.Today)  // future tenant
                .OrderBy(au => au.FromDate)
                .FirstOrDefaultAsync();

            if (futureTenant == null)
            {
                ModelState.AddModelError("", "Future tenant not found.");
                return Page();
            }

            // Validate new FromDate
            if (FromDate < futureTenant.FromDate)
            {
                ModelState.AddModelError("",
                    $"The new rental's start date must be after the current rental's end date ({futureTenant.FromDate:dd.MM.yyyy}).");
                SavedUser = new User { Email = Email };
                SavedApartmentUser = new ApartmentUser
                {
                    FromDate = FromDate,
                    EndDate = ToDate
                };

                return Page();
            }

            // Find user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.IdUser == futureTenant.IdUser);

            // Update dates of FUTURE tenant
            futureTenant.FromDate = FromDate;
            futureTenant.EndDate = ToDate;

            // Update UserAccess for the same tenant
            var userAccess = await _context.UserAccesses
                .FirstOrDefaultAsync(ua => ua.IdApartment == ApartmentId && ua.IdUser == futureTenant.IdUser);

            if (userAccess != null)
            {
                userAccess.FromDate = FromDate;
                userAccess.EndDate = ToDate;
            }

            await _context.SaveChangesAsync();

            // Update UI bind props
            SavedUser = user;
            SavedApartmentUser = futureTenant;

            // return Page();
            return RedirectToPage("/Apartments/Details", new { id = ApartmentId });
        }


    }
}



