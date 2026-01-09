using EntraID_APIM_IntegrationTests.Models;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace EntraID_APIM_IntegrationTests;

/// <summary>
/// Integration tests for the Users POST endpoint via APIM.
/// These tests hit the live APIM endpoint using DefaultAzureCredential.
/// 
/// Prerequisites:
/// - Sign into Azure in Visual Studio (Tools > Options > Azure Service Authentication)
/// - Or run 'az login' in Azure CLI
/// - Ensure your account has access to the API (proper app role assignments)
/// </summary>
public class UsersApiIntegrationTests : IClassFixture<ApimTestFixture>
{
    private readonly ApimTestFixture _fixture;
    private readonly string _endpoint;
    
    public UsersApiIntegrationTests(ApimTestFixture fixture)
    {
        _fixture = fixture;
        _endpoint = fixture.GetEndpoint("Users");
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task PostUser_ReturnsSuccessStatusCode()
    {
        // Arrange
        var user = new User { Id = 1, Name = "John Doe", Age = 30 };
        var content = new StringContent(
            JsonSerializer.Serialize(user),
            Encoding.UTF8,
            "application/json");
        
        // Act
        var response = await _fixture.HttpClient.PostAsync(_endpoint, content);
        
        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, 
            because: "the API should return 200 OK for authenticated POST requests");
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task PostUser_ReturnsValidJsonObject()
    {
        // Arrange
        var user = new User { Id = 2, Name = "Jane Smith", Age = 25 };
        var content = new StringContent(
            JsonSerializer.Serialize(user),
            Encoding.UTF8,
            "application/json");
        
        // Act
        var response = await _fixture.HttpClient.PostAsync(_endpoint, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var returnedUser = JsonSerializer.Deserialize<User>(responseContent);
        returnedUser.Should().NotBeNull();
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task PostUser_EchoesBackSameUserObject()
    {
        // Arrange
        var user = new User { Id = 42, Name = "Test User", Age = 28 };
        var content = new StringContent(
            JsonSerializer.Serialize(user),
            Encoding.UTF8,
            "application/json");
        
        // Act
        var response = await _fixture.HttpClient.PostAsync(_endpoint, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        
        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        
        var returnedUser = JsonSerializer.Deserialize<User>(responseContent);
        returnedUser.Should().NotBeNull();
        returnedUser!.Id.Should().Be(user.Id, because: "the API should echo back the same Id");
        returnedUser.Name.Should().Be(user.Name, because: "the API should echo back the same Name");
        returnedUser.Age.Should().Be(user.Age, because: "the API should echo back the same Age");
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task PostUser_WithDifferentUsers_EchoesCorrectly()
    {
        // Arrange - test with multiple users
        var users = new[]
        {
            new User { Id = 1, Name = "Alice", Age = 22 },
            new User { Id = 999, Name = "Bob Builder", Age = 45 },
            new User { Id = 100, Name = "Charlie Brown", Age = 8 }
        };
        
        foreach (var user in users)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(user),
                Encoding.UTF8,
                "application/json");
            
            // Act
            var response = await _fixture.HttpClient.PostAsync(_endpoint, content);
            var returnedUser = await response.Content.ReadFromJsonAsync<User>();
            
            // Assert
            response.IsSuccessStatusCode.Should().BeTrue();
            returnedUser.Should().NotBeNull();
            returnedUser!.Id.Should().Be(user.Id);
            returnedUser.Name.Should().Be(user.Name);
            returnedUser.Age.Should().Be(user.Age);
        }
    }
    
    [Fact]
    [Trait("Category", "Integration")]
    public async Task PostUser_ValidatesContentTypeIsJson()
    {
        // Arrange
        var user = new User { Id = 5, Name = "Content Type Test", Age = 35 };
        var content = new StringContent(
            JsonSerializer.Serialize(user),
            Encoding.UTF8,
            "application/json");
        
        // Act
        var response = await _fixture.HttpClient.PostAsync(_endpoint, content);
        
        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json",
            because: "the API should return JSON content");
    }
}
