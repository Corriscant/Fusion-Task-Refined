using Fusion;

namespace FusionTask.Gameplay
{
    /// <summary>
    /// Interface for objects that can be selected.
    /// </summary>
    public interface ISelectable
    {
        /// <summary>
        /// Indicates whether the object is currently selected.
        /// </summary>
        bool Selected { get; set; }

        /// <summary>
        /// Determines whether the object can be selected by the specified player.
        /// </summary>
        /// <param name="player">Player attempting the selection.</param>
        /// <returns>True if selection is allowed, otherwise false.</returns>
        bool CanBeSelectedBy(PlayerRef player);
    }
}
