using System.Security.Claims;
using ChastilianMotorFactors.Infrastructure.Identity;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ChastilianMotorFactors.Api.Identity
{
    // OpenID Connect endpoints. OpenIddict validates each request (client, redirect URI, PKCE, scopes)
    // before these actions run; the actions decide who the user is and which claims go into the tokens.
    [ApiExplorerSettings(IgnoreApi = true)]
    public sealed class AuthorizationController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager) : Controller
    {
        [HttpGet("~/" + AuthServerConstants.AuthorizationEndpoint)]
        [HttpPost("~/" + AuthServerConstants.AuthorizationEndpoint)]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Authorize(CancellationToken cancellationToken)
        {
            OpenIddictRequest request = GetOpenIddictRequest();

            // Is the user signed in to this server (Identity cookie from the Login page)?
            AuthenticateResult cookie = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            ApplicationUser? user = cookie.Succeeded ? await userManager.GetUserAsync(cookie.Principal) : null;

            if (user is null)
            {
                if (request.HasPromptValue(PromptValues.None))
                {
                    return ForbidWithError(Errors.LoginRequired, "The user is not signed in.");
                }

                // Send the user to the Login page, which returns here once they have signed in.
                return Challenge(
                    new AuthenticationProperties
                        { RedirectUri = Request.PathBase + Request.Path + GetRequestParameters() },
                    IdentityConstants.ApplicationScheme);
            }

            object application = await applicationManager.FindByClientIdAsync(request.ClientId!, cancellationToken)
                                 ?? throw new InvalidOperationException("The client application cannot be found.");

            // Only first-party clients exist, so no consent screen is implemented.
            if (await applicationManager.GetConsentTypeAsync(application, cancellationToken) != ConsentTypes.Implicit)
            {
                return ForbidWithError(Errors.ConsentRequired,
                    "This client requires user consent, which is not supported.");
            }

            ClaimsIdentity identity = CreateIdentity();
            await SetUserClaimsAsync(identity, user);
            identity.SetScopes(request.GetScopes());
            identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes(), cancellationToken)
                .ToListAsync(cancellationToken));

            // Reuse the user's existing authorization for this client, or create one. Refresh tokens are tied to it,
            // so revoking the authorization revokes them too.
            string userId = await userManager.GetUserIdAsync(user);
            string applicationId = await applicationManager.GetIdAsync(application, cancellationToken)
                                   ?? throw new InvalidOperationException("The client application has no identifier.");

            object? authorization = await authorizationManager.FindAsync(
                    userId,
                    applicationId,
                    Statuses.Valid,
                    AuthorizationTypes.Permanent,
                    identity.GetScopes(),
                    cancellationToken)
                .LastOrDefaultAsync(cancellationToken);

            authorization ??= await authorizationManager.CreateAsync(
                identity,
                userId,
                applicationId,
                AuthorizationTypes.Permanent,
                identity.GetScopes(),
                cancellationToken);

            identity.SetAuthorizationId(await authorizationManager.GetIdAsync(authorization, cancellationToken));
            identity.SetDestinations(ClaimDestinations.GetDestinations);

            // Returns an authorization code to the client's redirect URI.
            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [HttpPost("~/" + AuthServerConstants.TokenEndpoint)]
        [IgnoreAntiforgeryToken]
        [Produces("application/json")]
        public async Task<IActionResult> Exchange()
        {
            OpenIddictRequest request = GetOpenIddictRequest();

            if (!request.IsAuthorizationCodeGrantType() && !request.IsRefreshTokenGrantType())
            {
                throw new InvalidOperationException("The specified grant type is not supported.");
            }

            // The principal stored in the authorization code or refresh token, already validated by OpenIddict.
            AuthenticateResult result =
                await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            string? userId = result.Principal?.GetClaim(Claims.Subject);
            ApplicationUser? user = userId is null ? null : await userManager.FindByIdAsync(userId);

            // Re-check the account: it may have been deleted or locked since the code/refresh token was issued.
            if (result.Principal is null || user is null)
            {
                return ForbidWithError(Errors.InvalidGrant, "The token is no longer valid.");
            }

            if (!await signInManager.CanSignInAsync(user) || await userManager.IsLockedOutAsync(user))
            {
                return ForbidWithError(Errors.InvalidGrant, "The user is no longer allowed to sign in.");
            }

            // Keep the stored scopes, resources and authorization, but refresh the user's claims.
            ClaimsIdentity identity = CreateIdentity(result.Principal.Claims);
            await SetUserClaimsAsync(identity, user);
            identity.SetDestinations(ClaimDestinations.GetDestinations);

            return SignIn(new ClaimsPrincipal(identity), OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
        [HttpGet("~/" + AuthServerConstants.UserInfoEndpoint)]
        [HttpPost("~/" + AuthServerConstants.UserInfoEndpoint)]
        [IgnoreAntiforgeryToken]
        [Produces("application/json")]
        public async Task<IActionResult> UserInfo()
        {
            string? userId = User.GetClaim(Claims.Subject);
            ApplicationUser? user = userId is null ? null : await userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return Challenge(
                    CreateErrorProperties(Errors.InvalidToken, "The access token is no longer valid."),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            Dictionary<string, object> claims = new(StringComparer.Ordinal)
            {
                [Claims.Subject] = await userManager.GetUserIdAsync(user)
            };

            string? email = await userManager.GetEmailAsync(user);
            if (User.HasScope(Scopes.Email) && email is not null)
            {
                claims[Claims.Email] = email;
                claims[Claims.EmailVerified] = await userManager.IsEmailConfirmedAsync(user);
            }

            string? userName = await userManager.GetUserNameAsync(user);
            if (User.HasScope(Scopes.Profile) && userName is not null)
            {
                claims[Claims.Name] = userName;
            }

            return Ok(claims);
        }

        [HttpGet("~/" + AuthServerConstants.EndSessionEndpoint)]
        [HttpPost("~/" + AuthServerConstants.EndSessionEndpoint)]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EndSession()
        {
            // Sign out of this server, then let OpenIddict redirect to the client's post-logout URI
            // (if it sent a registered one), otherwise to the home page.
            await signInManager.SignOutAsync();

            return SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        private OpenIddictRequest GetOpenIddictRequest()
        {
            return HttpContext.GetOpenIddictServerRequest()
                   ?? throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");
        }

        private static ClaimsIdentity CreateIdentity(IEnumerable<Claim>? claims = null)
        {
            return new ClaimsIdentity(
                claims,
                TokenValidationParameters.DefaultAuthenticationType,
                Claims.Name,
                Claims.Role);
        }

        private async Task SetUserClaimsAsync(ClaimsIdentity identity, ApplicationUser user)
        {
            identity
                .SetClaim(Claims.Subject, await userManager.GetUserIdAsync(user))
                .SetClaim(Claims.Email, await userManager.GetEmailAsync(user))
                .SetClaim(Claims.Name, await userManager.GetUserNameAsync(user));
        }

        // The original query string (GET) or form fields (POST), so the request can be replayed after login.
        private QueryString GetRequestParameters()
        {
            return Request.HasFormContentType
                ? QueryString.Create(Request.Form.Select(field => KeyValuePair.Create(field.Key, field.Value)))
                : Request.QueryString;
        }

        private ForbidResult ForbidWithError(string error, string description)
        {
            return Forbid(CreateErrorProperties(error, description),
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        private static AuthenticationProperties CreateErrorProperties(string error, string description)
        {
            return new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
            });
        }
    }
}