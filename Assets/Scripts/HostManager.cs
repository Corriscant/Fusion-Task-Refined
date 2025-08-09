using Fusion;
using System.Collections.Generic;
using UnityEngine;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;

/// <summary>
/// Manages host-side command processing in a Fusion network game by gathering
/// player inputs, queuing them, and dispatching
/// time-ordered commands to the appropriate units under state authority.
/// </summary>
public class HostManager : NetworkBehaviour
{
    NetworkRunner NetRunner => ConnectionManager.Instance.NetRunner;
    private Dictionary<PlayerRef, GameObject> _cursorEchos => ConnectionManager.Instance.CursorEchos;

    public override void Spawned()
    {
        Log($"{GetLogCallPrefix(GetType())} HostManager {gameObject.name} spawned.");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Log($"{GetLogCallPrefix(GetType())} HostManager {gameObject.name} despawned. HasState: {hasState}");
        base.Despawned(runner, hasState);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        HostProcessCommandsFromNetwork();
    }

    public void HostProcessCommandsFromNetwork()
    {
        // Get data from all active players
        foreach (var player in NetRunner.ActivePlayers)
        {
            if (NetRunner.TryGetInputForPlayer<NetworkInputData>(player, out var input))
            {
                HostProcessPlayerCommand(player, input);
            }
            else
            {
                // This is normal and can happen if a player didn't provide input for a tick.
                // We can log it for debugging if needed.
                // LogWarning($"{GetLogCallPrefix(GetType())} Input for {player} not available at tick {NetRunner.Tick}");
            }
        }
    }

    /// <summary>
    /// Host processes a single command from a single player for the current tick.
    /// </summary>
    private void HostProcessPlayerCommand(PlayerRef player, NetworkInputData input)
    {

        /*  TEMP - better use new Codex branch - there is correct one
        foreach (var pair in _cursorEchos)
        {
            if (NetRunner.TryGetInputForPlayer<NetworkInputData>(pair.Key, out var input))
            {
                Log($"{GetLogCallPrefix(GetType())} input.mouseWorldPosition[{input.mouseWorldPosition}].");

                pair.Value.transform.position = input.mouseWorldPosition;
            }
        }
        */

        var changedUnits = Unit.GetUnitsInInput(input);
        if (changedUnits.Count == 0) return;

        var center = changedUnits.GetCenter();

        for (int i = 0; i < input.unitCount; i++)
        {
            var unitId = input.unitIds[i];
            var unit = FindUnitById(unitId);
            if (unit != null)
            {
                var unitTargetPosition = unit.GetUnitTargetPosition(center, input.targetPosition);

                // We pass the current tick as the command "timestamp" for logging or future logic.
                unit.HostSetTarget(unitTargetPosition, Runner.Tick);
            }
        }
    }

    // Refactored to use UnitRegistry for performance.
    private Unit FindUnitById(uint unitId)
    {
        if (UnitRegistry.Units.TryGetValue(unitId, out var unit))
        {
            return unit;
        }
        return null;
    }

}
