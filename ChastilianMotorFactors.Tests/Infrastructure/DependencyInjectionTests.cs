using ChastilianMotorFactors.Infrastructure;
using ChastilianMotorFactors.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace ChastilianMotorFactors.Tests.Infrastructure
{
    public class DependencyInjectionTests
    {
        private const string TestConnectionString =
            "Server=(localdb)\\MSSQLLocalDB;Database=Test;Trusted_Connection=True";

        [Test]
        public void AddInfrastructure_WithConnectionString_RegistersDbContext()
        {
            IConfiguration configuration = BuildConfiguration(TestConnectionString);
            ServiceCollection services = new();

            services.AddInfrastructure(configuration, CreateEnvironment(Environments.Development));

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

            Assert.Throws<InvalidOperationException>(() =>
                services.AddInfrastructure(configuration, CreateEnvironment(Environments.Development)));
        }

        [Test]
        public void AddInfrastructure_OutsideDevelopmentWithoutCertificates_Throws()
        {
            IConfiguration configuration = BuildConfiguration(TestConnectionString);
            ServiceCollection services = new();

            InvalidOperationException? exception = Assert.Throws<InvalidOperationException>(() =>
                services.AddInfrastructure(configuration, CreateEnvironment(Environments.Production)));
            Assert.That(exception?.Message, Does.Contain("SigningCertificateThumbprint"));
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

        private static IHostEnvironment CreateEnvironment(string environmentName)
        {
            return new HostingEnvironment { EnvironmentName = environmentName };
        }
    }
}