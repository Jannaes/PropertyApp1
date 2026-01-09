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

namespace PropertyApp.Pages.MeasureDevices
{
    public class EditModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;

        public EditModel(PropertyApp.Data.PropertyContext context)
        {
            _context = context;
        }

        [BindProperty]
        public MeasureDevice MeasureDevice { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var measuredevice =  await _context.MeasureDevices.FirstOrDefaultAsync(m => m.IdMeasureDevice == id);
            if (measuredevice == null)
            {
                return NotFound();
            }
            MeasureDevice = measuredevice;
           ViewData["IdApartment"] = new SelectList(_context.Apartments, "IdApartment", "IdApartment");
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

            _context.Attach(MeasureDevice).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MeasureDeviceExists(MeasureDevice.IdMeasureDevice))
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

        private bool MeasureDeviceExists(int id)
        {
            return _context.MeasureDevices.Any(e => e.IdMeasureDevice == id);
        }
    }
}
