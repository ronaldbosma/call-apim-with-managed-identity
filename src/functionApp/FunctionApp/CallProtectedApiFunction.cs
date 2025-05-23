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
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", "delete")] HttpRequest originalRequest)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient("apim");
            var method = GetHttpMethod(originalRequest.Method);
            var request = new HttpRequestMessage(method, "/protected");
            var result = await httpClient.SendAsync(request);

            return await CreateActionResultFromHttpResponseMessage(result);
        }
        catch (Exception ex)
        {
            return CreateActionResultFromException(ex);
        }
    }

    private static HttpMethod GetHttpMethod(string method)
    {
        return method?.ToUpperInvariant() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "DELETE" => HttpMethod.Delete,
            _ => new HttpMethod(method ?? "POST")
        };
    }

    private static async Task<IActionResult> CreateActionResultFromHttpResponseMessage(HttpResponseMessage? result)
    {
        return new ContentResult
        {
            StatusCode = (int?)result?.StatusCode,
            Content = result?.Content is not null ? await result.Content.ReadAsStringAsync() : string.Empty,
            ContentType = result?.Content?.Headers.ContentType?.ToString() ?? "text/plain"
        };
    }

    private static IActionResult CreateActionResultFromException(Exception ex)
    {
        return new ContentResult
        {
            StatusCode = StatusCodes.Status500InternalServerError,
            Content = ex.ToString(),
            ContentType = "text/plain"
        };
    }
}
