using System;
using System.Linq;
using System.Reflection;
using Xunit;

namespace QBrainAi.Client.Tests;

/// <summary>
/// Binary compatibility: pre-envelope constructor overloads must remain as distinct CLR signatures
/// so already-compiled consumers do not hit MissingMethodException.
/// </summary>
public sealed class McpClientExceptionCtorCompatTests
{
    [Fact]
    public void LegacyOverloads_ExistAsDistinctClrSignatures()
    {
        AssertHasCtor(typeof(McpClientException), [typeof(string), typeof(int)]);
        AssertHasCtor(typeof(McpClientException), [typeof(string), typeof(int), typeof(Exception)]);
        AssertHasCtor(typeof(McpClientException), [typeof(string), typeof(int), typeof(string), typeof(bool?)]);
        AssertHasCtor(typeof(McpClientException), [typeof(string), typeof(int), typeof(Exception), typeof(string), typeof(bool?)]);

        AssertHasCtor(typeof(McpValidationException), [typeof(string)]);
        AssertHasCtor(typeof(McpValidationException), [typeof(string), typeof(string), typeof(bool?)]);
        AssertHasCtor(typeof(McpUnauthorizedException), [typeof(string)]);
        AssertHasCtor(typeof(McpUnauthorizedException), [typeof(string), typeof(string), typeof(bool?)]);
        AssertHasCtor(typeof(McpNotFoundException), [typeof(string)]);
        AssertHasCtor(typeof(McpNotFoundException), [typeof(string), typeof(string), typeof(bool?)]);
        AssertHasCtor(typeof(McpConflictException), [typeof(string)]);
        AssertHasCtor(typeof(McpConflictException), [typeof(string), typeof(string), typeof(bool?)]);
        AssertHasCtor(typeof(QBrainAiException), [typeof(string), typeof(int)]);
        AssertHasCtor(typeof(QBrainAiException), [typeof(string), typeof(int), typeof(string), typeof(bool?)]);
    }

    [Fact]
    public void LegacyOneArgValidationCtor_LeavesEnvelopeMetadataNull()
    {
        var ex = new McpValidationException("bad");
        Assert.Equal(400, ex.StatusCode);
        Assert.Null(ex.ErrorCode);
        Assert.Null(ex.Retryable);
    }

    [Fact]
    public void MetadataCtor_PreservesEnvelopeFields()
    {
        var ex = new McpValidationException("bad", "validation_error", retryable: false);
        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("validation_error", ex.ErrorCode);
        Assert.False(ex.Retryable);
    }

    private static void AssertHasCtor(Type type, Type[] parameterTypes)
    {
        var ctor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public,
            binder: null,
            types: parameterTypes,
            modifiers: null);
        Assert.True(ctor is not null, $"{type.Name}({string.Join(", ", parameterTypes.Select(t => t.Name))}) missing");
    }
}