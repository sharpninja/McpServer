Feature: Repo File Access
  As an AI agent
  I want to read, write, and list repository files via the REST API
  So that I can access project files from within the MCP context

  Background:
    Given the MCP server is running

  # FR-SUPPORT-010 / TR-MCP-API-001

  Scenario: List repo files at root returns 200 with entries
    When I send a GET request to "/mcp/repo/list"
    Then the response status code should be 200
    And the response body should contain "entries"

  Scenario: List repo files with path returns 200
    When I send a GET request to "/mcp/repo/list?path=docs"
    Then the response status code is 200 or 400

  Scenario: Read repo file with missing path parameter returns 400
    When I send a GET request to "/mcp/repo/file"
    Then the response status code should be 400

  Scenario: Read repo file with valid path returns 200
    When I send a GET request to "/mcp/repo/file?path=README.md"
    Then the response status code is 200 or 400

  Scenario: Write repo file with missing path returns 400
    When I POST to "/mcp/repo/file" with body:
      """
      { "content": "some content without path" }
      """
    Then the response status code should be 400

  Scenario: Write repo file with path outside repo root returns 400
    When I POST to "/mcp/repo/file" with body:
      """
      { "path": "../../../etc/passwd", "content": "hack" }
      """
    Then the response status code should be 400

  Scenario: Write to disallowed path is rejected with 400
    When I POST to "/mcp/repo/file" with body:
      """
      { "path": "../../etc/passwd", "body": "hack attempt" }
      """
    Then the response status code should be 400

  Scenario: List returns path field and entries array
    When I send a GET request to "/mcp/repo/list"
    Then the response status code should be 200
    And the response body should contain "path"
    And the response body should contain "entries"
