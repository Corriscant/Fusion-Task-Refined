using System.Collections.Generic;
using Fusion;

/// <summary>
/// A static registry for all active player cursors, indexed by PlayerRef.
/// Provides a fast way to look up a player's cursor.
/// </summary>
public static class PlayerCursorRegistry
{
    /// <summary>
    /// The dictionary holding all active cursors. Key is the player's PlayerRef, value is the cursor instance.
    /// </summary>
    public static readonly Dictionary<PlayerRef, PlayerCursor> Cursors = new();

    /// <summary>
    /// Registers a cursor for the specified player.
    /// </summary>
    public static void Register(PlayerRef player, PlayerCursor cursor)
    {
        Cursors[player] = cursor;
    }

    /// <summary>
    /// Removes the cursor associated with the specified player.
    /// </summary>
    public static void Unregister(PlayerRef player)
    {
        Cursors.Remove(player);
    }

    /// <summary>
    /// Clears the registry of all player cursors.
    /// </summary>
    public static void Clear()
    {
        Cursors.Clear();
    }

    /// <summary>
    /// Attempts to get the cursor for the specified player.
    /// </summary>
    public static bool TryGet(PlayerRef player, out PlayerCursor cursor)
    {
        return Cursors.TryGetValue(player, out cursor);
    }
}
