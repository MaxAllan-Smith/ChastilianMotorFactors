using ChastilianMotorFactors.Application;
using Cortex.Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace ChastilianMotorFactors.Tests.Application
{
    public class DependencyInjectionTests
    {
        [Test]
        public void AddApplication_RegistersMediator()
        {
            ServiceCollection services = new();
            services.AddLogging();

            services.AddApplication();

            using ServiceProvider provider = services.BuildServiceProvider();
            using IServiceScope scope = provider.CreateScope();
            Assert.That(scope.ServiceProvider.GetService<IMediator>(), Is.Not.Null);
        }
    }
}