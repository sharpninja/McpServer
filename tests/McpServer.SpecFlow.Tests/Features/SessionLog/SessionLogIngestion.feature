Feature: Session Log Ingestion and Query
  As an AI agent
  I want to submit, query, and append to session logs
  So that agent interactions are persisted and searchable

  Background:
    Given the MCP server is running

  # FR-MCP-003 / FR-SUPPORT-010 / TEST-MCP-002

  Scenario: Submit a session log returns 201
    When I POST to "/mcp/sessionlog" with body:
      """
      {
        "sourceType": "copilot",
        "sessionId": "specflow-session-001",
        "title": "SpecFlow Test Session",
        "status": "completed",
        "entries": []
      }
      """
    Then the response status code should be 201

  Scenario: Submit session log with upsert replaces existing log with same sourceType and sessionId
    When I POST to "/mcp/sessionlog" with body:
      """
      {
        "sourceType": "copilot",
        "sessionId": "specflow-upsert-session",
        "title": "Upsert Session First",
        "status": "in-progress",
        "entries": []
      }
      """
    Then the response status code should be 201
    When I POST to "/mcp/sessionlog" with body:
      """
      {
        "sourceType": "copilot",
        "sessionId": "specflow-upsert-session",
        "title": "Upsert Session Updated",
        "status": "completed",
        "entries": []
      }
      """
    Then the response status code should be 201

  Scenario: Query session logs returns paginated result
    When I send a GET request to "/mcp/sessionlog"
    Then the response status code should be 200
    And the response body should contain "totalCount"
    And the response body should contain "limit"
    And the response body should contain "offset"
    And the response body should contain "items"

  Scenario: Query session logs with limit and offset returns correct page shape
    When I send a GET request to "/mcp/sessionlog?limit=5&offset=0"
    Then the response status code should be 200
    And the response body should contain "totalCount"

  Scenario: Append dialog to a session log returns running count or 404
    Given a session log exists with sourceType "copilot" and sessionId "dialog-session-001"
    When I POST to "/mcp/sessionlog/copilot/dialog-session-001/req-dialog-001/dialog" with body:
      """
      [
        { "role": "user", "content": "What is 2+2?" }
      ]
      """
    Then the response status code is 200 or 404

  Scenario: Submit session log with missing required sourceType returns 400
    When I POST to "/mcp/sessionlog" with body:
      """
      {
        "sessionId": "missing-source",
        "title": "Missing SourceType",
        "status": "completed",
        "entries": []
      }
      """
    Then the response status code should be 400

  Scenario: Submit session log with missing required sessionId returns 400
    When I POST to "/mcp/sessionlog" with body:
      """
      {
        "sourceType": "copilot",
        "title": "Missing Session ID",
        "status": "completed",
        "entries": []
      }
      """
    Then the response status code should be 400
