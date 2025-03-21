using System.Data;
using BabyKusto.Core;
using FluentAssertions;
using Kusto.Language.Utils;
using Xunit;

namespace Sample.CsvServer.Tests;

/// <summary>
/// Tests that validate data integrity by comparing Kusto query results with the original CSV data.
/// </summary>
public class DataComparisonTests(ServerFixture fixture) : CsvServerTestBase(fixture)
{
    private readonly CsvTableSource _usersTable = new(Path.Combine(CsvPath, "users.csv"));
    private readonly CsvTableSource _eventsTable = new(Path.Combine(CsvPath, "events.csv"));

    private const string CsvPath = "../../../../../samples/Sample.CsvServer/example";


    [Fact]
    public void UserData_MatchesCsvSource()
    {
        // Get all data from users table via Kusto
        using var reader = QueryProvider.ExecuteQuery("users");
        
        // Get data directly from CSV
        var csvData = _usersTable.GetData().First();
        
        // Compare row counts
        var rowCount = GetRowCount(reader);
        rowCount.Should().Be(csvData.RowCount);
        
        // Execute query again to read data
        using var readerForData = QueryProvider.ExecuteQuery("users | order by name asc");
        
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
        using var reader = QueryProvider.ExecuteQuery("events");
        
        // Get data directly from CSV
        var csvData = _eventsTable.GetData().First();
        
        // Compare row counts
        var rowCount = GetRowCount(reader);
        rowCount.Should().Be(csvData.RowCount);
        
        // Execute query again to read data
        using var readerForData = QueryProvider.ExecuteQuery("events | order by id asc");
        
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
        var nameCol = csvData.FindColumn<string>("name");
        var ageCol = csvData.FindColumn<long?>("age");
        var emailCol = csvData.FindColumn<string>("email");
        var isActiveCol = csvData.FindColumn<bool?>("is_active");
        
        // Create user objects from CSV columns
        for (int i = 0; i < csvData.RowCount; i++)
        {
            users.Add(new UserData
            {
                Name = nameCol[i]!,
                Age = ageCol[i] ?? 0,
                Email = emailCol[i]!,
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
        var idCol = csvData.FindColumn<long?>("id");
        var typeCol = csvData.FindColumn<string>("type");
        var sourceCol = csvData.FindColumn<string>("source");
        var severityCol = csvData.FindColumn<long?>("severity");

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

public static class TableExtensions
{
    public static Column<T> FindColumn<T>(this ITableChunk source, string columnName)
    {
        source.Table.Type.GetColumn(columnName);

        var colIdx = source.Table.Type.Columns.FirstIndex(c => c.Name == columnName);
        var col = source.Columns[colIdx];

        return (Column<T>)col;
    }
}