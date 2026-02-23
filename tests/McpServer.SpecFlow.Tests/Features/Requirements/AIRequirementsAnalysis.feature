Feature: AI-Assisted Requirements Analysis
  As a developer
  I want the server to invoke the Copilot CLI to analyze a TODO item
  So that FR/TR requirement IDs can be assigned automatically

  Background:
    Given the MCP server is running

  # FR-MCP-023 / TR-MCP-REQ-001 / TEST-MCP-014

  Scenario: Requirements endpoint returns 422 when Copilot CLI is unavailable
    Given a TODO item exists with title "AnalysisItem" and id stored as "analysisId"
    When I POST to "/mcp/todo/{analysisId}/requirements" with empty body
    Then the response status code should be 422

  Scenario: RequirementsService extracts JSON-block format requirement IDs
    Given a Copilot response containing:
      """
      {"functionalRequirements": ["FR-MCP-001"], "technicalRequirements": ["TR-MCP-ARCH-001"]}
      """
    When I extract requirement IDs from the JSON block response
    Then the extracted IDs should contain "FR-MCP-001"
    And the extracted IDs should contain "TR-MCP-ARCH-001"
    And the extracted IDs should be distinct

  Scenario: RequirementsService falls back to regex extraction when no JSON block present
    Given a Copilot response containing:
      """
      Based on the analysis, the following requirements apply:
      FR-MCP-002 covers the TODO API.
      TR-MCP-001 covers persistence.
      """
    When I extract requirement IDs using regex fallback
    Then the extracted IDs should contain "FR-MCP-002"
    And the extracted IDs should contain "TR-MCP-001"
    And the extracted IDs should be distinct

  Scenario: RequirementsService returns empty list for response with no IDs
    Given a Copilot response containing:
      """
      No matching requirements were found for this item.
      """
    When I extract requirement IDs using regex fallback
    Then the extracted IDs should be empty

  Scenario: RequirementsService matches FR pattern FR-[A-Z]+-\d{3}
    Given a Copilot response containing:
      """
      FR-SUPPORT-010 is relevant. FR-MCP-009 also applies.
      """
    When I extract requirement IDs using regex fallback
    Then the extracted IDs should contain "FR-SUPPORT-010"
    And the extracted IDs should contain "FR-MCP-009"

  Scenario: RequirementsService matches TR pattern TR-[A-Z]+-\d{3}
    Given a Copilot response containing:
      """
      TR-GH-013 and TR-MCP-002 are the technical requirements.
      """
    When I extract requirement IDs using regex fallback
    Then the extracted IDs should contain "TR-GH-013"
    And the extracted IDs should contain "TR-MCP-002"

  Scenario: Discovered IDs are merged without duplicates
    Given a Copilot response containing:
      """
      {"functionalRequirements": ["FR-MCP-001", "FR-MCP-002"], "technicalRequirements": []}
      """
    When I extract requirement IDs from the JSON block response
    Then the extracted IDs should contain "FR-MCP-001"
    And the extracted IDs should contain "FR-MCP-002"
    And the extracted IDs should be distinct
