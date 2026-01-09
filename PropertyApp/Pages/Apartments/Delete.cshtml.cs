using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.Apartments
{
    public class DeleteModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;

        public DeleteModel(PropertyApp.Data.PropertyContext context)
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

            var apartment = await _context.Apartments.FirstOrDefaultAsync(m => m.IdApartment == id);

            if (apartment is not null)
            {
                Apartment = apartment;

                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var apartment = await _context.Apartments.FindAsync(id);
            if (apartment != null)
            {
                Apartment = apartment;
                _context.Apartments.Remove(Apartment);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
