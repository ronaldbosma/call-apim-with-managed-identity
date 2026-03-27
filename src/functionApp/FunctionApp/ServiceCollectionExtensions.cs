using Azure.Core;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;

using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using OpenTelemetry.Trace;

namespace FunctionApp
{
    internal static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureOpenTelemetry(this IServiceCollection services)
        {
            services.AddOpenTelemetry()
                .WithTracing(tracing => tracing
                    // Enables HttpClient instrumentation.
                    .AddHttpClientInstrumentation());

            services.AddOpenTelemetry().UseAzureMonitorExporter(options =>
            {
                // Set the Azure Monitor credential to the DefaultAzureCredential.
                // This credential will use the Azure identity of the current user or
                // the service principal that the application is running as to authenticate
                // to Azure Monitor.
                // Use a more specific credential in production scenarios. For best practices, see
                // https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/best-practices?tabs=aspdotnet
                options.Credential = new DefaultAzureCredential();
            });

            services.AddOpenTelemetry().UseFunctionsWorkerDefaults();

            return services;
        }

        public static IServiceCollection RegisterDependencies(this IServiceCollection services)
        {
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