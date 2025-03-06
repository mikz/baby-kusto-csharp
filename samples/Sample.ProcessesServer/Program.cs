using BabyKusto.Core;
using BabyKusto.ProcessQuerier;
using BabyKusto.Server;
using BabyKusto.Server.Service;

namespace BabyKusto.SampleServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            var app = BuildWebApplication(builder);

            app.Run();
        }

        static WebApplication BuildWebApplication(WebApplicationBuilder builder)
        {
            builder.Services.AddControllers();

            builder.Services.AddSingleton<ITablesProvider, SimpleTableProvider>();
            builder.Services.AddBabyKustoServer();

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

        private class SimpleTableProvider : ITablesProvider
        {
            private readonly List<ITableSource> _tables;

            public SimpleTableProvider()
            {
                _tables = new List<ITableSource>
                {
                    new ProcessesTable("Processes"),
                };
            }

            public List<ITableSource> GetTables()
            {
                return _tables;
            }
        }
    }
}