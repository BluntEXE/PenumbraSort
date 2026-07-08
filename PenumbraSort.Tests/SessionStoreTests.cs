using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PenumbraSort.Session;
using Xunit;

namespace PenumbraSort.Tests;

public class SessionStoreTests
{
    [Fact]
    public void SaveThenLoad_RoundTripsProposal()
    {
        var path = Path.Combine(Path.GetTempPath(), $"psort-test-{Guid.NewGuid():N}.json");
        try
        {
            var proposal = new Dictionary<string, string?>
            {
                ["d1"] = "New/Path",
                ["d2"] = null,
            };

            SessionStore.Save(path, proposal);
            var loaded = SessionStore.Load(path);

            Assert.Equal("New/Path", loaded["d1"]);
            Assert.Null(loaded["d2"]);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsEmptyDictionary()
    {
        var result = SessionStore.Load(Path.Combine(Path.GetTempPath(), $"psort-missing-{Guid.NewGuid():N}.json"));

        Assert.Empty(result);
    }

    [Fact]
    public void Load_MalformedJson_Throws()
    {
        var path = Path.Combine(Path.GetTempPath(), $"psort-bad-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ not valid json");
        try
        {
            Assert.Throws<JsonException>(() => SessionStore.Load(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Save_TargetDirectoryMissing_Throws()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"psort-missing-dir-{Guid.NewGuid():N}");
        var path = Path.Combine(dir, "session.json");

        Assert.Throws<DirectoryNotFoundException>(
            () => SessionStore.Save(path, new Dictionary<string, string?>()));
    }
}
