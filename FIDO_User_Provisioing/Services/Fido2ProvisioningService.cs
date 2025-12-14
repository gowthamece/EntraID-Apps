using FIDO_User_Provisioing.Models;
using Microsoft.Graph;
using System.Text.Json;

namespace FIDO_User_Provisioing.Services
{
    public class Fido2ProvisioningService : IFido2ProvisioningService
    {
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<Fido2ProvisioningService> _logger;

        public Fido2ProvisioningService(GraphServiceClient graphServiceClient, ILogger<Fido2ProvisioningService> logger)
        {
            _graphServiceClient = graphServiceClient;
            _logger = logger;
        }

        public async Task<IEnumerable<UserSearchResult>> SearchUsersAsync(string searchTerm)
        {
            try
            {
                // Note: This requires UserAuthenticationMethod.ReadWrite.All (admin consent required)
                // For user-scoped access, consider limiting to current user only
                var users = await _graphServiceClient.Users
                    .Request()
                    .Filter($"startswith(displayName,'{searchTerm}') or startswith(userPrincipalName,'{searchTerm}') or startswith(mail,'{searchTerm}')")
                    .Select("id,displayName,userPrincipalName,mail")
                    .Top(20)
                    .GetAsync();

                return users?.Select(u => new UserSearchResult
                {
                    Id = u.Id ?? string.Empty,
                    DisplayName = u.DisplayName ?? string.Empty,
                    UserPrincipalName = u.UserPrincipalName ?? string.Empty,
                    Mail = u.Mail ?? string.Empty
                }) ?? Enumerable.Empty<UserSearchResult>();
            }
            catch (Microsoft.Graph.ServiceException ex) when (ex.Error.Code == "Authorization_RequestDenied")
            {
                _logger.LogWarning("Insufficient privileges to search users. Consider using 'me' endpoint for current user only.");
                throw new UnauthorizedAccessException("Insufficient privileges to search users. Admin consent required for UserAuthenticationMethod.ReadWrite.All permission.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with term: {SearchTerm}", searchTerm);
                return Enumerable.Empty<UserSearchResult>();
            }
        }

        public async Task<IEnumerable<Models.Fido2AuthenticationMethod>> GetUserFido2MethodsAsync(string userId)
        {
            try
            {
                var methods = await _graphServiceClient.Users[userId].Authentication.Fido2Methods
                    .Request()
                    .GetAsync();

                return methods?.Select(m => new Models.Fido2AuthenticationMethod
                {
                    Id = m.Id,
                    DisplayName = m.DisplayName,
                    CreatedDateTime = m.CreatedDateTime?.DateTime, // Convert DateTimeOffset? to DateTime?
                    Model = m.Model,
                    AttestationLevel = m.AttestationLevel.HasValue ? (int)m.AttestationLevel.Value : 1 // Convert enum to int with default
                }) ?? Enumerable.Empty<Models.Fido2AuthenticationMethod>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting FIDO2 methods for user: {UserId}", userId);
                return Enumerable.Empty<Models.Fido2AuthenticationMethod>();
            }
        }

        public Task<Fido2ProvisioningResponse> ProvisionFido2KeyAsync(string userId, Fido2ProvisioningRequest request)
        {
            try
            {
                // Note: The actual FIDO2 key provisioning requires the physical security key to be present
                // and the user to complete the registration process. This method prepares the provisioning
                // but the actual key registration happens client-side with WebAuthn.
                
                _logger.LogInformation("Initiating FIDO2 key provisioning for user: {UserId} with display name: {DisplayName}", userId, request.DisplayName);
                
                // In a real implementation, you would:
                // 1. Generate a challenge
                // 2. Return registration options to the client
                // 3. The client would use WebAuthn to create credentials
                // 4. The client would send back the credentials to be verified and stored
                
                // For now, we'll simulate the preparation phase with enhanced details
                var response = new Fido2ProvisioningResponse
                {
                    Success = true,
                    Message = $"FIDO2 key '{request.DisplayName}' provisioning initiated successfully. Please complete the registration using your security key device.",
                    MethodId = Guid.NewGuid().ToString()
                };

                _logger.LogInformation("FIDO2 key provisioning initiated for user: {UserId} with method ID: {MethodId}", userId, response.MethodId);
                return Task.FromResult(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error provisioning FIDO2 key for user: {UserId}", userId);
                return Task.FromResult(new Fido2ProvisioningResponse
                {
                    Success = false,
                    Error = $"Error provisioning FIDO2 key: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Creates a quick provisioning request with default values for a user
        /// </summary>
        public Fido2ProvisioningRequest CreateQuickProvisioningRequest(string userPrincipalName, string displayName)
        {
            return new Fido2ProvisioningRequest
            {
                UserPrincipalName = userPrincipalName,
                DisplayName = $"Security Key for {displayName}",
                Model = "Unknown", // Will be populated during actual registration
                AttestationLevel = 1, // Default attestation level
                IsBackupEligible = true // Allow backup by default
            };
        }

        public async Task<Fido2ProvisioningResponse> DeleteFido2KeyAsync(string userId, string methodId)
        {
            try
            {
                await _graphServiceClient.Users[userId].Authentication.Fido2Methods[methodId]
                    .Request()
                    .DeleteAsync();

                return new Fido2ProvisioningResponse
                {
                    Success = true,
                    Message = "FIDO2 authentication method deleted successfully."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting FIDO2 method {MethodId} for user: {UserId}", methodId, userId);
                return new Fido2ProvisioningResponse
                {
                    Success = false,
                    Error = $"Error deleting FIDO2 key: {ex.Message}"
                };
            }
        }

        public async Task<bool> ValidateUserExistsAsync(string userPrincipalName)
        {
            try
            {
                var user = await _graphServiceClient.Users[userPrincipalName]
                    .Request()
                    .Select("id")
                    .GetAsync();

                return user != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user exists: {UserPrincipalName}", userPrincipalName);
                return false;
            }
        }

        /// <summary>
        /// Gets the current user's information - requires only User.Read permission
        /// </summary>
        public async Task<UserSearchResult?> GetCurrentUserAsync()
        {
            try
            {
                var user = await _graphServiceClient.Me
                    .Request()
                    .Select("id,displayName,userPrincipalName,mail")
                    .GetAsync();

                if (user == null) return null;

                return new UserSearchResult
                {
                    Id = user.Id ?? string.Empty,
                    DisplayName = user.DisplayName ?? string.Empty,
                    UserPrincipalName = user.UserPrincipalName ?? string.Empty,
                    Mail = user.Mail ?? string.Empty
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user information");
                return null;
            }
        }

        /// <summary>
        /// Gets the current user's FIDO2 methods - requires UserAuthenticationMethod.ReadWrite permission
        /// </summary>
        public async Task<IEnumerable<Models.Fido2AuthenticationMethod>> GetCurrentUserFido2MethodsAsync()
        {
            try
            {
                var methods = await _graphServiceClient.Me.Authentication.Fido2Methods
                    .Request()
                    .GetAsync();

                return methods?.Select(m => new Models.Fido2AuthenticationMethod
                {
                    Id = m.Id,
                    DisplayName = m.DisplayName,
                    CreatedDateTime = m.CreatedDateTime?.DateTime,
                    Model = m.Model,
                    AttestationLevel = m.AttestationLevel.HasValue ? (int)m.AttestationLevel.Value : 1
                }) ?? Enumerable.Empty<Models.Fido2AuthenticationMethod>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user's FIDO2 methods");
                return Enumerable.Empty<Models.Fido2AuthenticationMethod>();
            }
        }
    }
}