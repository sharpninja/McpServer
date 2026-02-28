<#
.SYNOPSIS
    Sets up a Keycloak realm for McpServer with OIDC clients and GitHub Identity Provider.

.DESCRIPTION
    Creates the 'mcpserver' realm in Keycloak with:
    - API client (mcp-server-api) for JWT Bearer validation
    - CLI client (mcp-director) for Device Authorization Flow
    - Roles: admin, agent-manager, viewer
    - Optional GitHub Identity Provider for social login
    Reusable for deb/MSIX installation workflows.

.PARAMETER KeycloakUrl
    Base URL of the Keycloak server. Default: http://localhost:8080

.PARAMETER AdminUser
    Keycloak admin username. Default: admin

.PARAMETER AdminPassword
    Keycloak admin password. Default: admin

.PARAMETER RealmName
    Name of the realm to create. Default: mcpserver

.PARAMETER GitHubClientId
    GitHub OAuth App Client ID. If omitted, GitHub IdP is not configured.

.PARAMETER GitHubClientSecret
    GitHub OAuth App Client Secret. Required if GitHubClientId is provided.

.PARAMETER McpServerUrl
    The MCP Server base URL for redirect URIs. Default: http://localhost:7147

.EXAMPLE
    ./Setup-McpKeycloak.ps1
    ./Setup-McpKeycloak.ps1 -GitHubClientId "abc123" -GitHubClientSecret "secret456"
    ./Setup-McpKeycloak.ps1 -KeycloakUrl "https://keycloak.example.com" -RealmName "myorg"
#>
[CmdletBinding()]
param(
    [string]$KeycloakUrl = "http://localhost:8080",
    [string]$AdminUser = "admin",
    [string]$AdminPassword = "admin",
    [string]$RealmName = "mcpserver",
    [string]$GitHubClientId = "",
    [string]$GitHubClientSecret = "",
    [string]$McpServerUrl = "http://localhost:7147"
)

$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Message) Write-Host "  ✓ $Message" -ForegroundColor Green }
function Write-Info { param([string]$Message) Write-Host "  ℹ $Message" -ForegroundColor Cyan }
function Write-Warn { param([string]$Message) Write-Host "  ⚠ $Message" -ForegroundColor Yellow }

function Get-AdminToken {
    $body = @{
        grant_type    = "password"
        client_id     = "admin-cli"
        username      = $AdminUser
        password      = $AdminPassword
    }
    $response = Invoke-RestMethod -Uri "$KeycloakUrl/realms/master/protocol/openid-connect/token" `
        -Method Post -ContentType "application/x-www-form-urlencoded" -Body $body
    return $response.access_token
}

function Invoke-KeycloakApi {
    param(
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [string]$Token
    )
    $headers = @{ Authorization = "Bearer $Token" }
    $params = @{
        Uri         = "$KeycloakUrl$Path"
        Method      = $Method
        Headers     = $headers
        ContentType = "application/json"
    }
    if ($Body) {
        $params.Body = ($Body | ConvertTo-Json -Depth 10)
    }
    try {
        return Invoke-RestMethod @params
    }
    catch {
        if ($_.Exception.Response.StatusCode -eq 409) {
            return $null  # Already exists
        }
        throw
    }
}

Write-Host "`n🔐 McpServer Keycloak Realm Setup" -ForegroundColor Magenta
Write-Host "   Keycloak: $KeycloakUrl" -ForegroundColor Gray
Write-Host "   Realm:    $RealmName" -ForegroundColor Gray
Write-Host ""

# Step 1: Get admin token
Write-Info "Authenticating with Keycloak admin..."
$token = Get-AdminToken
Write-Step "Authenticated"

# Step 2: Create realm
Write-Info "Creating realm '$RealmName'..."
$realm = @{
    realm                    = $RealmName
    enabled                  = $true
    displayName              = "MCP Server"
    displayNameHtml          = "<h3>MCP Server</h3>"
    registrationAllowed      = $false
    loginWithEmailAllowed    = $true
    duplicateEmailsAllowed   = $false
    resetPasswordAllowed     = $true
    editUsernameAllowed      = $false
    bruteForceProtected      = $true
    # Issue tokens with at least 24h of validity (server enforces >= 86400s).
    # Use 25h for access tokens to avoid edge cases around issuance/clock skew.
    accessTokenLifespan      = 90000
    # Keep online sessions alive long enough so refresh_expires_in also clears the 24h minimum.
    ssoSessionIdleTimeout    = 172800
    ssoSessionMaxLifespan    = 172800
    clientSessionIdleTimeout = 172800
    clientSessionMaxLifespan = 172800
    offlineSessionIdleTimeout = 2592000
    oauth2DeviceCodeLifespan = 600
    oauth2DevicePollingInterval = 5
}
$result = Invoke-KeycloakApi -Method Post -Path "/admin/realms" -Body $realm -Token $token
if ($null -eq $result) {
    Write-Warn "Realm '$RealmName' already exists — updating..."
    Invoke-KeycloakApi -Method Put -Path "/admin/realms/$RealmName" -Body $realm -Token $token | Out-Null
}
Write-Step "Realm '$RealmName' ready"

# Step 3: Create roles
Write-Info "Creating realm roles..."
$roles = @("admin", "agent-manager", "viewer")
foreach ($role in $roles) {
    $roleBody = @{ name = $role; description = "McpServer $role role" }
    Invoke-KeycloakApi -Method Post -Path "/admin/realms/$RealmName/roles" -Body $roleBody -Token $token | Out-Null
}
Write-Step "Roles created: $($roles -join ', ')"

# Step 4: Create API client (mcp-server-api) — confidential, for JWT validation
Write-Info "Creating API client 'mcp-server-api'..."
$apiClient = @{
    clientId                  = "mcp-server-api"
    name                      = "MCP Server API"
    description               = "Confidential client for MCP Server JWT Bearer validation"
    enabled                   = $true
    protocol                  = "openid-connect"
    publicClient              = $false
    serviceAccountsEnabled    = $true
    standardFlowEnabled       = $false
    directAccessGrantsEnabled = $false
    authorizationServicesEnabled = $false
    attributes                = @{
        "oauth2.device.authorization.grant.enabled" = "false"
    }
}
Invoke-KeycloakApi -Method Post -Path "/admin/realms/$RealmName/clients" -Body $apiClient -Token $token | Out-Null
Write-Step "API client 'mcp-server-api' created"

# Get the API client's internal ID and secret
$clients = Invoke-KeycloakApi -Method Get -Path "/admin/realms/$RealmName/clients?clientId=mcp-server-api" -Token $token
$apiClientId = $clients[0].id
$apiSecret = (Invoke-KeycloakApi -Method Get -Path "/admin/realms/$RealmName/clients/$apiClientId/client-secret" -Token $token).value

# Step 5: Create Director CLI client (mcp-director) — public, device auth flow
Write-Info "Creating CLI client 'mcp-director'..."
$directorClient = @{
    clientId                  = "mcp-director"
    name                      = "MCP Director CLI"
    description               = "Public client for Director CLI Device Authorization Flow"
    enabled                   = $true
    protocol                  = "openid-connect"
    publicClient              = $true
    serviceAccountsEnabled    = $false
    standardFlowEnabled       = $true
    directAccessGrantsEnabled = $false
    redirectUris              = @("http://localhost:*", "$McpServerUrl/*")
    webOrigins                = @("http://localhost:*", $McpServerUrl)
    attributes                = @{
        "oauth2.device.authorization.grant.enabled" = "true"
        "oauth2.device.polling.interval"            = "5"
    }
}
Invoke-KeycloakApi -Method Post -Path "/admin/realms/$RealmName/clients" -Body $directorClient -Token $token | Out-Null
Write-Step "CLI client 'mcp-director' created"

# Step 6: Create default protocol mappers for the director client
$directorClients = Invoke-KeycloakApi -Method Get -Path "/admin/realms/$RealmName/clients?clientId=mcp-director" -Token $token
$directorClientInternalId = $directorClients[0].id

# Add audience mapper so the API client appears in the token audience
$audienceMapper = @{
    name            = "mcp-server-api-audience"
    protocol        = "openid-connect"
    protocolMapper  = "oidc-audience-mapper"
    config          = @{
        "included.client.audience" = "mcp-server-api"
        "id.token.claim"           = "false"
        "access.token.claim"       = "true"
    }
}
Invoke-KeycloakApi -Method Post -Path "/admin/realms/$RealmName/clients/$directorClientInternalId/protocol-mappers/models" -Body $audienceMapper -Token $token | Out-Null

# Add realm roles mapper
$rolesMapper = @{
    name            = "realm-roles"
    protocol        = "openid-connect"
    protocolMapper  = "oidc-usermodel-realm-role-mapper"
    config          = @{
        "claim.name"         = "realm_roles"
        "jsonType.label"     = "String"
        "multivalued"        = "true"
        "id.token.claim"     = "true"
        "access.token.claim" = "true"
        "userinfo.token.claim" = "true"
    }
}
Invoke-KeycloakApi -Method Post -Path "/admin/realms/$RealmName/clients/$directorClientInternalId/protocol-mappers/models" -Body $rolesMapper -Token $token | Out-Null
Write-Step "Protocol mappers configured"

# Step 7: Configure GitHub Identity Provider (optional)
if ($GitHubClientId -and $GitHubClientSecret) {
    Write-Info "Configuring GitHub Identity Provider..."
    $githubIdp = @{
        alias                     = "github"
        displayName               = "GitHub"
        providerId                = "github"
        enabled                   = $true
        trustEmail                = $true
        storeToken                = $true
        firstBrokerLoginFlowAlias = "first broker login"
        config                    = @{
            clientId     = $GitHubClientId
            clientSecret = $GitHubClientSecret
            defaultScope = "user:email read:org"
            syncMode     = "IMPORT"
        }
    }
    Invoke-KeycloakApi -Method Post -Path "/admin/realms/$RealmName/identity-provider/instances" -Body $githubIdp -Token $token | Out-Null

    # Add GitHub username mapper
    $githubUsernameMapper = @{
        name                   = "github-username"
        identityProviderAlias  = "github"
        identityProviderMapper = "github-user-attribute-mapper"
        config                 = @{
            syncMode                       = "INHERIT"
            "jsonField"                    = "login"
            "user.attribute"               = "github_username"
        }
    }
    Invoke-KeycloakApi -Method Post -Path "/admin/realms/$RealmName/identity-provider/instances/github/mappers" -Body $githubUsernameMapper -Token $token | Out-Null

    Write-Step "GitHub Identity Provider configured"
}
else {
    Write-Warn "GitHub IdP skipped (no --GitHubClientId provided). Users can auth with username/password only."
}

# Step 8: Create a default admin user
Write-Info "Creating default admin user 'mcpadmin'..."
$adminUserJson = @"
{
    "username": "mcpadmin",
    "email": "admin@mcpserver.local",
    "enabled": true,
    "emailVerified": true,
    "firstName": "MCP",
    "lastName": "Admin",
    "credentials": [
        {
            "type": "password",
            "value": "mcpadmin",
            "temporary": true
        }
    ]
}
"@
$headers = @{ Authorization = "Bearer $token" }
try {
    Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/users" -Method Post -Headers $headers -ContentType "application/json" -Body $adminUserJson | Out-Null
}
catch {
    if ($_.Exception.Response.StatusCode -ne 409) { throw }
}

# Assign admin role to the user
$users = Invoke-KeycloakApi -Method Get -Path "/admin/realms/$RealmName/users?username=mcpadmin" -Token $token
if ($users -and $users.Count -gt 0) {
    $userId = $users[0].id
    $adminRole = Invoke-KeycloakApi -Method Get -Path "/admin/realms/$RealmName/roles/admin" -Token $token
    $roleJson = "[" + ($adminRole | ConvertTo-Json -Depth 5 -Compress) + "]"
    $roleHeaders = @{ Authorization = "Bearer $token" }
    try {
        Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$userId/role-mappings/realm" -Method Post -Headers $roleHeaders -ContentType "application/json" -Body $roleJson | Out-Null
    }
    catch {
        if ($_.Exception.Response.StatusCode -ne 409) {
            Write-Warn "Role assignment warning: $($_.Exception.Message)"
        }
    }
}
Write-Step "Admin user 'mcpadmin' created (temporary password: mcpadmin)"

# Summary
Write-Host ""
Write-Host "✅ Keycloak realm setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "   Realm:           $RealmName" -ForegroundColor White
Write-Host "   OIDC Discovery:  $KeycloakUrl/realms/$RealmName/.well-known/openid-configuration" -ForegroundColor White
Write-Host "   API Client:      mcp-server-api (secret: $apiSecret)" -ForegroundColor White
Write-Host "   CLI Client:      mcp-director (public, device auth flow)" -ForegroundColor White
Write-Host "   Admin User:      mcpadmin / mcpadmin (temporary)" -ForegroundColor White
if ($GitHubClientId) {
    Write-Host "   GitHub IdP:      Enabled" -ForegroundColor White
}
Write-Host ""
Write-Host "   Add to appsettings.json:" -ForegroundColor Yellow
$configBlock = @"
   {
     "Mcp": {
       "Auth": {
         "Authority": "$KeycloakUrl/realms/$RealmName",
         "Audience": "mcp-server-api",
         "ClientSecret": "$apiSecret",
         "RequireHttpsMetadata": false
       }
     }
   }
"@
Write-Host $configBlock -ForegroundColor Gray
Write-Host ""
