using Fusion;
using System;
using Unity.VisualScripting;
using UnityEngine;

public class HostManager : NetworkBehaviour
{
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        // Call the spawner to process commands
        if (BasicSpawner.Instance != null)
        {
            BasicSpawner.Instance.HostProcessCommandsFromNetwork();
        }
    }

}
