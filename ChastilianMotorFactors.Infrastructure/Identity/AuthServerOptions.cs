namespace ChastilianMotorFactors.Infrastructure.Identity
{
    // Bound from the "AuthServer" configuration section.
    public sealed class AuthServerOptions
    {
        public const string SectionName = "AuthServer";

        // Thumbprints of the X.509 certificates used to sign and encrypt tokens outside Development
        // (loaded from the machine's certificate store, e.g. Azure App Service's WEBSITE_LOAD_CERTIFICATES).
        // Development uses OpenIddict's development certificates instead.
        public string? SigningCertificateThumbprint { get; init; }

        public string? EncryptionCertificateThumbprint { get; init; }

        // Client applications allowed to sign users in. Created or updated at startup.
        public List<AuthServerClientOptions> Clients { get; init; } = [];
    }
}