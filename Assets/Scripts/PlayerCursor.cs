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
        PlayerCursorRegistry.Register(Object.InputAuthority, this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        PlayerCursorRegistry.Unregister(Object.InputAuthority);
        base.Despawned(runner, hasState);
    }
}
