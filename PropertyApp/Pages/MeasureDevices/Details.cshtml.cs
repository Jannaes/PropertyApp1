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
    public class DetailsModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;

        public DetailsModel(PropertyApp.Data.PropertyContext context)
        {
            _context = context;
        }

        public MeasureDevice MeasureDevice { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var measuredevice = await _context.MeasureDevices.FirstOrDefaultAsync(m => m.IdMeasureDevice == id);

            if (measuredevice is not null)
            {
                MeasureDevice = measuredevice;

                return Page();
            }

            return NotFound();
        }
    }
}
