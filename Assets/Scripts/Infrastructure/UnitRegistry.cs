// This class provides a fast, centralized way to access units.
using System.Collections.Generic;
using FusionTask.Gameplay;

namespace FusionTask.Infrastructure
{
    /// <summary>
    /// A registry for all active units, indexed by their raw NetworkId value (uint).
    /// This provides a high-performance alternative to FindObjectsByType for looking up units.
    /// Units are responsible for registering themselves on spawn and unregistering on despawn.
    /// </summary>
    public class UnitRegistry : IUnitRegistry
    {
        private readonly Dictionary<uint, Unit> _units = new();
        /// <summary>
        /// Cached list of units for fast index-based iteration.
        /// </summary>
        private readonly List<Unit> _unitList = new();

        /// <summary>
        /// The collection of all active units.
        /// </summary>
        public IReadOnlyList<Unit> Units => _unitList;

        /// <summary>
        /// Registers the specified unit with the given NetworkId.
        /// </summary>
        public void Register(uint id, Unit unit)
        {
            _units[id] = unit;
            _unitList.Add(unit);
        }

        /// <summary>
        /// Removes the unit associated with the given NetworkId.
        /// </summary>
        public void Unregister(uint id)
        {
            if (_units.TryGetValue(id, out Unit unit))
            {
                _units.Remove(id);
                _unitList.Remove(unit);
            }
        }

        /// <summary>
        /// Attempts to retrieve a unit by its NetworkId.
        /// </summary>
        public bool TryGet(uint id, out Unit unit)
        {
            return _units.TryGetValue(id, out unit);
        }

        /// <summary>
        /// Clears the registry of all units.
        /// </summary>
        public void Clear()
        {
            _units.Clear();
            _unitList.Clear();
        }
    }
}
