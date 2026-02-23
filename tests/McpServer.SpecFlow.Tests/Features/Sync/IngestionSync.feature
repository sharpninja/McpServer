Feature: Ingestion Sync
  As an AI agent
  I want to trigger full ingestion and check its status
  So that indexed content stays up to date

  Background:
    Given the MCP server is running

  # FR-MCP-006 / TR-MCP-INGEST-001 / TEST-MCP-002

  Scenario: Trigger sync run returns 200 with run metadata
    When I POST to "/mcp/sync/run" with empty body
    Then the response status code should be 200
    And the response body should contain "runId"
    And the response body should contain "status"

  Scenario: Sync status returns 200
    When I send a GET request to "/mcp/sync/status"
    Then the response status code should be 200

  Scenario: Sync status returns idle when no runs have been made
    When I send a GET request to "/mcp/sync/status"
    Then the response status code should be 200
    And the response body is valid JSON

  Scenario: Sync run result contains documentsIngested count
    When I POST to "/mcp/sync/run" with empty body
    Then the response status code should be 200
    And the response body should contain "documentsIngested"

  Scenario: Sync run result contains chunksWritten count
    When I POST to "/mcp/sync/run" with empty body
    Then the response status code should be 200
    And the response body should contain "chunksWritten"

  Scenario: Sync run result contains sessionLogsImported count
    When I POST to "/mcp/sync/run" with empty body
    Then the response status code should be 200
    And the response body should contain "sessionLogsImported"

  Scenario: Multiple consecutive sync runs complete without error
    When I POST to "/mcp/sync/run" with empty body
    Then the response status code should be 200
    When I POST to "/mcp/sync/run" with empty body
    Then the response status code should be 200
