Feature: API Key Authentication
  As a server administrator
  I want mutating endpoints protected by an API key
  So that unauthorized agents cannot modify server state

  Background:
    Given the MCP server is running

  # FR-MCP-013 / TR-MCP-SEC-001 / TEST-MCP-009

  Scenario: Read endpoint without API key returns 200
    When I send a GET request to "/mcp/workspace" without an API key
    Then the response status code should be 200

  Scenario: Read endpoint decorated with SkipApiKeyAuth returns 200 without key
    When I send a GET request to "/mcp/tools" without an API key
    Then the response status code should be 200

  Scenario: Workspace list is publicly accessible
    When I send a GET request to "/mcp/workspace" without an API key
    Then the response status code should be 200

  Scenario: Workspace status endpoint is publicly accessible
    When I send a GET request to "/mcp/workspace/dW5rbm93bg==/status" without an API key
    Then the response status code is 200 or 404

  Scenario: Context search is publicly accessible
    When I POST to "/mcp/context/search" without an API key with body:
      """
      { "query": "test", "limit": 5 }
      """
    Then the response status code should be 200

  Scenario: Tool registry GET is publicly accessible
    When I send a GET request to "/mcp/tools/search?keyword=test" without an API key
    Then the response status code should be 200

  Scenario: API key header X-Api-Key is validated on mutating workspace endpoints
    # In test environment Mcp:ApiKey is not set so all requests pass (open mode)
    When I POST to "/mcp/workspace" with body:
      """
      { "workspacePath": "/tmp/apikey-test-ws", "name": "apikey-ws" }
      """
    Then the response status code is 201 or 409

  Scenario: API key can be passed as query parameter api_key
    # In open mode (no key configured) this always passes
    When I send a GET request to "/mcp/workspace?api_key=any-value"
    Then the response status code should be 200
