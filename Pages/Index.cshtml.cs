using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace StudentInformationSystem.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public string? InputText { get; set; }

        public void OnGet()
        {
        }
    }
}
