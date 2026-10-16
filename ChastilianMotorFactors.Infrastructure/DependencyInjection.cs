using ChastilianMotorFactors.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChastilianMotorFactors.Infrastructure
{
    public static class DependencyInjection
    {
        public const string DatabaseConnectionStringName = "Database";

        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Fail at startup rather than on the first database call if the connection string is missing.
            string connectionString = configuration.GetConnectionString(DatabaseConnectionStringName)
                                      ?? throw new InvalidOperationException(
                                          $"Connection string '{DatabaseConnectionStringName}' is not configured.");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString, sqlServerOptions =>
                    // Retries transient Azure SQL failures (e.g. failover, throttling).
                    sqlServerOptions.EnableRetryOnFailure()));

            return services;
        }
    }
}