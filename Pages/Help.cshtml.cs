using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StudentInformationSystem.Pages
{
    [Authorize(Roles = "Admin,Student")]
    public class HelpModel : PageModel
    {
        public void OnGet()
        {
            // No additional logic needed
        }
    }
}