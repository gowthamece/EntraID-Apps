using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;

namespace EntraID_FunctionApp_Client.Services;

public sealed class FunctionApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FunctionApiService> _logger;

    public FunctionApiService(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<FunctionApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<FunctionPingResult> GetAuthenticatedPingAsync(CancellationToken cancellationToken = default)
    {
        var scope = _configuration["FunctionApi:Scope"] ?? "api://2766a7d4-1ac2-4d65-be3f-7e6478edd00a/.default";
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

            var accessToken = await httpContext.GetTokenAsync("access_token");
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return new FunctionPingResult(
                    Success: false,
                    StatusCode: HttpStatusCode.Unauthorized,
                    ResponseBody: null,
                    ErrorMessage: $"No delegated access token was found in the session for scope '{scope}'. Sign in again and consent to API permissions.",
                    TokenExpiresOn: null);
            }

            var expiresAtRaw = await httpContext.GetTokenAsync("expires_at");
            DateTimeOffset? expiresAt = null;
            if (DateTimeOffset.TryParse(expiresAtRaw, out var parsedExpiresAt))
            {
                expiresAt = parsedExpiresAt;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, pingPath);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

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
