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

namespace PropertyApp.Pages.Apartments
{
    public class EditModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;

        public EditModel(PropertyApp.Data.PropertyContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Apartment Apartment { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var apartment =  await _context.Apartments.FirstOrDefaultAsync(m => m.IdApartment == id);
            if (apartment == null)
            {
                return NotFound();
            }
            Apartment = apartment;
           ViewData["IdProperty"] = new SelectList(_context.Properties, "IdProperty", "IdProperty");
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(Apartment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ApartmentExists(Apartment.IdApartment))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool ApartmentExists(int id)
        {
            return _context.Apartments.Any(e => e.IdApartment == id);
        }
    }
}
