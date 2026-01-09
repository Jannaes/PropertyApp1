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

namespace PropertyApp.Pages.Properties
{
    public class EditModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;
        private readonly ILogger<EditModel> _logger;

        public EditModel(PropertyApp.Data.PropertyContext context, ILogger<EditModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public Property Property { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var property =  await _context.Properties.FirstOrDefaultAsync(m => m.IdProperty == id);
            if (property == null)
            {
                return NotFound();
            }
            Property = property;
           ViewData["IdUser"] = new SelectList(_context.Users, "IdUser", "IdUser");
            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {   
            _logger.LogInformation("OnPostAsync called for Property.IdProperty={Id}", Property?.IdProperty);
            ModelState.Remove("Property.IdUserNavigation");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState invalid: {@ModelState}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                ViewData["IdUser"] = new SelectList(_context.Users, "IdUser", "IdUser");
                return Page();
            }

            _context.Attach(Property).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PropertyExists(Property.IdProperty))
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

        private bool PropertyExists(int id)
        {
            return _context.Properties.Any(e => e.IdProperty == id);
        }
    }
}
