using System.Net;
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication;

namespace EntraID_FunctionApp_Client.Services;

public sealed class FunctionApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly TokenCredential _credential;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FunctionApiService> _logger;

    public FunctionApiService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        TokenCredential credential,
        IConfiguration configuration,
        ILogger<FunctionApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _credential = credential;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<FunctionPingResult> GetAuthenticatedPingAsync(CancellationToken cancellationToken = default)
    {
        var scope = _configuration["FunctionApi:Scope"] ?? "api://84a651ee-de65-4753-ba10-f89389c9308d/.default";
        var pingPath = _configuration["FunctionApi:PingPath"] ?? "/api/auth/ping";

        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                return new FunctionPingResult(
                    Success: false,
                    StatusCode: null,
                    ResponseBody: null,
                    ErrorMessage: "No active HTTP context is available.",
                    TokenExpiresOn: null);
            }

            if (httpContext.User?.Identity?.IsAuthenticated != true)
            {
                return new FunctionPingResult(
                    Success: false,
                    StatusCode: HttpStatusCode.Unauthorized,
                    ResponseBody: null,
                    ErrorMessage: "User is not authenticated. Sign in before calling the Function API.",
                    TokenExpiresOn: null);
            }

            var token = await _credential.GetTokenAsync(new TokenRequestContext([scope]), cancellationToken);
            var expiresAt = token.ExpiresOn;

            using var request = new HttpRequestMessage(HttpMethod.Get, pingPath);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

            var client = _httpClientFactory.CreateClient("FunctionApi");
            using var response = await client.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            return new FunctionPingResult(
                Success: response.IsSuccessStatusCode,
                StatusCode: response.StatusCode,
                ResponseBody: body,
                ErrorMessage: null,
                TokenExpiresOn: expiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Function API call failed");
            return new FunctionPingResult(
                Success: false,
                StatusCode: HttpStatusCode.InternalServerError,
                ResponseBody: null,
                ErrorMessage: ex.Message,
                TokenExpiresOn: null);
        }
    }
}

public sealed record FunctionPingResult(
    bool Success,
    HttpStatusCode? StatusCode,
    string? ResponseBody,
    string? ErrorMessage,
    DateTimeOffset? TokenExpiresOn);
