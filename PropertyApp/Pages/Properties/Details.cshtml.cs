using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.Properties
{
    public class DetailsModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;

        public DetailsModel(PropertyApp.Data.PropertyContext context)
        {
            _context = context;
        }

        public Property Property { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var property = await _context.Properties.FirstOrDefaultAsync(m => m.IdProperty == id);

            if (property is not null)
            {
                Property = property;

                return Page();
            }

            return NotFound();
        }
    }
}
