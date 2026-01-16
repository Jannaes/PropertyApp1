using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyApp.Data;
using PropertyApp.Models;
using System.Security.Claims;

namespace PropertyApp.Pages.Apartments
{
    public class NewApartmentOrPropertyModel : PageModel
    {
        private readonly PropertyContext _context;

        // yhteys tietokantaan
        public NewApartmentOrPropertyModel(PropertyContext context)
        {
            _context = context;
        }


        [BindProperty]
        public Apartment Apartment { get; set; } = new Apartment();

        [BindProperty]
        public List<SelectListItem> UserProperties { get; set; } = new List<SelectListItem>();

        [BindProperty]
        public Property Property { get; set; } = new Property();

        [BindProperty]
        public bool CreateNewProperty { get; set; }

        public void OnGet()
        {
            // Hae kirjautuneen k‰ytt‰j‰n Id
            int userIdInt = HttpContext.Session.GetInt32("IdCurrentUser")!.Value;

            // 1? Hae kaikki k‰ytt‰j‰n Propertyt listaksi (SQL-kysely)
            var properties = _context.Properties
                .Where(p => p.IdUser == userIdInt)
                .ToList(); // -> tuodaan C#:n muistiin

            // 2? Hae kaikki Apartmentit, jotka liittyv‰t n‰ihin Propertyihin
            var apartments = _context.Apartments
                .Where(a => properties.Select(p => p.IdProperty).Contains(a.IdProperty))
                .ToList(); // -> myˆs C#:n muistiin

            // 3? Lis‰‰ EF Change Trackerin Apartmentit, jos ne on juuri lis‰tty
            var trackedApartments = _context.ChangeTracker.Entries<Apartment>()
                .Where(e => e.State != Microsoft.EntityFrameworkCore.EntityState.Deleted)
                .Select(e => e.Entity)
                .ToList();

            apartments.AddRange(trackedApartments);

            // 4? Suodata Propertyt n‰kyviin
            // K‰ytet‰‰n vain C# string-logiikkaa, ei IsNullOrWhiteSpace SQL:ss‰
            UserProperties = properties
                .Where(p => apartments.Any(a =>
                    a.IdProperty == p.IdProperty &&
                    (a.StaircaseDoor == null || a.StaircaseDoor.Trim() != "") // null tai ei-tyhj‰ merkkijono
                ))
                .Select(p => new SelectListItem
                {
                    Value = p.IdProperty.ToString(),
                    Text = p.Name
                })
                .ToList();
        }

        public async Task<IActionResult> OnPostApartmentAsync()
        {
            int userIdInt = HttpContext.Session.GetInt32("IdCurrentUser")!.Value;

            if (CreateNewProperty)
            {
                if (!string.IsNullOrWhiteSpace(Property.Name))
                {
                    Property.IdUser = userIdInt;

                    _context.Properties.Add(Property);
                    await _context.SaveChangesAsync();

                    Apartment.IdProperty = Property.IdProperty;
                }

                ModelState.Remove("Apartment.IdProperty"); // Ohitetaan validointi
            }

            if (!CreateNewProperty && Apartment.IdProperty == 0)
            {
                ModelState.AddModelError(string.Empty, "Please select or create a Property.");
                return Page();
            }

            // Lis‰t‰‰n Apartment
            _context.Apartments.Add(Apartment);
            await _context.SaveChangesAsync();

            // Lis‰t‰‰n ApartmentUser
            var apartmentUser = new ApartmentUser
            {
                IdApartment = Apartment.IdApartment,
                IdUser = userIdInt,
                UserRole = "owner",
                FromDate = DateTime.Today
            };
            _context.ApartmentUsers.Add(apartmentUser);

            // Lis‰t‰‰n UserAccess
            var userAccess = new UserAccess
            {
                IdApartment = Apartment.IdApartment,
                IdUser = userIdInt,
                FromDate = DateTime.Today
            };
            _context.UserAccesses.Add(userAccess);

            await _context.SaveChangesAsync();

            // Add default MeasureDevices
            string[] requiredTypes = { "water-hot", "water-cold", "electricity", "heating" };
            foreach (var type in requiredTypes)
            {
                var measureDevice = new MeasureDevice
                {
                    IdApartment = Apartment.IdApartment,
                    DeviceType = type,
                };
                _context.MeasureDevices.Add(measureDevice);
            }
            await _context.SaveChangesAsync();

            // Paluu Apartments Indexiin
            return RedirectToPage("/Apartments/Index");
        }

        public async Task<IActionResult> OnPostPropertyAsync()
        {
            try
            {
                int userIdInt = HttpContext.Session.GetInt32("IdCurrentUser")!.Value;
                Property.IdUser = userIdInt;

                _context.Properties.Add(Property);
                await _context.SaveChangesAsync();

                // Luodaan Apartment
                var apartment = new Apartment
                {
                    IdProperty = Property.IdProperty,
                    StaircaseDoor = null // omakotitalo
                };
                _context.Apartments.Add(apartment);
                await _context.SaveChangesAsync();

                // Lis‰t‰‰n ApartmentUser
                var apartmentUser = new ApartmentUser
                {
                    IdApartment = apartment.IdApartment,
                    IdUser = userIdInt,
                    UserRole = "owner",
                    FromDate = DateTime.Today
                };
                _context.ApartmentUsers.Add(apartmentUser);

                // Lis‰t‰‰n UserAccess
                var userAccess = new UserAccess
                {
                    IdApartment = apartment.IdApartment,
                    IdUser = userIdInt,
                    FromDate = DateTime.Today,
                    EndDate = null
                };
                _context.UserAccesses.Add(userAccess);

                // Add default MeasureDevices
                string[] requiredTypes = { "water-hot", "water-cold", "electricity", "heating" };
                foreach (var type in requiredTypes)
                {
                    var measureDevice = new MeasureDevice
                    {
                        IdApartment = apartment.IdApartment,
                        DeviceType = type,
                    };
                    _context.MeasureDevices.Add(measureDevice);
                }

                await _context.SaveChangesAsync();

                // Paluu Apartments Indexiin
                return RedirectToPage("/Apartments/Index");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                ModelState.AddModelError(string.Empty, ex.Message);
                return Page();
            }
        }



        //    public void OnGet()
        //    {

        //        // Haetaan nykyisen kirjautuneen k?ytt?j?n Id
        //        int userIdInt = HttpContext.Session.GetInt32("IdCurrentUser")!.Value;
        //        Property.IdUser = userIdInt;

        //       // Valitsetaan k?ytt? j?n Propertyt, vain Multi - apartment Properties
        //       UserProperties = _context.Properties
        //           .Where(p => p.IdUser == userIdInt &&
        //           _context.Apartments.Any(a => a.IdProperty == p.IdProperty && !string.IsNullOrWhiteSpace(a.StaircaseDoor)))
        //           // int == int
        //           .Select(p => new SelectListItem
        //           {
        //               Value = p.IdProperty.ToString(),
        //               Text = p.Name
        //           })
        //           .ToList();

        //    }

        //    public async Task<IActionResult> OnPostApartmentAsync()
        //    {
        //        //Haetaan kirjautuneen k?ytt?j?n Id 
        //        int userIdInt = HttpContext.Session.GetInt32("IdCurrentUser")!.Value;
        //        Property.IdUser = userIdInt;


        //        if (CreateNewProperty)
        //        {
        //            // Jos luodaan uusi Property
        //            if (!string.IsNullOrWhiteSpace(Property.Name))
        //            {
        //                Property.IdUser = userIdInt;

        //                _context.Properties.Add(Property);
        //                await _context.SaveChangesAsync();

        //                // yhdistetaan uusi Property Apartmenttiin
        //                Apartment.IdProperty = Property.IdProperty;
        //            }
        //        }
        //        // // Ohitetaan Apartment.IdProperty-validointi, kun luodaan uusi Property
        //        if (CreateNewProperty)
        //        {
        //            ModelState.Remove("Apartment.IdProperty");
        //        }

        //        // Valitsettu Property tarkistus
        //        if (!CreateNewProperty && Apartment.IdProperty == 0)
        //        {
        //            ModelState.AddModelError(string.Empty, "Please select or create a Property.");
        //            return Page();
        //        }

        //        // Lis?t??n Apartment
        //        _context.Apartments.Add(Apartment);
        //        await _context.SaveChangesAsync();

        //        // Lis?t??n ApartmentUser
        //        var apartmentUser = new ApartmentUser
        //        {
        //            IdApartment = Apartment.IdApartment,
        //            IdUser = userIdInt,
        //            UserRole = "owner",
        //            // FromDate = DateTime.Now
        //            FromDate = DateTime.Today
        //        };
        //        _context.ApartmentUsers.Add(apartmentUser);

        //        // Lis?t??n UserAccess
        //        var userAccess = new UserAccess
        //        {
        //            IdUser = userIdInt,
        //            IdApartment = Apartment.IdApartment,
        //            // FromDate = DateTime.Now
        //            FromDate = DateTime.Today
        //        };

        //        _context.UserAccesses.Add(userAccess);

        //        await _context.SaveChangesAsync();

        //        // Add default MeasureDevices for the new single-house
        //        string[] requiredTypes = { "water-hot", "water-cold", "electricity", "heating" };
        //        foreach (var type in requiredTypes)
        //        {
        //            var measureDevice = new MeasureDevice
        //            {
        //                IdApartment = Apartment.IdApartment,
        //                DeviceType = type,
        //            };
        //            _context.MeasureDevices.Add(measureDevice);
        //        }
        //        await _context.SaveChangesAsync();

        //        // Return to Apartments Index page
        //        return RedirectToPage("/Apartments/Index");

        //    }


        //    public async Task<IActionResult> OnPostPropertyAsync()
        //    {
        //        try
        //        {
        //            // Haetaan nykyisen kirjautuneen k?ytt?j?n Id
        //            int userIdInt = HttpContext.Session.GetInt32("IdCurrentUser")!.Value;
        //            Property.IdUser = userIdInt;

        //            _context.Properties.Add(Property);
        //            await _context.SaveChangesAsync();

        //            var apartment = new Apartment
        //            {
        //                IdProperty = Property.IdProperty,
        //                StaircaseDoor = null /*should be epmty field "Main"*/
        //            };

        //            _context.Apartments.Add(apartment);
        //            await _context.SaveChangesAsync();

        //            var apartmentUser = new ApartmentUser
        //            {
        //                IdApartment = apartment.IdApartment,
        //                IdUser = userIdInt,
        //                UserRole = "owner",
        //                FromDate = DateTime.Now
        //            };
        //            _context.ApartmentUsers.Add(apartmentUser);

        //            var userAccess = new UserAccess
        //            {
        //                IdApartment = apartment.IdApartment,
        //                IdUser = userIdInt,
        //                FromDate = DateTime.Now,
        //                EndDate = null
        //            };
        //            _context.UserAccesses.Add(userAccess);

        //            // Add default MeasureDevices for the new Apartment in a multi-apartment Property
        //            string[] requiredTypes = { "water-hot", "water-cold", "electricity", "heating" };
        //            foreach (var type in requiredTypes)
        //            {
        //                var measureDevice = new MeasureDevice
        //                {
        //                    IdApartment = apartment.IdApartment,
        //                    DeviceType = type,
        //                };
        //                _context.MeasureDevices.Add(measureDevice);
        //            }

        //            await _context.SaveChangesAsync();

        //            return RedirectToPage("/Apartments/Index");
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine(ex.Message);
        //            ModelState.AddModelError(string.Empty, ex.Message);
        //            return Page();
        //        }
        //    }

    }
}