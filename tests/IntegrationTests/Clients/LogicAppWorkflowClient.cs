using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using IntegrationTests.Configuration;
using System.Text;
using System.Text.Json;

namespace IntegrationTests.Clients
{
    internal class LogicAppWorkflowClient : IDisposable
    {
        private readonly TestConfiguration _configuration;
        private readonly string _workflowName;
        private readonly string _triggerName;

        private readonly Lazy<Task<HttpClient>> _httpClientLazy;
        private bool _disposed;

        public LogicAppWorkflowClient(TestConfiguration configuration, string workflowName)
            : this(configuration, workflowName, "When_a_HTTP_request_is_received")
        {
        }

        public LogicAppWorkflowClient(TestConfiguration configuration, string workflowName, string triggerName)
        {
            _configuration = configuration;
            _workflowName = workflowName;
            _triggerName = triggerName;

            _httpClientLazy = new Lazy<Task<HttpClient>>(CreateHttpClientAsync);
        }

        private async Task<HttpClient> CreateHttpClientAsync()
        {
            var armClient = new ArmClient(new DefaultAzureCredential());

            // Create the resource identifier for Logic App Standard Workflow Trigger
            var workflowTriggerId = WorkflowTriggerResource.CreateResourceIdentifier(
                subscriptionId: _configuration.AzureSubscriptionId,
                resourceGroupName: _configuration.AzureResourceGroup,
                name: _configuration.AzureLogicAppName,
                workflowName: _workflowName,
                triggerName: _triggerName
            );

            // Get the callback URL for the Logic App Workflow trigger
            var workflowTrigger = armClient.GetWorkflowTriggerResource(workflowTriggerId);
            var callbackUrl = await workflowTrigger.GetCallbackUrlAsync();

            return new HttpClient
            {
                BaseAddress = new Uri(callbackUrl.Value.Value)
            };
        }

        public async Task<HttpResponseMessage> PostAsync<T>(T data)
        {
            var httpClient = await _httpClientLazy.Value;
            
            var requestUri = string.Empty; // The callback URL is already set as the BaseAddress

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            return await httpClient.PostAsync(requestUri, content);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_httpClientLazy.IsValueCreated)
                {
                    var httpClientTask = _httpClientLazy.Value;
                    if (httpClientTask.IsCompletedSuccessfully)
                    {
                        httpClientTask.Result.Dispose();
                    }
                }
                _disposed = true;
            }
        }
    }
}
