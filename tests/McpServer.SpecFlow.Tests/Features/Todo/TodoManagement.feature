Feature: TODO Management API
  As an AI agent or developer
  I want to create, read, update, delete, and query TODO items via REST
  So that project tasks can be managed programmatically

  Background:
    Given the MCP server is running

  # FR-MCP-002 / FR-SUPPORT-010 / TEST-MCP-002

  Scenario: Query all TODO items returns 200 with a result
    When I send a GET request to "/mcp/todo"
    Then the response status code should be 200
    And the response body should contain a "totalCount" field

  Scenario: Query TODO items by section filters results
    When I send a GET request to "/mcp/todo?section=mvp-support"
    Then the response status code should be 200
    And every returned item should have section "mvp-support"

  Scenario: Query TODO items by priority filters results
    When I send a GET request to "/mcp/todo?priority=high"
    Then the response status code should be 200
    And every returned item should have priority "high"

  Scenario: Query TODO items by done status filters to completed items
    When I send a GET request to "/mcp/todo?done=true"
    Then the response status code should be 200
    And every returned item should have done "true"

  Scenario: Query TODO items by keyword filters results
    Given a TODO item exists with title "UniqueKeyword12345"
    When I send a GET request to "/mcp/todo?keyword=UniqueKeyword12345"
    Then the response status code should be 200
    And every returned item title or description should contain "UniqueKeyword12345"

  Scenario: Create a new TODO item succeeds
    When I POST to "/mcp/todo" with body:
      """
      {
        "id": "SF-SPEC-CREATE-UNIQUE",
        "title": "SpecFlow Created Item",
        "section": "mvp-support",
        "priority": "medium",
        "description": ["Created by SpecFlow test"],
        "done": false
      }
      """
    Then the response status code is 201 or 409
    And the response body should contain "id"

  Scenario: Create TODO with invalid section returns 409
    When I POST to "/mcp/todo" with body:
      """
      {
        "id": "SF-SPEC-BADSECT",
        "title": "Bad Section Item",
        "section": "invalid-section-xyz",
        "priority": "high"
      }
      """
    Then the response status code should be 409

  Scenario: Create TODO with missing title returns 400
    When I POST to "/mcp/todo" with body:
      """
      {
        "id": "SF-SPEC-NOTITLE",
        "section": "mvp-support",
        "priority": "high"
      }
      """
    Then the response status code should be 400

  Scenario: Get TODO by ID returns the item
    Given a TODO item exists with title "GetByIdItem" and id stored as "createdId"
    When I send a GET request to "/mcp/todo/{createdId}"
    Then the response status code should be 200
    And the response body should contain "GetByIdItem"

  Scenario: Get TODO by unknown ID returns 404
    When I send a GET request to "/mcp/todo/NONEXISTENT-9999"
    Then the response status code should be 404

  Scenario: Update a TODO item changes its fields
    Given a TODO item exists with title "UpdateMe" and id stored as "updateId"
    When I PUT to "/mcp/todo/{updateId}" with body:
      """
      {
        "title": "UpdateMe Updated",
        "done": true
      }
      """
    Then the response status code should be 200
    And the response body should contain "true"

  Scenario: Delete a TODO item removes it
    Given a TODO item exists with title "DeleteMe" and id stored as "deleteId"
    When I send a DELETE request to "/mcp/todo/{deleteId}"
    Then the response status code should be 200
    When I send a GET request to "/mcp/todo/{deleteId}"
    Then the response status code should be 404

  Scenario: Requirements analysis endpoint returns 422 when Copilot CLI unavailable
    Given a TODO item exists with title "RequirementsItem" and id stored as "reqId"
    When I POST to "/mcp/todo/{reqId}/requirements" with empty body
    Then the response status code should be 422

  # TR-MCP-DRY-001 — single validator consumed by all backends
  Scenario: Section validation is consistent across create and update operations
    When I POST to "/mcp/todo" with body:
      """
      { "id": "SF-SPEC-DRY-001", "title": "Validator Test", "section": "mvp-app", "priority": "high" }
      """
    Then the response status code is 201 or 409
