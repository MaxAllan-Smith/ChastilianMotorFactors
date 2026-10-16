using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ChastilianMotorFactors.Infrastructure.Identity
{
    // Turns a configured client into the OpenIddict application record stored in the database.
    public static class ClientDescriptorFactory
    {
        public static OpenIddictApplicationDescriptor Create(AuthServerClientOptions client)
        {
            OpenIddictApplicationDescriptor descriptor = new()
            {
                ClientId = client.ClientId,
                DisplayName = client.DisplayName,

                // Public client: no secret (it can't keep one), so PKCE protects the code exchange.
                ClientType = ClientTypes.Public,

                // First-party clients only: users are not shown a consent screen.
                ConsentType = ConsentTypes.Implicit,
                Permissions =
                {
                    Permissions.Endpoints.Authorization,
                    Permissions.Endpoints.Token,
                    Permissions.Endpoints.EndSession,
                    Permissions.GrantTypes.AuthorizationCode,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.ResponseTypes.Code,
                    Permissions.Scopes.Email,
                    Permissions.Scopes.Profile,
                    Permissions.Prefixes.Scope + AuthServerConstants.ApiScope
                },
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            };

            foreach (string uri in client.RedirectUris)
            {
                descriptor.RedirectUris.Add(new Uri(uri, UriKind.Absolute));
            }

            foreach (string uri in client.PostLogoutRedirectUris)
            {
                descriptor.PostLogoutRedirectUris.Add(new Uri(uri, UriKind.Absolute));
            }

            return descriptor;
        }
    }
}