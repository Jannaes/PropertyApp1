using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.Properties
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
        ViewData["IdUser"] = new SelectList(_context.Users, "IdUser", "IdUser");
            return Page();
        }

        [BindProperty]
        public Property Property { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("Property.IdUserNavigation");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Properties.Add(Property);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
