using System.Security.Claims;
using ChastilianMotorFactors.Api.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ChastilianMotorFactors.Tests.Api
{
    public class ClaimDestinationsTests
    {
        [TestCase(Claims.Name, Scopes.Profile)]
        [TestCase(Claims.Email, Scopes.Email)]
        public void GetDestinations_WithMatchingScope_IncludesIdentityToken(string claimType, string scope)
        {
            Claim claim = CreateClaim(claimType, scope);

            Assert.That(ClaimDestinations.GetDestinations(claim),
                Is.EquivalentTo(new[] { Destinations.AccessToken, Destinations.IdentityToken }));
        }

        [TestCase(Claims.Name)]
        [TestCase(Claims.Email)]
        public void GetDestinations_WithoutMatchingScope_OnlyAccessToken(string claimType)
        {
            Claim claim = CreateClaim(claimType, null);

            Assert.That(ClaimDestinations.GetDestinations(claim), Is.EquivalentTo(new[] { Destinations.AccessToken }));
        }

        [Test]
        public void GetDestinations_OtherClaim_OnlyAccessToken()
        {
            Claim claim = CreateClaim("custom", Scopes.Profile);

            Assert.That(ClaimDestinations.GetDestinations(claim), Is.EquivalentTo(new[] { Destinations.AccessToken }));
        }

        private static Claim CreateClaim(string type, string? scope)
        {
            ClaimsIdentity identity = new();
            if (scope is not null)
            {
                identity.SetScopes(scope);
            }

            identity.AddClaim(new Claim(type, "value"));
            return identity.FindFirst(type)!;
        }
    }
}