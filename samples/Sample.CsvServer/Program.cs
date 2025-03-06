using BabyKusto.Core;
using BabyKusto.Server;
using BabyKusto.Server.Service;
using Microsoft.Extensions.FileSystemGlobbing;

namespace BabyKusto.SampleCsvServer;

public class Program
{
    public static void Main(string[] args)
    {
        var csvFiles = GetCsvFiles(args);
        if (csvFiles.Count == 0)
        {
            Console.Error.WriteLine("No CSV files found. Use --csv <glob_pattern> to specify CSV files.");
            return;
        }

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddSingleton<ITablesProvider>(_ => new CsvTablesProvider(csvFiles));
        builder.Services.AddBabyKustoServer();

        var app = builder.Build();

        app.UseHttpsRedirection();
        app.UseRouting();
        app.MapControllers();

        app.Run();
    }

    private static List<string> GetCsvFiles(string[] args)
    {
        var csvFiles = new List<string>();
        var matcher = new Matcher();

        for (var i = 0; i < args.Length; i++)
        {
            if (args[i] == "--csv" && i + 1 < args.Length)
            {
                var pattern = args[i + 1];
                var directory = Path.GetDirectoryName(pattern) ?? ".";
                matcher.AddInclude(Path.GetFileName(pattern));
                
                var matches = matcher.GetResultsInFullPath(directory);
                csvFiles.AddRange(matches);
                
                i++; // Skip the pattern argument
            }
        }

        return csvFiles;
    }
}