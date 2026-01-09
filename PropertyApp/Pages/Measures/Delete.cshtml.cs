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
    public class DeleteModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;

        public DeleteModel(PropertyApp.Data.PropertyContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Measure Measure { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var measure = await _context.Measures.FirstOrDefaultAsync(m => m.MeasureId == id);

            if (measure is not null)
            {
                Measure = measure;

                return Page();
            }

            return NotFound();
        }

        public async Task<IActionResult> OnPostAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var measure = await _context.Measures.FindAsync(id);
            if (measure != null)
            {
                Measure = measure;
                _context.Measures.Remove(Measure);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index", new { IdMeasureDevice = Measure.IdMeasureDevice });  //välittää mittarin ID:n takaisin Indexiin
        }
    }
}
