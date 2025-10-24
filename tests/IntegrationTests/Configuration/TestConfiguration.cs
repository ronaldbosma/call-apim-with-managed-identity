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
            AzureApiManagementName = configuration.GetRequiredString("AZURE_API_MANAGEMENT_NAME"),
            AzureFunctionAppName = configuration.GetRequiredString("AZURE_FUNCTION_APP_NAME"),
            AzureLogicAppName = configuration.GetRequiredString("AZURE_LOGIC_APP_NAME")
        };
    });

    public required string AzureApiManagementName { get; init; }
    public required string AzureFunctionAppName { get; init; }
    public required string AzureLogicAppName { get; init; }

    public static TestConfiguration Load() => _instance.Value;
}