using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace QBrainAi.Support.Mcp.Storage;

/// <summary>
/// FR-MCP-PATHNORM-001 / TR-MCP-PATHNORM-001: persisted workspace keys (every <c>WorkspaceId</c>,
/// <c>OwnerWorkspaceId</c> and entity <c>WorkspacePath</c> column) are stored in the lexical canonical
/// form from <see cref="WorkspaceIdentity.CanonicalizeWorkspaceId"/>. The converter applies on writes
/// and, on relational providers, to query parameters compared with these properties, so a predicate
/// built from a caller's raw path (<c>Q:\ws</c>) matches the stored canonical row (<c>Q:/ws</c>). The
/// comparer makes the change tracker treat both spellings as one key.
/// </summary>
internal static class WorkspaceKeyConversion
{
    private static readonly HashSet<string> PropertyNames = new(StringComparer.Ordinal)
    {
        "WorkspaceId",
        "OwnerWorkspaceId",
        "WorkspacePath",
    };

    private static readonly ValueConverter<string, string> Converter = new(
        value => WorkspaceIdentity.CanonicalizeWorkspaceId(value),
        value => value);

    private static readonly ValueComparer<string> Comparer = new(
        (left, right) => left == null
            ? right == null
            : right != null && WorkspaceIdentity.CanonicalizeWorkspaceId(left) == WorkspaceIdentity.CanonicalizeWorkspaceId(right),
        value => value == null ? 0 : WorkspaceIdentity.CanonicalizeWorkspaceId(value).GetHashCode(StringComparison.Ordinal),
        value => value);

    /// <summary>Applies the canonical converter and comparer to every workspace-key property in the model.</summary>
    internal static void Apply(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetDeclaredProperties())
            {
                if (property.ClrType != typeof(string) || !PropertyNames.Contains(property.Name))
                    continue;

                property.SetValueConverter(Converter);
                property.SetValueComparer(Comparer);
            }
        }
    }

    /// <summary>
    /// Rewrites workspace-key values of added and modified entities to the canonical form in place, so
    /// tracked instances returned by later queries carry the same value as the stored row. The comparer
    /// treats raw and canonical spellings as equal, so an ordinary current-value assignment would be
    /// ignored; the CLR member is written directly instead.
    /// </summary>
    internal static void CanonicalizeTrackedValues(IEnumerable<EntityEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            foreach (var property in entry.Properties)
            {
                if (!PropertyNames.Contains(property.Metadata.Name) || property.CurrentValue is not string current)
                    continue;

                var canonical = WorkspaceIdentity.CanonicalizeWorkspaceId(current);
                if (string.Equals(current, canonical, StringComparison.Ordinal))
                    continue;

                if (property.Metadata.FieldInfo is { } field)
                    field.SetValue(entry.Entity, canonical);
                else if (property.Metadata.PropertyInfo is { CanWrite: true } clrProperty)
                    clrProperty.SetValue(entry.Entity, canonical);
            }
        }
    }
}
