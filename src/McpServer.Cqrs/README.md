# McpServer.Cqrs

Lightweight async CQRS framework with Result monad, decimal correlation IDs, pipeline behaviors, and ILoggerProvider integration.

## Features

- **ICommand\<T\> / IQuery\<T\>** marker interfaces for command/query separation
- **Result\<T\>** monad for structured success/failure returns
- **CallContext** with correlation ID, auth claims, timing, and ILogger
- **IPipelineBehavior** middleware for cross-cutting concerns
- **Dispatcher** with assembly-scanning DI registration
- **IDispatcher** interface for testability and dependency injection

## Quick Start

```csharp
services.AddCqrs(typeof(MyHandler).Assembly);

// Dispatch
var result = await dispatcher.SendAsync(new CreateItemCommand("name"));
var items = await dispatcher.QueryAsync(new GetItemsQuery());
```
