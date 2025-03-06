using BabyKusto.Core.DataSource;
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

        Assert.Equal("test_schema", table.Name);
        Assert.Equal(4, table.Columns.Count);

        Assert.Equal("name", table.Columns[0].Name);
        Assert.Equal(typeof(string), table.Columns[0].Type);

        Assert.Equal("age", table.Columns[1].Name);
        Assert.Equal(typeof(long), table.Columns[1].Type);

        Assert.Equal("active", table.Columns[2].Name);
        Assert.Equal(typeof(bool), table.Columns[2].Type);

        Assert.Equal("timestamp", table.Columns[3].Name);
        Assert.Equal(typeof(DateTime), table.Columns[3].Type);
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
        Assert.Equal("Alice", chunk.GetValue<string>(0, 0));
        Assert.Equal(123L, chunk.GetValue<long>(0, 1));
        
        // Second row
        Assert.Equal("Bob", chunk.GetValue<string>(1, 0));
        Assert.Equal(456L, chunk.GetValue<long>(1, 1));
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

        Assert.Equal("John", chunk.GetValue<string>(0, 0));
        Assert.Null(chunk.GetValue<long?>(0, 1));
        Assert.Null(chunk.GetValue<string>(1, 0));
        Assert.Equal(30L, chunk.GetValue<long>(1, 1));
    }
}