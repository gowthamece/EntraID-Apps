using System.Net;
using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;

namespace EntraID_FunctionApp_Client.Services;

public sealed class FunctionApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TokenCredential _credential;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FunctionApiService> _logger;

    public FunctionApiService(
        IHttpClientFactory httpClientFactory,
        TokenCredential credential,
        IConfiguration configuration,
        ILogger<FunctionApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _credential = credential;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<FunctionPingResult> GetAuthenticatedPingAsync(CancellationToken cancellationToken = default)
    {
        var scope = _configuration["FunctionApi:Scope"] ?? "api://2766a7d4-1ac2-4d65-be3f-7e6478edd00a/.default";
        var pingPath = _configuration["FunctionApi:PingPath"] ?? "/api/auth/ping";

        try
        {
            var token = await _credential.GetTokenAsync(new TokenRequestContext([scope]), cancellationToken);

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
                TokenExpiresOn: token.ExpiresOn);
        }
        catch (AuthenticationFailedException ex)
        {
            _logger.LogError(ex, "Token acquisition failed for scope {Scope}", scope);
            return new FunctionPingResult(
                Success: false,
                StatusCode: null,
                ResponseBody: null,
                ErrorMessage: ex.Message,
                TokenExpiresOn: null);
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
