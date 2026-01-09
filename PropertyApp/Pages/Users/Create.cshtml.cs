using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using PropertyApp.Data;
using PropertyApp.Models;

namespace PropertyApp.Pages.Users;

public class CreateModel : PageModel
{
    private readonly PropertyContext _context;

    public CreateModel(PropertyContext context)
    {
        _context = context;
    }

    [BindProperty]
    public User User { get; set; } = new User();

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();

        // Disallow empty password
        if (string.IsNullOrEmpty(User.Password) || string.IsNullOrWhiteSpace(User.Password))
        {
            // Bind error to the nested field so the validation message appears next to the password input
            ModelState.AddModelError("User.Password", "Password cannot be empty. Please enter a valid password.");
            return Page();
        }

        // Disallow placeholder password "0000" for new users so it is sure that means invited user  
        if (!string.IsNullOrEmpty(User.Password) && User.Password.Trim() == "0000")
        {
            // Bind error to the nested field so the validation message appears next to the password input
            ModelState.AddModelError("User.Password", "Password cannot be '0000'. Please choose a different password.");
            return Page();
        }

        // Disallow duplicate email addresses
        var existingUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == User.Email && u.IdUser != User.IdUser);
        if (existingUser != null)
        {
            ModelState.AddModelError("User.Email", "This email address is already in use. Please use a different email.");
            return Page();
        }

        _context.Users.Add(User);
        await _context.SaveChangesAsync();

        // Store IdUser into session so other pages can read it from there
        HttpContext.Session.SetInt32("IdCurrentUser", User.IdUser);
        HttpContext.Session.SetString("EmailCurrentUser", User.Email);
        HttpContext.Session.SetString("FirstnameCurrentUser", User.Firstname);
        HttpContext.Session.SetString("LastnameCurrentUser", User.Lastname);

        return RedirectToPage("Index");
    }
}
