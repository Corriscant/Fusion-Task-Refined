// Manages player lifecycle events like joining and leaving.
using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles game logic related to players, such as spawning units when a player joins
/// and cleaning up when they leave.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    // --- Serialized Fields ---
    [Header("Unit Spawn Settings")]
    [SerializeField]
    private NetworkPrefabRef _unitPrefab; // Prefab for the unit to be spawned.

    [SerializeField]
    private int unitCountPerPlayer = 5; // How many units to spawn for each player.

    // --- Private Fields ---
    // Tracks the spawned units for each player for easy cleanup.
    private Dictionary<PlayerRef, List<NetworkObject>> _spawnedPlayers = new();

    // Counter to assign a unique material index to each new player.
    private int _spawnedPlayersCount;


    // --- Public Methods (called by ConnectionManager) ---

    /// <summary>
    /// Handles the logic for when a new player joins the game.
    /// This is called from the OnPlayerJoined callback in the ConnectionManager.
    /// </summary>
    public void HandlePlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // Spawning logic should only be executed on the server/host.
        if (runner.IsServer)
        {
            SpawnPlayerUnits(runner, player);
            // Synchronize unit data (names, materials). Delayed to allow spawning for everyone.
            StartCoroutine(SyncUnitsData(runner));
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
            // Find the player's units and despawn them.
            if (_spawnedPlayers.TryGetValue(player, out var networkObjects))
            {
                foreach (var networkObject in networkObjects)
                {
                    if (networkObject != null)
                    {
                        runner.Despawn(networkObject);
                    }
                }

                // Remove the player from the dictionary.
                _spawnedPlayers.Remove(player);
            }
        }
    }

    // --- Private Methods ---

    /// <summary>
    /// Spawns the initial set of units for a given player.
    /// </summary>
    private void SpawnPlayerUnits(NetworkRunner runner, PlayerRef player)
    {
        _spawnedPlayersCount++;

        // Calculate a unique spawn center for the player to avoid overlap.
        var playerSpawnCenterPosition = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 1, 0);
        var unitList = new List<NetworkObject>();

        // Spawn units in a circle around the player's spawn center.
        for (int i = 0; i < unitCountPerPlayer; i++)
        {
            Vector3 spawnPosition = playerSpawnCenterPosition + new Vector3(Mathf.Cos(i * Mathf.PI * 2 / unitCountPerPlayer), 0, Mathf.Sin(i * Mathf.PI * 2 / unitCountPerPlayer));
            NetworkObject networkUnitObject = runner.Spawn(_unitPrefab, spawnPosition, Quaternion.identity, player);

            if (networkUnitObject == null)
            {
                Debug.LogError($"Failed to spawn unit for player {player}.");
                continue;
            }

            var unit = networkUnitObject.GetComponent<Unit>();
            unit.SetOwner(player);
            // We set the name and material index here on the server.
            // This will be sent to all clients via an RPC.
            unit.name = $"Unit_{player.RawEncoded}_{i}";
            unit.materialIndex = _spawnedPlayersCount;

            unitList.Add(networkUnitObject);
        }

        // Add the list of spawned units to our dictionary for tracking.
        _spawnedPlayers.Add(player, unitList);
    }

    /// <summary>
    /// Waits a moment and then tells all units to broadcast their info (name, material) to all clients.
    /// This ensures that all clients have the correct visual representation for all units.
    /// </summary>
    private IEnumerator SyncUnitsData(NetworkRunner runner)
    {
        // Wait briefly to ensure all initial objects have been spawned across all clients.
        yield return new WaitForSeconds(0.5f);

        // Find every unit in the scene and tell it to send its data to clients.
        // We use FindObjectsByType here because this is a one-time setup action, not a per-frame update.
        foreach (var unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            if (unit != null)
            {
                unit.RPC_RelaySpawnedUnitInfo(unit.Object.Id, unit.name, unit.materialIndex);
            }
        }
    }
}