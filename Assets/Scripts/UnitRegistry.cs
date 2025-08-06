// This class provides a fast, centralized way to access units.
using Fusion;
using System.Collections.Generic;

/// <summary>
/// A static registry for all active Unit instances, indexed by their raw NetworkId value (uint).
/// This provides a high-performance alternative to FindObjectsByType for looking up units.
/// Units are responsible for registering themselves on spawn and unregistering on despawn.
/// </summary>
public static class UnitRegistry
{
    /// <summary>
    /// The dictionary holding all active units. Key is the unit's raw NetworkId (uint), Value is the selectable component reference.
    /// </summary>
    public static readonly Dictionary<uint, ISelectable> Units = new();
}