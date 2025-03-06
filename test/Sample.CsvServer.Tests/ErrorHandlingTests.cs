using FluentAssertions;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Exceptions;
using Kusto.Data.Net.Client;
using System.Data;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace BabyKusto.SampleCsvServer.Tests;

/// <summary>
/// Tests that validate error handling in the CSV Server implementation.
/// </summary>
public class ErrorHandlingTests : CsvServerTestBase
{
    private readonly ICslQueryProvider _queryProvider;
    private const string ConnectionString = "Data Source=http://localhost:5220;";

    public ErrorHandlingTests() : base()
    {
        var kcsb = new KustoConnectionStringBuilder(ConnectionString);
        _queryProvider = KustoClientFactory.CreateCslQueryProvider(kcsb);
    }

    [Fact]
    public void InvalidQuery_ReturnsError()
    {
        // This should throw a Kusto exception for invalid syntax
        var ex = Assert.Throws<KustoClientException>(() =>
        {
            _queryProvider.ExecuteQuery("users | invalidoperator");
        });

        // Verify we get an appropriate error
        ex.Message.Should().Contain("invalidoperator");
    }

    [Fact]
    public void NonexistentTable_ReturnsError()
    {
        // Query a table that doesn't exist
        var ex = Assert.Throws<KustoClientException>(() =>
        {
            _queryProvider.ExecuteQuery("nonexistent_table");
        });

        // Verify we get an appropriate error
        ex.Message.Should().Contain("nonexistent_table");
    }

    [Fact]
    public void InvalidColumnReference_ReturnsError()
    {
        // Reference a column that doesn't exist
        var ex = Assert.Throws<KustoClientException>(() =>
        {
            _queryProvider.ExecuteQuery("users | project nonexistent_column");
        });

        // Verify we get an appropriate error
        ex.Message.Should().Contain("nonexistent_column");
    }
    
    [Fact]
    public async Task DirectRestApi_InvalidRequest_Returns400()
    {
        // Send an invalid request directly to the REST API
        var response = await Client.PostAsync("/v2/rest/query", 
            JsonContent.Create(new
            {
                db = "fake",
                csl = "invalid query syntax",
            }));
        
        // Should get a 400 Bad Request
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public void TypeMismatch_HandledGracefully()
    {
        // Try to filter a string column with a numeric comparison
        var ex = Assert.Throws<KustoClientException>(() =>
        {
            _queryProvider.ExecuteQuery("users | where name > 42");
        });
        
        // Should get a type mismatch error
        ex.Message.Should().Contain("name");
    }
}