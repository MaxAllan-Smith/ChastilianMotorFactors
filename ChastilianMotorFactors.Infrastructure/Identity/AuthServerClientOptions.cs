namespace ChastilianMotorFactors.Infrastructure.Identity
{
    // A public client (SPA, mobile or desktop app) using the authorization code flow with PKCE.
    public sealed class AuthServerClientOptions
    {
        public required string ClientId { get; init; }

        public required string DisplayName { get; init; }

        // Exact URIs the server may redirect back to after sign-in. Anything else is rejected.
        public List<string> RedirectUris { get; init; } = [];

        // Exact URIs the server may redirect back to after sign-out.
        public List<string> PostLogoutRedirectUris { get; init; } = [];
    }
}