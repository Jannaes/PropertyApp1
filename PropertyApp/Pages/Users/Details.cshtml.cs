using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.Users;

public class DetailsModel : PageModel
{
    private readonly PropertyContext _context;

    public DetailsModel(PropertyContext context)
    {
        _context = context;
    }

    public User? User { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        User = await _context.Users.AsNoTracking().FirstOrDefaultAsync(m => m.IdUser == id);

        if (User == null) return NotFound();

        return Page();
    }
}
