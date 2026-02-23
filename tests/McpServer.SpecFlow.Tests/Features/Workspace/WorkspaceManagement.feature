Feature: Workspace Management
  As an administrator
  I want to register, configure, and manage workspaces via the REST API
  So that multiple project workspaces can be hosted simultaneously

  Background:
    Given the MCP server is running

  # FR-MCP-009 / TR-MCP-WS-002 / TR-MCP-WS-004 / TEST-MCP-007

  Scenario: List workspaces returns 200 with valid result
    When I send a GET request to "/mcp/workspace"
    Then the response status code should be 200
    And the response body should contain "totalCount"

  Scenario: List workspaces endpoint is publicly accessible without API key
    When I send a GET request to "/mcp/workspace" without an API key
    Then the response status code should be 200

  Scenario: Create workspace assigns unique auto-incremented port starting at 7148
    Given I use a unique temp directory stored as "newWorkspacePath"
    When I POST to "/mcp/workspace" with body:
      """
      { "workspacePath": "{newWorkspacePath}", "name": "specflow-ws-porttest" }
      """
    Then the response status code should be 201
    And the response body should contain a port greater than or equal to 7148
    And I delete the created workspace using the path "{newWorkspacePath}"

  Scenario: Create workspace returns 201 with workspace details
    Given I use a unique temp directory stored as "wsPath"
    When I POST to "/mcp/workspace" with body:
      """
      { "workspacePath": "{wsPath}", "name": "specflow-ws-create" }
      """
    Then the response status code should be 201
    And the response body should contain "specflow-ws-create"
    And the response body should contain "workspacePort"
    And I delete the created workspace using the path "{wsPath}"

  Scenario: Create workspace with duplicate path returns 409
    Given I use a unique temp directory stored as "dupPath"
    And a workspace exists for path "{dupPath}"
    When I POST to "/mcp/workspace" with body:
      """
      { "workspacePath": "{dupPath}", "name": "dup-ws" }
      """
    Then the response status code should be 409
    And I delete the created workspace using the path "{dupPath}"

  Scenario: Get workspace by key returns 200 with workspace details
    Given I use a unique temp directory stored as "getWsPath"
    And a workspace exists for path "{getWsPath}" with key stored as "wsKey"
    When I send a GET request to "/mcp/workspace/{wsKey}"
    Then the response status code should be 200
    And the response body should contain "workspacePath"
    And I delete the created workspace using the path "{getWsPath}"

  Scenario: Get workspace by unknown key returns 404
    When I send a GET request to "/mcp/workspace/dW5rbm93bi1wYXRo"
    Then the response status code should be 404

  Scenario: Update workspace returns 200
    Given I use a unique temp directory stored as "updWsPath"
    And a workspace exists for path "{updWsPath}" with key stored as "updWsKey"
    When I PUT to "/mcp/workspace/{updWsKey}" with body:
      """
      { "name": "updated-name" }
      """
    Then the response status code should be 200
    And I delete the created workspace using the path "{updWsPath}"

  Scenario: Delete workspace returns 200
    Given I use a unique temp directory stored as "delWsPath"
    And a workspace exists for path "{delWsPath}" with key stored as "delWsKey"
    When I send a DELETE request to "/mcp/workspace/{delWsKey}"
    Then the response status code should be 200

  Scenario: Workspace keys are Base64URL-encoded path strings
    Given I use a unique temp directory stored as "b64Path"
    And a workspace exists for path "{b64Path}" with key stored as "b64Key"
    Then the key "{b64Key}" should be a valid Base64URL-encoded string
    And I delete the created workspace using the path "{b64Path}"

  Scenario: Start workspace returns 200
    Given I use a unique temp directory stored as "startWsPath"
    And a workspace exists for path "{startWsPath}" with key stored as "startWsKey"
    When I POST to "/mcp/workspace/{startWsKey}/start" with empty body
    Then the response status code should be 200
    And I delete the created workspace using the path "{startWsPath}"

  Scenario: Stop workspace returns 200
    Given I use a unique temp directory stored as "stopWsPath"
    And a workspace exists for path "{stopWsPath}" with key stored as "stopWsKey"
    When I POST to "/mcp/workspace/{stopWsKey}/stop" with empty body
    Then the response status code should be 200
    And I delete the created workspace using the path "{stopWsPath}"

  Scenario: Get workspace status returns 200 with running state
    Given I use a unique temp directory stored as "statusWsPath"
    And a workspace exists for path "{statusWsPath}" with key stored as "statusWsKey"
    When I send a GET request to "/mcp/workspace/{statusWsKey}/status"
    Then the response status code should be 200
    And the response body should contain "isRunning"
    And I delete the created workspace using the path "{statusWsPath}"

  Scenario: Workspace status endpoint is publicly accessible without API key
    Given I use a unique temp directory stored as "pubStatusPath"
    And a workspace exists for path "{pubStatusPath}" with key stored as "pubStatusKey"
    When I send a GET request to "/mcp/workspace/{pubStatusKey}/status" without an API key
    Then the response status code should be 200
    And I delete the created workspace using the path "{pubStatusPath}"

  # FR-MCP-021 — init scaffolding happens automatically on creation
  Scenario: Creating a workspace auto-initializes its directory scaffold
    Given I use a unique temp directory stored as "scaffoldPath"
    When I POST to "/mcp/workspace" with body:
      """
      { "workspacePath": "{scaffoldPath}", "name": "scaffold-ws" }
      """
    Then the response status code should be 201
    And the directory "{scaffoldPath}" should exist
    And I delete the created workspace using the path "{scaffoldPath}"
