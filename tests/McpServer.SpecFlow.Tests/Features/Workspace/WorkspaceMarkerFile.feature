Feature: Workspace Marker File Agent Discovery
  As an AI agent
  I want a marker file written to a workspace root when the host starts
  So that I can discover the correct server port and endpoints automatically

  Background:
    Given the MCP server is running

  # FR-MCP-018 / TR-MCP-WS-005 / TEST-MCP-013

  Scenario: Workspace start writes AGENTS-README-FIRST.yaml to workspace root
    Given I use a unique temp directory stored as "markerWsPath"
    And a workspace exists for path "{markerWsPath}" with key stored as "markerWsKey"
    When I POST to "/mcp/workspace/{markerWsKey}/start" with empty body
    Then the response status code should be 200
    And the file "AGENTS-README-FIRST.yaml" should exist in "{markerWsPath}"
    And I stop the workspace "{markerWsKey}"
    And I delete the created workspace using the path "{markerWsPath}"

  Scenario: Workspace stop removes the marker file
    Given I use a unique temp directory stored as "stopMarkerPath"
    And a workspace exists for path "{stopMarkerPath}" with key stored as "stopMarkerKey"
    And the workspace "{stopMarkerKey}" is started
    When I POST to "/mcp/workspace/{stopMarkerKey}/stop" with empty body
    Then the response status code should be 200
    And the file "AGENTS-README-FIRST.yaml" should not exist in "{stopMarkerPath}"
    And I delete the created workspace using the path "{stopMarkerPath}"

  Scenario: Marker file contains the correct port number
    Given I use a unique temp directory stored as "portMarkerPath"
    And a workspace exists for path "{portMarkerPath}" with key stored as "portMarkerKey" and port stored as "wsPort"
    And the workspace "{portMarkerKey}" is started
    Then the file "AGENTS-README-FIRST.yaml" in "{portMarkerPath}" should contain "{wsPort}"
    And I stop the workspace "{portMarkerKey}"
    And I delete the created workspace using the path "{portMarkerPath}"

  Scenario: Marker file contains baseUrl field
    Given I use a unique temp directory stored as "baseUrlMarkerPath"
    And a workspace exists for path "{baseUrlMarkerPath}" with key stored as "baseUrlMarkerKey"
    And the workspace "{baseUrlMarkerKey}" is started
    Then the file "AGENTS-README-FIRST.yaml" in "{baseUrlMarkerPath}" should contain "baseUrl"
    And I stop the workspace "{baseUrlMarkerKey}"
    And I delete the created workspace using the path "{baseUrlMarkerPath}"

  Scenario: Marker file contains endpoint paths
    Given I use a unique temp directory stored as "endpointMarkerPath"
    And a workspace exists for path "{endpointMarkerPath}" with key stored as "endpointMarkerKey"
    And the workspace "{endpointMarkerKey}" is started
    Then the file "AGENTS-README-FIRST.yaml" in "{endpointMarkerPath}" should contain "mcp/todo"
    And I stop the workspace "{endpointMarkerKey}"
    And I delete the created workspace using the path "{endpointMarkerPath}"

  Scenario: Marker file contains machine-readable prompt block
    Given I use a unique temp directory stored as "promptMarkerPath"
    And a workspace exists for path "{promptMarkerPath}" with key stored as "promptMarkerKey"
    And the workspace "{promptMarkerKey}" is started
    Then the file "AGENTS-README-FIRST.yaml" in "{promptMarkerPath}" should contain "prompt"
    And I stop the workspace "{promptMarkerKey}"
    And I delete the created workspace using the path "{promptMarkerPath}"

  Scenario: Legacy .mcp-server.yaml files are cleaned up on workspace stop
    Given I use a unique temp directory stored as "legacyMarkerPath"
    And a legacy ".mcp-server.yaml" file exists in "{legacyMarkerPath}"
    And a workspace exists for path "{legacyMarkerPath}" with key stored as "legacyMarkerKey"
    When I POST to "/mcp/workspace/{legacyMarkerKey}/stop" with empty body
    Then the response status code should be 200
    And the file ".mcp-server.yaml" should not exist in "{legacyMarkerPath}"
    And I delete the created workspace using the path "{legacyMarkerPath}"
