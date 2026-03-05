Feature: Hybrid Context Search
  As an AI agent
  I want to search indexed content using hybrid FTS5 and vector search
  So that I can retrieve relevant context for my queries

  Background:
    Given the MCP server is running

  # FR-MCP-004 / FR-SUPPORT-010 / TEST-MCP-004 / TR-MCP-DATA-002 / TR-MCP-DATA-003

  Scenario: Context search returns 200 with results
    When I POST to "/mcp/context/search" with body:
      """
      { "query": "test", "limit": 5 }
      """
    Then the response status code should be 200
    And the response body should contain "chunks"

  Scenario: Context search with source type filter restricts results
    When I POST to "/mcp/context/search" with body:
      """
      { "query": "test", "sourceType": "todo", "limit": 10 }
      """
    Then the response status code should be 200

  Scenario: Context search clamps limit to maximum of 100
    When I POST to "/mcp/context/search" with body:
      """
      { "query": "test", "limit": 9999 }
      """
    Then the response status code should be 200

  Scenario: Context search clamps limit to minimum of 1
    When I POST to "/mcp/context/search" with body:
      """
      { "query": "test", "limit": 0 }
      """
    Then the response status code should be 200

  Scenario: Context pack returns ordered chunks with sourceKeys
    When I POST to "/mcp/context/pack" with body:
      """
      { "queryId": "specflow-test-query", "query": "test context", "limit": 3 }
      """
    Then the response status code should be 200
    And the response body should contain "queryId"

  Scenario: Context sources returns indexed document list
    When I send a GET request to "/mcp/context/sources"
    Then the response status code should be 200

  Scenario: Context search empty query still returns 200
    When I POST to "/mcp/context/search" with body:
      """
      { "query": "", "limit": 5 }
      """
    Then the response status code should be 200
