using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.Apartments
{
    public class CreateModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;

        public CreateModel(PropertyApp.Data.PropertyContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
        ViewData["IdProperty"] = new SelectList(_context.Properties, "IdProperty", "IdProperty");
            return Page();
        }

        [BindProperty]
        public Apartment Apartment { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Apartments.Add(Apartment);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
