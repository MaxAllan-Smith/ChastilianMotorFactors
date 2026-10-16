using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace ChastilianMotorFactors.Infrastructure.Identity
{
    // At startup, creates or updates the API scope and every client listed in the "AuthServer" configuration.
    // Clients removed from configuration are left in the database; delete them deliberately if needed.
    // Requires the database to be migrated first.
    public sealed class AuthServerSeeder(
        IServiceScopeFactory scopeFactory,
        IOptions<AuthServerOptions> options,
        ILogger<AuthServerSeeder> logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            IOpenIddictScopeManager scopeManager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();
            IOpenIddictApplicationManager applicationManager =
                scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

            await SeedApiScopeAsync(scopeManager, cancellationToken);

            foreach (AuthServerClientOptions client in options.Value.Clients)
            {
                await SeedClientAsync(applicationManager, client, cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private static async Task SeedApiScopeAsync(IOpenIddictScopeManager scopeManager,
            CancellationToken cancellationToken)
        {
            if (await scopeManager.FindByNameAsync(AuthServerConstants.ApiScope, cancellationToken) is not null)
            {
                return;
            }

            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = AuthServerConstants.ApiScope,
                DisplayName = "Chastilian Motor Factors API",
                Resources = { AuthServerConstants.ApiResource }
            }, cancellationToken);
        }

        private async Task SeedClientAsync(
            IOpenIddictApplicationManager applicationManager,
            AuthServerClientOptions client,
            CancellationToken cancellationToken)
        {
            OpenIddictApplicationDescriptor descriptor = ClientDescriptorFactory.Create(client);
            object? existing = await applicationManager.FindByClientIdAsync(client.ClientId, cancellationToken);

            if (existing is null)
            {
                await applicationManager.CreateAsync(descriptor, cancellationToken);
                logger.LogInformation("Created OAuth client {ClientId}", client.ClientId);
            }
            else
            {
                await applicationManager.UpdateAsync(existing, descriptor, cancellationToken);
                logger.LogInformation("Updated OAuth client {ClientId}", client.ClientId);
            }
        }
    }
}