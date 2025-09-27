using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FunctionApp
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection RegisterDependencies(this IServiceCollection services)
        {
            services.AddApplicationInsightsTelemetryWorkerService()
                    .ConfigureFunctionsApplicationInsights();

            services.AddOptionsWithValidateOnStart<ApiManagementOptions>()
                    .BindConfiguration(ApiManagementOptions.SectionKey)
                    .ValidateDataAnnotations();

            // We're using DefaultAzureCredential which supports multiple authentication methods and picks the best one for the environment.
            // For production, prefer a specific TokenCredential implementation such as ManagedIdentityCredential for improved performance and predictability.
            // More info: https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/best-practices?tabs=aspdotnet
            services.AddSingleton<TokenCredential, DefaultAzureCredential>();

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
