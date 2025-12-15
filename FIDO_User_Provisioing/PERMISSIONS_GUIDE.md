# Microsoft Graph Permissions Guide for FIDO2 User Provisioning

## Current Error: Authorization_RequestDenied

This error occurs because your application doesn't have the necessary permissions granted in Azure AD to access Microsoft Graph APIs for authentication method management.

## Solution Options

### Option 1: Self-Service (Current User Only) - NO ADMIN CONSENT REQUIRED

**Configuration:** Already updated in your code
- Scopes: `User.Read UserAuthenticationMethod.ReadWrite`
- Permissions: Delegated permissions only
- Access: Current user can manage their own FIDO2 keys only

**Azure AD App Registration Setup:**
1. Go to [Azure Portal](https://portal.azure.com) → Azure Active Directory → App registrations
2. Find your app: `b75d3fff-d090-4cec-9738-a997c3168cfb`
3. Go to **API permissions**
4. Add these **Delegated permissions** for Microsoft Graph:
   - `User.Read` ✅
   - `UserAuthenticationMethod.ReadWrite` ✅
5. **No admin consent required** - users can consent individually

**Use these methods in your app:**
- `GetCurrentUserAsync()` - Get current user info
- `GetCurrentUserFido2MethodsAsync()` - Get current user's FIDO2 methods
- `ProvisionFido2KeyAsync(currentUserId, request)` - Provision for current user
- `DeleteFido2KeyAsync(currentUserId, methodId)` - Delete current user's method

### Option 2: Admin/Helpdesk (All Users) - REQUIRES ADMIN CONSENT

**Configuration:** 
- Scopes: `User.Read UserAuthenticationMethod.ReadWrite.All`
- Permissions: Application permissions (higher privilege)
- Access: Can manage FIDO2 keys for all users in the tenant

**Azure AD App Registration Setup:**
1. Same steps as Option 1, but add:
   - **Application permissions**: `UserAuthenticationMethod.ReadWrite.All`
2. Click **Grant admin consent for [tenant name]** ⚠️ **Required**
3. Admin must approve before the app can work

**Use these methods in your app:**
- `SearchUsersAsync(searchTerm)` - Search all users
- `GetUserFido2MethodsAsync(userId)` - Get any user's FIDO2 methods
- All other methods work for any user

## Recommendation

**Start with Option 1** (self-service) because:
- ✅ No admin consent required
- ✅ Users can manage their own keys immediately
- ✅ More secure (principle of least privilege)
- ✅ Easier to deploy

**Upgrade to Option 2** later if you need:
- Admin/helpdesk to manage all users' FIDO2 keys
- Bulk provisioning capabilities
- Reporting across all users

## Required Steps to Fix Current Error

1. **Configure Azure AD App Registration:**
   - Add `UserAuthenticationMethod.ReadWrite` delegated permission
   - Ensure `User.Read` is already present

2. **Update your application code:**
   - Use `GetCurrentUserAsync()` instead of `SearchUsersAsync()`
   - Use `GetCurrentUserFido2MethodsAsync()` for current user's methods

3. **Test the application:**
   - Users will be prompted to consent to permissions on first login
   - No admin approval needed

## Permission Scopes Explained

| Permission | Type | Admin Consent | Description |
|------------|------|---------------|-------------|
| `User.Read` | Delegated | No | Read current user's profile |
| `UserAuthenticationMethod.ReadWrite` | Delegated | No | Manage current user's auth methods |
| `UserAuthenticationMethod.ReadWrite.All` | Application | **Yes** | Manage all users' auth methods |

## Security Notes

- **Client Secret**: Your `appsettings.json` contains a client secret. Consider using Azure Key Vault or user secrets for production.
- **Permissions**: Start with minimal permissions and add more only when needed.
- **Audit**: All FIDO2 method changes are logged in Azure AD audit logs.
