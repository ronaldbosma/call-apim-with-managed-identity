using IntegrationTests.Clients;
using IntegrationTests.Configuration;
using System.Net;

namespace IntegrationTests
{
    [TestClass]
    public class LogicAppTests
    {
        private static LogicAppWorkflowClient? WorkflowClient;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var config = TestConfiguration.Load();

            // Reuse the same Logic App workflow client so we don't have to fetch the callback URL multiple times
            WorkflowClient = new LogicAppWorkflowClient(
                config.AzureSubscriptionId,
                config.AzureResourceGroup,
                config.AzureLogicAppName,
                "call-protected-api-workflow"
            );
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            WorkflowClient?.Dispose();
        }

        [TestMethod]
        public async Task PostAsyncWithGetHttpMethod_LogicAppHasSufficientPermissionsToCallProtected_200OkReturned()
        {
            // Arrange
            var requestData = new { httpMethod = "GET" };

            // Act
            var response = await WorkflowClient!.PostAsync(requestData);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code returned");
        }

        [TestMethod]
        public async Task PostAsyncWithPostHttpMethod_LogicAppHasSufficientPermissionsToCallProtected_200OkReturned()
        {
            // Arrange
            var requestData = new { httpMethod = "POST" };

            // Act
            var response = await WorkflowClient!.PostAsync(requestData);

            // Assert
            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "Unexpected status code returned");
        }

        [TestMethod]
        public async Task PostAsyncWithDeleteHttpMethod_LogicAppHasInsufficientPermissionsToCallProtected_401UnauthorizedReturned()
        {
            // Arrange
            var requestData = new { httpMethod = "DELETE" };

            // Act
            var response = await WorkflowClient!.PostAsync(requestData);

            // Assert
            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode, "Unexpected status code returned");
        }
    }
}
