using System.Text.Json.Serialization;

namespace EntraID_APIM_IntegrationTests.Models;

/// <summary>
/// User model for POST endpoint testing.
/// Matches the backend API User record structure.
/// </summary>
public class User
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("age")]
    public int Age { get; set; }
}
