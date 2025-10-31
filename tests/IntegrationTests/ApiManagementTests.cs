using IntegrationTests.Clients;
using IntegrationTests.Configuration;
using System.Net;

namespace IntegrationTests;

/// <summary>
/// Tests scenarios where API Management uses its managed identity to call an OAuth-Protected API.
/// </summary>
[TestClass]
public sealed class ApiManagementTests
{
    private static HttpClient? HttpClient;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        var config = TestConfiguration.Load();
        HttpClient = new IntegrationTestHttpClient(config.AzureApiManagementGatewayUrl);
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        HttpClient?.Dispose();
    }

    [TestMethod]
    public async Task GetAsync_ApimHasSufficientPermissionsToCallProtected_200OkReturned()
    {
        // Act
        var response = await HttpClient!.GetAsync("unprotected");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code returned");
    }

    [TestMethod]
    public async Task PostAsync_ApimHasSufficientPermissionsToCallProtected_200OkReturned()
    {
        // Act
        var response = await HttpClient!.PostAsync("unprotected", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code returned");
    }

    [TestMethod]
    public async Task PostAsync_ApimHasInsufficientPermissionsToCallProtected_401UnauthorizedReturned()
    {
        // Act
        var response = await HttpClient!.DeleteAsync("unprotected");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "Unexpected status code returned");
    }
}
