# Configuration Changes - First PR

Add configuration support while maintaining existing CLI functionality.

## Changes to Program.cs

```csharp
public class CsvServerOptions
{
    public string CsvGlobPattern { get; set; } = string.Empty;
}

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add configuration support while keeping CLI
        builder.Configuration.AddCommandLine(args, new Dictionary<string, string>
        {
            { "--csv", "CsvServer:CsvGlobPattern" }
        });
        
        builder.Services.Configure<CsvServerOptions>(
            builder.Configuration.GetSection("CsvServer"));

        // Existing CLI support remains
        var csvGlob = args.SkipWhile(arg => arg != "--csv").Skip(1).FirstOrDefault();
        
        // Use CLI or configuration
        var csvPattern = csvGlob ?? builder.Services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<CsvServerOptions>>()
            .Value.CsvGlobPattern;
            
        if (string.IsNullOrEmpty(csvPattern))
        {
            throw new ArgumentException("Missing CSV pattern. Use --csv argument or configuration.");
        }

        var csvFiles = /* existing glob handling */

        // Rest of the setup remains unchanged
    }
}
```

## Testing

Manual verification:
1. Works with CLI: `--csv "./data/*.csv"`
2. Works with configuration:
   ```json
   {
     "CsvServer": {
       "CsvGlobPattern": "./data/*.csv"
     }
   }
   ```

## Next PR Preview

Once this is merged, next PR will:
1. Add test project configuration
2. Add WebApplicationFactory setup
3. Add basic test using configuration