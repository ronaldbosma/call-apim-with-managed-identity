using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FunctionApp
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection RegisterDependencies(this IServiceCollection services, IConfigurationManager configuration)
        {
            services.AddApplicationInsightsTelemetryWorkerService()
                    .ConfigureFunctionsApplicationInsights();

            services.AddOptionsWithValidateOnStart<ApiManagementOptions>()
                    .BindConfiguration(ApiManagementOptions.SectionKey)
                    .ValidateDataAnnotations();

            services.AddScoped<AzureCredentialsAuthorizationHandler>();

            services.AddHttpClient("apim", (sp, client) =>
                    {
                        var options = sp.GetRequiredService<IOptions<ApiManagementOptions>>().Value;
                        client.BaseAddress = new Uri(options.GatewayUrl);
                    })
                    .AddHttpMessageHandler<AzureCredentialsAuthorizationHandler>();

            return services;
        }
    }
}
