using System.Data;
using BabyKusto.Core;
using BabyKusto.SampleCsvServer;
using FluentAssertions;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Xunit;

namespace Sample.CsvServer.Tests;

/// <summary>
/// Tests that validate data integrity by comparing Kusto query results with the original CSV data.
/// </summary>
public class DataComparisonTests : CsvServerTestBase
{
    private readonly ICslQueryProvider _queryProvider;
    private readonly CsvTableSource _usersTable;
    private readonly CsvTableSource _eventsTable;
    private const string ConnectionString = "Data Source=http://localhost:5220;";
    private const string CsvPath = "../../../samples/Sample.CsvServer/example";

    public DataComparisonTests() : base()
    {
        // Setup Kusto client
        var kcsb = new KustoConnectionStringBuilder(ConnectionString);
        _queryProvider = KustoClientFactory.CreateCslQueryProvider(kcsb);
        
        // Load CSV files directly for comparison
        _usersTable = new CsvTableSource(Path.Combine(CsvPath, "users.csv"));
        _eventsTable = new CsvTableSource(Path.Combine(CsvPath, "events.csv"));
    }

    [Fact]
    public void UserData_MatchesCsvSource()
    {
        // Get all data from users table via Kusto
        using var reader = _queryProvider.ExecuteQuery("users");
        
        // Get data directly from CSV
        var csvData = _usersTable.GetData().First();
        
        // Compare row counts
        var rowCount = GetRowCount(reader);
        rowCount.Should().Be(csvData.RowCount);
        
        // Reset reader for data comparison
        reader.Dispose();
        
        // Execute query again to read data
        using var readerForData = _queryProvider.ExecuteQuery("users | order by name asc");
        
        // Read all data into in-memory structures for comparison
        var kustoUsers = ReadUsersFromReader(readerForData);
        var csvUsers = ReadUsersFromCsv(csvData);
        
        // Compare data sets
        kustoUsers.Should().BeEquivalentTo(csvUsers);
    }
    
    [Fact]
    public void EventData_MatchesCsvSource()
    {
        // Get all data from events table via Kusto
        using var reader = _queryProvider.ExecuteQuery("events");
        
        // Get data directly from CSV
        var csvData = _eventsTable.GetData().First();
        
        // Compare row counts
        var rowCount = GetRowCount(reader);
        rowCount.Should().Be(csvData.RowCount);
        
        // Reset reader for data comparison
        reader.Dispose();
        
        // Execute query again to read data
        using var readerForData = _queryProvider.ExecuteQuery("events | order by id asc");
        
        // Verify high severity event matches between sources
        var kustoHighSeverityEvent = ReadHighSeverityEvent(readerForData);
        var csvHighSeverityEvent = FindHighSeverityEventInCsv(csvData);
        
        // Compare the specific event
        kustoHighSeverityEvent.Should().BeEquivalentTo(csvHighSeverityEvent);
    }
    
    #region Helper Methods
    private int GetRowCount(IDataReader reader)
    {
        int count = 0;
        while (reader.Read())
        {
            count++;
        }
        return count;
    }
    
    private List<UserData> ReadUsersFromReader(IDataReader reader)
    {
        var users = new List<UserData>();
        
        while (reader.Read())
        {
            users.Add(new UserData
            {
                Name = reader.GetString(reader.GetOrdinal("name")),
                Age = reader.GetInt64(reader.GetOrdinal("age")),
                Email = reader.GetString(reader.GetOrdinal("email")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("is_active"))
            });
        }
        
        return users;
    }
    
    private List<UserData> ReadUsersFromCsv(ITableChunk csvData)
    {
        var users = new List<UserData>();
        var nameCol = (Column<string>)csvData.Columns.First(c => c.Type.Name == "name");
        var ageCol = (Column<long?>)csvData.Columns.First(c => c.Type.Name == "age");
        var emailCol = (Column<string>)csvData.Columns.First(c => c.Type.Name == "email");
        var isActiveCol = (Column<bool?>)csvData.Columns.First(c => c.Type.Name == "is_active");
        
        // Create user objects from CSV columns
        for (int i = 0; i < csvData.RowCount; i++)
        {
            users.Add(new UserData
            {
                Name = nameCol[i],
                Age = ageCol[i] ?? 0,
                Email = emailCol[i],
                IsActive = isActiveCol[i] ?? false
            });
        }
        
        // Sort by name to match the Kusto query
        return users.OrderBy(u => u.Name).ToList();
    }
    
    private EventData ReadHighSeverityEvent(IDataReader reader)
    {
        EventData highSeverityEvent = null;
        
        while (reader.Read())
        {
            var severity = reader.GetInt64(reader.GetOrdinal("severity"));
            if (severity > 2)
            {
                highSeverityEvent = new EventData
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    Type = reader.GetString(reader.GetOrdinal("type")),
                    Source = reader.GetString(reader.GetOrdinal("source")),
                    Severity = severity
                };
                break;
            }
        }
        
        return highSeverityEvent;
    }
    
    private EventData FindHighSeverityEventInCsv(ITableChunk csvData)
    {
        var idCol = (Column<long?>)csvData.Columns.First(c => c.Type.Name == "id");
        var typeCol = (Column<string>)csvData.Columns.First(c => c.Type.Name == "type");
        var sourceCol = (Column<string>)csvData.Columns.First(c => c.Type.Name == "source");
        var severityCol = (Column<long?>)csvData.Columns.First(c => c.Type.Name == "severity");
        
        EventData highSeverityEvent = null;
        
        for (int i = 0; i < csvData.RowCount; i++)
        {
            var severity = severityCol[i] ?? 0;
            if (severity > 2)
            {
                highSeverityEvent = new EventData
                {
                    Id = idCol[i] ?? 0,
                    Type = typeCol[i],
                    Source = sourceCol[i],
                    Severity = severity
                };
                break;
            }
        }
        
        return highSeverityEvent;
    }
    #endregion
    
    #region Data Classes
    private class UserData
    {
        public string Name { get; set; }
        public long Age { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
    }
    
    private class EventData
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public string Source { get; set; }
        public long Severity { get; set; }
    }
    #endregion
}