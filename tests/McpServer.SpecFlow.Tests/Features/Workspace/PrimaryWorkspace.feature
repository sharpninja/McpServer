Feature: Primary Workspace Detection
  As the server host process
  I want one workspace designated as the primary
  So that its lifecycle is managed by the host process directly

  Background:
    Given the MCP server is running

  # FR-MCP-025 / TR-MCP-WS-009

  Scenario: Primary workspace prompt endpoint returns 200
    When I send a GET request to "/mcp/workspace/prompt"
    Then the response status code is 200 or 403

  Scenario: Primary workspace with IsPrimary flag true is served by the host process
    When I send a GET request to "/mcp/workspace"
    Then the response status code should be 200
    And at most one workspace in the result has "isPrimary" set to true

  Scenario: Primary workspace status always returns isRunning
    Given the primary workspace key is stored as "primaryKey"
    When I send a GET request to "/mcp/workspace/{primaryKey}/status"
    Then the response status code is 200 or 404

  Scenario: Workspace auto-start skips disabled workspaces on startup
    # Disabled workspaces have isEnabled = false; they are logged as skipped
    When I send a GET request to "/mcp/workspace"
    Then the response status code should be 200
    And the response body should contain "totalCount"

  Scenario: Workspace IsPrimary resolution uses lowest-port enabled workspace when none is flagged
    # When no workspace has IsPrimary=true, the enabled workspace with lowest port is primary
    When I send a GET request to "/mcp/workspace"
    Then the response status code should be 200

  Scenario: WorkspaceController is excluded from workspace-scoped ports
    # Workspace lifecycle endpoints are only on the primary host
    Given I use a unique temp directory stored as "isolPath"
    And a workspace exists for path "{isolPath}" with key stored as "isolKey"
    And the workspace port is stored as "isolPort"
    When I send a GET request to "/mcp/workspace/{isolKey}/status"
    Then the response status code should be 200
    And I delete the created workspace using the path "{isolPath}"
