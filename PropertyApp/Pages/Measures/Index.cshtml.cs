using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.Measures
{
    public class IndexModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;  // tietokantayhteys

        public IndexModel(PropertyApp.Data.PropertyContext context)
        {
            _context = context;
        }

        // Bindatut ominaisuudet lomakkeelle ja URL:iin:
        [BindProperty(SupportsGet = true)] // IdApartment sidotaan URL:iin, jotta Razor Page tietää mistä IdApartment tulee
        public int IdApartment { get; set; }  // asunnon ID, koska mittarit sidottu tiettyyn asuntoon 

        [BindProperty(SupportsGet = true)]
        public int IdMeasureDevice { get; set; }  // määrittää minkä mittarin mittauksia näytetään

        [BindProperty]
        public double Amount { get; set; } // uusi mittaustulos

        [BindProperty]
        public DateTime NewDate { get; set; } = DateTime.Today; // mittauspäivämäärä


        //Page Modelin ominaisuudet:
        public Apartment? Apartment { get; set; } 
        public Property? Property { get; set; }

        public int CurrentUserId { get; set; }
        public List<Measure> Measures { get;set; } = new(); // lista mittauksista kyseiselle mittarille
        public MeasureDevice CurrentDevice { get; set; } = default!;



        #region -----------------------------ONGETASYNC---------------------------------------------------------------
        public async Task<IActionResult> OnGetAsync(int idMeasureDevice)   // haetaan asunnon ID mittauslaitteesta, jotta back-nappi toimii (koska mittarit yhdistetty asuntoihin)
        {

            CurrentUserId = HttpContext.Session.GetInt32("IdCurrentUser") ?? 0;  //kirjautunut käyttäjä

            if (CurrentUserId == 0)
            {
                return RedirectToPage("/Login");  // ei kirjautunut
            }


            // Haetaan laite ja asunto + kiinteistö
            CurrentDevice = await _context.MeasureDevices
                .Include(d => d.IdApartmentNavigation)  // liitetään asunto mittauslaitteeseen
                    .ThenInclude(a => a.IdPropertyNavigation)  // liitetään asuntoon kiinteistö
                .FirstOrDefaultAsync(d => d.IdMeasureDevice == IdMeasureDevice)
            ?? throw new InvalidOperationException("Device not found.");  // CurrentDevice ei voi koskaan olla null (laite löytyy aina, koska linkitetty asuntoon, mutta varmuuden vuoksi tarkistus, koska koodiin tulee vihreä alleviivaus)

            if (CurrentDevice == null)
                return NotFound("Device not found.");

            Apartment = CurrentDevice.IdApartmentNavigation;  // asetetaan PageModelin propertyyn
            Property = CurrentDevice.IdApartmentNavigation?.IdPropertyNavigation; // -:- 
            IdApartment = CurrentDevice.IdApartment;   // tallennetaan asunnon ID paluuta varten (back-nappi)
            IdMeasureDevice = idMeasureDevice; // -:-



            Measures = await _context.Measures    // Haetaan mittaukset tietylle mittauslaitteelle
                .Include(m => m.IdMeasureDeviceNavigation)
                .Where(m => m.IdMeasureDevice == IdMeasureDevice)
                .OrderBy(m => m.Date) // vanhin ensin koska muutokset lasketaan vanhempaan verrattuna (järjestys käännetään myöhemmin koodissa)
                .ToListAsync();


           // Suodatetaan mittaukset sisältämään vain käyttäjän voimassaolevana asukasaikana tekemät mittaukset:
            Measures = Measures.Where(m =>
            {
                var apartment = _context.Apartments
                    .FirstOrDefault(a => a.IdApartment == m.IdMeasureDeviceNavigation.IdApartment);
                if (apartment == null)
                    return false;
                var ownerships = _context.ApartmentUsers
                    .Where(o => o.IdApartment == apartment.IdApartment && o.IdUser == CurrentUserId && o.UserRole == "owner")
                    .ToList();
                var tenancies = _context.ApartmentUsers
                    .Where(t => t.IdApartment == apartment.IdApartment && t.IdUser == CurrentUserId && t.UserRole == "tenant")
                    .ToList();
                bool wasOwnerOrTenant = ownerships.Any(o => o.FromDate <= m.Date && (o.EndDate == null || o.EndDate >= m.Date)) ||
                                       tenancies.Any(t => t.FromDate <= m.Date && (t.EndDate == null || t.EndDate >= m.Date));
                return wasOwnerOrTenant;
            }).ToList();


            // Lasketaan muutokset vanhempaan verrattuna
            decimal? previousAmount = null;

            foreach (var measure in Measures)
            {
                if (previousAmount != null)
                {
                    measure.Change = measure.Amount - previousAmount.Value;
                }
                else
                {
                    measure.Change = null; // ensimmäinen mittaus, ei muutosta mihinkään verrattuna
                }

                previousAmount = measure.Amount; // seuraavaa mittausta varten
            }

            Measures = Measures.OrderByDescending(m => m.Date)  // järjestys käännetään, uusin ensin
                .ToList();  

            return Page();
        }

        #endregion

        #region ----------------------------ONPOSTAYNC----------------------------------------------------------------
        public async Task<IActionResult> OnPostAsync()    // Lisätään uusi mittaus, joka näkyy taulukossa heti
        {
            CurrentUserId = HttpContext.Session.GetInt32("IdCurrentUser") ?? 0;  //kirjautunut käyttäjä
            if (CurrentUserId == 0)
                return RedirectToPage("/Login");

            var measure = new Measure
            {
                IdMeasureDevice = IdMeasureDevice, //Käytetään BindPropertyllä sidottua arvoa
                Amount = (decimal)Amount,
                Date = NewDate,
                IdUser = CurrentUserId
            };

            _context.Measures.Add(measure);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { IdMeasureDevice = this.IdMeasureDevice }); // uudelleenlataus OnGetAsync:lle, jotta uusi mittaus näkyy listassa
        }

        #endregion
    }
}