using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Exceptions;
using Kusto.Data.Net.Client;
using Xunit;

namespace Sample.CsvServer.Tests;

/// <summary>
/// Tests that validate error handling in the CSV Server implementation.
/// </summary>
public class ErrorHandlingTests(ServerFixture fixture) : CsvServerTestBase(fixture)
{
    [Fact]
    public void InvalidQuery_ReturnsError()
    {
        // This should throw a Kusto exception for invalid syntax
        var ex = Assert.Throws<KustoBadRequestException>(() =>
        {
            QueryProvider.ExecuteQuery("users | invalidoperator");
        });

        // Verify we get an appropriate error
        ex.Message.Should().Contain("KS176");
    }

    [Fact]
    public void NonexistentTable_ReturnsError()
    {
        // Query a table that doesn't exist
        var ex = Assert.Throws<KustoBadRequestException>(() =>
        {
            QueryProvider.ExecuteQuery("nonexistent_table");
        });

        // Verify we get an appropriate error
        ex.Message.Should().Contain("nonexistent_table");
    }

    [Fact]
    public void InvalidColumnReference_ReturnsError()
    {
        // Reference a column that doesn't exist
        var ex = Assert.Throws<KustoBadRequestException>(() =>
        {
            QueryProvider.ExecuteQuery("users | project nonexistent_column");
        });

        // Verify we get an appropriate error
        ex.Message.Should().Contain("nonexistent_column");
    }
    
    [Fact]
    public void TypeMismatch_HandledGracefully()
    {
        // Try to filter a string column with a numeric comparison
        var ex = Assert.Throws<KustoBadRequestException>(() =>
        {
            QueryProvider.ExecuteQuery("users | where name > 42");
        });
        
        // Should get a type mismatch error
        ex.Message.Should().Contain("KS106");
    }
}