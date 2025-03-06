using Kusto.Language.Symbols;
using Xunit;

namespace Sample.CsvServer.Tests;

public class CsvTablesProviderTests
{
    private const string TestDataDir = "TestData";

    [Fact]
    public void LoadMultipleCsvFiles_CreatesCorrectTables()
    {
        // Create test CSV files
        Directory.CreateDirectory(TestDataDir);
        
        var users = Path.Combine(TestDataDir, "users.csv");
        File.WriteAllText(users, @"name:string,age:long
Alice,30
Bob,25");

        var events = Path.Combine(TestDataDir, "events.csv");
        File.WriteAllText(events, @"id:long,type:string
1,login
2,logout");

        var provider = new CsvTablesProvider(new[] { users, events });
        var tables = provider.GetTables();

        Assert.Equal(2, tables.Count);

        // Verify users table
        var usersTable = tables.First(t => t.Type.Name == "users");
        Assert.Equal(2, usersTable.Type.Columns.Count);
        Assert.Collection(usersTable.Type.Columns,
            c => { Assert.Equal("name", c.Name); Assert.Equal(ScalarTypes.String.Name, c.Type.Name); },
            c => { Assert.Equal("age", c.Name); Assert.Equal(ScalarTypes.Long.Name, c.Type.Name); }
        );

        // Verify events table
        var eventsTable = tables.First(t => t.Type.Name == "events");
        Assert.Equal(2, eventsTable.Type.Columns.Count);
        Assert.Collection(eventsTable.Type.Columns,
            c => { Assert.Equal("id", c.Name); Assert.Equal(ScalarTypes.Long.Name, c.Type.Name); },
            c => { Assert.Equal("type", c.Name); Assert.Equal(ScalarTypes.String.Name, c.Type.Name); }
        );
    }

    [Fact]
    public void LoadNoValidCsvFiles_ThrowsException()
    {
        Directory.CreateDirectory(TestDataDir);
        var invalidCsv = Path.Combine(TestDataDir, "invalid.csv");
        File.WriteAllText(invalidCsv, "invalid content");

        var ex = Assert.Throws<InvalidOperationException>(() => 
            new CsvTablesProvider(new[] { invalidCsv }));
        Assert.Contains("No valid CSV tables were loaded", ex.Message);
    }

    [Fact]
    public void LoadMixedValidAndInvalidFiles_LoadsOnlyValidFiles()
    {
        Directory.CreateDirectory(TestDataDir);
        
        var validCsv = Path.Combine(TestDataDir, "valid.csv");
        File.WriteAllText(validCsv, @"name:string
Alice");

        var invalidCsv = Path.Combine(TestDataDir, "invalid.csv");
        File.WriteAllText(invalidCsv, "invalid content");

        var provider = new CsvTablesProvider(new[] { validCsv, invalidCsv });
        var tables = provider.GetTables();

        Assert.Single(tables);
        Assert.Equal("valid", tables[0].Type.Name);
    }

    [Fact]
    public void LoadEmptyFilesList_ThrowsException()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => 
            new CsvTablesProvider(Array.Empty<string>()));
        Assert.Contains("No valid CSV tables were loaded", ex.Message);
    }

    [Fact]
    public void LoadDuplicateTableNames_UsesLastOne()
    {
        Directory.CreateDirectory(TestDataDir);
        
        var csv1 = Path.Combine(TestDataDir, "data.csv");
        File.WriteAllText(csv1, @"col1:string
value1");

        var csv2 = Path.Combine(TestDataDir, "data.csv");
        File.WriteAllText(csv2, @"col2:string
value2");

        var provider = new CsvTablesProvider(new[] { csv1, csv2 });
        var tables = provider.GetTables();

        Assert.Single(tables);
        Assert.Equal("data", tables[0].Type.Name);
        Assert.Equal("col2", tables[0].Type.Columns[0].Name);
    }
}