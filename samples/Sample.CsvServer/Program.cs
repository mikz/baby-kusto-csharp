using BabyKusto.Core;
using BabyKusto.Server;
using BabyKusto.Server.Service;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Options;

namespace BabyKusto.SampleCsvServer;

public class CsvServerOptions
{
    public string CsvGlobPattern { get; set; } = string.Empty;
}

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add configuration support
        builder.Configuration.AddCommandLine(args, new Dictionary<string, string>
        {
            { "--csv", "CsvServer:CsvGlobPattern" }
        });
        
        builder.Services.AddControllers();
        builder.Services.Configure<CsvServerOptions>(
            builder.Configuration.GetSection("CsvServer"));

        // Initialize services
        var app = builder.Build();
        var options = app.Services.GetRequiredService<IOptions<CsvServerOptions>>();

        // Get CSV pattern from configuration
        if (string.IsNullOrEmpty(options.Value.CsvGlobPattern))
        {
            throw new ArgumentException("Missing CSV pattern. Use --csv argument or configuration.");
        }

        var csvFiles = GetCsvFiles(options.Value.CsvGlobPattern);
        if (csvFiles.Count == 0)
        {
            Console.Error.WriteLine("No CSV files found.");
            return;
        }

        // Add services after configuration is validated
        builder.Services.AddSingleton<ITablesProvider>(_ => new CsvTablesProvider(csvFiles));
        builder.Services.AddBabyKustoServer();

        app.UseHttpsRedirection();
        app.UseRouting();
        app.MapControllers();

        app.Run();
    }

    private static List<string> GetCsvFiles(string pattern)
    {
        var csvFiles = new List<string>();
        var matcher = new Matcher();

        var directory = Path.GetDirectoryName(pattern) ?? ".";
        matcher.AddInclude(Path.GetFileName(pattern));
        
        var matches = matcher.GetResultsInFullPath(directory);
        csvFiles.AddRange(matches);

        return csvFiles;
    }
}