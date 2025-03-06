# Sample.CsvServer Tests

This project contains both unit and integration tests for the CSV Server implementation.

## Test Structure

### Unit Tests
- CsvTableSourceTests: CSV file parsing and data handling
- CsvTablesProviderTests: Table management and loading

### Integration Tests
Tests that validate end-to-end functionality using real Kusto client.

#### Setup
- WebApplicationFactory running on fixed port
- Kusto.Data client for queries
- Reusing CsvTableSource for data validation

#### Test Categories

1. Server Tests
   - Server starts on fixed port
   - CSV files are loaded correctly
   - Tables are exposed properly

2. Kusto Client Tests
   - Connect to local server
   - Query table data
   - Validate results against CsvTableSource

3. Data Validation
   - Compare query results with CsvTableSource data
   - Verify type handling (string, long, datetime)
   - Check null value handling

## Dependencies

### NuGet Packages
- Microsoft.AspNetCore.Mvc.Testing
- Kusto.Data
- xunit
- FluentAssertions

### Test Data
Using example CSV files from samples/Sample.CsvServer/example/:
- users.csv
- events.csv

## Implementation Strategy

1. Integration Test Setup
   ```csharp
   public class CsvServerFactory : WebApplicationFactory<Program>
   {
     // Configure for fixed port
     // Load example CSV files
   }
   ```

2. Data Comparison
   ```csharp
   public class DataComparer
   {
     // Compare Kusto query results with CsvTableSource data
     // Handle type-specific comparisons
   }
   ```

3. Test Organization
   - Group tests by functionality
   - Share test data loading logic
   - Reuse existing CsvTableSource for validation

## Running Tests

1. Unit Tests:
   ```bash
   dotnet test --filter "Category!=Integration"
   ```

2. Integration Tests:
   ```bash
   dotnet test --filter "Category=Integration"
   ```

## Best Practices

1. Test Data Management
   - Use example CSV files
   - Load through CsvTableSource
   - Compare in-memory representations

2. Connection Handling
   - Use fixed port for predictability
   - Clean shutdown after tests
   - Handle connection errors

3. Data Validation
   - Type-aware comparisons
   - Handle null values
   - Verify schema matching