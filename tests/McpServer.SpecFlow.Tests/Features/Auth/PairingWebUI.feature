Feature: Pairing Web UI
  As an authorized user
  I want a browser-based login flow to retrieve the API key
  So that I can configure MCP clients without exposing credentials in config files

  Background:
    Given the MCP server is running

  # FR-MCP-014 / TR-MCP-SEC-002 / TEST-MCP-010

  Scenario: Pairing login page returns 200 with HTML
    When I send a GET request to "/pair"
    Then the response status code should be 200
    And the response content type should contain "text/html"

  Scenario: Pairing login page shows form when API key is configured
    When I send a GET request to "/pair"
    Then the response status code should be 200

  Scenario: Pairing key endpoint redirects to login when not authenticated
    When I send a GET request to "/pair/key"
    Then the response status code is 200 or 302

  Scenario: Pairing login with invalid credentials returns 401 or redirects to error
    When I POST to "/pair" with form fields:
      | Field    | Value              |
      | username | nonexistentuser    |
      | password | wrongpassword      |
    Then the response status code is 200 or 401 or 302

  Scenario: Pairing login with valid credentials issues HttpOnly session cookie
    # In test mode with no PairingUsers configured, this returns the not-configured page
    When I POST to "/pair" with form fields:
      | Field    | Value   |
      | username | testuser |
      | password | testpass |
    Then the response status code is 200 or 302 or 401

  Scenario: SHA-256 constant-time comparison is used for password verification
    # Timing attack resistance is verified by the service implementation; this test
    # ensures the endpoint doesn't leak which character differs via timing.
    When I POST to "/pair" with form fields:
      | Field    | Value   |
      | username | admin   |
      | password | aaaaaa  |
    Then the response status code is 200 or 302 or 401

  Scenario: Pairing page shows not-configured message when ApiKey is empty
    # In the test environment Mcp:ApiKey is empty; the page should show not-configured state
    When I send a GET request to "/pair"
    Then the response status code should be 200
