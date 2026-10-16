using System.Security.Claims;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ChastilianMotorFactors.Api.Identity
{
    // Decides which tokens each claim is copied into. OpenIddict leaves a claim out of every token
    // unless it has a destination, so new claim types must be added here deliberately.
    public static class ClaimDestinations
    {
        public static IEnumerable<string> GetDestinations(Claim claim)
        {
            switch (claim.Type)
            {
                case Claims.Name:
                    yield return Destinations.AccessToken;

                    // The identity token (read by the client app) only gets profile data the client asked for.
                    if (claim.Subject?.HasScope(Scopes.Profile) == true)
                    {
                        yield return Destinations.IdentityToken;
                    }

                    yield break;

                case Claims.Email:
                    yield return Destinations.AccessToken;

                    if (claim.Subject?.HasScope(Scopes.Email) == true)
                    {
                        yield return Destinations.IdentityToken;
                    }

                    yield break;

                default:
                    yield return Destinations.AccessToken;
                    yield break;
            }
        }
    }
}