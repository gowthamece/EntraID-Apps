# FIDO2 User Provisioning Application

This Blazor application provides an interface for administrators to manage FIDO2 security keys for users in Azure AD / Entra ID using the Microsoft Graph API.

## Features

- **User Search**: Search for users by display name, email, or user principal name
- **View Existing Keys**: View all FIDO2 security keys associated with a user
- **Provision New Keys**: Initiate the provisioning process for new FIDO2 security keys
- **Delete Keys**: Remove existing FIDO2 security keys from user accounts
- **Authentication**: Secure access using Azure AD authentication

## Prerequisites

### Azure AD App Registration

1. Register a new application in Azure AD / Entra ID
2. Configure the following settings:
   - **Redirect URIs**: Add `https://localhost:[port]/signin-oidc` (replace [port] with your application port)
   - **Client Secret**: Generate a new client secret and note it down

### API Permissions

Grant the following Microsoft Graph permissions to your app registration:
- `User.Read` (Delegated) - Sign in and read user profile
- `UserAuthenticationMethod.ReadWrite.All` (Application or Delegated) - Read and write all users' authentication methods

**Important**: Admin consent is required for the `UserAuthenticationMethod.ReadWrite.All` permission.

### Configuration

Update the `appsettings.json` or `appsettings.Development.json` file with your Azure AD configuration:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "your-domain.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "CallbackPath": "/signin-oidc"
  },
  "MicrosoftGraph": {
    "BaseUrl": "https://graph.microsoft.com/v1.0",
    "Scopes": "User.Read UserAuthenticationMethod.ReadWrite.All"
  }
}
```

Replace the following values:
- `your-domain.onmicrosoft.com`: Your Azure AD tenant domain
- `your-tenant-id`: Your Azure AD tenant ID
- `your-client-id`: The Application (client) ID from your app registration
- `your-client-secret`: The client secret you generated

## Running the Application

1. Clone this repository
2. Navigate to the project directory
3. Update the configuration as described above
4. Run the application:
   ```
   dotnet run
   ```
5. Navigate to `https://localhost:[port]` in your browser
6. Sign in with an administrator account that has the necessary permissions

## Usage

1. **Authentication**: Sign in using your Azure AD credentials
2. **Search Users**: Use the search functionality to find users in your organization
3. **Select User**: Click "Select" on a user to view their FIDO2 keys and provision new ones
4. **View Existing Keys**: The "Existing FIDO2 Keys" tab shows all current security keys
5. **Provision New Key**: Use the "Provision New Key" tab to initiate key provisioning

## Important Notes

### FIDO2 Key Provisioning Process

The actual FIDO2 key registration involves several steps:

1. **Server-side Challenge Generation**: The server generates a cryptographic challenge
2. **Client-side Credential Creation**: The user's browser uses the WebAuthn API to interact with their security key
3. **Key Registration**: The public key and attestation data are sent back to the server for verification and storage

This application provides the administrative interface for managing keys, but the actual key registration requires:
- WebAuthn-compatible browser
- Physical FIDO2 security key
- User interaction to complete the registration

### Security Considerations

- Ensure your Azure AD app registration has appropriate permissions
- Use HTTPS in production environments
- Regularly rotate client secrets
- Monitor access logs for security auditing
- Consider implementing additional authorization checks for administrative functions

### Troubleshooting

**Common Issues:**

1. **Authentication Errors**: Verify your Azure AD configuration and ensure admin consent has been granted
2. **Graph API Errors**: Check that the required permissions are granted and consented
3. **User Search Issues**: Ensure the signed-in user has permissions to read user directory information
4. **FIDO2 Method Access**: Verify the `UserAuthenticationMethod.ReadWrite.All` permission is properly configured

**Logging:**

The application includes comprehensive logging. Check the console output for detailed error messages and Graph API responses.

## API Reference

This application uses the Microsoft Graph API's Authentication Methods endpoints:

- **GET** `/users/{userId}/authentication/fido2Methods` - List user's FIDO2 methods
- **POST** `/users/{userId}/authentication/fido2Methods` - Create new FIDO2 method
- **DELETE** `/users/{userId}/authentication/fido2Methods/{methodId}` - Delete FIDO2 method

For more information, see the [Microsoft Graph Authentication Methods API documentation](https://docs.microsoft.com/en-us/graph/api/resources/authenticationmethods-overview).

## License

This project is licensed under the MIT License - see the LICENSE file for details.