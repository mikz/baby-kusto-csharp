# Sample.CsvServer Tests

This project contains both unit and integration tests for the CSV Server implementation.

## Test Categories

### Unit Tests
- `CsvTableSourceTests`: Tests for CSV file parsing, schema detection, and data handling
- `CsvTablesProviderTests`: Tests for table management, multiple file loading, and error handling

### Integration Tests
- `IntegrationTests`: Basic server configuration and startup tests
- `KustoClientTests`: Tests using the Kusto client to connect and query data
- `DataComparisonTests`: Tests that validate data integrity between source CSV and query results
- `ErrorHandlingTests`: Tests for various error scenarios and response handling

## Test Structure

The integration tests use a custom test fixture (`ServerFixture`) that:
- Creates a WebApplication instance of Sample.CsvServer
- Configures it with test settings
- Runs on a dynamic port
- Provides a Kusto client connection

## Running Tests

```bash
# Run all tests
dotnet test

# Run only unit tests
dotnet test --filter "FullyQualifiedName~CsvTableSource|FullyQualifiedName~CsvTablesProvider"

# Run only integration tests
dotnet test --filter "FullyQualifiedName~Integration|FullyQualifiedName~KustoClient"
```

## Test Data

The tests use example CSV files from `samples/Sample.CsvServer/example/`:
- `users.csv`: Contains user data with various data types
- `events.csv`: Contains event data with timestamps and numeric fields

For unit tests, test data is generated in-memory during the test run.

## Dependencies

- `Microsoft.AspNetCore.Mvc.Testing`: For hosting the application in tests
- `Microsoft.Azure.Kusto.Data`: For client connections to the test server
- `FluentAssertions`: For cleaner assertion syntax
- `xunit`: Test framework