namespace ChastilianMotorFactors.Infrastructure.Identity
{
    // Values shared by the OpenIddict server configuration and the Api's authorization endpoints.
    public static class AuthServerConstants
    {
        // Scope a client requests to call this API; access tokens carrying it get the API audience.
        public const string ApiScope = "api";

        // Audience (resource) written into access tokens for this API and checked when validating them.
        public const string ApiResource = "chastilian-motor-factors-api";

        public const string AuthorizationEndpoint = "connect/authorize";
        public const string TokenEndpoint = "connect/token";
        public const string EndSessionEndpoint = "connect/endsession";
        public const string UserInfoEndpoint = "connect/userinfo";
    }
}