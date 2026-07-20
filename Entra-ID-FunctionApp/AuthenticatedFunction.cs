using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OpenIdConfigurationManager = Microsoft.IdentityModel.Protocols.ConfigurationManager<Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration>;

namespace Entra_ID_FunctionApp;

public class AuthenticatedFunction
{
    private readonly ILogger<AuthenticatedFunction> _logger;
    private readonly string _tenantId;
    private readonly string _appId;
    private readonly string _audience;
    private readonly string _requiredScope;
    private readonly OpenIdConfigurationManager _oidcConfigurationManager;

    public AuthenticatedFunction(ILogger<AuthenticatedFunction> logger, IConfiguration configuration)
    {
        _logger = logger;

        _tenantId = configuration["EntraId:TenantId"] ?? "6e01b1f9-b1e5-4073-ac97-778069a0ad64";
        _appId = configuration["EntraId:AppId"] ?? "84a651ee-de65-4753-ba10-f89389c9308d";
        _audience = configuration["EntraId:Audience"] ?? "2766a7d4-1ac2-4d65-be3f-7e6478edd00a";
        _requiredScope = configuration["EntraId:RequiredScope"] ?? "api://2766a7d4-1ac2-4d65-be3f-7e6478edd00a/access_as_user";

        var metadataAddress = $"https://login.microsoftonline.com/{_tenantId}/v2.0/.well-known/openid-configuration";
        _oidcConfigurationManager = new OpenIdConfigurationManager(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever());
    }

    [Function("AuthenticatedPing")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "auth/ping")] HttpRequestData req,
        CancellationToken cancellationToken)
    {
        var authHeader = GetAuthorizationHeader(req);
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return await CreateResponseAsync(req, HttpStatusCode.Unauthorized, "Missing or invalid Authorization header.");
        }

        var token = authHeader["Bearer ".Length..].Trim();
        var principal = await ValidateAccessTokenAsync(token, cancellationToken);
        if (principal is null)
        {
            return await CreateResponseAsync(req, HttpStatusCode.Unauthorized, "Token validation failed.");
        }

        if (!HasRequiredScope(principal))
        {
            return await CreateResponseAsync(req, HttpStatusCode.Forbidden, "Missing required scope.");
        }

        _logger.LogInformation("Authenticated request for subject {Sub}.", principal.FindFirst("sub")?.Value ?? "unknown");
        return await CreateResponseAsync(req, HttpStatusCode.OK, "you are authenticated");
    }

    private static string? GetAuthorizationHeader(HttpRequestData req)
    {
        if (!req.Headers.TryGetValues("Authorization", out var values))
        {
            return null;
        }

        return values.FirstOrDefault();
    }

    private async Task<System.Security.Claims.ClaimsPrincipal?> ValidateAccessTokenAsync(string token, CancellationToken cancellationToken)
    {
        try
        {
            var oidcConfig = await _oidcConfigurationManager.GetConfigurationAsync(cancellationToken);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuers =
                [
                    $"https://login.microsoftonline.com/{_tenantId}/v2.0",
                    $"https://sts.windows.net/{_tenantId}/"
                ],
                ValidateAudience = true,
                ValidAudiences = [_audience, $"api://{_audience}", _appId, $"api://{_appId}"],
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = oidcConfig.SigningKeys,
                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed.");
            return null;
        }
    }

    private bool HasRequiredScope(System.Security.Claims.ClaimsPrincipal principal)
    {
        var scopeClaim = principal.FindFirst("scp")?.Value;
        if (string.IsNullOrWhiteSpace(scopeClaim))
        {
            return false;
        }

        var requiredScopeName = _requiredScope.Contains('/')
            ? _requiredScope[( _requiredScope.LastIndexOf('/') + 1)..]
            : _requiredScope;

        var scopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return scopes.Any(s =>
            string.Equals(s, _requiredScope, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(s, requiredScopeName, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<HttpResponseData> CreateResponseAsync(HttpRequestData req, HttpStatusCode statusCode, string message)
    {
        var response = req.CreateResponse(statusCode);
        await response.WriteStringAsync(message);
        return response;
    }
}