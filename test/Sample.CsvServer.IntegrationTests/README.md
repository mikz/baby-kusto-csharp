# CSV Server Integration Tests

This directory contains integration tests that validate the CSV Server functionality using the real Kusto client.

## Test Structure

### 1. Project Setup
- WebApplicationFactory for hosting CSV Server
- Kusto.Data NuGet package for client
- xUnit test framework
- FluentAssertions for assertions

### 2. Test Categories

#### Server Tests
- Server startup and configuration
- CSV file loading and table exposure
- Connection verification

#### Data Access Tests
- Query execution through Kusto client
- Data validation against source CSV
- Type handling verification

### 3. Test Data
Using existing CSV files from example directory:
- users.csv: Basic user data with multiple types
- events.csv: Event logs with timestamps

### 4. Test Cases

1. Basic Connectivity
   - Server starts successfully
   - Client can connect
   - Tables are listed correctly

2. Data Reading
   - Read entire table
   - Compare with source CSV data
   - Verify type conversions

3. Error Handling
   - Invalid queries
   - Connection issues
   - Server shutdown

## Implementation Plan

1. Create test project with dependencies:
   ```xml
   <ItemGroup>
     <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
     <PackageReference Include="Kusto.Data" />
     <PackageReference Include="xunit" />
     <PackageReference Include="FluentAssertions" />
   </ItemGroup>
   ```

2. Implement WebApplicationFactory setup:
   ```csharp
   public class CsvServerFactory : WebApplicationFactory<Program>
   {
     protected override void ConfigureWebHost(IWebHostBuilder builder)
     {
       // Configure test server
     }
   }
   ```

3. Implement test fixtures:
   - Server setup and teardown
   - CSV file management
   - Kusto client configuration

4. Create test classes:
   - ConnectionTests
   - QueryTests
   - ErrorHandlingTests

## Test Workflow

1. Start test server with WebApplicationFactory
2. Configure Kusto client with local endpoint
3. Load test CSV files
4. Execute queries
5. Validate results against in-memory data
6. Cleanup resources

## Dependencies

- Sample.CsvServer project
- BabyKusto.Server
- Test CSV files from example directory