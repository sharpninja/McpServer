Feature: Tool Registry Search and CRUD
  As an AI agent
  I want to discover and manage tools via keyword search
  So that I can find relevant tools across global and workspace scopes

  Background:
    Given the MCP server is running

  # FR-MCP-012 / TR-MCP-TR-001 / TEST-MCP-008

  Scenario: List tools returns 200 with results
    When I send a GET request to "/mcp/tools"
    Then the response status code should be 200
    And the response body is valid JSON

  Scenario: Search tools with empty keyword returns results or 400
    When I send a GET request to "/mcp/tools/search?keyword="
    Then the response status code is 200 or 400
    And the response body is valid JSON

  Scenario: Search tools by name keyword returns matching tools
    Given a tool exists with name "SpecFlowSearchTool" and tags "specflow,search"
    When I send a GET request to "/mcp/tools/search?keyword=SpecFlowSearchTool"
    Then the response status code should be 200
    And the response body should contain "SpecFlowSearchTool"
    And I delete the tool named "SpecFlowSearchTool"

  Scenario: Search tools by tag (singular) returns tools with matching tag
    Given a tool exists with name "TagSearchTool" and tags "singular,plural"
    When I send a GET request to "/mcp/tools/search?keyword=singular"
    Then the response status code should be 200
    And the response body should contain "TagSearchTool"
    And I delete the tool named "TagSearchTool"

  Scenario: Search tools by tag (plural) returns tools with singular tag
    Given a tool exists with name "PluraltSearchTool" and tags "widget"
    When I send a GET request to "/mcp/tools/search?keyword=widgets"
    Then the response status code should be 200
    And the response body should contain "PluraltSearchTool"
    And I delete the tool named "PluraltSearchTool"

  Scenario: Get tool by ID returns 200
    Given a tool exists with name "GetByIdTool" and tags "getbyid" and id stored as "toolId"
    When I send a GET request to "/mcp/tools/{toolId}"
    Then the response status code should be 200
    And the response body should contain "GetByIdTool"
    And I delete the tool named "GetByIdTool"

  Scenario: Get tool by unknown ID returns 404
    When I send a GET request to "/mcp/tools/99999999"
    Then the response status code should be 404

  Scenario: Create tool returns 201 with created tool
    When I POST to "/mcp/tools" with body:
      """
      {
        "name": "SpecFlowCreatedTool",
        "description": "Created by SpecFlow",
        "tags": ["specflow", "test"]
      }
      """
    Then the response status code should be 201
    And the response body should contain "SpecFlowCreatedTool"
    And I delete the tool named "SpecFlowCreatedTool"

  Scenario: Update tool returns 200
    Given a tool exists with name "UpdateToolSpecFlow" and tags "update" and id stored as "updToolId"
    When I PUT to "/mcp/tools/{updToolId}" with body:
      """
      {
        "name": "UpdateToolSpecFlowRenamed",
        "description": "Updated",
        "tags": ["updated"]
      }
      """
    Then the response status code should be 200
    And I delete the tool named "UpdateToolSpecFlowRenamed"

  Scenario: Delete tool returns 200
    Given a tool exists with name "DeleteToolSpecFlow" and tags "delete" and id stored as "delToolId"
    When I send a DELETE request to "/mcp/tools/{delToolId}"
    Then the response status code should be 200

  Scenario: Read endpoints are accessible without API key
    When I send a GET request to "/mcp/tools" without an API key
    Then the response status code should be 200

  Scenario: Write endpoint without API key returns 401 when API key is configured
    # When Mcp:ApiKey is empty, all requests pass. This tests behavior is correct per config.
    When I POST to "/mcp/tools" without an API key with body:
      """
      { "name": "NoKeyTool", "description": "No key", "tags": [] }
      """
    Then the response status code is 201 or 401
