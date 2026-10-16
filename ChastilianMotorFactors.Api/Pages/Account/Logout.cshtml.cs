using ChastilianMotorFactors.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChastilianMotorFactors.Api.Pages.Account
{
    // Signs out of this server only. Client apps sign users out through the OpenID Connect
    // end-session endpoint (connect/endsession) instead.
    public sealed class LogoutModel(SignInManager<ApplicationUser> signInManager) : PageModel
    {
        // Signing out only happens on POST (with the antiforgery token), so other sites can't log users out via a link.
        public IActionResult OnGet()
        {
            return RedirectToPage("/Index");
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await signInManager.SignOutAsync();
            return RedirectToPage("/Index");
        }
    }
}