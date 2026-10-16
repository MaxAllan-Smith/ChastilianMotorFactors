using System.ComponentModel.DataAnnotations;
using ChastilianMotorFactors.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace ChastilianMotorFactors.Api.Pages.Account
{
    public sealed class LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger) : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; } = new();

        // Where to go after signing in, usually the OpenID Connect authorize request that sent the user here.
        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // User names are the email address (see Register). Failed attempts count towards lockout.
            SignInResult result = await signInManager.PasswordSignInAsync(
                Input.Email, Input.Password, Input.RememberMe, true);

            if (result.Succeeded)
            {
                logger.LogInformation("User signed in");
                return LocalRedirect(GetSafeReturnUrl());
            }

            if (result.IsLockedOut)
            {
                logger.LogWarning("Sign-in rejected: account locked out");
                ModelState.AddModelError(string.Empty, "This account is temporarily locked. Try again later.");
                return Page();
            }

            // Same message whether the email or the password is wrong, so accounts can't be probed.
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return Page();
        }

        // Only redirect within this site, never to an address supplied by an attacker.
        private string GetSafeReturnUrl()
        {
            return Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/";
        }

        public sealed class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me")]
            public bool RememberMe { get; set; }
        }
    }
}