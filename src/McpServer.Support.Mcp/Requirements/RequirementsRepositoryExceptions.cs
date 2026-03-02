namespace McpServer.Support.Mcp.Requirements;

internal abstract class RequirementsRepositoryException : InvalidOperationException
{
    protected RequirementsRepositoryException(string message)
        : base(message)
    {
    }
}

internal sealed class RequirementsConflictException : RequirementsRepositoryException
{
    public RequirementsConflictException(string message)
        : base(message)
    {
    }
}

internal sealed class RequirementsNotFoundException : RequirementsRepositoryException
{
    public RequirementsNotFoundException(string message)
        : base(message)
    {
    }
}
