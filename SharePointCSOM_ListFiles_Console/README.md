# SharePointCSOM List Files Console

This sample is a minimal .NET Framework 4.8 console app that uses certificate-based app-only authentication to connect to SharePoint Online and list files from a configured folder.

## Configuration

Copy these values from SharePointCSOM_POC/Web.config into App.config:

- ida:ClientId
- ida:TenantId
- ida:CertThumbprint
- SharePoint:SiteUrl
- SharePoint:DefaultFolderServerRelativeUrl

## Run

Build and run the project. You can optionally pass a server-relative folder URL as the first command-line argument.