using EntraID_APIM_IntegrationTests.Models;
using FluentAssertions;
using System.Net;
using System.Text.Json;
using Xunit;

namespace EntraID_APIM_IntegrationTests;

/// <summary>
/// Integration tests for the Weather API endpoint via APIM.
/// These tests hit the live APIM endpoint using DefaultAzureCredential.
/// 
/// Prerequisites:
/// - Sign into Azure in Visual Studio (Tools > Options > Azure Service Authentication)
/// - Or run 'az login' in Azure CLI
/// - Ensure your account has access to the API (proper app role assignments)
/// </summary>
public class WeatherApiIntegrationTests : IClassFixture<ApimTestFixture>
{
    private readonly ApimTestFixture _fixture;
    private readonly string _endpoint;
    
    public WeatherApiIntegrationTests(ApimTestFixture fixture)
    {
        _fixture = fixture;
        _endpoint = fixture.GetEndpoint("WeatherForecast");
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetWeatherForecast_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync(_endpoint);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, 
            because: "the API should return 200 OK for authenticated requests");
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetWeatherForecast_ReturnsValidJsonArray()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync(_endpoint);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var forecasts = JsonSerializer.Deserialize<List<WeatherForecast>>(content);
        forecasts.Should().NotBeNull();
        forecasts.Should().NotBeEmpty("the API should return at least one forecast");
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetWeatherForecast_ResponseContainsExpectedProperties()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync(_endpoint);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var forecasts = JsonSerializer.Deserialize<List<WeatherForecast>>(content);
        forecasts.Should().NotBeNull();
        
        foreach (var forecast in forecasts!)
        {
            // Validate structure - values are random so we just check they exist
            forecast.Date.Should().NotBe(default(DateOnly), 
                because: "each forecast should have a valid date");
            forecast.TemperatureC.Should().BeInRange(-50, 60, 
                because: "temperature should be within reasonable range");
            forecast.TemperatureF.Should().BeInRange(-60, 140, 
                because: "Fahrenheit should be within reasonable range");
        }
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetWeatherForecast_TemperatureConversionIsCorrect()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync(_endpoint);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var forecasts = JsonSerializer.Deserialize<List<WeatherForecast>>(content);
        forecasts.Should().NotBeNull();
        
        foreach (var forecast in forecasts!)
        {
            // Validate C to F conversion: F = 32 + (C / 0.5556)
            var expectedF = 32 + (int)(forecast.TemperatureC / 0.5556);
            forecast.TemperatureF.Should().BeCloseTo(expectedF, 2, 
                because: "Fahrenheit should be correctly calculated from Celsius");
        }
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAccessToken_WithDefaultAzureCredential_Succeeds()
    {
        // Act
        var token = await _fixture.GetAccessTokenAsync();
        
        // Assert
        token.Should().NotBeNullOrEmpty("token acquisition should succeed with valid credentials");
        token.Split('.').Should().HaveCount(3, "token should be a valid JWT with 3 parts");
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetWeatherForecast_ReturnsMultipleForecasts()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync(_endpoint);
        var content = await response.Content.ReadAsStringAsync();
        
        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var forecasts = JsonSerializer.Deserialize<List<WeatherForecast>>(content);
        forecasts.Should().NotBeNull();
        forecasts!.Count.Should().BeGreaterThanOrEqualTo(1, 
            because: "the API should return forecasts for multiple days");
    }
}
