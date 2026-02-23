Feature: Markdown Session Log Ingestion
  As an AI agent
  I want legacy Markdown session log files to be parsed into the unified schema
  So that pre-existing agent records are retroactively indexed

  # FR-MCP-024 / TR-MCP-INGEST-002 / TEST-MCP-015

  Scenario: TryParse recognizes a Markdown file with a standard session log header
    Given a Markdown file with header "# Session Log – Specflow Test Session"
    And the file contains section "## Session Overview" with content "**Status:** completed"
    And the file contains section "## Model" with content "**Model:** claude-3-opus"
    When I call MarkdownSessionLogParser.TryParse on the file
    Then the result should not be null
    And the result title should be "Specflow Test Session"
    And the result should contain at least one entry

  Scenario: TryParse recognizes a Markdown file with the Copilot variant header
    Given a Markdown file with header "# Copilot Session Log – Another Test"
    And the file contains section "## Session Overview" with content "**Status:** in-progress"
    When I call MarkdownSessionLogParser.TryParse on the file
    Then the result should not be null
    And the result title should be "Another Test"

  Scenario: TryParse returns null for files without the session log header
    Given a Markdown file with header "# Regular Markdown File"
    And the file contains section "## Some Section" with content "Some content"
    When I call MarkdownSessionLogParser.TryParse on the file
    Then the result should be null

  Scenario: TryParse extracts individual Request subsections as separate entries
    Given a Markdown file with header "# Session Log – Multi-Request Session"
    And the file contains a "### Request" subsection with prompt "First request prompt"
    And the file contains a "### Request" subsection with prompt "Second request prompt"
    When I call MarkdownSessionLogParser.TryParse on the file
    Then the result should not be null
    And the result should contain at least 2 entries

  Scenario: NormalizeToStructuredText produces non-empty plain text representation
    Given a Markdown file with header "# Session Log – Normalize Test"
    And the file contains section "## Changes Made" with content "Added feature X"
    When I call MarkdownSessionLogParser.TryParse on the file
    And I call NormalizeToStructuredText on the result
    Then the normalized text should not be empty
    And the normalized text should contain "Normalize Test"

  Scenario: TryParse extracts model field from Model section
    Given a Markdown file with header "# Session Log – Model Extraction"
    And the file contains section "## Model" with content "**Model:** gpt-4o"
    When I call MarkdownSessionLogParser.TryParse on the file
    Then the result should not be null
    And the result model should be "gpt-4o"

  Scenario: TryParse extracts status from Session Overview section
    Given a Markdown file with header "# Session Log – Status Extraction"
    And the file contains section "## Session Overview" with content "**Status:** failed"
    When I call MarkdownSessionLogParser.TryParse on the file
    Then the result should not be null
    And the result status should be "failed"
