using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace FunctionApp
{
    internal class AzureCredentialsAuthorizationHandler : DelegatingHandler
    {
        private readonly ApiManagementOptions _apimOptions;

        public AzureCredentialsAuthorizationHandler(IOptions<ApiManagementOptions> apimOptions)
        {
            _apimOptions = apimOptions.Value;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var credentials = new DefaultAzureCredential();
            var tokenResult = await credentials.GetTokenAsync(new TokenRequestContext([_apimOptions.OAuthTargetResource]), cancellationToken);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
