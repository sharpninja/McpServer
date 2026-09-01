using McpServer.Support.Mcp.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-HANDOFF-005: unique-violation detection uses provider exception codes/types,
/// not English message fragments.
/// </summary>
public sealed class HandoffDbExceptionsTests
{
    /// <summary>SQLite unique constraint is recognized from the extended error code.</summary>
    [Fact]
    public void IsUniqueViolation_SqliteExtendedUniqueCode_IsTrue()
    {
        var inner = new SqliteException(string.Empty, 19, 2067);
        var ex = new DbUpdateException("failed", inner);
        Assert.True(HandoffDbExceptions.IsUniqueViolation(ex));
    }

    /// <summary>PostgreSQL unique_violation is recognized from SqlState 23505 without English text.</summary>
    [Fact]
    public void IsUniqueViolation_PostgresSqlState23505_IsTrue()
    {
        var inner = new PostgresException(string.Empty, "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation);
        var ex = new DbUpdateException("failed", inner);
        Assert.True(HandoffDbExceptions.IsUniqueViolation(ex));
    }

    /// <summary>SQL Server unique/index violations are recognized from numbers 2601 and 2627.</summary>
    [Theory]
    [InlineData(2601)]
    [InlineData(2627)]
    public void IsUniqueViolation_SqlServerNumbers_AreTrue(int number)
    {
        var inner = SqlExceptionFactory.Create(number);
        var ex = new DbUpdateException("failed", inner);
        Assert.True(HandoffDbExceptions.IsUniqueViolation(ex));
    }

    /// <summary>Unrelated exceptions are not treated as unique violations even when the message says unique.</summary>
    [Fact]
    public void IsUniqueViolation_EnglishMessageWithoutProviderCode_IsFalse()
    {
        var ex = new DbUpdateException("UNIQUE constraint failed", new InvalidOperationException("unique key"));
        Assert.False(HandoffDbExceptions.IsUniqueViolation(ex));
    }

    /// <summary>P2-5: SQL Server timeout number -2 is commit-ambiguous without English matching.</summary>
    [Fact]
    public void IsCommitAmbiguous_SqlServerTimeoutNumber_IsTrue()
    {
        Assert.True(HandoffDbExceptions.IsCommitAmbiguous(new DbUpdateException(string.Empty, SqlExceptionFactory.Create(-2))));
    }

    /// <summary>P2-5: PostgreSQL 40001 is commit-ambiguous from SQLSTATE only.</summary>
    [Fact]
    public void IsCommitAmbiguous_PostgresSqlState40001_IsTrue()
    {
        var inner = new PostgresException(string.Empty, "ERROR", "ERROR", "40001");
        Assert.True(HandoffDbExceptions.IsCommitAmbiguous(new DbUpdateException(string.Empty, inner)));
    }

    /// <summary>P2-5: SQLite locked (5) is commit-ambiguous from the numeric code.</summary>
    [Fact]
    public void IsCommitAmbiguous_SqliteBusyCode_IsTrue()
    {
        Assert.True(HandoffDbExceptions.IsCommitAmbiguous(new SqliteException(string.Empty, 5)));
    }

    /// <summary>P2-5: an English timeout sentence without a provider code is not sufficient.</summary>
    [Fact]
    public void IsCommitAmbiguous_EnglishMessageOnly_IsFalse()
    {
        Assert.False(HandoffDbExceptions.IsCommitAmbiguous(new DbUpdateException("The timeout period elapsed", new InvalidOperationException("connection timed out"))));
    }

    /// <summary>P2-5: a typed TimeoutException is conservatively ambiguous.</summary>
    [Fact]
    public void IsCommitAmbiguous_TimeoutException_IsTrue()
    {
        Assert.True(HandoffDbExceptions.IsCommitAmbiguous(new TimeoutException()));
    }
}
