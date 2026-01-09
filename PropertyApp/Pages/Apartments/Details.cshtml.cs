using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.Apartments
{
    public class DetailsModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;

        public DetailsModel(PropertyApp.Data.PropertyContext context)
        {
            _context = context;
        }

        public Apartment Apartment { get; set; } = default!;
        public string? UserRole { get; set; }
        public User? TenantCurrent { get; set; }
        public User? OwnerCurrent { get; set; }
        public IList<MeasureDevice> MeasureDevices { get; set; } = default!;
        public string ContractFromDate { get; set; }
        public string ContractEndDate { get; set; }
        public ApartmentUser? FutureTenant { get; set; } // to hold future tenant data if needed
        public string OwnershipFromDate { get; set; }
        public string OwnershipEndDate { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Check that user has access to view apartment details
            var idCurrentUser = HttpContext.Session.GetInt32("IdCurrentUser");
            if (idCurrentUser == null) {
                return RedirectToPage("/Users/Login");
            }
            // Does this user have UserRole == "tenant" today in ApartmentUser table
            var hasAccessAsTenant = await _context.ApartmentUsers
                .AnyAsync(au => au.IdApartment == id &&
                                au.IdUser == idCurrentUser &&
                                au.UserRole == "tenant" &&
                                au.FromDate <= DateTime.Now &&
                                (au.EndDate == null || au.EndDate >= DateTime.Now));
            if (hasAccessAsTenant)
            {
                UserRole = "tenant";
            }

            // Does this user have UserRole == "owner" today in ApartmentUser table
            var hasAccessAsOwner = await _context.ApartmentUsers
                .AnyAsync(au => au.IdApartment == id &&
                                au.IdUser == idCurrentUser &&
                                au.UserRole == "owner" &&
                                au.FromDate <= DateTime.Now &&
                                (au.EndDate == null || au.EndDate >= DateTime.Now));
            if (hasAccessAsOwner)
            {
                UserRole = "owner";
            }

            if (UserRole == null)
            {
                // user has no access to this apartment
                return RedirectToPage("/Index");
            }

            var apartment = await _context.Apartments.FirstOrDefaultAsync(m => m.IdApartment == id);

            if (apartment is not null)
            {
                Apartment = apartment;

                // Get data of the linked Property
                await _context.Entry(Apartment)
                    .Reference(a => a.IdPropertyNavigation)
                    .LoadAsync();

                // Get both tenant and owner
                var tenantApartmentUser = await _context.ApartmentUsers
                    .Where(au => au.IdApartment == id &&
                                    au.UserRole == "tenant" &&
                                    au.FromDate <= DateTime.Now &&
                                    (au.EndDate == null || au.EndDate >= DateTime.Now))
                    .Include(au => au.IdUserNavigation)
                    .FirstOrDefaultAsync();
                if (tenantApartmentUser != null)
                {
                    // Get the tenant's data in Users table
                    TenantCurrent = await _context.Users
                        .FirstOrDefaultAsync(u => u.IdUser == tenantApartmentUser.IdUser);

                    // Get contract dates
                    ContractFromDate = tenantApartmentUser.FromDate.ToString("dd.MM.yyyy");
                    ContractEndDate = tenantApartmentUser.EndDate.HasValue
                        ? tenantApartmentUser.EndDate.Value.ToString("dd.MM.yyyy")
                        : "-";
                }
                

                
                var ownerApartmentUser = await _context.ApartmentUsers
                    .Where(au => au.IdApartment == id &&
                                    au.UserRole == "owner" &&
                                    au.FromDate <= DateTime.Now &&
                                    (au.EndDate == null || au.EndDate >= DateTime.Now))
                    .Include(au => au.IdUserNavigation)
                    .FirstOrDefaultAsync();
                if (ownerApartmentUser != null)
                {
                    // Get the owner's data in Users table
                    OwnerCurrent = await _context.Users
                        .FirstOrDefaultAsync(u => u.IdUser == ownerApartmentUser.IdUser);

                    // Get ownership dates
                    OwnershipFromDate = ownerApartmentUser.FromDate.ToString("dd.MM.yyyy");
                    OwnershipEndDate = ownerApartmentUser.EndDate.HasValue
                        ? ownerApartmentUser.EndDate.Value.ToString("dd.MM.yyyy")
                        : "-";
                }
                

                // Get MeasureDevices linked to this Apartment. These are in table MeasureDevices with foreign key IdApartment
                MeasureDevices = await _context.MeasureDevices
                    .Where(md => md.IdApartment == id)
                    .ToListAsync();

                // finally return the page and cross fingers
                return Page();
            }

            return NotFound();
        }
    }
}
