# Sample CSV Server

This sample demonstrates how to use BabyKusto.Server with CSV files as table sources.
The server exposes CSV files as queryable tables through the Kusto protocol.

## Features

- Load CSV files as Kusto tables
- Support for multiple CSV files (each becomes a table)
- Kusto-style column type definitions in CSV headers
- Docker container support with volume mounting for CSV files
- Configuration via CLI or appsettings.json

## Usage with Docker

The recommended way to use Sample.CsvServer is with Docker:

```bash
# Build the Docker image
docker build -t baby-kusto-csv -f samples/Sample.CsvServer/Dockerfile .

# Run with your CSV files mounted
docker run -p 5220:5220 -v /path/to/your/csv/files:/data baby-kusto-csv --csv "/data/*.csv"
```

### Connecting to the server

After starting the container, you can connect to the CSV Server using the Kusto client:

```csharp
// Connect to the server
var connectionString = "Data Source=http://localhost:5220;";
var kcsb = new KustoConnectionStringBuilder(connectionString);
var queryProvider = KustoClientFactory.CreateCslQueryProvider(kcsb);

// Execute a query
var reader = queryProvider.ExecuteQuery("your_table | limit 10");
```

## CSV File Format

CSV files must have a header row defining column names and types using Kusto-style type annotations:

```csv
name:string,age:long,registered:datetime
"John Doe",30,2024-01-01T10:00:00Z
"Jane Smith",25,2024-01-02T15:30:00Z
```

### Supported Column Types

- `string`: Text values
- `long`: 64-bit integers
- `int`: 32-bit integers
- `real` or `double`: Floating point numbers
- `bool`: Boolean values
- `datetime`: Date and time values
- `timespan`: Time interval values

## Running Locally

To run the server directly without Docker:

```bash
cd samples/Sample.CsvServer
dotnet run --csv "./path/to/your/csvfiles/*.csv"
```

Or using configuration:

```json
// appsettings.json
{
  "CsvServer": {
    "Pattern": "./path/to/your/csvfiles/*.csv"
  }
}
```

## Project Structure

```
samples/Sample.CsvServer/
├── Program.cs                 # Entry point and service configuration
├── CsvTableSource.cs          # CSV file table implementation
├── CsvTablesProvider.cs       # Provider managing multiple CSV files
├── Dockerfile                 # Container build definition
└── example/                   # Example CSV files for testing
    ├── users.csv               
    └── events.csv              
```

## Using with Testcontainers

For automated testing, you can use Testcontainers for .NET to spin up the CSV Server in a Docker container:

### Health Check Implementation

The CSV Server can be health-checked using the Kusto management API. The following HTTP request validates that the server is ready:

```
POST /v1/rest/mgmt
Content-Type: application/json

{
  "csl": ".show databases"
}
```

A successful response indicates that the server is fully operational.

### Creating a Custom Container Class

Create a custom container class adapting the existing Testcontainers.Kusto components:

```csharp
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace YourProject.Tests;

public class BabyCsvConfiguration
{
    public string[] CsvPaths { get; set; } = Array.Empty<string>();
    public int Port { get; set; } = 5220;
    public string ContainerCsvPath { get; set; } = "/data";
}

public class BabyCsvBuilder : ContainerBuilder<BabyCsvBuilder, BabyCsvContainer, BabyCsvConfiguration>
{
    public const string ImageName = "baby-kusto-csv:latest";
    public const string HealthCheckCommand = "{ \"csl\": \".show databases\" }";

    public BabyCsvBuilder()
        : base()
    {
        DockerResourceConfiguration = Init().DockerResourceConfiguration;
        DockerResourceConfiguration.ImageName = ImageName;
    }

    protected override BabyCsvBuilder Init()
    {
        return base.Init()
            .WithPortBinding(DefaultConfig.Port, true)
            .WithWaitStrategy(
                Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(r => r
                    .WithPort(DefaultConfig.Port)
                    .WithPath("/v1/rest/mgmt")
                    .WithMethod(HttpMethod.Post)
                    .WithBody(HealthCheckCommand)
                    .WithContentType("application/json")
                )
            );
    }

    public BabyCsvBuilder WithCsvFiles(params string[] csvPaths)
    {
        foreach (var csvPath in csvPaths)
        {
            var fileName = Path.GetFileName(csvPath);
            var containerPath = $"{DefaultConfig.ContainerCsvPath}/{fileName}";
            WithBindMount(csvPath, containerPath);
        }

        DefaultConfig.CsvPaths = csvPaths;
        return this;
    }

    public override BabyCsvContainer Build()
    {
        // Add the CSV glob pattern command
        var csvPaths = DefaultConfig.CsvPaths.Length > 0
            ? $"{DefaultConfig.ContainerCsvPath}/*.csv"
            : "example/*.csv";

        WithCommand("--csv", $"\"{csvPaths}\"");

        return new BabyCsvContainer(DockerResourceConfiguration, DefaultConfig);
    }

    private static readonly BabyCsvConfiguration DefaultConfig = new();
}

public class BabyCsvContainer : DockerContainer
{
    private readonly BabyCsvConfiguration _configuration;

    public BabyCsvContainer(IDockerResourceConfiguration resourceConfiguration, BabyCsvConfiguration configuration)
        : base(resourceConfiguration)
    {
        _configuration = configuration;
    }

    public string GetConnectionString()
    {
        var mappedPort = GetMappedPublicPort(_configuration.Port);
        return $"Data Source=http://localhost:{mappedPort};";
    }

    public ICslQueryProvider CreateQueryProvider()
    {
        var kcsb = new KustoConnectionStringBuilder(GetConnectionString());
        return KustoClientFactory.CreateCslQueryProvider(kcsb);
    }
}
```

### Using the Container in Tests

Once you've defined the container class, you can use it in your tests:

```csharp
using Xunit;
using Kusto.Data;
using Kusto.Data.Common;
using DotNet.Testcontainers.Builders;

namespace YourProject.Tests;

public class CsvServerTests : IAsyncLifetime
{
    private readonly BabyCsvContainer _container;
    private ICslQueryProvider _queryProvider;

    public CsvServerTests()
    {
        // Create and build the container
        _container = new BabyCsvBuilder()
            .WithCsvFiles("./testdata/users.csv", "./testdata/events.csv")
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start the container before tests
        await _container.StartAsync();
        _queryProvider = _container.CreateQueryProvider();
    }

    public async Task DisposeAsync()
    {
        _queryProvider?.Dispose();
        await _container.DisposeAsync();
    }

    [Fact]
    public void Can_Query_Data()
    {
        // Execute your query
        using var reader = _queryProvider.ExecuteQuery("users | where age > 30");

        // Assert on the results
        Assert.True(reader.Read());
    }
}
```

### Prerequisites

To use the above code, you'll need the following NuGet packages:

```xml
<PackageReference Include="Testcontainers" Version="3.6.0" />
<PackageReference Include="Microsoft.Azure.Kusto.Data" Version="12.1.0" />
<PackageReference Include="xunit" Version="2.7.0" />
```

### Building the Docker Image

Before running tests with Testcontainers, make sure to build the Docker image:

```bash
# From the repository root
docker build -t baby-kusto-csv:latest -f samples/Sample.CsvServer/Dockerfile .
```

## Testing

The server includes comprehensive tests in the `/test/Sample.CsvServer.Tests` folder:

```bash
# Run all tests
dotnet test test/Sample.CsvServer.Tests

# Run only unit tests
dotnet test --filter "FullyQualifiedName~CsvTableSourceTests|FullyQualifiedName~CsvTablesProviderTests"

# Run only integration tests
dotnet test --filter "FullyQualifiedName~IntegrationTests|FullyQualifiedName~KustoClientTests"
```