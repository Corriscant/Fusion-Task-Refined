// This class provides a fast, centralized way to access units.
using System.Collections.Generic;

/// <summary>
/// A static registry for all active units, indexed by their raw NetworkId value (uint).
/// This provides a high-performance alternative to FindObjectsByType for looking up units.
/// Units are responsible for registering themselves on spawn and unregistering on despawn.
/// </summary>
public static class UnitRegistry
{
    /// <summary>
    /// The dictionary holding all active units. Key is the unit's raw NetworkId (uint), Value is the unit reference.
    /// </summary>
    public static readonly Dictionary<uint, Unit> Units = new();

    /// <summary>
    /// Clears the registry of all units.
    /// </summary>
    public static void Clear()
    {
        Units.Clear();
    }
}
