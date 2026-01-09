using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.Users;

public class DeleteModel : PageModel
{
    private readonly PropertyContext _context;

    public DeleteModel(PropertyContext context)
    {
        _context = context;
    }

    [BindProperty]
    public User? User { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        User = await _context.Users.AsNoTracking().FirstOrDefaultAsync(m => m.IdUser == id);
        if (User == null) return NotFound();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null) return NotFound();

        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("Index");
    }
}
