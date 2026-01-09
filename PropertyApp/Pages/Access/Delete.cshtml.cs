using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.Access
{
    public class DeleteModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;

        public DeleteModel(PropertyApp.Data.PropertyContext context)
        {
            _context = context;
        }

        [BindProperty]
        public UserAccess UserAccess { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var useraccess = await _context.UserAccesses.FirstOrDefaultAsync(m => m.Id == id);

            if (useraccess is not null)
            {
                UserAccess = useraccess;

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

            var useraccess = await _context.UserAccesses.FindAsync(id);
            if (useraccess != null)
            {
                UserAccess = useraccess;
                _context.UserAccesses.Remove(UserAccess);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
