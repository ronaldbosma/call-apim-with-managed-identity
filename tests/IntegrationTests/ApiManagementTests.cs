using IntegrationTests.Configuration;
using System.Net;

namespace IntegrationTests;

/// <summary>
/// Tests scenarios where API Management uses its managed identity to call an OAuth-Protected API.
/// </summary>
[TestClass]
public sealed class ApiManagementTests : IDisposable
{
    private readonly HttpClient _httpClient;

    public ApiManagementTests()
    {
        var config = TestConfiguration.Load();
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"https://{config.AzureApiManagementName}.azure-api.net")
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [TestMethod]
    public async Task GetAsync_ApimHasSufficientPermissionsToCallProtected_200OkReturned()
    {
        // Act
        var response = await _httpClient!.GetAsync("unprotected");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code returned");
    }

    [TestMethod]
    public async Task PostAsync_ApimHasSufficientPermissionsToCallProtected_200OkReturned()
    {
        // Act
        var response = await _httpClient!.PostAsync("unprotected", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code returned");
    }

    [TestMethod]
    public async Task PostAsync_ApimHasInsufficientPermissionsToCallProtected_401UnauthorizedReturned()
    {
        // Act
        var response = await _httpClient!.DeleteAsync("unprotected");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "Unexpected status code returned");
    }
}
