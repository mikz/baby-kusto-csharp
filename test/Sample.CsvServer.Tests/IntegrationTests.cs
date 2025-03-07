using BabyKusto.Server.Service;
using FluentAssertions;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Sample.CsvServer.Tests;

public class CsvServerTestBase(ServerFixture fixture) : IClassFixture<ServerFixture>, IAsyncLifetime
{
    internal ICslQueryProvider QueryProvider = null!;

    internal readonly ServerFixture Fixture = fixture;

    public Task InitializeAsync()
    {
        var kcsb = new KustoConnectionStringBuilder($"Data Source={Fixture.ServerUrl}");
        QueryProvider = KustoClientFactory.CreateCslQueryProvider(kcsb);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        QueryProvider?.Dispose();
        return Task.CompletedTask;
    }
}

public class ServerFixture : IAsyncLifetime
{
    internal WebApplication App;
    private readonly WebApplicationBuilder _builder = WebApplication.CreateBuilder();

    public ServerFixture()
    {
        _builder.WebHost.UseUrls("http://127.0.0.1:0"); // Auto-assign port
        _builder.Environment.ContentRootPath =   Path.GetFullPath("../../../../../samples/Sample.CsvServer");

        App = Program.BuildWebApplication(_builder);
    }

    public string? ServerUrl { get; private set; }

    public async Task InitializeAsync()
    {
        Console.WriteLine("Initializing Server");
        await App.StartAsync();

        ServerUrl = App.Urls.First();
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("Disposing Server");
        await App.DisposeAsync();
    }
}

public class IntegrationTests(ServerFixture fixture) : CsvServerTestBase(fixture)
{
    [Fact]
    public void Server_LoadsConfiguration()
    {
        // Get configured options
        var options = Fixture.App.Services.GetRequiredService<IOptions<CsvServerOptions>>();
        
        // Should load from appsettings.Testing.json
        options.Value.CsvGlobPattern.Should()
            .Be("example/*.csv");
    }

    [Fact]
    public void Server_LoadsCsvFiles()
    {
        // Get table provider
        var provider = Fixture.App.Services.GetRequiredService<ITablesProvider>();
        var tables = provider.GetTables();

        // Should find both example files
        tables.Should().HaveCount(2);
        tables.Select(t => t.Type.Name).Should().BeEquivalentTo("users", "events");
    }
}