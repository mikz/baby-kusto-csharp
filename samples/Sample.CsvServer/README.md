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
    "CsvGlobPattern": "./path/to/your/csvfiles/*.csv"
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