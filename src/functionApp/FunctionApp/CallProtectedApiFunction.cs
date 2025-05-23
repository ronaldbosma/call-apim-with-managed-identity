using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace FunctionApp;

public class CallProtectedApiFunction
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CallProtectedApiFunction(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    [Function(nameof(CallProtectedApiFunction))]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequest req)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient("apim");
            var request = new HttpRequestMessage(HttpMethod.Get, "/protected");
            var result = await httpClient.SendAsync(request);

            return new ContentResult
            {
                StatusCode = (int?)result?.StatusCode,
                Content = result?.Content is not null ? await result.Content.ReadAsStringAsync() : string.Empty,
                ContentType = result?.Content?.Headers.ContentType?.ToString() ?? "text/plain"
            };
        }
        catch (Exception ex)
        {
            return new ContentResult
            {
                StatusCode = StatusCodes.Status500InternalServerError,
                Content = ex.ToString(),
                ContentType = "text/plain"
            };
        }
    }
}
