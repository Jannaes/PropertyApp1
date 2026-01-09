using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PropertyApp.Config;
using PropertyApp.Data;
using PropertyApp.Models;
using System.ComponentModel.DataAnnotations;

namespace PropertyApp.Pages
{
    public class IndexModel : PageModel
    {
        private readonly PropertyContext _context;
        private readonly DevelopmentVariables _devVars;

        public IndexModel(PropertyContext context, IOptions<DevelopmentVariables> devVars)
        {
            _context = context;
            _devVars = devVars.Value;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Password is required")]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
        }

        public void OnGet()
        {
            var IdCurrentUser = HttpContext.Session.GetInt32("IdCurrentUser") ?? _devVars.IdCurrentUser;
            // If IdCurrentUser is not null try to get User data
            if (IdCurrentUser > 0)
                {
                var user = _context.Users.Find(IdCurrentUser);
                if (user != null)
                {
                    // Set session variables
                    HttpContext.Session.SetInt32("IdCurrentUser", user.IdUser);
                    HttpContext.Session.SetString("EmailCurrentUser", user.Email);
                    HttpContext.Session.SetString("FirstnameCurrentUser", user.Firstname);
                    HttpContext.Session.SetString("LastnameCurrentUser", user.Lastname);
                    // Redirect to Apartments/Index
                    Response.Redirect("/Apartments/Index");
                }
            }
        }

        // OnPostLogin for inline login form
        public async Task<IActionResult> OnPostLoginAsync()
        {
            if (!ModelState.IsValid)
                return Page();  // Stay on landing with errors

            // Query ERD User table for email/password match
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == Input.Email && u.Password == Input.Password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
                return Page();
            }

            // Set session (simple demo – use cookies/Identity later)
            HttpContext.Session.SetInt32("IdCurrentUser", user.IdUser);
            HttpContext.Session.SetString("EmailCurrentUser", user.Email);
            HttpContext.Session.SetString("FirstnameCurrentUser", user.Firstname);
            HttpContext.Session.SetString("LastnameCurrentUser", user.Lastname);

            return RedirectToPage("/Apartments/Index");  // To main home/dashboard with top bar
        }
    }
}