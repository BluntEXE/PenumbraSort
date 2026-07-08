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

    // Design decision (flagged, not silently changed): a row with the wrong field
    // count (e.g. hand-edited CSV that drops a comma) is dropped entirely - it shows
    // up in neither Applied nor Skipped. This differs from the live-mod-list mismatch
    // case, which is surfaced via Skipped with a reason. A user who mangles the CSV by
    // hand gets no feedback that a row silently vanished. If that's undesirable, malformed
    // rows should also be routed into Skipped with a "malformed row" reason.
    [Fact]
    public void Import_MalformedRow_IsSilentlyDroppedNotReportedAsSkipped()
    {
        var path = Path.Combine(Path.GetTempPath(), $"psort-csv-{Guid.NewGuid():N}.csv");
        try
        {
            File.WriteAllText(path, "ModDirectory,ModName,ProposedPath\nd1,MissingThirdField\n");

            var result = WorkbookCsv.Import(path, new Dictionary<string, string> { ["d1"] = "MissingThirdField" });

            Assert.Empty(result.Applied);
            Assert.Empty(result.Skipped);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
