using Azure.Core;
using Azure.Identity;
using IntegrationTests.Configuration;
using System.Net;
using System.Net.Http.Headers;

namespace IntegrationTests;

/// <summary>
/// Tests scenarios where the pipeline credentials (Azure CLI or Azure Developer CLI) are used to call an OAuth-Protected API.
/// </summary>
[TestClass]
public sealed class PipelineCredentialsTests
{
    private static HttpClient? HttpClient;

    [ClassInitialize]
    public static async Task ClassInitialize(TestContext context)
    {
        var config = TestConfiguration.Load();
        HttpClient = new HttpClient
        {
            BaseAddress = new Uri($"https://{config.AzureApiManagementName}.azure-api.net")
        };

        // Create token credential that uses either the Azure CLI or Azure Developer CLI credentials
        var tokenCredential = new ChainedTokenCredential(new AzureCliCredential(), new AzureDeveloperCliCredential());

        // Retrieve JWT access token and use it in the Authorization header
        var tokenResult = await tokenCredential.GetTokenAsync(new TokenRequestContext([config.OAuthTargetResource]));
        HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        HttpClient?.Dispose();
    }

    [TestMethod]
    public async Task GetAsync_PipelineHasSufficientPermissionsToCallProtected_200OkReturned()
    {
        // Act
        var response = await HttpClient!.GetAsync("protected");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code returned");
    }

    [TestMethod]
    public async Task PostAsync_PipelineHasSufficientPermissionsToCallProtected_200OkReturned()
    {
        // Act
        var response = await HttpClient!.PostAsync("protected", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code returned");
    }

    [TestMethod]
    public async Task PostAsync_PipelineHasInsufficientPermissionsToCallProtected_401UnauthorizedReturned()
    {
        // Act
        var response = await HttpClient!.DeleteAsync("protected");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "Unexpected status code returned");
    }
}
