using IntegrationTests.Configuration;
using System.Net;

namespace IntegrationTests;

/// <summary>
/// Tests scenarios where an Azure Function uses its managed identity to call an OAuth-Protected API.
/// </summary>
[TestClass]
public sealed class FunctionAppTests
{
    private static HttpClient? HttpClient;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        var config = TestConfiguration.Load();
        HttpClient = new HttpClient
        {
            BaseAddress = config.AzureFunctionAppEndpoint
        };
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        HttpClient?.Dispose();
    }

    [TestMethod]
    public async Task GetAsync_FunctionAppHasSufficientPermissionsToCallProtected_200OkReturned()
    {
        // Act
        var response = await HttpClient!.GetAsync("api/CallProtectedApiFunction");

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code returned");
    }

    [TestMethod]
    public async Task PostAsync_FunctionAppHasSufficientPermissionsToCallProtected_200OkReturned()
    {
        // Act
        var response = await HttpClient!.PostAsync("api/CallProtectedApiFunction", null);

        // Assert
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code returned");
    }

    [TestMethod]
    public async Task PostAsync_FunctionAppHasInsufficientPermissionsToCallProtected_401UnauthorizedReturned()
    {
        // Act
        var response = await HttpClient!.DeleteAsync("api/CallProtectedApiFunction");

        // Assert
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "Unexpected status code returned");
    }
}
