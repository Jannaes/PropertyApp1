using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.Users;

public class IndexModel : PageModel
{
    private readonly PropertyContext _context;

    public IndexModel(PropertyContext context)
    {
        _context = context;
    }

    public IList<User> Users { get; set; } = new List<User>();

    public async Task OnGetAsync()
    {
        Users = await _context.Users.AsNoTracking().ToListAsync();
    }
}
