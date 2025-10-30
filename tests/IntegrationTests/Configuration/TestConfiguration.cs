using IntegrationTests.Configuration.Azd;
using Microsoft.Extensions.Configuration;

namespace IntegrationTests.Configuration;

/// <summary>
/// Contains configuration settings for the integration tests.
/// </summary>
internal class TestConfiguration
{
    private static readonly Lazy<TestConfiguration> _instance = new(() =>
    {
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddAzdEnvironmentVariables(optional: true) // Adds Azure Developer CLI environment variables; optional since CI/CD pipelines may use standard environment variables instead
            .Build();

        return new TestConfiguration
        {
            AzureSubscriptionId = configuration.GetRequiredString("AZURE_SUBSCRIPTION_ID"),
            AzureResourceGroup = configuration.GetRequiredString("AZURE_RESOURCE_GROUP"),
            AzureApiManagementGatewayUrl = configuration.GetRequiredUri("AZURE_API_MANAGEMENT_GATEWAY_URL"),
            AzureFunctionAppEndpoint = configuration.GetRequiredUri("AZURE_FUNCTION_APP_ENDPOINT"),
            AzureLogicAppName = configuration.GetRequiredString("AZURE_LOGIC_APP_NAME"),
            OAuthTargetResource = configuration.GetRequiredString("ENTRA_ID_APIM_APP_REGISTRATION_IDENTIFIER_URI")
        };
    });

    public required Uri AzureApiManagementGatewayUrl { get; init; }
    public required Uri AzureFunctionAppEndpoint { get; init; }

    public required string AzureSubscriptionId { get; init; }
    public required string AzureResourceGroup { get; init; }
    public required string AzureLogicAppName { get; init; }
    
    public required string OAuthTargetResource {  get; init; }

    public static TestConfiguration Load() => _instance.Value;
}