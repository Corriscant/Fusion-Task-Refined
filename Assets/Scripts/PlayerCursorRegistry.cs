using System.Collections.Generic;
using Fusion;

/// <summary>
/// A registry for all active player cursors, indexed by PlayerRef.
/// Provides a fast way to look up a player's cursor.
/// </summary>
public class PlayerCursorRegistry : IPlayerCursorRegistry
{
    private readonly Dictionary<PlayerRef, PlayerCursor> _cursors = new();

    /// <summary>
    /// All active cursors.
    /// </summary>
    public IEnumerable<PlayerCursor> Cursors => _cursors.Values;

    /// <summary>
    /// Registers a cursor for the specified player.
    /// </summary>
    public void Register(PlayerRef player, PlayerCursor cursor)
    {
        _cursors[player] = cursor;
    }

    /// <summary>
    /// Removes the cursor associated with the specified player.
    /// </summary>
    public void Unregister(PlayerRef player)
    {
        _cursors.Remove(player);
    }

    /// <summary>
    /// Attempts to get the cursor for the specified player.
    /// </summary>
    public bool TryGet(PlayerRef player, out PlayerCursor cursor)
    {
        return _cursors.TryGetValue(player, out cursor);
    }

    /// <summary>
    /// Clears the registry of all player cursors.
    /// </summary>
    public void Clear()
    {
        _cursors.Clear();
    }
}
