using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Sample.CsvServer.Tests;

public class DockerizedServerTests : IAsyncLifetime
{
    private readonly CsvServerContainer _container;
    private readonly HttpClient _client;
    private string? _serviceUri;

    public DockerizedServerTests()
    {
        _container = new CsvServerContainer();
        _client = new HttpClient();
    }

    public async Task InitializeAsync()
    {
        await _container.InitializeAsync();
        _serviceUri = _container.GetServiceUri();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _container.DisposeAsync();
    }

    [Fact]
    public async Task Container_StartsAndRespondsToHealthCheck()
    {
        // Arrange
        var request = new { csl = ".show databases" };

        // Act
        var response = await _client.PostAsJsonAsync($"{_serviceUri}/v1/rest/mgmt", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("Tables").GetArrayLength().Should().Be(1);
        
        var rows = content.GetProperty("Tables")[0].GetProperty("Rows");
        rows.GetArrayLength().Should().BeGreaterThan(0);
        rows[0][0].GetString().Should().Be("BabyKusto");
    }

    [Fact(Skip = ".show tables not yet implemented")]
    public async Task Container_CanQueryTables()
    {
        // Arrange
        var request = new { csl = ".show tables" };

        // Act
        var response = await _client.PostAsJsonAsync($"{_serviceUri}/v1/rest/mgmt", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("Tables").GetArrayLength().Should().Be(1);
        
        // There should be tables from the mounted CSV files
        var rows = content.GetProperty("Tables")[0].GetProperty("Rows");
        rows.GetArrayLength().Should().BeGreaterThan(0);

        // Verify we can see both example CSV files
        var tableNames = Enumerable.Range(0, rows.GetArrayLength())
            .Select(i => rows[i][2].GetString())
            .ToList();
        tableNames.Should().Contain("events");
        tableNames.Should().Contain("users");
    }
}