using ChastilianMotorFactors.Infrastructure.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ChastilianMotorFactors.Tests.Infrastructure
{
    public class ClientDescriptorFactoryTests
    {
        private static readonly AuthServerClientOptions Client = new()
        {
            ClientId = "spa",
            DisplayName = "Single-page app",
            RedirectUris = ["https://app.example.com/callback"],
            PostLogoutRedirectUris = ["https://app.example.com/"]
        };

        [Test]
        public void Create_ProducesPublicClientRequiringPkceWithoutConsentScreen()
        {
            OpenIddictApplicationDescriptor descriptor = ClientDescriptorFactory.Create(Client);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.ClientId, Is.EqualTo("spa"));
                Assert.That(descriptor.ClientType, Is.EqualTo(ClientTypes.Public));
                Assert.That(descriptor.ClientSecret, Is.Null);
                Assert.That(descriptor.ConsentType, Is.EqualTo(ConsentTypes.Implicit));
                Assert.That(descriptor.Requirements, Does.Contain(Requirements.Features.ProofKeyForCodeExchange));
            });
        }

        [Test]
        public void Create_AllowsOnlyAuthorizationCodeAndRefreshTokenFlows()
        {
            OpenIddictApplicationDescriptor descriptor = ClientDescriptorFactory.Create(Client);

            Assert.That(
                descriptor.Permissions.Where(permission =>
                    permission.StartsWith(Permissions.Prefixes.GrantType, StringComparison.Ordinal)),
                Is.EquivalentTo(new[] { Permissions.GrantTypes.AuthorizationCode, Permissions.GrantTypes.RefreshToken }));
        }

        [Test]
        public void Create_GrantsApiScopeAndCopiesRedirectUris()
        {
            OpenIddictApplicationDescriptor descriptor = ClientDescriptorFactory.Create(Client);

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.Permissions,
                    Does.Contain(Permissions.Prefixes.Scope + AuthServerConstants.ApiScope));
                Assert.That(descriptor.RedirectUris,
                    Is.EquivalentTo(new[] { new Uri("https://app.example.com/callback") }));
                Assert.That(descriptor.PostLogoutRedirectUris,
                    Is.EquivalentTo(new[] { new Uri("https://app.example.com/") }));
            });
        }
    }
}