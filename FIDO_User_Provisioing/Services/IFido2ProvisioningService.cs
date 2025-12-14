using FIDO_User_Provisioing.Models;

namespace FIDO_User_Provisioing.Services
{
    public interface IFido2ProvisioningService
    {
        Task<IEnumerable<UserSearchResult>> SearchUsersAsync(string searchTerm);
        Task<IEnumerable<Fido2AuthenticationMethod>> GetUserFido2MethodsAsync(string userId);
        Task<Fido2ProvisioningResponse> ProvisionFido2KeyAsync(string userId, Fido2ProvisioningRequest request);
        Task<Fido2ProvisioningResponse> DeleteFido2KeyAsync(string userId, string methodId);
        Task<bool> ValidateUserExistsAsync(string userPrincipalName);
        
        // Methods that work with delegated permissions (no admin consent required)
        Task<UserSearchResult?> GetCurrentUserAsync();
        Task<IEnumerable<Fido2AuthenticationMethod>> GetCurrentUserFido2MethodsAsync();
        
        // Utility methods
        Fido2ProvisioningRequest CreateQuickProvisioningRequest(string userPrincipalName, string displayName);
    }
}