using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.Measures
{
    public class EditModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;

        public EditModel(PropertyApp.Data.PropertyContext context)
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

            var measure =  await _context.Measures.FirstOrDefaultAsync(m => m.MeasureId == id);
            if (measure == null)
            {
                return NotFound();
            }
            Measure = measure;
           ViewData["IdMeasureDevice"] = new SelectList(_context.MeasureDevices, "IdMeasureDevice", "IdMeasureDevice");
           ViewData["IdUser"] = new SelectList(_context.Users, "IdUser", "IdUser");
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
                return Page();

            var measureInDb = await _context.Measures
                .FirstOrDefaultAsync(m => m.MeasureId == Measure.MeasureId);

            if (measureInDb == null)
                return NotFound();

            // Päivitä vain sallitut kentät
            measureInDb.Amount = Measure.Amount;
            measureInDb.Date = Measure.Date;

            // Näitä EI muuteta Editissä, koska ne ovat hidden
            // measureInDb.IdMeasureDevice = Measure.IdMeasureDevice;
            // measureInDb.IdUser = Measure.IdUser;

            await _context.SaveChangesAsync();

            return RedirectToPage("./Index", new { IdMeasureDevice = measureInDb.IdMeasureDevice });
        }

        private bool MeasureExists(long id)
        {
            return _context.Measures.Any(e => e.MeasureId == id);
        }
    }
}
