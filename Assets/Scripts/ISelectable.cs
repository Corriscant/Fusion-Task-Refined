using Fusion;

/// <summary>
/// Interface for objects that can be selected.
/// </summary>
public interface ISelectable : IPositionable
{
    /// <summary>
    /// Indicates whether the object is currently selected.
    /// </summary>
    bool Selected { get; set; }

    /// <summary>
    /// Checks if the object belongs to the specified player.
    /// </summary>
    /// <param name="player">Player to check ownership against.</param>
    /// <returns>True if owned by the player, otherwise false.</returns>
    bool IsOwnedBy(PlayerRef player);
}
