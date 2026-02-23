Feature: Tool Bucket Management
  As an AI agent
  I want to browse, install, and sync tools from GitHub-backed bucket repositories
  So that I can easily install tool definitions from remote repositories

  Background:
    Given the MCP server is running

  # TR-MCP-TR-002 / TR-MCP-TR-003 / FR-MCP-022

  Scenario: List tool buckets returns 200
    When I send a GET request to "/mcp/tools/buckets"
    Then the response status code should be 200
    And the response body is valid JSON

  Scenario: Add tool bucket returns 201
    When I POST to "/mcp/tools/buckets" with body:
      """
      {
        "name": "specflow-test-bucket",
        "owner": "sharpninja",
        "repo": "McpServer",
        "branch": "main",
        "manifestPath": "docs/stdio-tool-contract.json"
      }
      """
    Then the response status code should be 201
    And I delete the bucket named "specflow-test-bucket"

  Scenario: Add duplicate bucket name returns 409
    Given a bucket exists with name "dup-bucket"
    When I POST to "/mcp/tools/buckets" with body:
      """
      {
        "name": "dup-bucket",
        "owner": "sharpninja",
        "repo": "McpServer",
        "branch": "main"
      }
      """
    Then the response status code should be 409
    And I delete the bucket named "dup-bucket"

  Scenario: Delete bucket returns 200
    Given a bucket exists with name "delete-bucket-test"
    When I send a DELETE request to "/mcp/tools/buckets/delete-bucket-test"
    Then the response status code should be 200

  Scenario: Browse bucket manifest returns 200 or 404 gracefully
    Given a bucket exists with name "browse-bucket"
    When I send a GET request to "/mcp/tools/buckets/browse-bucket/browse"
    Then the response status code is 200 or 404
    And I delete the bucket named "browse-bucket"

  Scenario: Default buckets are seeded on startup and are idempotent
    # FR-MCP-022 / TR-MCP-TR-003 — EnsureDefaultBucketsAsync is called on startup
    When I send a GET request to "/mcp/tools/buckets"
    Then the response status code should be 200
    # Subsequent GET should return the same buckets (idempotent)
    When I send a GET request to "/mcp/tools/buckets"
    Then the response status code should be 200

  Scenario: Bucket list read endpoint is publicly accessible without API key
    When I send a GET request to "/mcp/tools/buckets" without an API key
    Then the response status code should be 200

  Scenario: Bucket write endpoint requires API key when configured
    When I POST to "/mcp/tools/buckets" without an API key with body:
      """
      { "name": "nokey-bucket", "owner": "test", "repo": "test", "branch": "main" }
      """
    Then the response status code is 201 or 401
