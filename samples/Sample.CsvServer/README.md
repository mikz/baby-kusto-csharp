# Sample CSV Server

This sample demonstrates how to use BabyKusto.Server with CSV files as table sources.
The server exposes CSV files as queryable tables through the Kusto protocol.

## Features

- Load CSV files as Kusto tables
- Support for multiple CSV files (each becomes a table)
- Kusto-style column type definitions in CSV headers
- Docker container support
- Simple CLI interface

## Project Structure

```
samples/Sample.CsvServer/
├── Program.cs                    # Entry point and service configuration
├── Sample.CsvServer.csproj       # Project file with dependencies
├── CsvTableSource.cs            # CSV file table implementation
├── CsvTablesProvider.cs         # Provider managing multiple CSV files
├── Dockerfile                   # Container build definition
├── Makefile                     # Build and run automation
├── test/                        # Integration tests
│   └── Sample.CsvServer.Tests/  # Test project
└── example/                     # Example CSV files
    ├── users.csv               
    └── events.csv              
```

## Implementation Plan

### 1. CSV File Support
- `CsvTableSource` implements `ITableSource`
- Parses Kusto-style headers (columnName:columnType)
- Supports all basic Kusto types
- Loads data on-demand from CSV files

### 2. CLI Parameters
```bash
Sample.CsvServer --csv ./data/*.csv
```
- Accepts glob patterns for multiple CSV files
- Uses file names as table names

### 3. Docker Support
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
# Build steps

FROM mcr.microsoft.com/dotnet/aspnet:9.0
# Runtime image with CSV mounting support
```

### 4. Makefile Targets
```makefile
build:      # Build the application
test:       # Run integration tests
docker:     # Build Docker image
run:        # Run container with mounted CSV files
```

### 5. Integration Tests
- Test CSV parsing
- Validate table schema detection
- Query execution tests
- Docker container tests

## CSV File Format

CSV files must have a header row defining column names and types using Kusto-style type annotations:

```csv
name:string,age:long,registered:datetime
"John Doe",30,2024-01-01T10:00:00Z
"Jane Smith",25,2024-01-02T15:30:00Z
```

## Usage

1. Prepare CSV files with proper headers
2. Run the server:
   ```bash
   make run
   # or directly
   dotnet run --csv ./data/*.csv
   ```
3. Query data using Kusto protocol