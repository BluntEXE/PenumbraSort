using System;
using System.Reflection;

namespace PenumbraSort.Sorting;

/// <summary>
/// Reads the real equipment/customization slot out of the boxed value objects Penumbra's
/// GetChangedItems IPC call returns, via reflection (Penumbra's internal types -
/// Penumbra.GameData.Structs.EquipItem, Penumbra.GameData.Data.IdentifiedCustomization - are
/// not part of the public Penumbra.Api package we depend on, so we can't reference them at
/// compile time, but since the plugin runs in the same process we can inspect the live object).
///
/// Penumbra itself marks unmapped equipment with FullEquipType.Unknown as an explicit
/// placeholder - TryResolve treats that (and anything else it can't confidently read) as
/// "no answer" (null) rather than guessing, so callers fall back to the name-based keyword
/// classifier instead of trusting a bogus category.
/// </summary>
public static class PenumbraSlotResolver
{
    public static ChangedItemCategory? TryResolve(object? changedItemValue)
    {
        if (changedItemValue is null)
            return null;

        var valueType = changedItemValue.GetType();

        // IdentifiedItem-shaped: public field "Item" of type EquipItem, itself has a public
        // field "Type" of type FullEquipType.
        var itemField = valueType.GetField("Item", BindingFlags.Public | BindingFlags.Instance);
        if (itemField is not null)
        {
            var equipItem = itemField.GetValue(changedItemValue);
            var equipTypeField = equipItem?.GetType().GetField("Type", BindingFlags.Public | BindingFlags.Instance);
            if (equipTypeField?.GetValue(equipItem) is Enum equipType)
                return MapFullEquipTypeName(equipType.ToString());
        }

        // IdentifiedCustomization-shaped: public field "Type" of type CustomizeIndex.
        var customizeTypeField = valueType.GetField("Type", BindingFlags.Public | BindingFlags.Instance);
        if (customizeTypeField is not null && customizeTypeField.FieldType.Name == "CustomizeIndex")
        {
            if (customizeTypeField.GetValue(changedItemValue) is Enum customizeType)
                return MapCustomizeIndexName(customizeType.ToString());
        }

        // Some other Identified* variant (Model/Name/Counter/Action/Emote) - none of these
        // map to an equipment or customization slot, so there's nothing reliable to classify.
        return null;
    }

    /// <summary>
    /// FullEquipType has ~50 weapon/tool subtypes (Sword, Axe, Bow, Pickaxe, ...) that Penumbra's
    /// own ToSlot() extension all maps to MainHand/OffHand - rather than enumerate every one,
    /// anything not in the explicit non-weapon allowlist below defaults to Weapon.
    /// </summary>
    private static ChangedItemCategory? MapFullEquipTypeName(string? name) => name switch
    {
        null => null,
        "Unknown" => null,
        "Head" => ChangedItemCategory.Head,
        "Body" => ChangedItemCategory.Top,
        "Hands" => ChangedItemCategory.Hands,
        "Legs" => ChangedItemCategory.Bottom,
        "Feet" => ChangedItemCategory.Shoes,
        "Ears" or "Neck" or "Wrists" or "Finger" or "Glasses" => ChangedItemCategory.Accessory,
        _ => ChangedItemCategory.Weapon,
    };

    private static ChangedItemCategory? MapCustomizeIndexName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return null;

        if (name.Contains("Hair", StringComparison.OrdinalIgnoreCase))
            return ChangedItemCategory.Hair;

        if (name.Contains("Face", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Eye", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Lip", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Nose", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Jaw", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Mouth", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Eyebrow", StringComparison.OrdinalIgnoreCase)
            || name.Contains("FacialFeature", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Tattoo", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Paint", StringComparison.OrdinalIgnoreCase))
            return ChangedItemCategory.Face;

        return ChangedItemCategory.Body;
    }
}
