using Cortex.Mediator.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace ChastilianMotorFactors.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            // Registers IMediator and every command, query and notification handler in this assembly.
            services.AddCortexMediator(new[] { typeof(DependencyInjection) });

            return services;
        }
    }
}