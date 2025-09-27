using Azure.Core;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace FunctionApp
{
    internal class AzureCredentialsAuthorizationHandler : DelegatingHandler
    {
        private readonly ApiManagementOptions _apimOptions;
        private readonly TokenCredential _tokenCredential;

        public AzureCredentialsAuthorizationHandler(IOptions<ApiManagementOptions> apimOptions, TokenCredential tokenCredential)
        {
            _apimOptions = apimOptions.Value;
            _tokenCredential = tokenCredential;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var tokenResult = await _tokenCredential.GetTokenAsync(new TokenRequestContext([_apimOptions.OAuthTargetResource]), cancellationToken);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Token);

            return await base.SendAsync(request, cancellationToken);
        }
    }
}
