using BabyKusto.Server;
using BabyKusto.Server.Service;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Sample.CsvServer;

public class CsvServerOptions
{
    public string CsvGlobPattern { get; set; } = string.Empty;
}

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.UseSetting(WebHostDefaults.HttpPortsKey, "5220");
        builder.Configuration.AddCommandLine(args, new Dictionary<string, string>
        {
            { "--csv", "CsvServer:CsvGlobPattern" }
        });

        var app = BuildWebApplication(builder);

        app.Run();
    }

    internal static WebApplication BuildWebApplication(WebApplicationBuilder builder)
    {
        builder.Services.Configure<CsvServerOptions>(
            builder.Configuration.GetSection("CsvServer"));

        builder.Services.AddSingleton<ITablesProvider>(sp =>
            TablesProviderFactory.Create(
                builder.Environment.ContentRootPath,
                sp.GetRequiredService<IConfiguration>(),
                sp.GetRequiredService<ILogger<Program>>()));
        builder.Services.AddBabyKustoServer();
        builder.Services.AddControllers();
        builder.Services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
        });

        var app = builder.Build();

        // app.Services.GetRequiredService<ITablesProvider>(); // validate csv server

        app.UseRouting();
        app.MapControllers();
        app.UseHttpLogging();

        return app;
    }

}

public abstract class TablesProviderFactory
{
    public static ITablesProvider Create(string root, IConfiguration configuration, ILogger logger)
    {
        var options = new CsvServerOptions();
        configuration.GetSection("CsvServer").Bind(options);

        if (string.IsNullOrEmpty(options.CsvGlobPattern))
        {
            throw new ArgumentException("Missing CSV pattern");
        }

        var csvFiles = GetCsvFiles(root, options.CsvGlobPattern, logger);
        if (csvFiles.Count == 0)
        {
            throw new InvalidOperationException("No CSV files found");
        }

        return new CsvTablesProvider(csvFiles);
    }

    private static List<string> GetCsvFiles(string root, string pattern, ILogger logger)
    {
        var csvFiles = new List<string>();
        var matcher = new Matcher();

        var cwd = Path.GetFullPath(Directory.GetCurrentDirectory());

        matcher.AddInclude(pattern);

        Console.WriteLine($"Searching for csv files in {cwd} with pattern {pattern}");
        logger.LogInformation($"Searching for csv files in {cwd} with pattern {pattern}");
        csvFiles.AddRange(matcher.GetResultsInFullPath(cwd));

        Console.WriteLine($"Searching for csv files in {root} with pattern {pattern}");
        logger.LogInformation($"Searching for csv files in {root} with pattern {pattern}");
        csvFiles.AddRange(matcher.GetResultsInFullPath(root));

        return csvFiles;
    }
}