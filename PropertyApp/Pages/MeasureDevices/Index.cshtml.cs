using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.MeasureDevices
{
    public class IndexModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;

        public IndexModel(PropertyApp.Data.PropertyContext context)
        {
            _context = context;
        }

        public List<MeasureDevice> MeasureDevice { get; set; } = new();


        public Property Property { get; set; }  // Asunnot sidottu kiinteistöön
        public Apartment Apartment { get; set; }  // Mittarit sidottu asuntoon (jotta saadaan asunnon tiedot näkyviin)
        public ApartmentUser? ApartmentUser { get; set; } // Asukas näkyy MeasureDevices-sivulla


        [BindProperty(SupportsGet = true)]
        public int IdApartment { get; set; }


        #region-------------------------------ONGETASYNC---------------------------------------------------------------
        public async Task<IActionResult> OnGetAsync()
        {

            Apartment = await _context.Apartments
                .Include(a => a.IdPropertyNavigation)  // Liitetään kiinteistötiedot asuntoon
                .FirstOrDefaultAsync(a => a.IdApartment == IdApartment)
                ?? throw new InvalidOperationException("Apartment not found.");  // Apartment ei voi koskaan olla null (koska mittarit sidottu asuntoon, mutta varmuuden vuoksi tarkistus, koska koodiin tulee vihreä alleviivaus)

            if (Apartment == null)
            { 
                return NotFound("Apartment not found.");
            }

            Property = Apartment.IdPropertyNavigation;  // Haetaan kiinteistön tiedot asunnon kautta


            ApartmentUser = await _context.ApartmentUsers  // Haetaan asunnon aktiivinen asukas
                .Include(au => au.IdUserNavigation)        // Haetaan käyttäjän tiedot asukkaan roolia varten
                .Where(au => au.IdApartment == IdApartment &&
                             au.FromDate <= DateTime.Now &&
                             (au.EndDate == null || au.EndDate >= DateTime.Now))  
                .FirstOrDefaultAsync(); 


            // Haetaan mittarilista
            MeasureDevice = await _context.MeasureDevices
                .Where(d => d.IdApartment == IdApartment)
                .ToListAsync();


            // Tarkistetaan, onko asunto olemassa
            var apartmentExists = await _context.Apartments.AnyAsync(a => a.IdApartment == IdApartment);

            if (!apartmentExists)
            {
                return NotFound("Apartment not found, cannot create devices.");
            }

            // Asuntoon tarvittavat mittarityypit
            string[] requiredTypes = { "water-hot", "water-cold", "electricity", "heating" };
            bool changes = false;


            // Luodaan puuttuvat mittarit
            foreach (var type in requiredTypes)
            {
                if (!MeasureDevice.Any(d => d.DeviceType == type))
                {
                    var newDevice = new MeasureDevice
                    {
                        IdApartment = IdApartment,
                        DeviceType = type
                    };

                    _context.MeasureDevices.Add(newDevice);
                    MeasureDevice.Add(newDevice);
                    changes = true;
                }
            }

            // Tallennetaan uudet mittarit tietokantaan
            if (changes)
                await _context.SaveChangesAsync();


            // Suodatetaan listasta vain ne mittarit, jotka kuuluvat requiredTypes-listaan (jostain syystä loi ylimääräisiä mittareita, tämä varmistaa että vain tarvittavat näytetään)
            MeasureDevice = MeasureDevice
            .Where(d => requiredTypes.Contains(d.DeviceType))
            .ToList();

            return Page();
        }
    }
    #endregion
}
