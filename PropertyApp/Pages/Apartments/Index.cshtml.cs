using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using PropertyApp.Config;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.Apartments
{
    public class IndexModel : PageModel
    {
        private readonly PropertyContext _context;
        private readonly DevelopmentVariables _devVars;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(PropertyContext context, IOptions<DevelopmentVariables> devVars, ILogger<IndexModel> logger)
        {
            _context = context;
            _devVars = devVars.Value;
            _logger = logger;
        }

        // Expose to the Razor page
        public IList<Apartment> Apartments { get; set; } = default!;
        public int CurrentUserId { get; set; }
        public string? CurrentUserEmail { get; set; }

        // Map of apartment id -> user role for the current user
        public Dictionary<int, string?> ApartmentUserRoles { get; set; } = new Dictionary<int, string?>();


        public async Task OnGetAsync()
        {
            var currentUserId = HttpContext.Session.GetInt32("IdCurrentUser") ?? _devVars.IdCurrentUser;

            // log the raw config value early
            _logger.LogInformation("DevelopmentVariables.IdCurrentUser = {IdCurrentUserFromConfig}", currentUserId);

            // if currentUserId is null or 0 then get all apartments
            if (currentUserId == 0)
            {
                Apartments = await _context.Apartments
                    .Include(a => a.IdPropertyNavigation)
                    .ToListAsync();
                CurrentUserId = currentUserId;
                CurrentUserEmail = "All users";

                _logger.LogInformation("Showing all apartments because CurrentUserId is {Id}", currentUserId);
                return;
            }

            // set properties for the Razor page
            CurrentUserId = currentUserId;

            // Get Email of current user
            var currentUserEmail = await _context.Users
                .Where(u => u.IdUser == currentUserId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();

            CurrentUserEmail = currentUserEmail;

            // log retrieved email (avoid logging sensitive data in production)
            _logger.LogInformation("Resolved user email for Id {Id} => {Email}", CurrentUserId, CurrentUserEmail ?? "NULL");

            // Get Apartments where IdUser in ApartmentUser matches current user and current date is within FromDate and EndDate
            Apartments = await _context.Apartments
                .Include(a => a.IdPropertyNavigation)
                .Where(a => _context.ApartmentUsers
                    .Any(au => au.IdApartment == a.IdApartment &&
                               au.IdUser == currentUserId &&
                               au.FromDate <= DateTime.Now &&
                               (au.EndDate == null || au.EndDate >= DateTime.Now)))
                .ToListAsync();

            // Loop through each apartment and drop apartments where the user does not have access to the property in UserAccess
            //Apartments = Apartments.Where(a => _context.UserAccesses
            //    .Any(ua => ua.IdUser == currentUserId &&
            //               ua.IdApartment == a.IdApartment &&
            //               ua.FromDate <= DateTime.Now &&
            //               (ua.EndDate == null || ua.EndDate >= DateTime.Now)))
            //    .ToList();

            // Populate ApartmentUserRoles for the current user and the apartments we will display
            var apartmentIds = Apartments.Select(a => a.IdApartment).ToList();
            if (apartmentIds.Count > 0)
            {
                var auList = await _context.ApartmentUsers
                    .Where(au => au.IdUser == currentUserId
                                 && apartmentIds.Contains(au.IdApartment)
                                 && au.FromDate <= DateTime.Now
                                 && (au.EndDate == null || au.EndDate >= DateTime.Now))
                    .ToListAsync();

                ApartmentUserRoles = auList.ToDictionary(au => au.IdApartment, au => au.UserRole);
            }

            // log summary information about the loaded apartments and roles
            _logger.LogInformation("Loaded {ApartmentCount} apartments for user {UserId}. Roles loaded for {RoleCount} apartments.",
                Apartments.Count, CurrentUserId, ApartmentUserRoles.Count);
        }
    }
}
