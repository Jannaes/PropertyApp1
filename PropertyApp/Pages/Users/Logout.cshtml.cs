using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PropertyApp.Pages.Users
{
    public class LogoutModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Clear session and sign out if needed
            HttpContext.Session.Clear();

            // Set session overrides to disable DevelopmentVariables fallbacks (taken from appsettings.Development.json)
            // Many pages use: HttpContext.Session.GetInt32("IdCurrentUser") ?? _devVars.IdCurrentUser
            // By setting these to 0 we avoid using dev config values after logout.
            HttpContext.Session.SetInt32("IdCurrentUser", 0);
            HttpContext.Session.SetInt32("IdMeasureDeviceOfInterest", 0);

            // Remove other user-specific strings if present
            HttpContext.Session.Remove("EmailCurrentUser");
            HttpContext.Session.Remove("FirstnameCurrentUser");
            HttpContext.Session.Remove("LastnameCurrentUser");

            return RedirectToPage("/Index");
        }
    }
}
