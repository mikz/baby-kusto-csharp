﻿// See https://aka.ms/new-console-template for more information
using KustoExecutionEngine.Core;

var query = @"
MyTable
| project a
";

var playground = new ParserPlayground();

playground.DumpTree(query);


var engine = new StirlingEngine();
var result = engine.Evaluate(query);
if (result is ITabularSource tabularResult)
{
    IRow? row;
    while ((row = tabularResult.GetNextRow()) != null)
    {
        Console.WriteLine(string.Join(", ", row.Select(r => $"{r.Key}={r.Value}")));
    }
}

Console.WriteLine("Done");
