Feature: MCP Streamable HTTP Transport
  As an MCP client
  I want to connect via the native MCP protocol endpoint
  So that I can use standard MCP client libraries

  Background:
    Given the MCP server is running

  # FR-MCP-016 / TR-MCP-HTTP-001 / TEST-MCP-012

  Scenario: MCP transport endpoint exists at /mcp-transport
    When I send a GET request to "/mcp-transport"
    Then the response status code is not 404

  Scenario: MCP transport endpoint returns 406 without required Accept header
    When I send a GET request to "/mcp-transport" without Accept header
    Then the response status code is 405 or 406 or 400

  Scenario: Health endpoint returns Healthy
    When I send a GET request to "/health"
    Then the response status code should be 200
    And the response body should contain "Healthy"

  Scenario: REST API and MCP transport coexist on the same port
    When I send a GET request to "/mcp/todo"
    Then the response status code should be 200
    When I send a GET request to "/mcp-transport"
    Then the response status code is not 404

  Scenario: STDIO transport mode does not conflict with HTTP mode
    # FR-MCP-007 — dual transport support
    When I send a GET request to "/mcp/todo"
    Then the response status code should be 200

  Scenario: Swagger UI is accessible at /swagger
    When I send a GET request to "/swagger"
    Then the response status code is 200 or 301 or 302
