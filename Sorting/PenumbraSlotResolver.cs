using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace PenumbraSort.Sorting;

/// <summary>
/// Reads the real equipment/customization slot out of the boxed value objects Penumbra's
/// GetChangedItems IPC call returns, via reflection (Penumbra's internal types -
/// Penumbra.GameData.Structs.EquipItem, Penumbra.GameData.Data.IdentifiedCustomization - are
/// not part of the public Penumbra.Api package we depend on, so we can't reference them at
/// compile time, but since the plugin runs in the same process we can inspect the live object).
///
/// IIdentifiedObjectData.ToInternalObject() unwraps before we ever see it: for equipment it
/// returns the raw EquipItem struct directly (its "Type" field, of type FullEquipType, is at
/// the top level - there is no wrapping "Item" field), and for customization it returns a raw
/// ValueTuple (Race, Gender, Type, Value) where "Type" is positionally element 3 (a
/// CustomizeIndex), accessed via ITuple rather than a named field.
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
        switch (changedItemValue)
        {
            case null:
                return null;

            // IdentifiedCustomization.ToInternalObject() -> (Race, Gender, Type, Value).
            case ITuple { Length: 4 } tuple when tuple[2] is Enum customizeType
                && customizeType.GetType().Name == "CustomizeIndex":
                return MapCustomizeIndexName(customizeType.ToString());

            default:
                // IdentifiedItem.ToInternalObject() -> the raw EquipItem struct, whose public
                // "Type" field (FullEquipType) sits at the top level of the boxed value.
                var typeField = changedItemValue.GetType().GetField("Type", BindingFlags.Public | BindingFlags.Instance);
                if (typeField?.FieldType.Name == "FullEquipType" && typeField.GetValue(changedItemValue) is Enum equipType)
                    return MapFullEquipTypeName(equipType.ToString());

                // Some other Identified* variant (Model/Name/Counter/Action/Emote) - none of
                // these map to an equipment or customization slot, so there's nothing reliable
                // to classify.
                return null;
        }
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
