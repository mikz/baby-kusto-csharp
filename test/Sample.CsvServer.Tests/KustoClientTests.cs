using FluentAssertions;
using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;
using Xunit;

namespace Sample.CsvServer.Tests;

public class KustoClientTests(ServerFixture fixture) : CsvServerTestBase(fixture)
{
    [Fact]
    public void Client_CanQueryUsers()
    {
        // Execute a query against the users table
        using var reader = QueryProvider.ExecuteQuery("users | project name, age");
        
        // Verify we can read data
        reader.Should().NotBeNull();
        
        // Get column information
        reader.Read().Should().BeTrue();
        
        // Check column names
        reader.GetName(0).Should().Be("name");
        reader.GetName(1).Should().Be("age");
        
        // We should have at least the three users from our sample file
        var userCount = 1; // Already read one row
        while (reader.Read())
        {
            userCount++;
        }
        
        userCount.Should().BeGreaterOrEqualTo(3);
    }
    
    [Fact]
    public void Client_CanQueryWithFilter()
    {
        // Query with a filter
        using var reader = QueryProvider.ExecuteQuery("users | where age > 25 | project name, age");
        
        // Read all users over 25
        var usersOver25 = new List<(string name, long age)>();
        
        while (reader.Read())
        {
            var name = reader.GetString(0);
            var age = reader.GetInt64(1);
            usersOver25.Add((name, age));
        }
        
        // Should include "John Doe" (30) and "Bob Wilson" (45) but not "Jane Smith" (25)
        usersOver25.Should().Contain(u => u.name == "John Doe" && u.age == 30);
        usersOver25.Should().Contain(u => u.name == "Bob Wilson" && u.age == 45);
        usersOver25.Should().NotContain(u => u.name == "Jane Smith");
    }
    
    [Fact]
    public void Client_CanQueryEventsTable()
    {
        // Query the events table
        using var reader = QueryProvider.ExecuteQuery("events | where severity > 2 | project id, type, severity");
        
        // There should be at least one high severity event (severity > 2)
        reader.Read().Should().BeTrue();
        
        // It should be the error event from our sample data (id=3, type=error, severity=3)
        reader.GetInt64(0).Should().Be(3);
        reader.GetString(1).Should().Be("error");
        reader.GetInt64(2).Should().Be(3);
    }
    
    [Fact]
    public void Client_CanJoinTables()
    {
        // Get a count of events per user using timestamp correlation
        var query = @"
            events 
            | project source, timestamp
            | join (users | project name, registered) on $left.timestamp == $right.registered
            | count
        ";
        
        using var reader = QueryProvider.ExecuteQuery(query);
        
        // We should be able to execute a join query
        reader.Should().NotBeNull();
    }
}