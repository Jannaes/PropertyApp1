using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PropertyApp.Data;
using PropertyApp.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PropertyApp.Pages.Users
{
    //[IgnoreAntiforgeryToken]
    public class LoginModel : PageModel
    {
        private readonly PropertyApp.Data.PropertyContext _context;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(PropertyApp.Data.PropertyContext context, ILogger<LoginModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public User User { get; set; } = default!;

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
        }

        public void OnGet()
        {
            _logger.LogInformation("Login page visited");
        }


        public async Task<IActionResult> OnPostAsync()
        {
            _logger.LogInformation("Login attempt started for email: {Email}", Input?.Email ?? "(null)");
            _logger.LogCritical("Login test error for {Email}", Input?.Email ?? "(null)");


            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login attempt with invalid model state for email: {Email}", Input?.Email ?? "(null)");
                return Page();
            }

            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(m => m.Email == Input.Email);

                if (user is not null)
                {
                    _logger.LogInformation("User record found for email: {Email}, IdUser: {IdUser}", user.Email, user.IdUser);

                    if (user.Password == Input.Password)
                    {
                        // Store IdUser into session so other pages can read it from there
                        HttpContext.Session.SetInt32("IdCurrentUser", user.IdUser);
                        HttpContext.Session.SetString("EmailCurrentUser", user.Email);
                        HttpContext.Session.SetString("FirstnameCurrentUser", user.Firstname);
                        HttpContext.Session.SetString("LastnameCurrentUser", user.Lastname);

                        User = user;
                        _logger.LogInformation("User {Email} (Id: {IdUser}) authenticated successfully and stored in session", user.Email, user.IdUser);

                        
                        if (user.Password != "0000")
                        {
                            // Successful login, redirect to Index page of Apartments
                            return RedirectToPage("/Apartments/Index");
                        }
                        else
                        {   // New user invated by apartment owner to login wiht email and password "0000"
                            // User/Create does not allow password "0000", so we can be sure this is invited user
                            _logger.LogInformation("User {Email} (Id: {IdUser}) is using default password, redirecting to User/Edit to update account details", user.Email, user.IdUser);
                            // Redirect to User/Edit page to force user to change password and update details
                            return RedirectToPage("/Users/Edit", new { id = user.IdUser });
                        }
                    }
                    else
                    {
                        // Password incorrect. For security reasons, do not reveal which one was
                        // invalid, email or password.
                        _logger.LogWarning("Authentication failed for email: {Email} — password mismatch", Input?.Email ?? "(null)");
                        ModelState.AddModelError(string.Empty, "Invalid Email (....or Password).");
                        return Page();
                    }
                }
                else
                {
                    // User with provided email not found. For security reasons, do not reveal which one was
                    // invalid, email or password.
                    _logger.LogWarning("Authentication failed — no user found for email: {Email}", Input?.Email ?? "(null)");
                    ModelState.AddModelError(string.Empty, "Invalid Email (....or Password).");
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An exception occurred during login attempt for email: {Email}", Input?.Email ?? "(null)");
                ModelState.AddModelError(string.Empty, "An unexpected error occurred. Please try again later.");
                return Page();
            }
        }
    }
}
