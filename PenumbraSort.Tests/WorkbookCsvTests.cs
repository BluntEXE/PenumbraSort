using System;
using System.Collections.Generic;
using System.IO;
using PenumbraSort.Csv;
using PenumbraSort.ModTree;
using Xunit;

namespace PenumbraSort.Tests;

public class WorkbookCsvTests
{
    [Fact]
    public void ExportThenImport_RoundTripsAgainstLiveModList()
    {
        var path = Path.Combine(Path.GetTempPath(), $"psort-csv-{Guid.NewGuid():N}.csv");
        try
        {
            var mods = new List<ModEntry>
            {
                new() { Directory = "d1", Name = "Mod, One", CurrentPath = "Old/Mod1", ProposedPath = "New/Mod1" },
                new() { Directory = "d2", Name = "Mod Two", CurrentPath = "Old/Mod2" },
            };

            WorkbookCsv.Export(path, mods);

            var liveList = new Dictionary<string, string> { ["d1"] = "Mod, One", ["d2"] = "Mod Two" };
            var result = WorkbookCsv.Import(path, liveList);

            Assert.Equal(2, result.Applied.Count);
            Assert.Empty(result.Skipped);
            Assert.Contains(result.Applied, r => r.ModDirectory == "d1" && r.ProposedPath == "New/Mod1");
            Assert.Contains(result.Applied, r => r.ModDirectory == "d2" && r.ProposedPath == "Old/Mod2");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Import_SkipsRowsNotMatchingLiveModList()
    {
        var path = Path.Combine(Path.GetTempPath(), $"psort-csv-{Guid.NewGuid():N}.csv");
        try
        {
            var mods = new List<ModEntry>
            {
                new() { Directory = "d1", Name = "Mod One", CurrentPath = "Old/Mod1" },
                new() { Directory = "d2", Name = "Mod Two", CurrentPath = "Old/Mod2" },
            };
            WorkbookCsv.Export(path, mods);

            // d2 renamed live since export, no longer matches
            var liveList = new Dictionary<string, string> { ["d1"] = "Mod One", ["d2"] = "Mod Two Renamed" };
            var result = WorkbookCsv.Import(path, liveList);

            Assert.Single(result.Applied);
            Assert.Single(result.Skipped);
            Assert.Equal("d1", result.Applied[0].ModDirectory);
            Assert.Equal("d2", result.Skipped[0].Row.ModDirectory);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ExportThenImport_RoundTripsFieldWithEmbeddedQuote()
    {
        var path = Path.Combine(Path.GetTempPath(), $"psort-csv-{Guid.NewGuid():N}.csv");
        try
        {
            var mods = new List<ModEntry>
            {
                new() { Directory = "d1", Name = "Mod \"Quoted\" Name", CurrentPath = "Old/Mod1" },
            };
            WorkbookCsv.Export(path, mods);

            var liveList = new Dictionary<string, string> { ["d1"] = "Mod \"Quoted\" Name" };
            var result = WorkbookCsv.Import(path, liveList);

            Assert.Single(result.Applied);
            Assert.Equal("Mod \"Quoted\" Name", result.Applied[0].ModName);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ExportThenImport_RoundTripsFieldWithCommaAndQuote()
    {
        var path = Path.Combine(Path.GetTempPath(), $"psort-csv-{Guid.NewGuid():N}.csv");
        try
        {
            var mods = new List<ModEntry>
            {
                new() { Directory = "d1", Name = "Mod, \"Special\" Edition", CurrentPath = "Old/Mod1" },
            };
            WorkbookCsv.Export(path, mods);

            var liveList = new Dictionary<string, string> { ["d1"] = "Mod, \"Special\" Edition" };
            var result = WorkbookCsv.Import(path, liveList);

            Assert.Single(result.Applied);
            Assert.Equal("Mod, \"Special\" Edition", result.Applied[0].ModName);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Import_HeaderOnlyFile_ReturnsEmptyResult()
    {
        var path = Path.Combine(Path.GetTempPath(), $"psort-csv-{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(path, "ModDirectory,ModName,ProposedPath\n");

            var result = WorkbookCsv.Import(path, new Dictionary<string, string>());

            Assert.Empty(result.Applied);
            Assert.Empty(result.Skipped);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Import_CompletelyEmptyFile_ReturnsEmptyResultWithoutThrowing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"psort-csv-{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(path, string.Empty);

            var result = WorkbookCsv.Import(path, new Dictionary<string, string>());

            Assert.Empty(result.Applied);
            Assert.Empty(result.Skipped);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // A row with the wrong field count (e.g. hand-edited CSV that drops a comma) is
    // routed into Skipped with a reason, same as a live-mod-list mismatch, so the user
    // gets a visible count instead of a row silently vanishing from both lists.
    [Fact]
    public void Import_MalformedRow_IsRoutedToSkippedWithReason()
    {
        var path = Path.Combine(Path.GetTempPath(), $"psort-csv-{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(path, "ModDirectory,ModName,ProposedPath\nd1,MissingThirdField\n");

            var result = WorkbookCsv.Import(path, new Dictionary<string, string> { ["d1"] = "MissingThirdField" });

            Assert.Empty(result.Applied);
            Assert.Single(result.Skipped);
            Assert.Equal("d1", result.Skipped[0].Row.ModDirectory);
            Assert.Contains("Malformed", result.Skipped[0].Reason);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ExportThenImport_RoundTripsTrailingEmptyField()
    {
        var path = Path.Combine(Path.GetTempPath(), $"psort-csv-{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(path, "ModDirectory,ModName,ProposedPath\nd1,Name,\n");

            var result = WorkbookCsv.Import(path, new Dictionary<string, string> { ["d1"] = "Name" });

            Assert.Single(result.Applied);
            Assert.Equal(string.Empty, result.Applied[0].ProposedPath);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // A stray, unescaped quote in the middle of a field (not produced by our own
    // Export, but possible if a user hand-edits the CSV) used to flip the parser into
    // quote mode mid-field, swallowing the closing quote and silently merging the
    // following comma into the field value instead of splitting on it. That produced
    // exactly 3 wrongly-merged fields, so the row parsed "successfully" with corrupted
    // data and no signal to the user. Quotes are now only treated as field delimiters
    // at the start of a field, so the same input instead splits into 4 raw fields and
    // is correctly caught by the field-count check and routed to Skipped - corruption
    // becomes a visible "malformed row", not silent bad data.
    [Fact]
    public void Import_StrayMidFieldQuote_DoesNotMergeFollowingFieldIntoOne()
    {
        var path = Path.Combine(Path.GetTempPath(), $"psort-csv-{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(path, "ModDirectory,ModName,ProposedPath\nd1,A\"B,C\"D,E\n");

            var result = WorkbookCsv.Import(path, new Dictionary<string, string> { ["d1"] = "AB,CD" });

            Assert.Empty(result.Applied);
            Assert.Single(result.Skipped);
            Assert.Contains("Malformed", result.Skipped[0].Reason);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
