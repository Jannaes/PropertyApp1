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
    public class IndexModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;

        public IndexModel(PropertyApp.Data.PropertyContext context)
        {
            _context = context;
        }

        public IList<Property> Property { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Property = await _context.Properties.ToListAsync();
            // .Include(@ => @.IdUserNavigation).ToListAsync();
        }
    }
}
