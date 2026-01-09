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
    public class IndexModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;

        public IndexModel(PropertyApp.Data.PropertyContext context)
        {
            _context = context;
        }

        public IList<UserAccess> UserAccess { get;set; } = default!;

        public async Task OnGetAsync()
        {
            UserAccess = await _context.UserAccesses
                .Include(u => u.IdApartmentNavigation)
                .Include(u => u.IdUserNavigation).ToListAsync();
        }
    }
}
