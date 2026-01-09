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
    public class DetailsModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;

        public DetailsModel(PropertyApp.Data.PropertyContext context)
        {
            _context = context;
        }

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
    }
}
