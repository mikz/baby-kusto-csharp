using BabyKusto.Core;
using Kusto.Language.Symbols;
using Xunit;

namespace BabyKusto.SampleCsvServer.Tests;

public class CsvTableSourceTests
{
    private const string TestDataDir = "TestData";

    [Fact]
    public void LoadValidCsvFile_CreatesCorrectSchema()
    {
        // Create test CSV file
        var csvPath = Path.Combine(TestDataDir, "test_schema.csv");
        Directory.CreateDirectory(TestDataDir);
        File.WriteAllText(csvPath, @"name:string,age:long,active:bool,timestamp:datetime
John,30,true,2024-01-01T10:00:00Z");

        var table = new CsvTableSource(csvPath);

        Assert.Equal("test_schema", table.Type.Name);
        Assert.Equal(4, table.Type.Columns.Count);

        // Check each column's name and type
        var cols = table.Type.Columns;
        Assert.Equal("name", cols[0].Name);
        Assert.Equal(ScalarTypes.String.Name, cols[0].Type.Name);

        Assert.Equal("age", cols[1].Name);
        Assert.Equal(ScalarTypes.Long.Name, cols[1].Type.Name);

        Assert.Equal("active", cols[2].Name);
        Assert.Equal(ScalarTypes.Bool.Name, cols[2].Type.Name);

        Assert.Equal("timestamp", cols[3].Name);
        Assert.Equal(ScalarTypes.DateTime.Name, cols[3].Type.Name);
    }

    [Fact]
    public void LoadValidCsvFile_ParsesDataCorrectly()
    {
        var csvPath = Path.Combine(TestDataDir, "test_data.csv");
        Directory.CreateDirectory(TestDataDir);
        File.WriteAllText(csvPath, @"name:string,value:long
Alice,123
Bob,456");

        var table = new CsvTableSource(csvPath);
        var chunks = table.GetData().ToList();

        Assert.Single(chunks);
        var chunk = chunks[0];
        
        Assert.Equal(2, chunk.RowCount);
        
        // First row
        var nameCol = (Column<string>)chunk.Columns[0];
        var valueCol = (Column<long?>)chunk.Columns[1];

        Assert.Equal("Alice", nameCol[0]);
        Assert.Equal(123L, valueCol[0]);
        
        Assert.Equal("Bob", nameCol[1]);
        Assert.Equal(456L, valueCol[1]);
    }

    [Fact]
    public void LoadInvalidCsvHeader_ThrowsException()
    {
        var csvPath = Path.Combine(TestDataDir, "invalid_header.csv");
        Directory.CreateDirectory(TestDataDir);
        File.WriteAllText(csvPath, "invalid header format\nsome data");

        var ex = Assert.Throws<InvalidDataException>(() => new CsvTableSource(csvPath));
        Assert.Contains("Invalid column definition", ex.Message);
    }

    [Fact]
    public void LoadCsvWithInvalidType_ThrowsException()
    {
        var csvPath = Path.Combine(TestDataDir, "invalid_type.csv");
        Directory.CreateDirectory(TestDataDir);
        File.WriteAllText(csvPath, "name:invalid_type\nsome data");

        var ex = Assert.Throws<NotSupportedException>(() => new CsvTableSource(csvPath));
        Assert.Contains("Unsupported Kusto type", ex.Message);
    }

    [Fact]
    public void LoadCsvWithMismatchedColumns_ThrowsException()
    {
        var csvPath = Path.Combine(TestDataDir, "mismatched_columns.csv");
        Directory.CreateDirectory(TestDataDir);
        File.WriteAllText(csvPath, @"name:string,age:long
John,30,extra");

        var table = new CsvTableSource(csvPath);
        var ex = Assert.Throws<InvalidDataException>(() => table.GetData().ToList());
        Assert.Contains("CSV row has", ex.Message);
    }

    [Fact]
    public void LoadCsvWithEmptyFile_ThrowsException()
    {
        var csvPath = Path.Combine(TestDataDir, "empty.csv");
        Directory.CreateDirectory(TestDataDir);
        File.WriteAllText(csvPath, "");

        var ex = Assert.Throws<InvalidDataException>(() => new CsvTableSource(csvPath));
        Assert.Contains("empty or has no header", ex.Message);
    }

    [Fact]
    public void LoadCsvWithNullValues_HandlesCorrectly()
    {
        var csvPath = Path.Combine(TestDataDir, "null_values.csv");
        Directory.CreateDirectory(TestDataDir);
        File.WriteAllText(csvPath, @"name:string,age:long
John,
,30");

        var table = new CsvTableSource(csvPath);
        var chunks = table.GetData().ToList();
        var chunk = chunks[0];

        var nameCol = (Column<string>)chunk.Columns[0];
        var valueCol = (Column<long?>)chunk.Columns[1];

        Assert.Equal("John", nameCol[0]);
        Assert.Null(valueCol[0]);
        Assert.Null(nameCol[1]);
        Assert.Equal(30L, valueCol[1]);
    }
}