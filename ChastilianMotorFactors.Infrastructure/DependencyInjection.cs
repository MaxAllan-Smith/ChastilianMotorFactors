using ChastilianMotorFactors.Infrastructure.Identity;
using ChastilianMotorFactors.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace ChastilianMotorFactors.Infrastructure
{
    public static class DependencyInjection
    {
        public const string DatabaseConnectionStringName = "Database";

        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            // Fail at startup rather than on the first database call if the connection string is missing.
            string connectionString = configuration.GetConnectionString(DatabaseConnectionStringName)
                                      ?? throw new InvalidOperationException(
                                          $"Connection string '{DatabaseConnectionStringName}' is not configured.");

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(connectionString, sqlServerOptions =>
                    // Retries transient Azure SQL failures (e.g. failover, throttling).
                    sqlServerOptions.EnableRetryOnFailure());

                // Adds the OpenIddict entities (applications, authorizations, scopes, tokens) to the model.
                options.UseOpenIddict();
            });

            AddIdentity(services);
            AddAuthServer(services, configuration, environment);

            return services;
        }

        private static void AddIdentity(IServiceCollection services)
        {
            services
                .AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.User.RequireUniqueEmail = true;
                    options.Password.RequiredLength = 8;

                    // No email sender exists yet, so accounts can't be confirmed by email.
                    options.SignIn.RequireConfirmedAccount = false;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
        }

        private static void AddAuthServer(
            IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            IConfigurationSection section = configuration.GetSection(AuthServerOptions.SectionName);
            services.Configure<AuthServerOptions>(section);
            AuthServerOptions authServerOptions = section.Get<AuthServerOptions>() ?? new AuthServerOptions();

            services.AddOpenIddict()
                .AddCore(options => options
                    .UseEntityFrameworkCore()
                    .UseDbContext<ApplicationDbContext>())
                .AddServer(options =>
                {
                    options
                        .SetAuthorizationEndpointUris(AuthServerConstants.AuthorizationEndpoint)
                        .SetTokenEndpointUris(AuthServerConstants.TokenEndpoint)
                        .SetEndSessionEndpointUris(AuthServerConstants.EndSessionEndpoint)
                        .SetUserInfoEndpointUris(AuthServerConstants.UserInfoEndpoint);

                    options
                        .AllowAuthorizationCodeFlow()
                        .AllowRefreshTokenFlow()
                        .RequireProofKeyForCodeExchange();

                    options.RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.OfflineAccess,
                        AuthServerConstants.ApiScope);

                    AddCertificates(options, authServerOptions, environment);

                    // The Api's AuthorizationController handles these endpoints itself.
                    options.UseAspNetCore()
                        .EnableAuthorizationEndpointPassthrough()
                        .EnableTokenEndpointPassthrough()
                        .EnableEndSessionEndpointPassthrough()
                        .EnableUserInfoEndpointPassthrough();
                })
                .AddValidation(options =>
                {
                    // Validates access tokens issued by this same server for the API's own endpoints.
                    options.UseLocalServer();
                    options.AddAudiences(AuthServerConstants.ApiResource);
                    options.UseAspNetCore();
                });

            services.AddHostedService<AuthServerSeeder>();
        }

        private static void AddCertificates(
            OpenIddictServerBuilder options,
            AuthServerOptions authServerOptions,
            IHostEnvironment environment)
        {
            if (environment.IsDevelopment())
            {
                // Self-signed certificates generated and stored in the current user's certificate store.
                options
                    .AddDevelopmentSigningCertificate()
                    .AddDevelopmentEncryptionCertificate();
                return;
            }

            // Outside Development, real certificates are required so tokens survive restarts and scale-out.
            string signingThumbprint = RequireSetting(
                authServerOptions.SigningCertificateThumbprint, nameof(AuthServerOptions.SigningCertificateThumbprint));
            string encryptionThumbprint = RequireSetting(
                authServerOptions.EncryptionCertificateThumbprint,
                nameof(AuthServerOptions.EncryptionCertificateThumbprint));

            options
                .AddSigningCertificate(signingThumbprint)
                .AddEncryptionCertificate(encryptionThumbprint);
        }

        private static string RequireSetting(string? value, string name)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    $"Setting '{AuthServerOptions.SectionName}:{name}' is required outside Development.");
            }

            return value;
        }
    }
}