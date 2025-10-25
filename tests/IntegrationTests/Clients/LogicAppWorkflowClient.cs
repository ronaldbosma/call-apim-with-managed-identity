using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.AppService;
using System.Text;
using System.Text.Json;

namespace IntegrationTests.Clients
{
    /// <summary>
    /// Provides a client for invoking Azure Logic App Standard workflows via HTTP requests.
    /// </summary>
    internal class LogicAppWorkflowClient : IDisposable
    {
        private readonly string _subscriptionId;
        private readonly string _resourceGroupName;
        private readonly string _logicAppName;
        private readonly string _workflowName;
        private readonly string _triggerName;

        private readonly Lazy<Task<HttpClient>> _httpClientLazy;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppWorkflowClient"/> class with the default trigger name.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription ID containing the Logic App.</param>
        /// <param name="resourceGroupName">The name of the resource group containing the Logic App.</param>
        /// <param name="logicAppName">The name of the Logic App.</param>
        /// <param name="workflowName">The name of the workflow within the Logic App.</param>
        public LogicAppWorkflowClient(string subscriptionId, string resourceGroupName, string logicAppName, string workflowName)
            : this(subscriptionId, resourceGroupName, logicAppName, workflowName, "When_a_HTTP_request_is_received")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LogicAppWorkflowClient"/> class with a custom trigger name.
        /// </summary>
        /// <param name="subscriptionId">The Azure subscription ID containing the Logic App.</param>
        /// <param name="resourceGroupName">The name of the resource group containing the Logic App.</param>
        /// <param name="logicAppName">The name of the Logic App.</param>
        /// <param name="workflowName">The name of the workflow within the Logic App.</param>
        /// <param name="triggerName">The name of the trigger within the workflow.</param>
        /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
        public LogicAppWorkflowClient(string subscriptionId, string resourceGroupName, string logicAppName, string workflowName, string triggerName)
        {
            _subscriptionId = subscriptionId ?? throw new ArgumentNullException(nameof(subscriptionId));
            _resourceGroupName = resourceGroupName ?? throw new ArgumentNullException(nameof(resourceGroupName));
            _logicAppName = logicAppName ?? throw new ArgumentNullException(nameof(logicAppName));
            _workflowName = workflowName ?? throw new ArgumentNullException(nameof(workflowName));
            _triggerName = triggerName ?? throw new ArgumentNullException(nameof(triggerName));

            _httpClientLazy = new Lazy<Task<HttpClient>>(CreateHttpClientAsync);
        }

        /// <summary>
        /// Creates and configures an HttpClient with the Logic App workflow trigger callback URL as the base address.
        /// This method is called lazily when the first HTTP request is made.
        /// </summary>
        /// <returns>A configured HttpClient instance.</returns>
        private async Task<HttpClient> CreateHttpClientAsync()
        {
            var armClient = new ArmClient(new DefaultAzureCredential());

            // Create the resource identifier for Logic App Standard workflow trigger
            var workflowTriggerId = WorkflowTriggerResource.CreateResourceIdentifier(
                subscriptionId: _subscriptionId,
                resourceGroupName: _resourceGroupName,
                name: _logicAppName,
                workflowName: _workflowName,
                triggerName: _triggerName
            );

            // Get the callback URL for the Logic App Standard workflow trigger
            var workflowTrigger = armClient.GetWorkflowTriggerResource(workflowTriggerId);
            var callbackUrl = await workflowTrigger.GetCallbackUrlAsync();

            return new HttpClient
            {
                BaseAddress = new Uri(callbackUrl.Value.Value)
            };
        }

        /// <summary>
        /// Sends a POST request to the Logic App workflow trigger with the specified data serialized as JSON.
        /// </summary>
        /// <typeparam name="T">The type of the data to serialize and send.</typeparam>
        /// <param name="data">The data to serialize to JSON and send in the request body.</param>
        /// <returns>The HTTP response message from the Logic App workflow.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when the client has been disposed.</exception>
        public async Task<HttpResponseMessage> PostAsync<T>(T data)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

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
