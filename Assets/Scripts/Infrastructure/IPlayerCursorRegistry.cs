using System.Collections.Generic;
using Fusion;
using FusionTask.Gameplay;

namespace FusionTask.Infrastructure
{
    /// <summary>
    /// Provides access to active player cursors.
    /// </summary>
    public interface IPlayerCursorRegistry
    {
        /// <summary>
        /// All cursors currently tracked by the registry.
        /// </summary>
        IReadOnlyList<PlayerCursor> Cursors { get; }

        /// <summary>
        /// Registers a cursor for the specified player.
        /// </summary>
        void Register(PlayerRef player, PlayerCursor cursor);

        /// <summary>
        /// Removes the cursor associated with the specified player.
        /// </summary>
        void Unregister(PlayerRef player);

        /// <summary>
        /// Attempts to get the cursor for the specified player.
        /// </summary>
        bool TryGet(PlayerRef player, out PlayerCursor cursor);

        /// <summary>
        /// Clears the registry of all cursors.
        /// </summary>
        void Clear();
    }
}
