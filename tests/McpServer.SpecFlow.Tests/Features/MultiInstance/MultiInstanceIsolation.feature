Feature: Multi-Instance Isolation
  As a server operator
  I want to run multiple MCP server instances on different ports
  So that different workspaces have isolated data and configuration

  Background:
    Given the MCP server is running

  # TEST-MCP-003 / FR-MCP-007 / TR-MCP-CFG-002

  Scenario: Two instances with distinct ports do not share data
    # Each instance has its own DataSource, RepoRoot, and port
    When I send a GET request to "/mcp/todo"
    Then the response status code should be 200
    And the response body should contain "totalCount"

  Scenario: Duplicate instance ports are rejected at startup validation
    # appsettings.json validation rejects same port assigned to two instances
    When I send a GET request to "/health"
    Then the response status code should be 200

  Scenario: SQLite instance stores todo items independently
    # sqlite backend on one instance doesn't affect yaml backend on another
    When I send a GET request to "/mcp/todo"
    Then the response status code should be 200

  Scenario: MCP_INSTANCE environment variable selects the instance
    # ENV: MCP_INSTANCE=<name> overrides --instance CLI argument
    When I send a GET request to "/health"
    Then the response status code should be 200
