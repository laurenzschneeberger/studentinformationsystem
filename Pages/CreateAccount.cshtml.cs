using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudentInformationSystem.Models;
using StudentInformationSystem.Services;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace StudentInformationSystem.Pages
{
    public class CreateAccountModel : PageModel
    {
        private readonly UserService _userService;

        public CreateAccountModel(UserService userService)
        {
            _userService = userService;
        }

        [BindProperty]
        public required string Username { get; set; }

        [BindProperty]
        public required string Password { get; set; }

        [BindProperty]
        public required string ConfirmPassword { get; set; }

        [BindProperty]
        public bool IsAdmin { get; set; } = false;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Username is required.";
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Password is required.";
                return Page();
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return Page();
            }

            var user = new User
            {
                Username = Username,
                Password = Password,
                Role = IsAdmin ? "Admin" : "Student",
                Created = DateTime.Now
            };

            bool result = await _userService.CreateUserAsync(user);

            if (!result)
            {
                ErrorMessage = "Username already exists or there was an error creating the account.";
                return Page();
            }

            // Success message
            SuccessMessage = "Thank you for creating an account with SIS!";

            // Automatically log in the user
            await SignInUserAsync(Username, user.Role);

            // After a brief delay, redirect to appropriate page based on role
            string redirectPage = user.Role == "Admin" ? "/Students/Index" : "/Help";
            Response.Headers.Add("REFRESH", $"2;URL={redirectPage}");
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