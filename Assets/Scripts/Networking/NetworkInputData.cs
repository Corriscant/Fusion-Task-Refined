using Fusion;
using UnityEngine;

/// <summary>
/// Network input payload sent every simulation tick.
/// Currently holds only the player’s mouse position in world space so other clients can visualize it.
/// </summary>
public struct NetworkInputData : INetworkInput
{
    // The mouse world position is used to show opponent's mouse position in the game world.
    public Vector3 mouseWorldPosition;
}


