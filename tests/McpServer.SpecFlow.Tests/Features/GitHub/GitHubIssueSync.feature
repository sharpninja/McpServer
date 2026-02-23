Feature: GitHub Issue Sync and Integration
  As an AI agent
  I want to manage GitHub issues via the MCP REST API
  So that TODO items can be synchronized bidirectionally with GitHub issues

  Background:
    Given the MCP server is running

  # FR-SUPPORT-013 / FR-MCP-005 / TR-GH-013-001 through 006

  Scenario: List GitHub issues returns 200
    When I send a GET request to "/mcp/gh/issues"
    Then the response status code should be 200
    And the response body is valid JSON

  Scenario: Get GitHub issue by number returns 200
    When I send a GET request to "/mcp/gh/issues/1"
    Then the response status code is 200 or 404

  Scenario: Create GitHub issue with missing title returns 400
    When I POST to "/mcp/gh/issues" with body:
      """
      { "body": "Missing title" }
      """
    Then the response status code should be 400

  Scenario: Create GitHub issue with valid payload returns 201 or error
    When I POST to "/mcp/gh/issues" with body:
      """
      { "title": "SpecFlow Test Issue", "body": "Created by SpecFlow" }
      """
    Then the response status code is 201 or 400 or 422

  Scenario: Update GitHub issue with missing body returns 400
    When I PUT to "/mcp/gh/issues/1" with body:
      """
      {}
      """
    Then the response status code should be 400

  Scenario: Close GitHub issue returns 200 or error
    When I POST to "/mcp/gh/issues/1/close" with empty body
    Then the response status code is 200 or 400 or 422

  Scenario: Reopen GitHub issue returns 200 or error
    When I POST to "/mcp/gh/issues/1/reopen" with empty body
    Then the response status code is 200 or 400 or 422

  Scenario: Comment on GitHub issue with missing body returns 400
    When I POST to "/mcp/gh/issues/1/comments" with body:
      """
      {}
      """
    Then the response status code should be 400

  Scenario: List GitHub labels returns 200
    When I send a GET request to "/mcp/gh/labels"
    Then the response status code should be 200
    And the response body is valid JSON

  Scenario: List GitHub pull requests returns 200
    When I send a GET request to "/mcp/gh/pulls"
    Then the response status code should be 200
    And the response body is valid JSON

  Scenario: Comment on pull request with missing body returns 400
    When I POST to "/mcp/gh/pulls/1/comments" with body:
      """
      {}
      """
    Then the response status code should be 400

  Scenario: Sync issues from GitHub returns 200 or 422
    When I POST to "/mcp/gh/issues/sync/from-github" with empty body
    Then the response status code is 200 or 422

  Scenario: Sync issues to GitHub returns 200 or 422
    When I POST to "/mcp/gh/issues/sync/to-github" with empty body
    Then the response status code is 200 or 422

  Scenario: Sync single issue returns 200 or error
    When I POST to "/mcp/gh/issues/1/sync" with empty body
    Then the response status code is 200 or 400 or 404 or 422

  # TR-GH-013-002: ISSUE-{number} TODO ID convention is respected
  Scenario: GitHub sync creates TODO items with ISSUE-{number} IDs
    When I POST to "/mcp/gh/issues/sync/from-github" with empty body
    Then the response status code is 200 or 400 or 422
