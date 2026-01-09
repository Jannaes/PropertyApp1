using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using PropertyApp.Data;
using PropertyApp.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PropertyApp.Pages.Tenants
{
    public class TenantModel : PageModel
    {
        private readonly PropertyContext _context;

        public TenantModel(PropertyContext context)
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

        [BindProperty]
        public int ApartmentId { get; set; }

        //Uncomment to add the calendar for setting the dates.
        //[BindProperty] 
        //public DateTime FromDate { get; set; } = DateTime.Now;

        //[BindProperty]
        //public DateTime? ToDate { get; set; }


        public int? CurrentUserId { get; set; }
        public string? CurrentUserEmail { get; set; }
        // Data to display after saving
        public User? SavedUser { get; set; }
        public ApartmentUser? SavedApartmentUser { get; set; }


        public void OnGet(int apartmentId)
        {

            // Get current user id from session
            var currentUserId = HttpContext.Session.GetInt32("IdCurrentUser");


            // Check if the current user is the owner of the apartment
            var hasAccess = _context.ApartmentUsers.Any(au =>
                au.IdApartment == apartmentId &&
                au.IdUser == currentUserId &&
                au.UserRole == "owner"
            );

            if (!hasAccess)
            {
                ModelState.AddModelError("", "You are not the owner of this apartment.");
                return;
            }

            // Take apartment id from query string
            ApartmentId = apartmentId;

            // Get existing tenant for the apartment
            SavedApartmentUser = _context.ApartmentUsers
            .Include(au => au.IdApartmentNavigation) //Get data about apartment for the tenant
            .FirstOrDefault(au => au.IdApartment == ApartmentId
                           && au.UserRole == "tenant"
                           && (au.EndDate == null || au.EndDate >= DateTime.Today));

            if (SavedApartmentUser != null)
            {
                // Find tenant user details
                SavedUser = _context.Users.FirstOrDefault(u => u.IdUser == SavedApartmentUser.IdUser);

                if (SavedUser != null)
                {
                    // Auto-fill tenant email
                    Email = SavedUser.Email;
                }
                // Rental dates for display (Uncomment for calendar)
                //FromDate = SavedApartmentUser.FromDate;
                //ToDate = SavedApartmentUser.EndDate;
            }
        }
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var currentUserId = HttpContext.Session.GetInt32("IdCurrentUser");

            // Verify that the currently logged-in user is the owner of the apartment before adding a tenant
            var isOwner = _context.ApartmentUsers
                .Any(au => au.IdApartment == ApartmentId && au.IdUser == currentUserId!.Value && au.UserRole == "owner");

            if (!isOwner)
            {
                ModelState.AddModelError("", "Only the apartment owner can add a tenant.");
                return Page();
            }

            // Check if user with the given email exists
            var existingUser = _context.Users.FirstOrDefault(u => u.Email == Email);

            if (existingUser != null)
            {
                var isOwnerUser = _context.ApartmentUsers
                    .Any(au => au.IdApartment == ApartmentId && au.IdUser == existingUser.IdUser && au.UserRole.ToLower() == "owner");

            }


            if (existingUser == null)
            {
                // If not, create a new user with default values
                var newUser = new User
                {
                    Email = Email,
                    Password = "0000",
                    Firstname = "NotSpecified",
                    Lastname = "NotSpecified"
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                existingUser = newUser;
            }

            // Find an active tenant for this apartment (EndDate not set or still in the future)
            var existingApartmentUser = _context.ApartmentUsers
           .FirstOrDefault(au => au.IdUser == existingUser.IdUser && au.IdApartment == ApartmentId && au.UserRole == "tenant" && (au.EndDate == null || au.EndDate >= DateTime.Today));

            if (existingApartmentUser == null)
            {
                // Add new apartment user entry
                var apartmentUser = new ApartmentUser
                {
                    IdApartment = ApartmentId,
                    IdUser = existingUser.IdUser,
                    UserRole = "tenant",
                    //FromDate = DateTime.Now,
                    //EndDate = ToDate // Uncomment to set end date from calendar
                };
                _context.ApartmentUsers.Add(apartmentUser);

                // Add access for the tenant so they can see the apartment after login
                var userAccess = new UserAccess
                {
                    IdApartment = ApartmentId,
                    IdUser = existingUser.IdUser,
                    FromDate = apartmentUser.FromDate,
                    EndDate = apartmentUser.EndDate
                };
                _context.UserAccesses.Add(userAccess);


                await _context.SaveChangesAsync();

                // Save for display
                SavedUser = existingUser;
                SavedApartmentUser = apartmentUser;

                ApartmentId = SavedApartmentUser.IdApartment;

                // Update BindProperties to reflect saved dates(Uncomment for calendar)
                //FromDate = SavedApartmentUser.FromDate;
                //ToDate = SavedApartmentUser.EndDate;

                // Clear form 
                ModelState.Clear();

                return Page();
            }
            else
            {
                // Existing tenant found — load data for display and editing
                SavedApartmentUser = existingApartmentUser;

                var tenantUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.IdUser == existingApartmentUser.IdUser);

                SavedUser = tenantUser;

                ApartmentId = SavedApartmentUser.IdApartment;

                // Initialize BindProperties to reflect current tenant dates (Uncomment for calendar)
                //FromDate = SavedApartmentUser.FromDate;
                //ToDate = SavedApartmentUser.EndDate;
            }


            // Ñlear email input after processing
            Email = string.Empty;

            return Page(); // stay on the same page

        }

        #region Uncomment to add the calendar for setting the dates.

        //public async Task<IActionResult> OnPostUpdateAsync()
        //{
        //    // Find apartment user entry
        //    var apartmentUser = await _context.ApartmentUsers
        //        .FirstOrDefaultAsync(au => au.IdApartment == ApartmentId && au.UserRole == "tenant");

        //    if (apartmentUser == null)
        //        return Page();

        //    // Find user
        //    var user = await _context.Users.FirstOrDefaultAsync(u => u.IdUser == apartmentUser.IdUser);

        //    // Update dates
        //    apartmentUser.FromDate = FromDate;
        //    apartmentUser.EndDate = ToDate;

        //    await _context.SaveChangesAsync();

        //    //Update BindProperties to reflect changes
        //    SavedUser = user;
        //    SavedApartmentUser = apartmentUser;

        //    return Page();
        //}

        #endregion

    }
}
