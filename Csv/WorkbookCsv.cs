using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PenumbraSort.ModTree;

namespace PenumbraSort.Csv;

public static class WorkbookCsv
{
    private const string Header = "ModDirectory,ModName,ProposedPath";

    public static void Export(string filePath, IEnumerable<ModEntry> mods)
    {
        using var writer = new StreamWriter(filePath);
        writer.WriteLine(Header);

        foreach (var mod in mods)
        {
            var path = mod.ProposedPath ?? mod.CurrentPath;
            writer.WriteLine($"{EscapeCsv(mod.Directory)},{EscapeCsv(mod.Name)},{EscapeCsv(path)}");
        }
    }

    public sealed record ImportRow(string ModDirectory, string ModName, string ProposedPath);

    public sealed record ImportResult(
        IReadOnlyList<ImportRow> Applied,
        IReadOnlyList<(ImportRow Row, string Reason)> Skipped);

    public static ImportResult Import(string filePath, IReadOnlyDictionary<string, string> liveModList)
    {
        var lines = File.ReadAllLines(filePath);
        var applied = new List<ImportRow>();
        var skipped = new List<(ImportRow, string)>();

        foreach (var line in lines.Skip(1))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var fields = ParseCsvLine(line);
            if (fields.Length != 3)
                continue;

            var row = new ImportRow(fields[0], fields[1], fields[2]);

            if (!liveModList.TryGetValue(row.ModDirectory, out var liveName) || liveName != row.ModName)
            {
                skipped.Add((row, "Mod no longer matches live Penumbra state"));
                continue;
            }

            applied.Add(row);
        }

        return new ImportResult(applied, skipped);
    }

    private static string EscapeCsv(string value)
        => value.Contains(',') || value.Contains('"')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;

    private static string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                if (c == '"')
                    inQuotes = true;
                else if (c == ',')
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                    current.Append(c);
            }
        }

        result.Add(current.ToString());
        return result.ToArray();
    }
}
