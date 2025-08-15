using System.Collections.Generic;

/// <summary>
/// Provides access to the collection of active units.
/// </summary>
public interface IUnitRegistry
{
    /// <summary>
    /// All units currently tracked by the registry.
    /// </summary>
    IReadOnlyList<Unit> Units { get; }

    /// <summary>
    /// Registers the specified unit with the given NetworkId.
    /// </summary>
    void Register(uint id, Unit unit);

    /// <summary>
    /// Removes the unit associated with the given NetworkId.
    /// </summary>
    void Unregister(uint id);

    /// <summary>
    /// Attempts to retrieve a unit by its NetworkId.
    /// </summary>
    bool TryGet(uint id, out Unit unit);

    /// <summary>
    /// Clears the registry of all units.
    /// </summary>
    void Clear();
}
