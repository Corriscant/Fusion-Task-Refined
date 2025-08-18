using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using FusionTask.Gameplay;

namespace FusionTask.Infrastructure
{
    /// <summary>
    /// Factory interface for spawning and recycling networked game objects.
    /// </summary>
    public interface IGameFactory
    {
        /// <summary>
        /// Spawns a unit for the given owner at the specified position and rotation.
        /// </summary>
        Unit CreateUnit(NetworkRunner runner, Vector3 position, Quaternion rotation, PlayerRef owner);

        /// <summary>
        /// Asynchronously spawns a unit for the given owner at the specified position and rotation.
        /// Useful when assets require asynchronous loading.
        /// </summary>
        Task<Unit> CreateUnitAsync(NetworkRunner runner, Vector3 position, Quaternion rotation, PlayerRef owner);

        /// <summary>
        /// Spawns a cursor for the given owner at the specified position and rotation.
        /// </summary>
        PlayerCursor CreateCursor(NetworkRunner runner, Vector3 position, Quaternion rotation, PlayerRef owner);

        /// <summary>
        /// Asynchronously spawns a cursor for the given owner at the specified position and rotation.
        /// Useful when assets require asynchronous loading.
        /// </summary>
        Task<PlayerCursor> CreateCursorAsync(NetworkRunner runner, Vector3 position, Quaternion rotation, PlayerRef owner);

        /// <summary>
        /// Returns a network object back to its pool.
        /// </summary>
        void Release(NetworkObject obj);

        /// <summary>
        /// Preloads objects for all pools.
        /// </summary>
        Task WarmupPools();
    }
}

