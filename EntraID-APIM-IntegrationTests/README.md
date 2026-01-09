# EntraID-APIM-IntegrationTests

Integration test project for testing Azure API Management (APIM) endpoints using `DefaultAzureCredential` from Visual Studio.

## Why xUnit Instead of Postman?

Postman does **not** support Azure.Identity credential chains like `DefaultAzureCredential`. This test project allows you to:
- Use Visual Studio's signed-in Azure account for authentication
- Run tests directly from Test Explorer
- Automate tests in CI/CD pipelines
- Assert response structure and values

## Prerequisites

### 1. Sign into Azure in Visual Studio

1. Open Visual Studio
2. Go to **Tools** → **Options** → **Azure Service Authentication**
3. Select your Azure account that has access to the APIM backend API
4. Ensure the account has the required API permissions/app roles

### 2. Or Use Azure CLI (Fallback)

```bash
az login --tenant YOUR_TENANT_ID
```

### 3. Configure appsettings.json

Update `appsettings.json` with your actual TenantId:

```json
{
  "ApimSettings": {
    "BaseUrl": "https://apim-pi-tracking.azure-api.net",
    "Scope": "api://Your API APP ID/.default",
    "TenantId": "YOUR_ACTUAL_TENANT_ID"
  }
}
```

## Running Tests

### From Visual Studio Test Explorer

1. Open Test Explorer (**Test** → **Test Explorer**)
2. Build the solution
3. Run all tests or select specific tests

### From Command Line

```bash
cd EntraID-APIM-IntegrationTests
dotnet test
```

### Run Only Integration Tests

```bash
dotnet test --filter "Category=Integration"
```

## Test Structure

| Test | Description |
|------|-------------|
| `GetWeatherForecast_ReturnsSuccessStatusCode` | Validates 200 OK response |
| `GetWeatherForecast_ReturnsValidJsonArray` | Validates JSON array response |
| `GetWeatherForecast_ResponseContainsExpectedProperties` | Validates schema (Date, TemperatureC, Summary) |
| `GetWeatherForecast_TemperatureConversionIsCorrect` | Validates C to F conversion |
| `GetAccessToken_WithDefaultAzureCredential_Succeeds` | Validates token acquisition |
| `GetWeatherForecast_ReturnsMultipleForecasts` | Validates multiple results |

## Adding New Endpoint Tests

1. Add the endpoint path to `appsettings.json`:
   ```json
   "Endpoints": {
     "WeatherForecast": "/weatherforecast",
     "NewEndpoint": "/api/new-endpoint"
   }
   ```

2. Create a new test class following the `WeatherApiIntegrationTests` pattern:
   ```csharp
   public class NewEndpointTests : IClassFixture<ApimTestFixture>
   {
       private readonly ApimTestFixture _fixture;
       
       public NewEndpointTests(ApimTestFixture fixture)
       {
           _fixture = fixture;
       }
       
       [Fact]
       [Trait("Category", "Integration")]
       public async Task NewEndpoint_ReturnsSuccess()
       {
           var endpoint = _fixture.GetEndpoint("NewEndpoint");
           var response = await _fixture.HttpClient.GetAsync(endpoint);
           response.IsSuccessStatusCode.Should().BeTrue();
       }
   }
   ```

## Troubleshooting

### "Authentication failed" error

- Ensure you're signed into the correct Azure account in Visual Studio
- Verify your account has access to the backend API (check app role assignments)
- Try running `az login --tenant YOUR_TENANT_ID` as fallback

### "Token acquisition failed" error

- Check the `Scope` value matches your App Registration
- Verify the TenantId is correct
- Ensure the App Registration has the required API permissions

### Tests timeout

- Check network connectivity to APIM
- Verify APIM endpoint is running
- Try increasing test timeout if needed

## Project Structure

```
EntraID-APIM-IntegrationTests/
├── appsettings.json              # APIM configuration
├── ApimTestFixture.cs            # Shared fixture with auth
├── WeatherApiIntegrationTests.cs # Weather endpoint tests
├── Models/
│   └── WeatherForecast.cs        # Response model
└── README.md                     # This file
```
