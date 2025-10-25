using IntegrationTests.Configuration;
using System.Net;

namespace IntegrationTests;

[TestClass]
public sealed class FunctionAppTests : IDisposable
{
    private readonly HttpClient _httpClient;

    public FunctionAppTests()
    {
        var config = TestConfiguration.Load();
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"https://{config.AzureFunctionAppName}.azurewebsites.net")
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [TestMethod]
    public async Task GetAsync_FunctionAppHasSufficientPermissionsToCallProtected_200OkReturned()
    {
        // Act
        var response = await _httpClient!.GetAsync("api/CallProtectedApiFunction");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code returned");
    }

    [TestMethod]
    public async Task PostAsync_FunctionAppHasSufficientPermissionsToCallProtected_200OkReturned()
    {
        // Act
        var response = await _httpClient!.PostAsync("api/CallProtectedApiFunction", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code returned");
    }

    [TestMethod]
    public async Task PostAsync_FunctionAppHasInsufficientPermissionsToCallProtected_401UnauthorizedReturned()
    {
        // Act
        var response = await _httpClient!.DeleteAsync("api/CallProtectedApiFunction");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "Unexpected status code returned");
    }
}
