# TEST-MCP-PRODUCT

[Back to Testing Requirements](Testing-Requirements)

## TEST-MCP-PRODUCT

### TEST-MCP-PRODUCT-001

Unit tests for ProductEntity/membership CQRS handlers: isolation, PROD-* key accept (PROD-MCPSERVER) and reject (mcpserver, empty, missing prefix), unique key, soft-delete, owner add/remove, self-leave, unknown workspace rejected. Files: ProductEntityTests, CreateProductCommandHandlerTests, AddProductMemberCommandHandlerTests.


### TEST-MCP-PRODUCT-002

Unit tests for product effective-requirements share: union, productScope=local, id collision two origins, origin layer miss excludes, leave drops sibling, outsider cannot share. File: GetProductEffectiveRequirementsQueryHandlerTests.


### TEST-MCP-PRODUCT-003

Controller/API unit tests: 400 invalid key, 409 duplicate, 403 non-owner, 404 outsider get. File: ProductsControllerTests.


### TEST-MCP-PRODUCT-004

Client and MCP/REPL contract tests prove adapters dispatch CQRS only. File: ProductClientTests plus MCP/REPL allow-list tests.


### TEST-MCP-PRODUCT-005

Migration apply on empty and production-shaped DBs for SQLite, PostgreSQL, and SQL Server harnesses. Must not re-add unrelated SessionLogs agent-header columns. File: ProductMigrationApplyTests.


### TEST-MCP-PRODUCT-006

Context pack/search: member includes sibling FR body with origin; does not include sibling .cs chunks; source type product-requirements filters to those chunks. File: ProductRequirementContextTests.
