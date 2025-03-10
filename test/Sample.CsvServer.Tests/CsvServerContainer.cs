using System.Diagnostics;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Configurations;
using Xunit;

namespace Sample.CsvServer.Tests;

public sealed class CsvServerContainer : IAsyncLifetime
{
    private const int ContainerPort = 5220;
    private const string DatabaseName = "BabyKusto";
    private readonly IContainer _container;
    private readonly string _solutionRoot;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(60);
    private readonly IFutureDockerImage _image;

    public CsvServerContainer()
    {
        _solutionRoot = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(_solutionRoot, "BabyKusto.sln")))
        {
            _solutionRoot = Path.GetDirectoryName(_solutionRoot)!;
            if (string.IsNullOrEmpty(_solutionRoot))
            {
                throw new InvalidOperationException("Could not find solution root directory.");
            }
        }

        Debug.WriteLine($"Solution root: {_solutionRoot}");
        
        var projectDir = Path.Combine(_solutionRoot, "samples/Sample.CsvServer");
        var dataDir = Path.Combine(_solutionRoot, "samples/Sample.CsvServer/example");

        Debug.WriteLine($"Project directory: {projectDir}");
        Debug.WriteLine($"Data directory: {dataDir}");

        // Set up the image builder
        _image = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(_solutionRoot)
            .WithDockerfile("samples/Sample.CsvServer/Dockerfile")
            .WithName("babykusto-csv-server:test")
            .Build();

        // Create container
        _container = new ContainerBuilder()
            .WithImage(_image)
            .WithPortBinding(ContainerPort, true)
            .WithBindMount(dataDir, "/data", AccessMode.ReadOnly)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithCommand("--CsvServer:Pattern=/data/*.csv", "--CsvServer:Root=/data", "--Logging:LogLevel:Default=Debug")
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilPortIsAvailable(ContainerPort))
            .Build();
    }

    public string GetServiceUri()
    {
        return $"http://{_container.Hostname}:{_container.GetMappedPublicPort(ContainerPort)}";
    }

    private async Task CheckHealth(CancellationToken ct)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var uri = GetServiceUri();
        
        Debug.WriteLine("Waiting for service to become healthy...");
        var retryCount = 30; // 1 minute total with 2s timeouts
        var success = false;

        while (retryCount-- > 0 && !success && !ct.IsCancellationRequested)
        {
            try
            {
                // Try the management API
                var mgmtResponse = await client.PostAsJsonAsync(
                    $"{uri}/v1/rest/mgmt",
                    new { csl = ".show databases" },
                    ct);
                    
                if (mgmtResponse.IsSuccessStatusCode)
                {
                    var content = await mgmtResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                    if (content.GetProperty("Tables")[0].GetProperty("Rows")[0][0].GetString()!.Contains(DatabaseName))
                    {
                        Debug.WriteLine("Management API check successful");
                        success = true;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Health check attempt failed: {ex.Message}");
            }

            if (!ct.IsCancellationRequested)
            {
                await Task.Delay(2000, ct);
            }
        }

        if (!success)
        {
            throw new Exception("Container failed to initialize within the timeout period");
        }
    }

    public async Task InitializeAsync()
    {
        using var cts = new CancellationTokenSource(_timeout);
        
        Debug.WriteLine("Building Docker image...");
        await _image.CreateAsync(cts.Token);
        
        Debug.WriteLine("Starting container...");
        await _container.StartAsync(cts.Token);
        Debug.WriteLine($"Container started, service URI: {GetServiceUri()}");

        await CheckHealth(cts.Token);
        Debug.WriteLine("Container initialized successfully");
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}