using PenumbraSort.Sorting;
using Xunit;

namespace PenumbraSort.Tests;

/// <summary>
/// PenumbraSlotResolver reflects into objects shaped like Penumbra's real internal types
/// rather than referencing them directly (they're not in the public Penumbra.Api package).
/// These fakes are duck-typed to match exactly what IIdentifiedObjectData.ToInternalObject()
/// actually returns (verified against the real installed Penumbra.GameData.dll) - a raw
/// struct with a public "Type" field named "FullEquipType" for equipment, or a raw 4-tuple
/// (Race, Gender, Type, Value) for customization, where "Type" is a "CustomizeIndex".
/// If these fakes ever drift from Penumbra's real shape, this file catches it before an
/// in-game test does - which is exactly the gap that let a completely broken resolver ship
/// with 75/75 tests green (every existing test used null values, never exercising this path).
/// </summary>
public class PenumbraSlotResolverTests
{
    public enum FullEquipType
    {
        Unknown,
        Head,
        Body,
        Hands,
        Legs,
        Feet,
        Ears,
        Neck,
        Wrists,
        Finger,
        Sword,
    }

    public enum CustomizeIndex
    {
        Hairstyle,
        HairColor,
        Face,
        EyeColorRight,
        SkinColor,
    }

    // A genuine public field, not an auto-property - the real Penumbra.GameData.Structs.EquipItem
    // declares "public readonly FullEquipType Type;" as a plain field, and PenumbraSlotResolver
    // uses GetField (not GetProperty), so this must match that shape exactly.
    private readonly struct FakeEquipItem
    {
        public readonly FullEquipType Type;

        public FakeEquipItem(FullEquipType type) => Type = type;
    }

    [Fact]
    public void TryResolve_Null_ReturnsNull()
    {
        Assert.Null(PenumbraSlotResolver.TryResolve(null));
    }

    [Theory]
    [InlineData(FullEquipType.Head, ChangedItemCategory.Head)]
    [InlineData(FullEquipType.Body, ChangedItemCategory.Top)]
    [InlineData(FullEquipType.Hands, ChangedItemCategory.Hands)]
    [InlineData(FullEquipType.Legs, ChangedItemCategory.Bottom)]
    [InlineData(FullEquipType.Feet, ChangedItemCategory.Shoes)]
    [InlineData(FullEquipType.Ears, ChangedItemCategory.Accessory)]
    [InlineData(FullEquipType.Neck, ChangedItemCategory.Accessory)]
    [InlineData(FullEquipType.Wrists, ChangedItemCategory.Accessory)]
    [InlineData(FullEquipType.Finger, ChangedItemCategory.Accessory)]
    [InlineData(FullEquipType.Sword, ChangedItemCategory.Weapon)]
    public void TryResolve_EquipItemShape_MapsToExpectedCategory(FullEquipType type, ChangedItemCategory expected)
    {
        var equipItem = new FakeEquipItem(type);

        var result = PenumbraSlotResolver.TryResolve(equipItem);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryResolve_EquipItemUnknown_ReturnsNullRatherThanGuessing()
    {
        var equipItem = new FakeEquipItem(FullEquipType.Unknown);

        var result = PenumbraSlotResolver.TryResolve(equipItem);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(CustomizeIndex.Hairstyle, ChangedItemCategory.Hair)]
    [InlineData(CustomizeIndex.HairColor, ChangedItemCategory.Hair)]
    [InlineData(CustomizeIndex.Face, ChangedItemCategory.Face)]
    [InlineData(CustomizeIndex.EyeColorRight, ChangedItemCategory.Face)]
    [InlineData(CustomizeIndex.SkinColor, ChangedItemCategory.Body)]
    public void TryResolve_CustomizationTupleShape_MapsToExpectedCategory(CustomizeIndex index, ChangedItemCategory expected)
    {
        // Mirrors IdentifiedCustomization.ToInternalObject() -> (Race, Gender, Type, Value).
        var value = (Race: 0, Gender: 0, Type: index, Value: 0);

        var result = PenumbraSlotResolver.TryResolve(value);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryResolve_UnrecognizedShape_ReturnsNull()
    {
        var result = PenumbraSlotResolver.TryResolve("just a plain string, not a real Identified* payload");

        Assert.Null(result);
    }
}
