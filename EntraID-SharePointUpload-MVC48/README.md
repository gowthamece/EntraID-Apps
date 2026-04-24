# EntraID-SharePointUpload-MVC48

ASP.NET MVC 5 (.NET Framework 4.8) application that:

- Authenticates users with Microsoft Entra ID (OpenID Connect)
- Uploads files to SharePoint Online using **CSOM only** (`Microsoft.SharePointOnline.CSOM`)

## Configuration

Update in `Web.config`:

- `SharePoint:SiteUrl`
- `SharePoint:DefaultFolderServerRelativeUrl`

This project is pre-configured with the requested app registration values:

- `ida:ClientId = 602da53c-f112-4193-82ad-b19989ed1d0d`
- `ida:TenantId = b8f1747e-93a5-4b5b-8abc-91ce417dd3d6`
- `ida:ClientSecret = <set-in-local-config>`

## Important app registration setup

In Entra ID app registration:

- Add Redirect URI: `https://localhost:44345/signin-oidc` (Web)
- Grant admin consent for SharePoint application permissions required for file upload.

## Build and run

1. Open `EntraID-SharePointUpload-MVC48.csproj` in Visual Studio 2022.
2. Restore NuGet packages.
3. Run with IIS Express.
4. Sign in and upload a file from `/SharePoint/Index`.
