using UnityEngine;

namespace FusionTask.Gameplay
{
    /// <summary>
    /// Intersface for objects that have a position.
    /// </summary>
    public interface IPositionable
    {
        Vector3 Position { get; }
    }
}
