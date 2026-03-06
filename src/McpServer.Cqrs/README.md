# SharpNinja.McpServer.Cqrs

Lightweight async CQRS primitives used across the McpServer solution.

## Includes

- `Dispatcher` orchestration for commands and queries
- `Result<T>` success/failure wrapper patterns
- `CallContext` correlation and pipeline context propagation
- `IPipelineBehavior` extension points for cross-cutting concerns

This package is consumed by `SharpNinja.McpServer.Cqrs.Mvvm`, `SharpNinja.McpServer.UI.Core`, and host applications.
