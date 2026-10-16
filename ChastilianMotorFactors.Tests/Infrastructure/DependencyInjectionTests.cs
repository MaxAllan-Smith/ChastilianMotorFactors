using ChastilianMotorFactors.Infrastructure;
using ChastilianMotorFactors.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChastilianMotorFactors.Tests.Infrastructure
{
    public class DependencyInjectionTests
    {
        [Test]
        public void AddInfrastructure_WithConnectionString_RegistersDbContext()
        {
            IConfiguration configuration = BuildConfiguration(
                "Server=(localdb)\\MSSQLLocalDB;Database=Test;Trusted_Connection=True");
            ServiceCollection services = new();

            services.AddInfrastructure(configuration);

            // Resolving the context does not open a database connection.
            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();
            Assert.That(scope.ServiceProvider.GetService<ApplicationDbContext>(), Is.Not.Null);
        }

        [Test]
        public void AddInfrastructure_WithoutConnectionString_Throws()
        {
            IConfiguration configuration = BuildConfiguration(null);
            ServiceCollection services = new();

            Assert.Throws<InvalidOperationException>(() => services.AddInfrastructure(configuration));
        }

        private static IConfiguration BuildConfiguration(string? connectionString)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [$"ConnectionStrings:{DependencyInjection.DatabaseConnectionStringName}"] = connectionString
                })
                .Build();
        }
    }
}