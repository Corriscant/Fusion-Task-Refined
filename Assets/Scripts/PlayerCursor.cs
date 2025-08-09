using Fusion;
using UnityEngine;

/// <summary>
/// Networked cursor data for a player.
/// </summary>
public class PlayerCursor : NetworkBehaviour
{
    [Networked] public Vector3 CursorPosition { get; set; }

    public override void Spawned()
    {
        base.Spawned();
        // Register this cursor with the PlayerManager when it spawns.
        FindObjectOfType<PlayerManager>()?.RegisterPlayerCursor(Object.InputAuthority, this);
    }
}

