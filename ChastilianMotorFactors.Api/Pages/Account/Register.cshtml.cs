using System.ComponentModel.DataAnnotations;
using ChastilianMotorFactors.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ChastilianMotorFactors.Api.Pages.Account
{
    public sealed class RegisterModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<RegisterModel> logger) : PageModel
    {
        [BindProperty]
        public InputModel Input { get; set; } = new();

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

            // The email doubles as the user name used to sign in.
            ApplicationUser user = new() { UserName = Input.Email, Email = Input.Email };
            IdentityResult result = await userManager.CreateAsync(user, Input.Password);

            if (!result.Succeeded)
            {
                // Identity's password rules and duplicate-account checks, shown on the form.
                foreach (IdentityError error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return Page();
            }

            logger.LogInformation("User {UserId} registered", user.Id);
            await signInManager.SignInAsync(user, false);

            return LocalRedirect(Url.IsLocalUrl(ReturnUrl) ? ReturnUrl : "/");
        }

        public sealed class InputModel
        {
            [Required]
            [EmailAddress]
            [StringLength(256)]
            public string Email { get; set; } = string.Empty;

            // Length and character rules are enforced by Identity (see Infrastructure DependencyInjection).
            [Required]
            [DataType(DataType.Password)]
            [StringLength(128)]
            public string Password { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }
    }
}