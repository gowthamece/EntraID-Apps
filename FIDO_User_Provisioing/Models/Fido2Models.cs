using System.ComponentModel.DataAnnotations;

namespace FIDO_User_Provisioing.Models
{
    public class Fido2AuthenticationMethod
    {
        public string? Id { get; set; }
        public string? DisplayName { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public string? Model { get; set; }
        public byte[]? PublicKeyCredential { get; set; }
        public string? AttestationCertificates { get; set; }
        public int AttestationLevel { get; set; } = 1;
        public bool IsBackupEligible { get; set; }
        public bool IsBackedUp { get; set; }
    }

    public class Fido2ProvisioningRequest
    {
        [Required]
        [Display(Name = "User Principal Name")]
        public string UserPrincipalName { get; set; } = string.Empty;
        
        [Required]
        [Display(Name = "Display Name")]
        public string DisplayName { get; set; } = string.Empty;
        
        [Display(Name = "Model")]
        public string? Model { get; set; }
        
        [Display(Name = "Attestation Level")]
        public int AttestationLevel { get; set; } = 1;
        
        [Display(Name = "Is Backup Eligible")]
        public bool IsBackupEligible { get; set; } = false;
    }

    public class Fido2ProvisioningResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? MethodId { get; set; }
        public string? Error { get; set; }
    }

    public class UserSearchResult
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string UserPrincipalName { get; set; } = string.Empty;
        public string Mail { get; set; } = string.Empty;
    }
}