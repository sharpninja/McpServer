#!/usr/bin/env bash
# Sets up a Keycloak realm for McpServer with OIDC clients and GitHub Identity Provider.
# Reusable for deb/MSIX installation workflows.
#
# Usage:
#   ./setup-mcp-keycloak.sh
#   ./setup-mcp-keycloak.sh --github-client-id abc123 --github-client-secret secret456
#   ./setup-mcp-keycloak.sh --keycloak-url https://keycloak.example.com --realm myorg

set -euo pipefail

# Defaults
KEYCLOAK_URL="${KEYCLOAK_URL:-http://localhost:8080}"
ADMIN_USER="${ADMIN_USER:-admin}"
ADMIN_PASSWORD="${ADMIN_PASSWORD:-admin}"
REALM_NAME="${REALM_NAME:-mcpserver}"
GITHUB_CLIENT_ID=""
GITHUB_CLIENT_SECRET=""
MCP_SERVER_URL="${MCP_SERVER_URL:-http://localhost:7148}"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case "$1" in
        --keycloak-url)       KEYCLOAK_URL="$2"; shift 2 ;;
        --admin-user)         ADMIN_USER="$2"; shift 2 ;;
        --admin-password)     ADMIN_PASSWORD="$2"; shift 2 ;;
        --realm)              REALM_NAME="$2"; shift 2 ;;
        --github-client-id)   GITHUB_CLIENT_ID="$2"; shift 2 ;;
        --github-client-secret) GITHUB_CLIENT_SECRET="$2"; shift 2 ;;
        --mcp-server-url)     MCP_SERVER_URL="$2"; shift 2 ;;
        -h|--help)
            echo "Usage: $0 [options]"
            echo "  --keycloak-url URL        Keycloak base URL (default: http://localhost:8080)"
            echo "  --admin-user USER         Admin username (default: admin)"
            echo "  --admin-password PASS     Admin password (default: admin)"
            echo "  --realm NAME              Realm name (default: mcpserver)"
            echo "  --github-client-id ID     GitHub OAuth App Client ID"
            echo "  --github-client-secret S  GitHub OAuth App Client Secret"
            echo "  --mcp-server-url URL      MCP Server URL (default: http://localhost:7148)"
            exit 0
            ;;
        *) echo "Unknown option: $1"; exit 1 ;;
    esac
done

# Colors
GREEN='\033[0;32m'
CYAN='\033[0;36m'
YELLOW='\033[1;33m'
MAGENTA='\033[0;35m'
GRAY='\033[0;37m'
WHITE='\033[1;37m'
NC='\033[0m'

step()  { echo -e "  ${GREEN}✓ $1${NC}"; }
info()  { echo -e "  ${CYAN}ℹ $1${NC}"; }
warn()  { echo -e "  ${YELLOW}⚠ $1${NC}"; }

# Helper: call Keycloak Admin REST API
kc_api() {
    local method="$1"
    local path="$2"
    local body="${3:-}"
    local token="$4"

    local args=(-s -w "\n%{http_code}" -X "$method" \
        -H "Authorization: Bearer $token" \
        -H "Content-Type: application/json" \
        "${KEYCLOAK_URL}${path}")

    if [[ -n "$body" ]]; then
        args+=(-d "$body")
    fi

    local response
    response=$(curl "${args[@]}" 2>/dev/null)
    local http_code
    http_code=$(echo "$response" | tail -1)
    local body_out
    body_out=$(echo "$response" | sed '$d')

    if [[ "$http_code" == "409" ]]; then
        echo "__CONFLICT__"
        return 0
    elif [[ "$http_code" -ge 200 && "$http_code" -lt 300 ]]; then
        echo "$body_out"
        return 0
    else
        echo "ERROR: HTTP $http_code — $body_out" >&2
        return 1
    fi
}

echo -e "\n${MAGENTA}🔐 McpServer Keycloak Realm Setup${NC}"
echo -e "   ${GRAY}Keycloak: $KEYCLOAK_URL${NC}"
echo -e "   ${GRAY}Realm:    $REALM_NAME${NC}"
echo ""

# Step 1: Get admin token
info "Authenticating with Keycloak admin..."
TOKEN_RESPONSE=$(curl -s -X POST "${KEYCLOAK_URL}/realms/master/protocol/openid-connect/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "grant_type=password&client_id=admin-cli&username=${ADMIN_USER}&password=${ADMIN_PASSWORD}")
TOKEN=$(echo "$TOKEN_RESPONSE" | python3 -c "import sys,json; print(json.load(sys.stdin)['access_token'])" 2>/dev/null \
    || echo "$TOKEN_RESPONSE" | grep -o '"access_token":"[^"]*"' | cut -d'"' -f4)

if [[ -z "$TOKEN" ]]; then
    echo "ERROR: Failed to authenticate with Keycloak" >&2
    exit 1
fi
step "Authenticated"

# Step 2: Create realm
info "Creating realm '$REALM_NAME'..."
REALM_JSON=$(cat <<EOF
{
    "realm": "$REALM_NAME",
    "enabled": true,
    "displayName": "MCP Server",
    "displayNameHtml": "<h3>MCP Server</h3>",
    "registrationAllowed": false,
    "loginWithEmailAllowed": true,
    "duplicateEmailsAllowed": false,
    "resetPasswordAllowed": true,
    "editUsernameAllowed": false,
    "bruteForceProtected": true,
    "accessTokenLifespan": 300,
    "ssoSessionIdleTimeout": 1800,
    "ssoSessionMaxLifespan": 36000,
    "offlineSessionIdleTimeout": 2592000,
    "oauth2DeviceCodeLifespan": 600,
    "oauth2DevicePollingInterval": 5
}
EOF
)
RESULT=$(kc_api POST "/admin/realms" "$REALM_JSON" "$TOKEN")
if [[ "$RESULT" == "__CONFLICT__" ]]; then
    warn "Realm '$REALM_NAME' already exists — updating..."
    kc_api PUT "/admin/realms/$REALM_NAME" "$REALM_JSON" "$TOKEN" > /dev/null
fi
step "Realm '$REALM_NAME' ready"

# Step 3: Create roles
info "Creating realm roles..."
for ROLE in admin agent-manager viewer; do
    kc_api POST "/admin/realms/$REALM_NAME/roles" \
        "{\"name\":\"$ROLE\",\"description\":\"McpServer $ROLE role\"}" "$TOKEN" > /dev/null
done
step "Roles created: admin, agent-manager, viewer"

# Step 4: Create API client (mcp-server-api)
info "Creating API client 'mcp-server-api'..."
API_CLIENT_JSON=$(cat <<EOF
{
    "clientId": "mcp-server-api",
    "name": "MCP Server API",
    "description": "Confidential client for MCP Server JWT Bearer validation",
    "enabled": true,
    "protocol": "openid-connect",
    "publicClient": false,
    "serviceAccountsEnabled": true,
    "standardFlowEnabled": false,
    "directAccessGrantsEnabled": false,
    "authorizationServicesEnabled": false,
    "attributes": {
        "oauth2.device.authorization.grant.enabled": "false"
    }
}
EOF
)
kc_api POST "/admin/realms/$REALM_NAME/clients" "$API_CLIENT_JSON" "$TOKEN" > /dev/null
step "API client 'mcp-server-api' created"

# Get API client internal ID and secret
API_CLIENTS=$(kc_api GET "/admin/realms/$REALM_NAME/clients?clientId=mcp-server-api" "" "$TOKEN")
API_CLIENT_ID=$(echo "$API_CLIENTS" | python3 -c "import sys,json; print(json.load(sys.stdin)[0]['id'])" 2>/dev/null \
    || echo "$API_CLIENTS" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
API_SECRET_RESPONSE=$(kc_api GET "/admin/realms/$REALM_NAME/clients/$API_CLIENT_ID/client-secret" "" "$TOKEN")
API_SECRET=$(echo "$API_SECRET_RESPONSE" | python3 -c "import sys,json; print(json.load(sys.stdin)['value'])" 2>/dev/null \
    || echo "$API_SECRET_RESPONSE" | grep -o '"value":"[^"]*"' | cut -d'"' -f4)

# Step 5: Create Director CLI client (mcp-director)
info "Creating CLI client 'mcp-director'..."
DIRECTOR_CLIENT_JSON=$(cat <<EOF
{
    "clientId": "mcp-director",
    "name": "MCP Director CLI",
    "description": "Public client for Director CLI Device Authorization Flow",
    "enabled": true,
    "protocol": "openid-connect",
    "publicClient": true,
    "serviceAccountsEnabled": false,
    "standardFlowEnabled": true,
    "directAccessGrantsEnabled": false,
    "redirectUris": ["http://localhost:*", "$MCP_SERVER_URL/*"],
    "webOrigins": ["http://localhost:*", "$MCP_SERVER_URL"],
    "attributes": {
        "oauth2.device.authorization.grant.enabled": "true",
        "oauth2.device.polling.interval": "5"
    }
}
EOF
)
kc_api POST "/admin/realms/$REALM_NAME/clients" "$DIRECTOR_CLIENT_JSON" "$TOKEN" > /dev/null
step "CLI client 'mcp-director' created"

# Step 6: Protocol mappers
DIRECTOR_CLIENTS=$(kc_api GET "/admin/realms/$REALM_NAME/clients?clientId=mcp-director" "" "$TOKEN")
DIRECTOR_CLIENT_INTERNAL_ID=$(echo "$DIRECTOR_CLIENTS" | python3 -c "import sys,json; print(json.load(sys.stdin)[0]['id'])" 2>/dev/null \
    || echo "$DIRECTOR_CLIENTS" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

# Audience mapper
kc_api POST "/admin/realms/$REALM_NAME/clients/$DIRECTOR_CLIENT_INTERNAL_ID/protocol-mappers/models" \
    '{"name":"mcp-server-api-audience","protocol":"openid-connect","protocolMapper":"oidc-audience-mapper","config":{"included.client.audience":"mcp-server-api","id.token.claim":"false","access.token.claim":"true"}}' \
    "$TOKEN" > /dev/null

# Realm roles mapper
kc_api POST "/admin/realms/$REALM_NAME/clients/$DIRECTOR_CLIENT_INTERNAL_ID/protocol-mappers/models" \
    '{"name":"realm-roles","protocol":"openid-connect","protocolMapper":"oidc-usermodel-realm-role-mapper","config":{"claim.name":"realm_roles","jsonType.label":"String","multivalued":"true","id.token.claim":"true","access.token.claim":"true","userinfo.token.claim":"true"}}' \
    "$TOKEN" > /dev/null
step "Protocol mappers configured"

# Step 7: GitHub Identity Provider (optional)
if [[ -n "$GITHUB_CLIENT_ID" && -n "$GITHUB_CLIENT_SECRET" ]]; then
    info "Configuring GitHub Identity Provider..."
    GITHUB_IDP_JSON=$(cat <<EOF
{
    "alias": "github",
    "displayName": "GitHub",
    "providerId": "github",
    "enabled": true,
    "trustEmail": true,
    "storeToken": true,
    "firstBrokerLoginFlowAlias": "first broker login",
    "config": {
        "clientId": "$GITHUB_CLIENT_ID",
        "clientSecret": "$GITHUB_CLIENT_SECRET",
        "defaultScope": "user:email read:org",
        "syncMode": "IMPORT"
    }
}
EOF
    )
    kc_api POST "/admin/realms/$REALM_NAME/identity-provider/instances" "$GITHUB_IDP_JSON" "$TOKEN" > /dev/null

    # GitHub username mapper
    kc_api POST "/admin/realms/$REALM_NAME/identity-provider/instances/github/mappers" \
        '{"name":"github-username","identityProviderAlias":"github","identityProviderMapper":"github-user-attribute-mapper","config":{"syncMode":"INHERIT","jsonField":"login","user.attribute":"github_username"}}' \
        "$TOKEN" > /dev/null

    step "GitHub Identity Provider configured"
else
    warn "GitHub IdP skipped (no --github-client-id provided). Users can auth with username/password only."
fi

# Step 8: Create default admin user
info "Creating default admin user 'mcpadmin'..."
ADMIN_USER_JSON=$(cat <<EOF
{
    "username": "mcpadmin",
    "email": "admin@mcpserver.local",
    "enabled": true,
    "emailVerified": true,
    "firstName": "MCP",
    "lastName": "Admin",
    "credentials": [{
        "type": "password",
        "value": "mcpadmin",
        "temporary": true
    }]
}
EOF
)
kc_api POST "/admin/realms/$REALM_NAME/users" "$ADMIN_USER_JSON" "$TOKEN" > /dev/null

# Assign admin role
USERS=$(kc_api GET "/admin/realms/$REALM_NAME/users?username=mcpadmin" "" "$TOKEN")
USER_ID=$(echo "$USERS" | python3 -c "import sys,json; print(json.load(sys.stdin)[0]['id'])" 2>/dev/null \
    || echo "$USERS" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
if [[ -n "$USER_ID" ]]; then
    ADMIN_ROLE=$(kc_api GET "/admin/realms/$REALM_NAME/roles/admin" "" "$TOKEN")
    kc_api POST "/admin/realms/$REALM_NAME/users/$USER_ID/role-mappings/realm" "[$ADMIN_ROLE]" "$TOKEN" > /dev/null
fi
step "Admin user 'mcpadmin' created (temporary password: mcpadmin)"

# Summary
echo ""
echo -e "${GREEN}✅ Keycloak realm setup complete!${NC}"
echo ""
echo -e "   ${WHITE}Realm:           $REALM_NAME${NC}"
echo -e "   ${WHITE}OIDC Discovery:  $KEYCLOAK_URL/realms/$REALM_NAME/.well-known/openid-configuration${NC}"
echo -e "   ${WHITE}API Client:      mcp-server-api (secret: $API_SECRET)${NC}"
echo -e "   ${WHITE}CLI Client:      mcp-director (public, device auth flow)${NC}"
echo -e "   ${WHITE}Admin User:      mcpadmin / mcpadmin (temporary)${NC}"
if [[ -n "$GITHUB_CLIENT_ID" ]]; then
    echo -e "   ${WHITE}GitHub IdP:      Enabled${NC}"
fi
echo ""
echo -e "   ${YELLOW}Add to appsettings.json:${NC}"
echo -e "   ${GRAY}{${NC}"
echo -e "   ${GRAY}  \"Mcp\": {${NC}"
echo -e "   ${GRAY}    \"Auth\": {${NC}"
echo -e "   ${GRAY}      \"Authority\": \"$KEYCLOAK_URL/realms/$REALM_NAME\",${NC}"
echo -e "   ${GRAY}      \"Audience\": \"mcp-server-api\",${NC}"
echo -e "   ${GRAY}      \"ClientSecret\": \"$API_SECRET\",${NC}"
echo -e "   ${GRAY}      \"RequireHttpsMetadata\": false${NC}"
echo -e "   ${GRAY}    }${NC}"
echo -e "   ${GRAY}  }${NC}"
echo -e "   ${GRAY}}${NC}"
echo ""
