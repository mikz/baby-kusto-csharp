using BabyKusto.SampleCsvServer;
using BabyKusto.Server.Service;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Sample.CsvServer.Tests;

public class CsvServerTestBase : IDisposable
{
    protected WebApplicationFactory<Program> Factory { get; }
    protected HttpClient Client { get; }

    public CsvServerTestBase()
    {
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile(Path.GetFullPath("../../../appsettings.Testing.json"), optional: false);
                    
                    // Mock CLI args for backward compatibility
                    var args = new[] { "--csv", "../../../samples/Sample.CsvServer/example/*.csv" };
                    config.AddCommandLine(args);
                });
            });

        Client = Factory.CreateClient();
    }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
    }
}

public class IntegrationTests : CsvServerTestBase
{
    [Fact]
    public void Server_LoadsConfiguration()
    {
        // Get configured options
        var options = Factory.Services.GetRequiredService<IOptions<CsvServerOptions>>();
        
        // Should load from appsettings.Testing.json
        options.Value.CsvGlobPattern.Should()
            .Be("../../../samples/Sample.CsvServer/example/*.csv");
    }

    [Fact]
    public void Server_LoadsCsvFiles()
    {
        // Get table provider
        var provider = Factory.Services.GetRequiredService<ITablesProvider>();
        var tables = provider.GetTables();

        // Should find both example files
        tables.Should().HaveCount(2);
        tables.Select(t => t.Type.Name).Should().BeEquivalentTo("users", "events");
    }
}