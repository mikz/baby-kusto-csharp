using System.Net.Http;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Sample.CsvServer.Tests;

/// <summary>
/// Tests that demonstrate the health check implementation used by Testcontainers
/// </summary>
public class ServerHealthCheckTests(ServerFixture fixture) : CsvServerTestBase(fixture), IAsyncLifetime
{
    private HttpClient _httpClient = null!;
    private string _baseUrl = null!;

    public new Task InitializeAsync()
    {
        _httpClient = new HttpClient();
        _baseUrl = Fixture.ServerUrl!;
        return Task.CompletedTask;
    }


    [Fact]
    public async Task HealthCheck_EndpointResponds()
    {
        // This is the same request that Testcontainers uses to check server health
        using var content = new StringContent(
            "{ \"csl\": \".show databases\" }",
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.PostAsync($"{_baseUrl}/v1/rest/mgmt", content);

        // Ensure the request succeeds as expected
        response.EnsureSuccessStatusCode(); 
        
        // Read response content
        var responseBody = await response.Content.ReadAsStringAsync();
        
        // Verify response contains expected data
        var responseJson = JsonDocument.Parse(responseBody);
        responseJson.RootElement.GetProperty("Tables").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task HealthCheck_ReturnsDatabase()
    {
        // Create a request with the same body that's used in the wait strategy
        using var content = new StringContent(
            "{ \"csl\": \".show databases\" }",
            Encoding.UTF8,
            "application/json");

        // Send the request to the mgmt endpoint
        using var response = await _httpClient.PostAsync($"{_baseUrl}/v1/rest/mgmt", content);
        
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        
        // Parse and verify the response contains a database with name "BabyKusto"
        var responseJson = JsonDocument.Parse(responseBody);
        var tables = responseJson.RootElement.GetProperty("Tables");
        var rows = tables[0].GetProperty("Rows");
        
        // Should have at least one row in the response
        rows.GetArrayLength().Should().BeGreaterThan(0);
        
        // First row, first column should contain the database name "BabyKusto"
        var dbName = rows[0][0].GetString();
        dbName.Should().Be("BabyKusto");
    }
}