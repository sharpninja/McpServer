# TEST-MCP-REQEXPORT

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-REQEXPORT

### TEST-MCP-REQEXPORT-002

Validates TR-MCP-REQEXPORT-002. tests/McpServer.Support.Mcp.Tests/Controllers/RequirementsControllerGenerateTests.cs: GenerateAsync_WikiUnlistedException_ReturnsStructuredErrorNamingExceptionType substitutes IRequirementsDocumentService.GenerateWikiAsync to throw KeyNotFoundException and asserts the action returns an ObjectResult status 500 whose body contains the message and KeyNotFoundException (exceptionType). Red before the catch-all (the exception escaped/threw), green after adding catch(Exception)->BuildGenerateExportError to both wiki try blocks. Existing GenerateAsync_WikiConfigFailure/WikiConflictFailure/WikiZipAssemblyFailure tests remain green (AC3).


### TEST-MCP-REQEXPORT-003

Verifies generateDocument accepts format=markdown for docType=matrix (and other non-wiki docTypes) without a format rejection at the schema, validator, and workflow layers. Validates TR-MCP-REQEXPORT-003 / BUG-TRIAGE-074.
