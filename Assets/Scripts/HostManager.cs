using Fusion;
using UnityEngine;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;

/// <summary>
/// Legacy class previously used for host-side command processing.
/// Kept for reference but no longer active in the simulation.
/// </summary>
public class HostManager : NetworkBehaviour
{
    NetworkRunner NetRunner => ConnectionManager.Instance.NetRunner;

    public override void Spawned()
    {
        Log($"{GetLogCallPrefix(GetType())} HostManager {gameObject.name} spawned.");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Log($"{GetLogCallPrefix(GetType())} HostManager {gameObject.name} despawned. HasState: {hasState}");
        base.Despawned(runner, hasState);
    }

}
