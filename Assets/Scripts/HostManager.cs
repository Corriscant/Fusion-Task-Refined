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

    private PlayerCursor PlayerCursorPrefab => ConnectionManager.Instance.PlayerCursorPrefab;

    // Stores spawned network cursors for players.
    private readonly Dictionary<PlayerRef, PlayerCursor> _playerCursors = new();

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
        if (TryGetPlayerCursor(player, out var playerCursor))
        {
            playerCursor.CursorPosition = input.mouseWorldPosition;
        }

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

    // --- Unity & Event Subscription ---
    private void OnEnable()
    {
        ConnectionManager.On_PlayerJoined += HandlePlayerJoined;
        ConnectionManager.On_PlayerLeft += HandlePlayerLeft;
    }

    private void OnDisable()
    {
        ConnectionManager.On_PlayerLeft -= HandlePlayerLeft;
        ConnectionManager.On_PlayerJoined -= HandlePlayerJoined;
    }

    private void Update()
    {
        if (NetRunner == null)
            return;

        UpdateCursorsEcho();
    }
    private void UpdateCursorsEcho()
    {
        foreach (var pair in _playerCursors)
        {
            if (_playerCursors.TryGetValue(pair.Key, out var cursor))
            {
                pair.Value.transform.position = cursor.CursorPosition;
            }
        }
    }

    /// <summary>
    /// Handles the logic for when a new player joins the game.
    /// This is called from the OnPlayerJoined callback in the ConnectionManager.
    /// </summary>
    public void HandlePlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Spawning logic should only be executed on the server/host.
        if (runner.IsServer)
        {
            if (PlayerCursorPrefab != null)
            {
                var cursor = NetRunner.Spawn(PlayerCursorPrefab, Vector3.zero, Quaternion.identity, player);
                _playerCursors[player] = cursor;
            }
            else
            {
                LogWarning($"{GetLogCallPrefix(GetType())} PlayerCursor prefab is null. Cannot spawn cursor for player {player}.");
            }

        }

    }

    /// <summary>
    /// Handles the logic for when a player leaves the game.
    /// This is called from the OnPlayerLeft callback in the ConnectionManager.
    /// </summary>
    public void HandlePlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Despawning logic should only be executed on the server/host.
        if (runner.IsServer)
        {
            if (_playerCursors.TryGetValue(player, out var cursor))
            {
                NetRunner.Despawn(cursor.Object);
                _playerCursors.Remove(player);
            }
        }
    }


    public bool TryGetPlayerCursor(PlayerRef player, out PlayerCursor cursor)
    {
        return _playerCursors.TryGetValue(player, out cursor);
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
