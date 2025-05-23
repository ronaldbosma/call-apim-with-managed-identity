using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;

namespace FunctionApp
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection RegisterDependencies(this IServiceCollection services, IConfigurationManager configuration)
        {
            services.AddApplicationInsightsTelemetryWorkerService()
                    .ConfigureFunctionsApplicationInsights();

            services.AddHttpClient("apim", client =>
            {
                client.BaseAddress = new Uri(configuration["ApiManagement_gatewayUrl"] ?? throw new ConfigurationErrorsException("Setting ApiManagement_gatewayUrl not specified"));
            });

            return services;
        }
    }
}
