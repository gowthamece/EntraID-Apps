using Azure.Core;
using Azure.Identity;
using Microsoft.Graph;

namespace MS_Entra_ID_Blazor_Server.Services;

public class ManagedIdentityGraphService
{
    private readonly GraphServiceClient _graphClient;

    // Constructor for Azure (Managed Identity)
    public ManagedIdentityGraphService(IWebHostEnvironment environment)
    {
        // Use Managed Identity in Azure
        var credential = new DefaultAzureCredential();
        var scopes = new[] { "https://graph.microsoft.com/.default" };

        var authProvider = new DelegateAuthenticationProvider(async (requestMessage) =>
        {
            var token = await credential.GetTokenAsync(
                new TokenRequestContext(scopes),
                default);
            requestMessage.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token.Token);
        });

        _graphClient = new GraphServiceClient(authProvider);
        Console.WriteLine("Using Managed Identity for Graph API access");
    }

    // Constructor for Local (Delegated via injected GraphServiceClient)
    public ManagedIdentityGraphService(GraphServiceClient graphServiceClient)
    {
        _graphClient = graphServiceClient;
        Console.WriteLine("Using delegated permissions (user token) for Graph API access");
    }

    public async Task<List<Group>> GetAllGroupsAsync()
    {
        try
        {
            var groups = new List<Group>();

            var result = await _graphClient.Groups
                .Request()
                .Select("id,displayName,description,mail,mailEnabled,securityEnabled")
                .Top(999)
                .GetAsync();

            if (result?.CurrentPage != null)
            {
                groups.AddRange(result.CurrentPage);

                // Handle pagination if there are more than 999 groups
                while (result?.NextPageRequest != null)
                {
                    result = await result.NextPageRequest.GetAsync();
                    if (result?.CurrentPage != null)
                    {
                        groups.AddRange(result.CurrentPage);
                    }
                }
            }

            return groups.OrderBy(g => g.DisplayName).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting groups: {ex.Message}");
            throw;
        }
    }

    public async Task<User?> GetUserAsync(string userPrincipalName)
    {
        try
        {
            return await _graphClient.Users[userPrincipalName]
                .Request()
                .GetAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user: {ex.Message}");
            throw;
        }
    }
}
