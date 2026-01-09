using System.Text.Json.Serialization;

namespace EntraID_APIM_IntegrationTests.Models;

/// <summary>
/// Weather forecast model matching the API response structure.
/// </summary>
public class WeatherForecast
{
    [JsonPropertyName("date")]
    public DateOnly Date { get; set; }
    
    [JsonPropertyName("temperatureC")]
    public int TemperatureC { get; set; }
    
    [JsonPropertyName("temperatureF")]
    public int TemperatureF { get; set; }
    
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }
}
