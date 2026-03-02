<#
.SYNOPSIS
    Creates a user in the McpServer Keycloak realm.

.DESCRIPTION
    Creates a new user in the 'mcpserver' Keycloak realm, sets their password,
    and assigns a realm role. Uses the Keycloak Admin REST API.

.PARAMETER Username
    Username for the new user. Required.

.PARAMETER Password
    Password for the new user. Required.

.PARAMETER Email
    Email address. Default: {username}@mcpserver.local

.PARAMETER FirstName
    First name of the user.

.PARAMETER LastName
    Last name of the user.

.PARAMETER Role
    Realm role to assign. One of: admin, agent-manager, viewer. Default: viewer.

.PARAMETER Temporary
    If set, the user must change their password on first login.

.PARAMETER KeycloakUrl
    Base URL of the Keycloak server. Default: http://localhost:7080

.PARAMETER AdminUser
    Keycloak admin username. Default: admin

.PARAMETER AdminPassword
    Keycloak admin password. Default: admin

.PARAMETER RealmName
    Name of the realm. Default: mcpserver

.EXAMPLE
    .\New-McpUser.ps1 -Username "jdoe" -Password "SecurePass123"

.EXAMPLE
    .\New-McpUser.ps1 -Username "jdoe" -Password "SecurePass123" -Role "admin" -Email "jdoe@example.com"

.EXAMPLE
    .\New-McpUser.ps1 -Username "jdoe" -Password "SecurePass123" -KeycloakUrl "https://keycloak.example.com"
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Username,

    [Parameter(Mandatory = $true, Position = 1)]
    [string]$Password,

    [string]$Email,

    [string]$FirstName = "",

    [string]$LastName = "",

    [ValidateSet("admin", "agent-manager", "viewer")]
    [string]$Role = "viewer",

    [switch]$Temporary,

    [string]$KeycloakUrl = "http://localhost:7080",

    [string]$AdminUser = "admin",

    [string]$AdminPassword = "admin",

    [string]$RealmName = "mcpserver"
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Email)) {
    $Email = "$Username@mcpserver.local"
}

# ── Helpers ───────────────────────────────────────────────────────────────

function Get-AdminToken {
    $body = @{
        grant_type = "password"
        client_id  = "admin-cli"
        username   = $AdminUser
        password   = $AdminPassword
    }
    $response = Invoke-RestMethod -Uri "$KeycloakUrl/realms/master/protocol/openid-connect/token" `
        -Method Post -ContentType "application/x-www-form-urlencoded" -Body $body
    return $response.access_token
}

function Invoke-KeycloakApi {
    param(
        [string]$Method = "Get",
        [string]$Path,
        [object]$Body,
        [string]$Token
    )
    $params = @{
        Uri     = "$KeycloakUrl$Path"
        Method  = $Method
        Headers = @{ Authorization = "Bearer $Token" }
    }
    if ($Body) {
        $params.ContentType = "application/json"
        $params.Body = $Body | ConvertTo-Json -Depth 10 -Compress
    }
    try {
        return Invoke-RestMethod @params
    }
    catch {
        $status = $_.Exception.Response.StatusCode.value__
        if ($status -eq 409) { return $null }  # Conflict — already exists
        throw
    }
}

# ── Main ──────────────────────────────────────────────────────────────────

Write-Host ""
Write-Host "👤 Create McpServer User" -ForegroundColor Magenta
Write-Host "   Keycloak: $KeycloakUrl" -ForegroundColor Gray
Write-Host "   Realm:    $RealmName" -ForegroundColor Gray
Write-Host "   Username: $Username" -ForegroundColor Gray
Write-Host "   Role:     $Role" -ForegroundColor Gray
Write-Host ""

# Step 1: Get admin token
Write-Host "  → Authenticating with Keycloak admin..." -ForegroundColor Cyan
$token = Get-AdminToken
Write-Host "    ✓ Admin token acquired" -ForegroundColor Green

# Step 2: Check if user already exists
Write-Host "  → Checking if user '$Username' exists..." -ForegroundColor Cyan
$existingUsers = Invoke-KeycloakApi -Method Get -Path "/admin/realms/$RealmName/users?username=$Username&exact=true" -Token $token
if ($existingUsers -and $existingUsers.Count -gt 0) {
    Write-Host "    ⚠ User '$Username' already exists (id: $($existingUsers[0].id))" -ForegroundColor Yellow
    Write-Host "    Updating password and role assignment..." -ForegroundColor Yellow
    $userId = $existingUsers[0].id
}
else {
    # Step 3: Create user
    Write-Host "  → Creating user '$Username'..." -ForegroundColor Cyan
    $userBody = @{
        username      = $Username
        email         = $Email
        firstName     = $FirstName
        lastName      = $LastName
        enabled       = $true
        emailVerified = $true
    }
    Invoke-KeycloakApi -Method Post -Path "/admin/realms/$RealmName/users" -Body $userBody -Token $token | Out-Null

    # Get the user ID
    $users = Invoke-KeycloakApi -Method Get -Path "/admin/realms/$RealmName/users?username=$Username&exact=true" -Token $token
    if (-not $users -or $users.Count -eq 0) {
        Write-Host "    ✗ Failed to create user" -ForegroundColor Red
        exit 1
    }
    $userId = $users[0].id
    Write-Host "    ✓ User created (id: $userId)" -ForegroundColor Green
}

# Step 4: Set password
Write-Host "  → Setting password..." -ForegroundColor Cyan
$passwordBody = @{
    type      = "password"
    value     = $Password
    temporary = [bool]$Temporary
}
$headers = @{
    Authorization  = "Bearer $token"
    "Content-Type" = "application/json"
}
Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$userId/reset-password" `
    -Method Put -Headers $headers -Body ($passwordBody | ConvertTo-Json -Compress) | Out-Null
$tempText = if ($Temporary) { " (temporary — must change on first login)" } else { "" }
Write-Host "    ✓ Password set$tempText" -ForegroundColor Green

# Step 5: Assign role
Write-Host "  → Assigning role '$Role'..." -ForegroundColor Cyan
$roleObj = Invoke-KeycloakApi -Method Get -Path "/admin/realms/$RealmName/roles/$Role" -Token $token
if (-not $roleObj) {
    Write-Host "    ✗ Role '$Role' not found in realm '$RealmName'" -ForegroundColor Red
    exit 1
}
$roleJson = "[" + ($roleObj | ConvertTo-Json -Depth 5 -Compress) + "]"
$roleHeaders = @{
    Authorization  = "Bearer $token"
    "Content-Type" = "application/json"
}
try {
    Invoke-RestMethod -Uri "$KeycloakUrl/admin/realms/$RealmName/users/$userId/role-mappings/realm" `
        -Method Post -Headers $roleHeaders -Body $roleJson | Out-Null
}
catch {
    # Role may already be assigned
    $status = $_.Exception.Response.StatusCode.value__
    if ($status -ne 409) { throw }
}
Write-Host "    ✓ Role '$Role' assigned" -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "✅ User created successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "   Username:  $Username" -ForegroundColor White
Write-Host "   Email:     $Email" -ForegroundColor White
Write-Host "   Role:      $Role" -ForegroundColor White
Write-Host "   Realm:     $RealmName" -ForegroundColor White
Write-Host ""
Write-Host "   The user can now authenticate via:" -ForegroundColor Gray
Write-Host "     director login" -ForegroundColor White
Write-Host "   or at:" -ForegroundColor Gray
Write-Host "     $KeycloakUrl/realms/$RealmName/protocol/openid-connect/auth/device" -ForegroundColor White
Write-Host ""
