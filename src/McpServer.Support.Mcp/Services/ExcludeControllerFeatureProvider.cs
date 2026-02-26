using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Removes specified controller types from the MVC feature so they are not discovered.
/// Used to exclude primary-only controllers (e.g. DiagnosticController) from production builds.
/// </summary>
internal sealed class ExcludeControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly HashSet<TypeInfo> _excluded;

    /// <summary>Initializes a new instance of the <see cref="ExcludeControllerFeatureProvider"/> class.</summary>
    public ExcludeControllerFeatureProvider(params Type[] excludedControllers)
    {
        _excluded = new HashSet<TypeInfo>(excludedControllers.Select(t => t.GetTypeInfo()));
    }

    /// <inheritdoc />
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach (var type in _excluded)
            feature.Controllers.Remove(type);
    }
}
