using Fusion;
using UnityEngine;

/// <summary>
/// Networked cursor data for a player.
/// </summary>
public class PlayerCursor : NetworkBehaviour
{
    [Networked] public Vector3 CursorPosition { get; set; }
}

