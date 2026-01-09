using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.Users;

public class EditModel : PageModel
{
    private readonly PropertyContext _context;

    public EditModel(PropertyContext context)
    {
        _context = context;
    }

    [BindProperty]
    public User User { get; set; } = default!;

    // Indicates that the existing user has the invited placeholder password "0000"
    public bool IsInvitedUser { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        // If id is not currently logged in user, redirect to index
        // so only current user can edit their own details
        var idCurrentUser = HttpContext.Session.GetInt32("IdCurrentUser");
        if (id != idCurrentUser) return RedirectToPage("/Apartments/Index");

        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.IdUser == id);
        if (user == null) return NotFound();

        User = user;
        IsInvitedUser = string.Equals(user.Password?.Trim(), "0000", StringComparison.Ordinal);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Load existing user first to decide password rules
        var existingUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.IdUser == User.IdUser);
        if (existingUser == null) return NotFound();

        // If the existing password is the invited placeholder "0000", require a new password
        var isInvitedUser = string.Equals(existingUser.Password?.Trim(), "0000", StringComparison.Ordinal);
        IsInvitedUser = isInvitedUser; // ensure view knows the invited state on redisplay

        // Validate ModelState first for other fields
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // If invited user, password must be provided and cannot be "0000"
        if (isInvitedUser)
        {
            if (string.IsNullOrWhiteSpace(User.Password))
            {
                ModelState.AddModelError("User.Password", "You must set a new password before continuing.");
                return Page();
            }

            if (User.Password.Trim() == "0000")
            {
                ModelState.AddModelError("User.Password", "Password cannot be '0000'. Please choose a different password.");
                return Page();
            }

            // For invited user: allow the provided password (could hash here)
        }
        else
        {
            // Non-invited users: if password field is empty, preserve existing password
            if (string.IsNullOrWhiteSpace(User.Password))
            {
                User.Password = existingUser.Password;
            }
            else
            {
                // Disallow insecure placeholder password "0000"
                if (User.Password.Trim() == "0000")
                {
                    ModelState.AddModelError("User.Password", "Password cannot be '0000'. Please choose a different password.");
                    return Page();
                }
                // Otherwise a new password was provided (could hash here)
            }
        }

        // Disallow duplicate email addresses if user changed email to one already in use
        var existingEmail = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Email == User.Email && e.IdUser != User.IdUser);
        if (existingEmail != null)
        {
            ModelState.AddModelError("User.Email", "This email address is already in use. Please use a different email.");
            return Page();
        }

        _context.Attach(User).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(User.IdUser)) return NotFound();
            throw;
        }

        // Store IdUser into session so other pages can read it from there
        HttpContext.Session.SetInt32("IdCurrentUser", User.IdUser);
        HttpContext.Session.SetString("EmailCurrentUser", User.Email);
        HttpContext.Session.SetString("FirstnameCurrentUser", User.Firstname);
        HttpContext.Session.SetString("LastnameCurrentUser", User.Lastname);

        // return RedirectToPage("/MeasureDevices/Index", new { IdApartment = IdApartment });  //välittää asunnon ID:n takaisin Indexiin
        // Return to Apartments/Index page after editing user
        return RedirectToPage("/Apartments/Index");
    }

    private bool UserExists(int id) => _context.Users.Any(e => e.IdUser == id);
}
