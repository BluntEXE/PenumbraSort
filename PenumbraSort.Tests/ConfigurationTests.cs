using PenumbraSort;
using PenumbraSort.Sorting;
using Xunit;

namespace PenumbraSort.Tests;

public class ConfigurationTests
{
    [Fact]
    public void NewConfiguration_HasCurrentVersionAndEmptyProtectedSet()
    {
        var config = new Configuration();

        Assert.Equal(1, config.Version);
        Assert.Empty(config.ProtectedModDirectories);
        Assert.Equal(SortStrategyKind.Manual, config.LastStrategy);
    }

    [Fact]
    public void ProtectedModDirectories_RoundTripsThroughProtectionStore()
    {
        var config = new Configuration();
        config.ProtectedModDirectories.Add("d1");
        config.ProtectedModDirectories.Add("d2");

        var store = new PenumbraSort.Protection.ProtectionStore();
        store.LoadFrom(config.ProtectedModDirectories);

        Assert.True(store.IsProtected("d1"));
        Assert.True(store.IsProtected("d2"));
        Assert.False(store.IsProtected("d3"));
    }

    [Fact]
    public void LastStrategy_IsMutable()
    {
        var config = new Configuration { LastStrategy = SortStrategyKind.ByCreator };

        Assert.Equal(SortStrategyKind.ByCreator, config.LastStrategy);
    }

    [Fact]
    public void Save_WithoutInitialize_DoesNotThrow()
    {
        // Known limitation: Save() cannot be fully exercised without a real
        // IDalamudPluginInterface (no test double available for this Dalamud type).
        // This only verifies the no-op path when Initialize() was never called.
        var config = new Configuration();

        var exception = Record.Exception(() => config.Save());

        Assert.Null(exception);
    }
}
