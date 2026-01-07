# Securing Azure API Management with Microsoft Entra ID, Zero Trust, and DefaultAzureCredential

**A Comprehensive Guide to Passwordless API Authentication**

---

**Author:** Development Team  
**Date:** January 2025  
**Version:** 1.0  

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Introduction](#introduction)
3. [The Problem: Traditional Secret-Based Authentication](#the-problem-traditional-secret-based-authentication)
4. [Zero Trust Security Model](#zero-trust-security-model)
5. [The Solution: DefaultAzureCredential and Managed Identity](#the-solution-defaultazurecredential-and-managed-identity)
6. [Understanding DefaultAzureCredential](#understanding-defaultazurecredential)
7. [Implementation Guide](#implementation-guide)
8. [Testing with Visual Studio](#testing-with-visual-studio)
9. [Production Deployment with Managed Identity](#production-deployment-with-managed-identity)
10. [Troubleshooting Guide](#troubleshooting-guide)
11. [Conclusion](#conclusion)
12. [References](#references)

---

## Executive Summary

This document presents a modern, secure approach to API authentication that eliminates the need for shared secrets among development teams. By leveraging **Microsoft Entra ID**, **DefaultAzureCredential**, and **Managed Identities**, organizations can implement a Zero Trust security model that:

- **Eliminates secret sprawl** across developer machines and configuration files
- **Reduces operational overhead** of rotating and managing client secrets
- **Enables seamless authentication** from local development to production
- **Enforces fine-grained authorization** through App Roles validated by Azure API Management

This approach aligns with Microsoft's Zero Trust principles: **verify explicitly**, **use least privilege access**, and **assume breach**.

---

## Introduction

### The Challenge

In enterprise environments, securing APIs while maintaining developer productivity is a constant balancing act. Traditional approaches often involve:

- Sharing client secrets among team members
- Storing credentials in configuration files
- Managing separate authentication flows for development and production
- Complex secret rotation procedures

These practices introduce security vulnerabilities and operational complexity that modern cloud-native applications cannot afford.

### The Solution

This guide demonstrates how to build a **Blazor Server application** that securely calls APIs hosted behind **Azure API Management (APIM)** using a passwordless authentication approach. Developers authenticate using their own identities through Visual Studio or Azure CLI, while production workloads use Managed Identities�all through a unified `DefaultAzureCredential` implementation.

### Architecture Overview

```
???????????????????????????????????????????????????????????????????????????????
?                           DEVELOPMENT ENVIRONMENT                            ?
???????????????????????????????????????????????????????????????????????????????
?                                                                              ?
?   ???????????????????         ???????????????????         ???????????????? ?
?   ?  Developer PC   ?         ?   Azure API     ?         ?  Backend     ? ?
?   ?                 ?  HTTPS  ?   Management    ?  HTTPS  ?  API         ? ?
?   ?  Blazor Server  ???????????                 ???????????              ? ?
?   ?  Application    ?         ?  JWT Validation ?         ?  (Weather)   ? ?
?   ???????????????????         ?  + App Roles    ?         ???????????????? ?
?            ?                  ???????????????????                          ?
?            ?                           ?                                    ?
?            ? Visual Studio             ?                                    ?
?            ? Credential                ?                                    ?
?            ?    OR                     ?                                    ?
?            ? Azure CLI                 ?                                    ?
?            ?                           ?                                    ?
?   ???????????????????????????????????????????????????????????????????????  ?
?   ?                     Microsoft Entra ID                               ?  ?
?   ?                                                                      ?  ?
?   ?  ??????????????????  ??????????????????  ?????????????????????????? ?  ?
?   ?  ? API App        ?  ? Developer      ?  ? App Roles              ? ?  ?
?   ?  ? Registration   ?  ? Identity       ?  ? (weather.read)         ? ?  ?
?   ?  ?                ?  ? (Personal AAD) ?  ?                        ? ?  ?
?   ?  ??????????????????  ??????????????????  ?????????????????????????? ?  ?
?   ???????????????????????????????????????????????????????????????????????  ?
?                                                                              ?
???????????????????????????????????????????????????????????????????????????????

???????????????????????????????????????????????????????????????????????????????
?                           PRODUCTION ENVIRONMENT                             ?
???????????????????????????????????????????????????????????????????????????????
?                                                                              ?
?   ???????????????????         ???????????????????         ???????????????? ?
?   ?  Azure App      ?         ?   Azure API     ?         ?  Backend     ? ?
?   ?  Service        ?  HTTPS  ?   Management    ?  HTTPS  ?  API         ? ?
?   ?                 ???????????                 ???????????              ? ?
?   ?  Blazor Server  ?         ?  JWT Validation ?         ?  (Weather)   ? ?
?   ???????????????????         ?  + App Roles    ?         ???????????????? ?
?            ?                  ???????????????????                          ?
?            ?                           ?                                    ?
?            ? Managed                   ?                                    ?
?            ? Identity                  ?                                    ?
?            ? (No Secrets!)             ?                                    ?
?            ?                           ?                                    ?
?   ???????????????????????????????????????????????????????????????????????  ?
?   ?                     Microsoft Entra ID                               ?  ?
?   ?                                                                      ?  ?
?   ?  ??????????????????  ??????????????????  ?????????????????????????? ?  ?
?   ?  ? API App        ?  ? Managed        ?  ? App Roles              ? ?  ?
?   ?  ? Registration   ?  ? Identity       ?  ? (weather.read)         ? ?  ?
?   ?  ?                ?  ? (System/User)  ?  ?                        ? ?  ?
?   ?  ??????????????????  ??????????????????  ?????????????????????????? ?  ?
?   ???????????????????????????????????????????????????????????????????????  ?
?                                                                              ?
???????????????????????????????????????????????????????????????????????????????
```

---

## The Problem: Traditional Secret-Based Authentication

### How Organizations Typically Handle API Authentication

In traditional development workflows, teams often resort to sharing client secrets for API testing:

```
???????????????????????????????????????????????????????????????????????????????
?                    TRADITIONAL (PROBLEMATIC) APPROACH                        ?
???????????????????????????????????????????????????????????????????????????????
?                                                                              ?
?   ???????????????    ???????????????    ???????????????    ??????????????? ?
?   ? Developer 1 ?    ? Developer 2 ?    ? Developer 3 ?    ? Developer 4 ? ?
?   ?             ?    ?             ?    ?             ?    ?             ? ?
?   ? appsettings ?    ? appsettings ?    ? appsettings ?    ? appsettings ? ?
?   ? .json       ?    ? .json       ?    ? .json       ?    ? .json       ? ?
?   ?             ?    ?             ?    ?             ?    ?             ? ?
?   ? ClientSecret?    ? ClientSecret?    ? ClientSecret?    ? ClientSecret? ?
?   ? = "abc123"  ?    ? = "abc123"  ?    ? = "abc123"  ?    ? = "abc123"  ? ?
?   ???????????????    ???????????????    ???????????????    ??????????????? ?
?          ?                  ?                  ?                  ?        ?
?          ?                  ?                  ?                  ?        ?
?          ?                  ?                  ?                  ?        ?
?   ???????????????????????????????????????????????????????????????????????  ?
?   ?                         SHARED CLIENT SECRET                         ?  ?
?   ?                                                                      ?  ?
?   ?   � Same secret on multiple machines                                 ?  ?
?   ?   � Stored in plain text configuration files                        ?  ?
?   ?   � Often committed to source control accidentally                   ?  ?
?   ?   � No individual accountability                                     ?  ?
?   ?   � Rotation requires coordinating with entire team                  ?  ?
?   ???????????????????????????????????????????????????????????????????????  ?
?                                                                              ?
???????????????????????????????????????????????????????????????????????????????
```

### The Hidden Costs of Shared Secrets

#### 1. Security Risks

| Risk | Description | Impact |
|------|-------------|--------|
| **Secret Sprawl** | Secrets copied across multiple developer machines, chat messages, emails, and documentation | Increased attack surface; difficult to track who has access |
| **Accidental Exposure** | Secrets committed to Git repositories, logged in application outputs, or visible in screenshots | Potential data breach; credential theft |
| **No Accountability** | When multiple people share the same credential, audit logs cannot distinguish individual actions | Compliance violations; inability to investigate incidents |
| **Lateral Movement** | Compromised secret on one machine can be used to access resources from anywhere | Full API access from any location |

#### 2. Operational Overhead

| Challenge | Description | Time/Cost Impact |
|-----------|-------------|------------------|
| **Secret Distribution** | Securely sharing new secrets with team members | 30-60 minutes per rotation |
| **Secret Rotation** | Coordinating rotation across all developers simultaneously | 2-4 hours per rotation event |
| **Onboarding** | Setting up new developers with correct credentials | 1-2 hours per new team member |
| **Offboarding** | Ensuring departing employees no longer have valid credentials | Immediate rotation required; risk of oversight |
| **Environment Parity** | Managing different secrets for dev/test/staging/prod | Configuration drift; deployment failures |
| **Expiration Management** | Tracking when secrets expire and planning renewals | Unexpected outages if missed |

#### 3. Compliance Challenges

Many regulatory frameworks explicitly discourage or prohibit shared credentials:

- **SOC 2 Type II**: Requires unique identification and authentication for each user
- **ISO 27001**: Mandates individual accountability for access
- **PCI DSS**: Requires unique IDs for computer access
- **HIPAA**: Requires unique user identification

### Real-World Scenario: The Cost of a Leaked Secret

Consider this common scenario:

1. A developer copies the client secret to a local file for testing
2. The file is accidentally committed to a public GitHub repository
3. Automated scanners detect the secret within minutes
4. Attackers use the secret to access your API
5. **Response required:**
   - Immediate secret rotation
   - Notify all developers to update their configurations
   - Audit all API access logs
   - Assess data exposure
   - File incident report (if regulated industry)

**Estimated cost: $10,000 - $100,000+** depending on data sensitivity and regulatory requirements.

---

## Zero Trust Security Model

### What is Zero Trust?

Zero Trust is a security framework that assumes no implicit trust for any user, device, or network�whether inside or outside the organization's perimeter. Every access request must be fully authenticated, authorized, and encrypted.

### Microsoft's Zero Trust Principles

```
???????????????????????????????????????????????????????????????????????????????
?                         ZERO TRUST PRINCIPLES                                ?
???????????????????????????????????????????????????????????????????????????????
?                                                                              ?
?   ???????????????????????????????????????????????????????????????????????   ?
?   ?                      1. VERIFY EXPLICITLY                            ?   ?
?   ?                                                                      ?   ?
?   ?   Always authenticate and authorize based on all available data     ?   ?
?   ?   points, including user identity, location, device health,         ?   ?
?   ?   service or workload, data classification, and anomalies.          ?   ?
?   ?                                                                      ?   ?
?   ?   ? Use strong authentication (MFA, passwordless)                   ?   ?
?   ?   ? Validate every request with real-time signals                   ?   ?
?   ?   ? Use conditional access policies                                 ?   ?
?   ???????????????????????????????????????????????????????????????????????   ?
?                                                                              ?
?   ???????????????????????????????????????????????????????????????????????   ?
?   ?                    2. USE LEAST PRIVILEGE ACCESS                     ?   ?
?   ?                                                                      ?   ?
?   ?   Limit user access with just-in-time and just-enough-access        ?   ?
?   ?   (JIT/JEA), risk-based adaptive policies, and data protection      ?   ?
?   ?   to protect both data and productivity.                            ?   ?
?   ?                                                                      ?   ?
?   ?   ? Grant only permissions needed for the task                      ?   ?
?   ?   ? Use App Roles for fine-grained authorization                    ?   ?
?   ?   ? Implement time-limited access where possible                    ?   ?
?   ???????????????????????????????????????????????????????????????????????   ?
?                                                                              ?
?   ???????????????????????????????????????????????????????????????????????   ?
?   ?                        3. ASSUME BREACH                              ?   ?
?   ?                                                                      ?   ?
?   ?   Minimize blast radius and segment access. Verify end-to-end       ?   ?
?   ?   encryption and use analytics to get visibility, drive threat      ?   ?
?   ?   detection, and improve defenses.                                  ?   ?
?   ?                                                                      ?   ?
?   ?   ? Segment access by environment and role                          ?   ?
?   ?   ? Monitor and log all access attempts                             ?   ?
?   ?   ? Use short-lived tokens instead of long-lived secrets            ?   ?
?   ???????????????????????????????????????????????????????????????????????   ?
?                                                                              ?
???????????????????????????????????????????????????????????????????????????????
```

### How This Solution Implements Zero Trust

| Principle | Traditional Approach | Zero Trust Approach (This Solution) |
|-----------|---------------------|-------------------------------------|
| **Verify Explicitly** | Static client secret validates the app | Each developer authenticates with their own identity; tokens include user context |
| **Least Privilege** | Full API access once authenticated | App Roles limit access to specific operations (e.g., `weather.read`) |
| **Assume Breach** | Shared secret = full compromise | Individual credentials = limited blast radius; easy revocation |

### Individual Identity vs. Shared Secrets

```
???????????????????????????????????????????????????????????????????????????????
?                    SHARED SECRET vs. INDIVIDUAL IDENTITY                     ?
???????????????????????????????????????????????????????????????????????????????
?                                                                              ?
?   SHARED SECRET (Anti-Pattern)          INDIVIDUAL IDENTITY (Zero Trust)    ?
?   ?????????????????????????????         ????????????????????????????????    ?
?                                                                              ?
?   Developer A ??                        Developer A ??? Identity A           ?
?                ?                                         ?                   ?
?   Developer B ????? ClientSecret123     Developer B ??? Identity B           ?
?                ?         ?                               ?                   ?
?   Developer C ??         ?              Developer C ??? Identity C           ?
?                          ?                               ?                   ?
?                          ?                               ?                   ?
?                    ????????????                    ????????????              ?
?                    ?   API    ?                    ?   API    ?              ?
?                    ????????????                    ????????????              ?
?                                                                              ?
?   Audit Log:                            Audit Log:                           ?
?   "ClientSecret123 accessed /weather"   "john@company.com accessed /weather" ?
?   "ClientSecret123 accessed /weather"   "jane@company.com accessed /weather" ?
?   "ClientSecret123 accessed /weather"   "bob@company.com accessed /weather"  ?
?                                                                              ?
?   ? Cannot identify who made request   ? Full accountability                ?
?   ? Cannot revoke individual access    ? Revoke individual access easily   ?
?   ? Secret rotation affects everyone   ? No secrets to rotate               ?
?                                                                              ?
???????????????????????????????????????????????????????????????????????????????
```

---

## The Solution: DefaultAzureCredential and Managed Identity

### Overview

The Azure Identity library provides `DefaultAzureCredential`, a unified authentication mechanism that:

1. **During Development**: Uses the developer's own identity (Visual Studio, Azure CLI, etc.)
2. **In Production**: Uses Azure Managed Identity (no secrets required)

This single line of code works in both environments:

```csharp
var credential = new DefaultAzureCredential();
```

### Benefits Summary

| Benefit | Description |
|---------|-------------|
| **No Shared Secrets** | Each developer uses their own Microsoft Entra ID identity |
| **No Secret Management** | Managed Identities are automatically provisioned and rotated by Azure |
| **Unified Code** | Same authentication code works locally and in production |
| **Full Accountability** | Every API call is associated with a specific identity |
| **Easy Revocation** | Disable a user or Managed Identity instantly |
| **Compliance Ready** | Meets requirements for individual identification |

### Cost-Benefit Analysis

| Factor | Shared Secrets | DefaultAzureCredential + Managed Identity |
|--------|---------------|------------------------------------------|
| **Initial Setup** | Low (copy secret to config) | Medium (configure Entra ID, assign roles) |
| **Ongoing Maintenance** | High (rotation, distribution) | None (automatic) |
| **Security Risk** | High | Minimal |
| **Compliance** | Requires compensating controls | Native compliance |
| **Developer Experience** | Simple but risky | Simple and secure |
| **Incident Response** | Complex (shared credential) | Simple (individual revocation) |

---

## Understanding DefaultAzureCredential

### What is DefaultAzureCredential?

`DefaultAzureCredential` is a class in the Azure Identity library that provides a simplified, unified authentication experience. It automatically attempts multiple authentication methods in a predefined order, selecting the first one that succeeds.

### The Credential Chain

When you create a `DefaultAzureCredential`, it attempts authentication in this order:

```
???????????????????????????????????????????????????????????????????????????????
?                     DefaultAzureCredential Chain                             ?
???????????????????????????????????????????????????????????????????????????????
?                                                                              ?
?   1. EnvironmentCredential                                                   ?
?      ??? Uses AZURE_CLIENT_ID, AZURE_CLIENT_SECRET, AZURE_TENANT_ID         ?
?          (Typically disabled for security)                                   ?
?                                                                              ?
?   2. WorkloadIdentityCredential                                              ?
?      ??? For Kubernetes workloads with Azure AD Workload Identity            ?
?                                                                              ?
?   3. ManagedIdentityCredential        ??? PRODUCTION                        ?
?      ??? For Azure-hosted apps (App Service, VMs, Functions, AKS)           ?
?          No secrets required - Azure manages everything                      ?
?                                                                              ?
?   4. SharedTokenCacheCredential                                              ?
?      ??? Uses tokens cached by other Azure tools                             ?
?                                                                              ?
?   5. VisualStudioCredential           ??? DEVELOPMENT                       ?
?      ??? Uses the account signed into Visual Studio                          ?
?                                                                              ?
?   6. VisualStudioCodeCredential                                              ?
?      ??? Uses the Azure Account extension in VS Code                         ?
?                                                                              ?
?   7. AzureCliCredential               ??? DEVELOPMENT (Fallback)            ?
?      ??? Uses 'az login' credentials                                         ?
?                                                                              ?
?   8. AzurePowerShellCredential                                               ?
?      ??? Uses 'Connect-AzAccount' credentials                                ?
?                                                                              ?
?   9. InteractiveBrowserCredential                                            ?
?      ??? Opens browser for interactive login (usually disabled)              ?
?                                                                              ?
???????????????????????????????????????????????????????????????????????????????
```

### Customizing the Credential Chain

For optimal security, customize which credentials are attempted:

```csharp
var tenantId = "your-tenant-id";

var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions
{
    TenantId = tenantId,
    
    // EXCLUDE: Credentials that use secrets or aren't needed
    ExcludeEnvironmentCredential = true,       // Don't use env var secrets
    ExcludeWorkloadIdentityCredential = true,  // Not using Kubernetes
    ExcludeSharedTokenCacheCredential = true,  // Avoid token cache issues
    ExcludeInteractiveBrowserCredential = true, // Don't prompt unexpectedly
    
    // INCLUDE: Developer-friendly credentials
    ExcludeVisualStudioCredential = false,     // Primary for VS developers
    ExcludeAzureCliCredential = false,         // Fallback / CI-CD
    
    // INCLUDE/EXCLUDE: Based on environment
    ExcludeManagedIdentityCredential = isDevelopment  // Only in production
});
```

### Why Specify TenantId?

When calling an API protected by Entra ID, the token must be issued by the correct tenant. Without specifying `TenantId`:

- The credential might use your default tenant (personal Microsoft account)
- The token's issuer won't match the API's expected issuer
- Authentication will fail with "issuer validation failed"

**Always specify the tenant ID** for enterprise applications.

---

## Implementation Guide

### Prerequisites

Before implementing, ensure you have:

- **Azure Subscription** with permissions to create resources
- **Microsoft Entra ID** tenant (Azure AD)
- **Azure API Management** instance
- **Visual Studio 2022** or later with Azure workload
- **.NET 8 SDK** installed

### Part 1: Microsoft Entra ID Configuration

#### Step 1: Create the Backend API App Registration

This app registration represents your API that APIM protects.

1. Navigate to **Azure Portal** ? **Microsoft Entra ID** ? **App registrations**
2. Click **New registration**
3. Configure:
   - **Name**: `Weather-API` (or your API name)
   - **Supported account types**: Accounts in this organizational directory only (Single tenant)
4. Click **Register**
5. Note the **Application (client) ID** - this is your API's identifier

#### Step 2: Expose an API

1. In your API app registration, go to **Expose an API**
2. Click **Set** next to Application ID URI ? Accept default (`api://{client-id}`) or customize
3. Click **Add a scope**:
   - **Scope name**: `access_as_user`
   - **Who can consent**: Admins and users
   - **Admin consent display name**: `Access Weather API`
   - **Admin consent description**: `Allows the application to access the Weather API on behalf of the signed-in user`
   - **User consent display name**: `Access Weather API`
   - **User consent description**: `Allows the application to access the Weather API on your behalf`
   - **State**: Enabled

#### Step 3: Define App Roles

App Roles provide fine-grained authorization. APIM can validate that a token contains specific roles before allowing access.

1. In your API app registration, go to **App roles**
2. Click **Create app role**:
   - **Display name**: `Weather Reader`
   - **Allowed member types**: `Users/Groups` (or `Both` if service principals will also call the API)
   - **Value**: `weather.read`
   - **Description**: `Allows reading weather forecast data`
   - **Do you want to enable this app role?**: Checked

```json
{
  "allowedMemberTypes": ["User", "Application"],
  "description": "Allows reading weather forecast data",
  "displayName": "Weather Reader",
  "isEnabled": true,
  "value": "weather.read"
}
```

#### Step 4: Assign App Roles to Users/Groups

1. Go to **Microsoft Entra ID** ? **Enterprise applications** (not App registrations)
2. Find your API application (search by name)
3. Go to **Users and groups** ? **Add user/group**
4. Select users or a security group (e.g., "Development Team")
5. Select the `Weather Reader` role
6. Click **Assign**

#### Step 5: Authorize Developer Tools

To allow Visual Studio and Azure CLI to request tokens for your API without requiring admin consent:

1. In your API app registration, go to **Expose an API**
2. Under **Authorized client applications**, click **Add a client application**
3. Add each developer tool:

| Tool | Client ID | Description |
|------|-----------|-------------|
| Visual Studio | `04f0c124-f2bc-4f59-8241-bf6df9866bbd` | Visual Studio IDE |
| Azure CLI | `04b07795-8ddb-461a-bbee-02f9e1bf7b46` | Azure CLI (`az login`) |
| VS Code | `aebc6443-996d-45c2-90f0-388ff96faa56` | VS Code Azure extension |
| Azure PowerShell | `1950a258-227b-4e31-a9cf-717495945fc2` | Azure PowerShell module |

4. For each, select your scope (`api://{client-id}/access_as_user`) and click **Add application**

### Part 2: Azure API Management Configuration

#### JWT Validation Policy

Configure APIM to validate incoming JWT tokens and enforce App Roles:

```xml
<policies>
    <inbound>
        <base />
        
        <!-- Validate JWT Token from Microsoft Entra ID -->
        <validate-jwt header-name="Authorization" 
                      failed-validation-httpcode="401" 
                      failed-validation-error-message="Unauthorized. Access token is missing or invalid."
                      require-expiration-time="true"
                      require-signed-tokens="true">
            
            <!-- OpenID Configuration for automatic key discovery -->
            <openid-config url="https://login.microsoftonline.com/{tenant-id}/v2.0/.well-known/openid-configuration" />
            
            <!-- Validate the token audience matches our API -->
            <audiences>
                <audience>api://YOUR_BACKEND_API_APP_ID_HERE</audience>
            </audiences>
            
            <!-- Accept tokens from both v1 and v2 endpoints -->
            <issuers>
                <issuer>https://sts.windows.net/{tenant-id}/</issuer>
                <issuer>https://login.microsoftonline.com/{tenant-id}/v2.0</issuer>
            </issuers>
            
            <!-- AUTHORIZATION: Require the weather.read App Role -->
            <required-claims>
                <claim name="roles" match="any">
                    <value>weather.read</value>
                </claim>
            </required-claims>
            
        </validate-jwt>
        
        <!-- Optional: Extract claims for downstream services or logging -->
        <set-header name="X-User-Id" exists-action="override">
            <value>@{
                var authHeader = context.Request.Headers.GetValueOrDefault("Authorization", "");
                if (authHeader.StartsWith("Bearer "))
                {
                    var jwt = authHeader.Substring(7).AsJwt();
                    return jwt?.Claims.GetValueOrDefault("oid", "unknown") ?? "unknown";
                }
                return "unknown";
            }</value>
        </set-header>
        
        <set-header name="X-User-Email" exists-action="override">
            <value>@{
                var authHeader = context.Request.Headers.GetValueOrDefault("Authorization", "");
                if (authHeader.StartsWith("Bearer "))
                {
                    var jwt = authHeader.Substring(7).AsJwt();
                    return jwt?.Claims.GetValueOrDefault("email", 
                           jwt?.Claims.GetValueOrDefault("preferred_username", "unknown")) ?? "unknown";
                }
                return "unknown";
            }</value>
        </set-header>
        
    </inbound>
    <backend>
        <base />
    </backend>
    <outbound>
        <base />
    </outbound>
    <on-error>
        <base />
        <return-response>
            <set-status code="401" reason="Unauthorized" />
            <set-header name="WWW-Authenticate" exists-action="override">
                <value>Bearer error="invalid_token", error_description="The access token is invalid or has expired"</value>
            </set-header>
            <set-body>@{
                return new JObject(
                    new JProperty("error", "unauthorized"),
                    new JProperty("message", context.LastError.Message)
                ).ToString();
            }</set-body>
        </return-response>
    </on-error>
</policies>
```

#### Policy Components Explained

| Element | Purpose |
|---------|---------|
| `validate-jwt` | Main element that enables JWT validation |
| `openid-config` | Points to your tenant's OpenID configuration for automatic signing key discovery |
| `audiences` | Validates the `aud` claim matches your API's Application ID URI |
| `issuers` | Validates tokens come from your Microsoft Entra ID tenant |
| `required-claims` | Ensures the token contains the required `weather.read` App Role |
| `set-header` | Extracts user information for logging and downstream services |

### Part 3: Blazor Application Implementation

#### Project Structure

```
EntraID-Blazor-APIM-Client/
??? Components/
?   ??? App.razor
?   ??? Routes.razor
?   ??? Layout/
?   ?   ??? MainLayout.razor
?   ?   ??? NavMenu.razor
?   ??? Pages/
?       ??? Home.razor
?       ??? Weather.razor
??? Services/
?   ??? ApiService.cs
?   ??? AuthService.cs
??? appsettings.json
??? appsettings.Development.json
??? Program.cs
??? EntraID-Blazor-APIM-Client.csproj
```

#### Step 1: Install Required NuGet Packages

```xml
<ItemGroup>
    <PackageReference Include="Azure.Identity" Version="1.10.4" />
</ItemGroup>
```

#### Step 2: Configure appsettings.json

```json
{
  "BackendAPI": {
    "BaseUrl": "https://your-apim-instance.azure-api.net",
    "Scopes": "api://YOUR_BACKEND_API_APP_ID_HERE/.default"
  },
  "AzureAd": {
    "TenantId": "your-tenant-id"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**Important Note on Scope Format:**

When using `DefaultAzureCredential`, use the `/.default` scope suffix. This requests all statically configured permissions for the application, which is required for client credentials and developer credentials flows.

- ? Correct: `api://YOUR_BACKEND_API_APP_ID_HERE/.default`
- ? Incorrect: `api://YOUR_BACKEND_API_APP_ID_HERE/access_as_user`

#### Step 3: Configure Program.cs

```csharp
using Azure.Core;
using Azure.Identity;
using EntraID_Blazor_APIM_Client.Components;
using EntraID_Blazor_APIM_Client.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// AUTHENTICATION CONFIGURATION
// ============================================================================

TokenCredential credential;
string authMethod;

var tenantId = builder.Configuration["AzureAd:TenantId"] 
    ?? throw new InvalidOperationException("TenantId not configured");

// Configure DefaultAzureCredential with appropriate options
// In Development: Uses Visual Studio or Azure CLI credentials
// In Production: Uses Managed Identity
try
{
    var options = new DefaultAzureCredentialOptions
    {
        TenantId = tenantId,
        
        // Security: Exclude credentials that use secrets
        ExcludeEnvironmentCredential = true,
        ExcludeSharedTokenCacheCredential = true,
        
        // Development: Enable developer tooling credentials
        ExcludeVisualStudioCredential = false,
        ExcludeAzureCliCredential = false,
        
        // Production: Managed Identity (enabled in production only)
        ExcludeManagedIdentityCredential = builder.Environment.IsDevelopment(),
        
        // Disable less common credentials
        ExcludeWorkloadIdentityCredential = true,
        ExcludeVisualStudioCodeCredential = true,
        ExcludeAzurePowerShellCredential = true,
        ExcludeInteractiveBrowserCredential = true
    };
    
    credential = new DefaultAzureCredential(options);
    
    if (builder.Environment.IsDevelopment())
    {
        authMethod = "DefaultAzureCredential (Developer Identity)";
        Console.WriteLine("??????????????????????????????????????????????????????????????");
        Console.WriteLine("?  DEVELOPMENT MODE - Using Developer Identity               ?");
        Console.WriteLine("??????????????????????????????????????????????????????????????");
        Console.WriteLine($"?  Tenant: {tenantId}");
        Console.WriteLine("?  Credential chain:");
        Console.WriteLine("?    1. Visual Studio (primary)");
        Console.WriteLine("?    2. Azure CLI (fallback)");
        Console.WriteLine("??????????????????????????????????????????????????????????????");
        Console.WriteLine("?  ? Ensure you're signed into Visual Studio with an        ?");
        Console.WriteLine("?    account that has the 'weather.read' App Role assigned  ?");
        Console.WriteLine("??????????????????????????????????????????????????????????????");
    }
    else
    {
        authMethod = "DefaultAzureCredential (Managed Identity)";
        Console.WriteLine("??????????????????????????????????????????????????????????????");
        Console.WriteLine("?  PRODUCTION MODE - Using Managed Identity                  ?");
        Console.WriteLine("??????????????????????????????????????????????????????????????");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"? DefaultAzureCredential setup failed: {ex.Message}");
    throw;
}

// ============================================================================
// SERVICE REGISTRATION
// ============================================================================

// Register the credential as a singleton (thread-safe, handles token caching)
builder.Services.AddSingleton<TokenCredential>(credential);
builder.Services.AddSingleton(new AuthenticationInfo { Method = authMethod });

// Register HttpClient for APIM with retry and timeout policies
builder.Services.AddHttpClient("BackendAPI", client =>
{
    var baseUrl = builder.Configuration["BackendAPI:BaseUrl"]
        ?? throw new InvalidOperationException("BackendAPI:BaseUrl not configured");
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    // Production: Consider certificate pinning
});

// Register application services
builder.Services.AddScoped<ApiService>();

// Add Blazor services
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ============================================================================
// APPLICATION PIPELINE
// ============================================================================

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

// ============================================================================
// SUPPORTING TYPES
// ============================================================================

public class AuthenticationInfo
{
    public string Method { get; set; } = "Unknown";
}
```

#### Step 4: Create the API Service

```csharp
using Azure.Core;
using System.Net.Http.Headers;

namespace EntraID_Blazor_APIM_Client.Services;

public class ApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TokenCredential _credential;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApiService> _logger;

    public ApiService(
        IHttpClientFactory httpClientFactory,
        TokenCredential credential,
        IConfiguration configuration,
        ILogger<ApiService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _credential = credential;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetWeatherForecastAsync()
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("BackendAPI");
            
            // Get the scope from configuration
            var apiScope = _configuration["BackendAPI:Scopes"] 
                ?? throw new InvalidOperationException("BackendAPI:Scopes not configured");
            
            _logger.LogDebug("Requesting token for scope: {Scope}", apiScope);
            
            // Request token using DefaultAzureCredential
            // This will use Visual Studio/Azure CLI in development
            // and Managed Identity in production
            var tokenRequestContext = new TokenRequestContext(new[] { apiScope });
            var accessToken = await _credential.GetTokenAsync(
                tokenRequestContext, 
                CancellationToken.None);

            _logger.LogDebug("Token acquired, expires at: {ExpiresOn}", accessToken.ExpiresOn);

            // Debug: Log token claims (remove in production)
            #if DEBUG
            LogTokenClaims(accessToken.Token);
            #endif

            // Add Bearer token to request
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", accessToken.Token);

            // Call the APIM endpoint
            var response = await httpClient.GetAsync("/weather/forecast");
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully retrieved weather data");
                return await response.Content.ReadAsStringAsync();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "API returned {StatusCode}: {Content}", 
                    response.StatusCode, 
                    errorContent);
                return $"Error: {response.StatusCode} - {errorContent}";
            }
        }
        catch (Azure.Identity.AuthenticationFailedException authEx)
        {
            _logger.LogError(authEx, "Authentication failed");
            return $"Authentication Error: {authEx.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling weather API");
            return $"Exception: {ex.Message}";
        }
    }

    private void LogTokenClaims(string token)
    {
        try
        {
            var tokenParts = token.Split('.');
            if (tokenParts.Length >= 2)
            {
                var payload = tokenParts[1];
                // Add padding if needed for base64 decoding
                var paddedPayload = payload.PadRight(
                    payload.Length + (4 - payload.Length % 4) % 4, '=');
                var decodedBytes = Convert.FromBase64String(paddedPayload);
                var decodedJson = System.Text.Encoding.UTF8.GetString(decodedBytes);
                
                _logger.LogDebug("Token Claims: {Claims}", decodedJson);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decode token for logging");
        }
    }
}
```

#### Step 5: Create the Weather Page

```razor
@page "/weather"
@using EntraID_Blazor_APIM_Client.Services
@inject ApiService ApiService
@inject AuthenticationInfo AuthInfo
@rendermode InteractiveServer

<PageTitle>Weather Forecast</PageTitle>

<div class="container">
    <h1>Weather Forecast from APIM</h1>
    
    <div class="card mb-4">
        <div class="card-header bg-info text-white">
            <strong>?? Authentication Information</strong>
        </div>
        <div class="card-body">
            <p class="mb-0"><strong>Method:</strong> @AuthInfo.Method</p>
        </div>
    </div>

    <p class="text-muted">
        This component demonstrates fetching data from Azure API Management 
        using App Role authentication with DefaultAzureCredential.
    </p>

    @if (loading)
    {
        <div class="d-flex align-items-center">
            <div class="spinner-border text-primary me-3" role="status">
                <span class="visually-hidden">Loading...</span>
            </div>
            <span>Fetching weather data...</span>
        </div>
    }
    else if (!string.IsNullOrEmpty(errorMessage))
    {
        <div class="alert alert-danger" role="alert">
            <h5 class="alert-heading">?? Error</h5>
            <p>@errorMessage</p>
            <hr />
            <p class="mb-0">
                <strong>Troubleshooting Tips:</strong>
                <ul class="mt-2">
                    <li>Verify you're signed into Visual Studio with the correct account</li>
                    <li>Ensure your account has the 'weather.read' App Role assigned</li>
                    <li>Check that Visual Studio is authorized in the API's app registration</li>
                    <li>Try running <code>az login --tenant your-tenant-id</code> as a fallback</li>
                </ul>
            </p>
        </div>
    }
    else
    {
        <div class="alert alert-success" role="alert">
            <strong>? Success!</strong> Weather data retrieved successfully.
        </div>
        
        <div class="card">
            <div class="card-header">
                API Response
            </div>
            <div class="card-body">
                <pre class="mb-0" style="white-space: pre-wrap;">@weatherData</pre>
            </div>
        </div>
    }

    <div class="mt-4">
        <button class="btn btn-primary" @onclick="LoadWeatherData" disabled="@loading">
            <span class="me-2">??</span>
            @(loading ? "Loading..." : "Refresh Data")
        </button>
    </div>
</div>

@code {
    private bool loading = false;
    private string? weatherData;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        await LoadWeatherData();
    }

    private async Task LoadWeatherData()
    {
        loading = true;
        errorMessage = null;
        weatherData = null;
        StateHasChanged();
        
        try
        {
            var result = await ApiService.GetWeatherForecastAsync();
            
            if (result.StartsWith("Error:") || 
                result.StartsWith("Exception:") || 
                result.StartsWith("Authentication Error:"))
            {
                errorMessage = result;
            }
            else
            {
                weatherData = result;
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Unexpected error: {ex.Message}";
        }
        finally
        {
            loading = false;
            StateHasChanged();
        }
    }
}
```

---

## Testing with Visual Studio

### Prerequisites Checklist

Before testing, verify the following:

- [ ] Visual Studio 2022 or later installed
- [ ] Azure workload installed in Visual Studio
- [ ] Signed into Visual Studio with a Microsoft Entra ID account
- [ ] Account belongs to the same tenant as the API
- [ ] Account has been assigned the `weather.read` App Role
- [ ] Visual Studio client ID is authorized in the API's app registration

### Step 1: Verify Visual Studio Sign-In

1. Open Visual Studio
2. Go to **File** ? **Account Settings**
3. Verify you're signed in with the correct organizational account
4. The account should be from the same tenant as your API

Alternatively:

1. Go to **Tools** ? **Options**
2. Navigate to **Azure Service Authentication**
3. Verify the selected account and tenant

### Step 2: Authorize Visual Studio (One-Time Setup)

In Azure Portal:

1. Navigate to **Microsoft Entra ID** ? **App registrations**
2. Select your API app registration
3. Go to **Expose an API** ? **Authorized client applications**
4. Click **Add a client application**
5. Enter Visual Studio's client ID: `04f0c124-f2bc-4f59-8241-bf6df9866bbd`
6. Select your scope and click **Add application**

### Step 3: Run the Application

1. Press **F5** or click **Start Debugging** in Visual Studio
2. Observe the console output:

```
??????????????????????????????????????????????????????????????
?  DEVELOPMENT MODE - Using Developer Identity               ?
??????????????????????????????????????????????????????????????
?  Tenant: your-tenant-id
?  Credential chain:
?    1. Visual Studio (primary)
?    2. Azure CLI (fallback)
??????????????????????????????????????????????????????????????
?  ? Ensure you're signed into Visual Studio with an        ?
?    account that has the 'weather.read' App Role assigned  ?
??????????????????????????????????????????????????????????????
```

3. Navigate to `/weather` in your browser
4. Verify data is retrieved successfully

### Alternative: Using Azure CLI

If Visual Studio credentials don't work:

1. Open a terminal
2. Log in with Azure CLI:

```powershell
az login --tenant your-tenant-id
```

3. Verify your account:

```powershell
az account show
```

4. Run the application - it will fall back to Azure CLI credentials

---

## Production Deployment with Managed Identity

### Why Managed Identity?

Managed Identity is Azure's solution for service-to-service authentication without secrets:

| Feature | Description |
|---------|-------------|
| **No Secrets** | Azure manages the credentials automatically |
| **Automatic Rotation** | Credentials are rotated automatically by Azure |
| **No Storage** | Nothing to store, backup, or accidentally expose |
| **Azure-Native** | Works seamlessly with all Azure services |
| **Auditable** | All access is logged in Azure AD sign-in logs |

### Types of Managed Identity

| Type | Use Case | Lifecycle |
|------|----------|-----------|
| **System-Assigned** | Single resource needs identity | Created with resource, deleted when resource is deleted |
| **User-Assigned** | Multiple resources share an identity | Independent lifecycle, can be shared across resources |

### Step 1: Enable Managed Identity on Azure App Service

#### Azure Portal

1. Navigate to your App Service
2. Go to **Identity** ? **System assigned**
3. Set **Status** to **On**
4. Click **Save**
5. Note the **Object ID** - you'll need this for role assignment

#### Azure CLI

```bash
az webapp identity assign \
    --name your-app-name \
    --resource-group your-resource-group
```

### Step 2: Assign App Role to Managed Identity

Since Managed Identities are service principals, you need to use PowerShell or Azure CLI to assign App Roles (the portal only supports user assignments).

#### PowerShell

```powershell
# Connect to Azure
Connect-AzAccount

# Variables
$managedIdentityObjectId = "your-managed-identity-object-id"  # From Step 1
$apiAppId = "YOUR_BACKEND_API_APP_ID_HERE"            # Your API's App ID
$appRoleValue = "weather.read"                                  # The App Role to assign

# Get the API's service principal
$apiSp = Get-AzADServicePrincipal -ApplicationId $apiAppId

# Find the specific App Role
$appRole = $apiSp.AppRole | Where-Object { $_.Value -eq $appRoleValue }

if ($null -eq $appRole) {
    Write-Error "App Role '$appRoleValue' not found on the API"
    return
}

# Assign the App Role to the Managed Identity
New-AzADServicePrincipalAppRoleAssignment `
    -ServicePrincipalId $managedIdentityObjectId `
    -ResourceId $apiSp.Id `
    -AppRoleId $appRole.Id

Write-Host "Successfully assigned '$appRoleValue' role to Managed Identity"
```

#### Azure CLI

```bash
# Variables
MANAGED_IDENTITY_OBJECT_ID="your-managed-identity-object-id"
API_APP_ID="YOUR_BACKEND_API_APP_ID_HERE"

# Get the API's service principal object ID
API_SP_OBJECT_ID=$(az ad sp show --id $API_APP_ID --query id -o tsv)

# Get the App Role ID (assumes 'weather.read' role exists)
APP_ROLE_ID=$(az ad sp show --id $API_APP_ID --query "appRoles[?value=='weather.read'].id" -o tsv)

# Assign the role using Microsoft Graph API
az rest --method POST \
    --uri "https://graph.microsoft.com/v1.0/servicePrincipals/$MANAGED_IDENTITY_OBJECT_ID/appRoleAssignments" \
    --headers "Content-Type=application/json" \
    --body "{
        \"principalId\": \"$MANAGED_IDENTITY_OBJECT_ID\",
        \"resourceId\": \"$API_SP_OBJECT_ID\",
        \"appRoleId\": \"$APP_ROLE_ID\"
    }"
```

### Step 3: Deploy Application

Deploy your Blazor application to Azure App Service. The code automatically detects the production environment and uses Managed Identity:

```csharp
// From Program.cs - this happens automatically
ExcludeManagedIdentityCredential = builder.Environment.IsDevelopment()
// In production: ManagedIdentityCredential is enabled and used first
```

### Step 4: Verify Production Authentication

1. Navigate to your deployed application
2. Go to the weather page
3. Verify data is retrieved successfully
4. Check Azure AD sign-in logs:
   - Go to **Microsoft Entra ID** ? **Sign-in logs**
   - Filter by your Managed Identity name
   - Verify successful authentications

---

## Troubleshooting Guide

### Common Errors and Solutions

#### Error: "The resource principal named api://... was not found"

**Cause:** Incorrect scope format.

**Solution:** Use the `/.default` scope format:

```json
{
  "BackendAPI": {
    "Scopes": "api://YOUR_BACKEND_API_APP_ID_HERE/.default"
  }
}
```

#### Error: "consent_required" or "AADSTS65001"

**Cause:** Visual Studio or Azure CLI hasn't been authorized to request tokens for your API.

**Solution:** Add the tool's client ID to your API's authorized client applications:

1. Go to your API's App Registration ? **Expose an API**
2. Under **Authorized client applications**, add:
   - Visual Studio: `04f0c124-f2bc-4f59-8241-bf6df9866bbd`
   - Azure CLI: `04b07795-8ddb-461a-bbee-02f9e1bf7b46`

#### Error: "AADSTS700016: Application not found"

**Cause:** The API app registration doesn't exist or is in a different tenant.

**Solution:**
1. Verify the API's Application ID is correct in your scope
2. Ensure you're authenticated to the correct tenant
3. Verify `TenantId` is set in `DefaultAzureCredentialOptions`

#### Error: 401 from APIM with "roles claim missing"

**Cause:** Your identity doesn't have the required App Role assigned.

**Solution:**
1. For users: Go to **Enterprise applications** ? Your API ? **Users and groups** ? Assign the role
2. For Managed Identity: Use PowerShell/CLI to assign the App Role (see production deployment section)

#### Error: "DefaultAzureCredential failed to retrieve a token"

**Cause:** No credential in the chain could authenticate.

**Solution:**
1. Verify Visual Studio is signed in with the correct account
2. Try Azure CLI as fallback: `az login --tenant your-tenant-id`
3. Check the full error message for specific credential failures

### Debugging Token Contents

To inspect what claims are in your token, temporarily add this code:

```csharp
var tokenParts = accessToken.Token.Split('.');
if (tokenParts.Length >= 2)
{
    var payload = tokenParts[1];
    var paddedPayload = payload.PadRight(
        payload.Length + (4 - payload.Length % 4) % 4, '=');
    var decodedBytes = Convert.FromBase64String(paddedPayload);
    var decodedJson = System.Text.Encoding.UTF8.GetString(decodedBytes);
    Console.WriteLine($"Token Claims: {decodedJson}");
}
```

Check for:
- `aud`: Should match your API's Application ID URI
- `iss`: Should be your tenant's issuer URL
- `roles`: Should contain `weather.read`

---

## Conclusion

### Summary of Benefits

By implementing `DefaultAzureCredential` with Managed Identity, organizations achieve:

| Benefit | Impact |
|---------|--------|
| **Zero Secrets in Code** | Eliminates risk of secret exposure in source control |
| **Individual Accountability** | Every API call is traceable to a specific identity |
| **Simplified Operations** | No secret rotation, distribution, or expiration management |
| **Seamless Environment Transition** | Same code works in development and production |
| **Fine-Grained Authorization** | App Roles provide precise access control |
| **Compliance Ready** | Meets regulatory requirements for unique identification |
| **Reduced Attack Surface** | No shared credentials means smaller blast radius |

### Zero Trust Implementation Checklist

- [x] **Verify Explicitly**: Each request includes a JWT validated by APIM
- [x] **Least Privilege**: App Roles limit access to specific operations
- [x] **Assume Breach**: Individual credentials enable quick revocation

### Operational Improvements

| Before (Shared Secrets) | After (DefaultAzureCredential) |
|------------------------|-------------------------------|
| Secret rotation: 2-4 hours | No rotation needed |
| Developer onboarding: 1-2 hours | 5 minutes (assign App Role) |
| Developer offboarding: Manual, error-prone | Instant (disable account) |
| Incident response: Rotate everything | Revoke single identity |
| Compliance audits: Compensating controls | Native compliance |

### Next Steps

1. **Implement this pattern** in your organization's API clients
2. **Review existing applications** for shared secret usage
3. **Create security policies** requiring DefaultAzureCredential for new projects
4. **Train development teams** on the new authentication approach
5. **Monitor and audit** using Azure AD sign-in logs

---

## References

### Microsoft Documentation

- [DefaultAzureCredential Overview](https://learn.microsoft.com/en-us/dotnet/azure/sdk/authentication/)
- [Managed Identities for Azure Resources](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview)
- [App Roles in Microsoft Entra ID](https://learn.microsoft.com/en-us/entra/identity-platform/howto-add-app-roles-in-apps)
- [Validate JWT Policy in APIM](https://learn.microsoft.com/en-us/azure/api-management/validate-jwt-policy)
- [Zero Trust Security Model](https://learn.microsoft.com/en-us/security/zero-trust/)

### Azure Identity Library

- [Azure.Identity NuGet Package](https://www.nuget.org/packages/Azure.Identity)
- [Azure Identity Client Library for .NET](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/identity-readme)
- [Troubleshooting Guide](https://aka.ms/azsdk/net/identity/defaultazurecredential/troubleshoot)

### Security Best Practices

- [Microsoft Security Development Lifecycle](https://www.microsoft.com/en-us/securityengineering/sdl)
- [OWASP Authentication Best Practices](https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html)

---

**Document Version:** 1.0  
**Last Updated:** January 2025  
**Author:** Development Team  

---

*This document is intended for internal use and provides guidance on implementing secure API authentication using Microsoft Entra ID and Azure services.*
