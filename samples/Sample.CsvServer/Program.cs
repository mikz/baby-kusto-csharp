using System.Diagnostics;
using BabyKusto.Server;
using BabyKusto.Server.Service;
using Microsoft.Extensions.FileSystemGlobbing;

namespace Sample.CsvServer;

public class CsvServerOptions
{
    public string Pattern { get; init; } = string.Empty;
    public string Root { get; init; } = Directory.GetCurrentDirectory();
    public bool Required { get; init; } = false;
}

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.UseSetting(WebHostDefaults.HttpPortsKey, "5220");
        builder.Configuration.AddCommandLine(args, new Dictionary<string, string>
        {
            { "--csv", "CsvServer:Pattern" },
            { "--root", "CsvServer:Root" },
            { "--required", "CsvServer:Required" }
        });

        var app = BuildWebApplication(builder);

        try {
            app.Services.GetRequiredService<ITablesProvider>();
        } catch (System.InvalidOperationException ex) {
            Console.WriteLine(ex);
            Environment.Exit(1);
        }

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
        builder.Services.AddControllers()
            .AddApplicationPart(typeof(Program).Assembly) // Add the assembly with controllers
            .AddControllersAsServices();
        builder.Services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
        });

        var app = builder.Build();

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

        if (string.IsNullOrEmpty(options.Pattern))
        {
            throw new ArgumentException("Missing CSV pattern");
        }

        var csvFiles = GetCsvFiles(root, options, logger);
        if (csvFiles.Count == 0 && options.Required)
        {
            throw new InvalidOperationException("No CSV files found");
        }

        return new CsvTablesProvider(csvFiles);
    }

    private static List<string> GetCsvFiles(string root, CsvServerOptions options, ILogger logger)
    {
        var csvFiles = new List<string>();
        var matcher = new Matcher();
        var pattern = options.Pattern;
        var cwd = options.Root;

        matcher.AddInclude(pattern);

        Console.WriteLine($"Searching for csv files in {cwd} with pattern {pattern}");
        logger.LogInformation($"Searching for csv files in {cwd} with pattern {pattern}");
        csvFiles.AddRange(matcher.GetResultsInFullPath(cwd));

        Console.WriteLine($"Searching for csv files in {root} with pattern {pattern}");
        logger.LogInformation($"Searching for csv files in {root} with pattern {pattern}");
        csvFiles.AddRange(matcher.GetResultsInFullPath(root));

        if (pattern.StartsWith("/"))
        {
            var components = pattern.Split('*');
            var dir = components[0];
            var patt = "*" +string.Join('*', components.Skip(1));

            matcher.AddInclude(patt);
            Console.WriteLine($"Searching for csv files in {dir} with pattern {patt}");
            logger.LogInformation($"Searching for csv files in {dir} with pattern {patt}");
            csvFiles.AddRange(matcher.GetResultsInFullPath(dir));
        }

        return csvFiles;
    }
}