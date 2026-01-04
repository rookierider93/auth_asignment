using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AuthApp.Pages.Account;

public class LoginModel : PageModel
{
    public IActionResult OnGet()
    {
        return Page();
    }

    // The actual SAML Challenge is triggered by the ExternalLogin endpoint
}
