using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudentInformationSystem.Services;
using System.Security.Claims;

namespace StudentInformationSystem.Pages
{
    public class LoginModel : PageModel
    {
        private readonly UserService _userService;

        public LoginModel(UserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public required string Username { get; set; }

        [BindProperty]
        public required string Password { get; set; }

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            // Check if user is already authenticated
            if (User.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole("Admin"))
                {
                    Response.Redirect("/Students/Index");
                }
                else
                {
                    Response.Redirect("/Help");
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Check hardcoded credentials first
            if (Username == "Laurenz" && Password == "$4lauSchne!")
            {
                await SignInUserAsync(Username, "Admin"); // Laurenz is an Admin
                return RedirectToPage("/Students/Index");
            }

            // If not hardcoded, check the user database
            var user = await _userService.ValidateUserAsync(Username, Password);
            if (user != null)
            {
                await SignInUserAsync(user.Username, user.Role);

                // Redirect based on role
                if (user.Role == "Admin")
                {
                    return RedirectToPage("/Students/Index");
                }
                else
                {
                    return RedirectToPage("/Help");
                }
            }

            ErrorMessage = "Invalid username or password";
            return Page();
        }

        private async Task SignInUserAsync(string username, string role)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }
    }
}