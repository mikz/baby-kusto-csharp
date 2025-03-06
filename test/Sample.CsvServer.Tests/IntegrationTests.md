# Integration Tests Implementation Plan

## Phase 1: Basic Setup with Configuration

### 1. Configuration Changes

Program.cs changes:
```csharp
public class CsvServerOptions
{
    public int Port { get; set; } = 8080;
    public string CsvGlobPattern { get; set; } = string.Empty;
}

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        
        // Map --csv to configuration
        builder.Configuration.AddCommandLine(args, new Dictionary<string, string>
        {
            { "--csv", "CsvServer:CsvGlobPattern" }
        });

        builder.Services.Configure<CsvServerOptions>(
            builder.Configuration.GetSection("CsvServer"));
        
        // Rest of the setup
    }
}
```

### 2. Test Configuration

appsettings.Testing.json:
```json
{
  "CsvServer": {
    "Port": 8080,
    "CsvGlobPattern": "../../../samples/Sample.CsvServer/example/*.csv"
  }
}
```

### 3. Test Base Class

```csharp
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
                    config.AddJsonFile("appsettings.Testing.json", optional: false);
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
```

### 4. Initial Test Setup

Basic connectivity test:
```csharp
public class ServerTests : CsvServerTestBase
{
    [Fact]
    public void Server_StartsSuccessfully()
    {
        // Server startup is verified by the base class constructor
        Factory.Services.GetRequiredService<IOptions<CsvServerOptions>>()
            .Value.Port.Should().Be(8080);
    }
}
```

### 5. Implementation Steps

1. Update Program:
   - [ ] Add CsvServerOptions class
   - [ ] Add configuration mapping
   - [ ] Update services setup

2. Test Project:
   - [ ] Add test configuration file
   - [ ] Implement test base class
   - [ ] Add basic test

3. Test Data:
   - [ ] Use example CSV files directly
   - [ ] Verify files are loaded

### 6. Next Steps

Once the basic setup works:
1. Add Kusto.Data package
2. Implement client connection
3. Add data reading tests

### 7. Success Criteria

- Server starts with configuration
- CSV files are loaded
- Ready for Kusto client testing