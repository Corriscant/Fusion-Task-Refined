// Manages player lifecycle events like joining and leaving.
using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;

/// <summary>
/// Handles game logic related to players, such as spawning units when a player joins,
/// cleaning up when they leave, and processing their commands and translates local player input into network-ready commands.
/// </summary>
public class PlayerManager : NetworkBehaviour
{
    NetworkRunner NetRunner => ConnectionManager.Instance.NetRunner;

    // --- Dependencies ---
    [Header("Dependencies")]
    [SerializeField] private SelectionManager _selectionManager;
    [SerializeField] private GameObject _destinationMarkerPrefab;

    [Header("Unit Spawn Settings")]
    [SerializeField] private NetworkPrefabRef _unitPrefab; // Prefab for the unit to be spawned.
    [SerializeField] private int unitCountPerPlayer = 5; // How many units to spawn for each player.

    [Header("Other")]
    [Tooltip("Prefab of Cursor Echo of other players")]
    [SerializeField] private PlayerCursor PlayerCursorPrefab;

    [Header("Other")]
    /// <summary>
    /// The maximum allowed offset for a unit from the center of the group.
    /// </summary>
    [SerializeField] public int unitAllowedOffset = 1;

    // --- State for Network Input ---
    private Vector3 _pendingTargetPosition;
    private bool _hasPendingTarget;
    private Vector3 _currentMousePosition;

    // --- Private Fields ---
    // Tracks the spawned units for each player for easy cleanup.
    private Dictionary<PlayerRef, List<NetworkObject>> _spawnedPlayers = new();
    // Counter to assign a unique material index to each new player.
    private int _spawnedPlayersCount;

    // --- Unity & Event Subscription ---
    private void OnEnable()
    {
        InputManager.OnSecondaryMouseClick_World += HandleMoveCommand;
        InputManager.OnMouseMove += CacheMousePosition;
        ConnectionManager.On_PlayerJoined += HandlePlayerJoined;
        ConnectionManager.On_PlayerLeft += HandlePlayerLeft;
        ConnectionManager.On_Input += TryGetNetworkInput;
    }

    private void OnDisable()
    {
        InputManager.OnSecondaryMouseClick_World -= HandleMoveCommand;
        InputManager.OnMouseMove -= CacheMousePosition;
        ConnectionManager.On_PlayerLeft -= HandlePlayerLeft;
        ConnectionManager.On_PlayerJoined -= HandlePlayerJoined;
        ConnectionManager.On_Input -= TryGetNetworkInput;
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
            HostProcessCommandsFromNetwork();
    }

    private void HostProcessCommandsFromNetwork()
    {
        // Get data from all active players
        foreach (var player in NetRunner.ActivePlayers)
        {
            if (NetRunner.TryGetInputForPlayer<NetworkInputData>(player, out var input))
            {
                if (PlayerCursorRegistry.TryGet(player, out var playerCursor))
                {
                    playerCursor.CursorPosition = input.mouseWorldPosition;
                }
            }
            else
            {
                // This is normal and can happen if a player didn't provide input for a tick.
                // We can log it for debugging if needed.
                // LogWarning($"{GetLogCallPrefix(GetType())} Input for {player} not available at tick {NetRunner.Tick}");
            }
        }
    }

    public void LateUpdate()
    {
        if (PlayerCursorRegistry.Cursors.Count > 0)
            UpdateCursorsEcho();
    }

    private void UpdateCursorsEcho()
    {
        foreach (var cursor in PlayerCursorRegistry.Cursors.Values)
        {
            cursor.transform.position = cursor.CursorPosition;
        }
    }

    private void CacheMousePosition(Vector3 position)
    {
        _currentMousePosition = position;
    }

    /// <summary>
    /// Generates network input data for the local player.
    /// Always provides the current mouse world position and, when available,
    /// also includes any pending move commands.
    /// </summary>
    /// <param name="data">The generated input data.</param>
    private void TryGetNetworkInput(ref NetworkInputData data)
    {
        // Determine where the cursor points in the world.
        Vector3 mouseWorld = Vector3.zero;

        var ray = Camera.main.ScreenPointToRay(_currentMousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            mouseWorld = hit.point;
        }

        data = new NetworkInputData
        {
            mouseWorldPosition = mouseWorld
        };

        // Include move command data if one is queued.
        if (_hasPendingTarget)
        {
            data.targetPosition = _pendingTargetPosition;
            data.unitCount = Mathf.Min(_selectionManager.SelectedUnits.Count, UnitIdList.MaxUnits);

            for (int i = 0; i < data.unitCount; i++)
            {
                var selectable = _selectionManager.SelectedUnits[i];
                if (selectable is NetworkBehaviour nb)
                {
                    data.unitIds[i] = nb.Object.Id.Raw;
                }
                else if (selectable is Component component)
                {
                    LogError($"Unit {component.name} is missing a NetworkObject!");
                }
            }

            // Clear the flag internally after providing the data.
            _hasPendingTarget = false;
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
            SpawnPlayerUnits(runner, player);
            // Synchronize unit data (names, materials). Delayed to allow spawning for everyone.
            StartCoroutine(SyncUnitsData(runner));

            if (PlayerCursorPrefab != null)
            {
                runner.Spawn(PlayerCursorPrefab, Vector3.zero, Quaternion.identity, player);
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

            if (PlayerCursorRegistry.TryGet(player, out var cursor))
            {
                runner.Despawn(cursor.Object);
            }
        }
    }

    // --- Command Handling ---

    private void HandleMoveCommand(Vector3 targetPosition)
    {
        // Don't issue a move command if no units are selected
        if (_selectionManager == null || _selectionManager.SelectedUnits.Count == 0)
            return;

        // Instantiate a purely visual marker for immediate feedback
        if (_destinationMarkerPrefab != null)
        {
            var marker = Instantiate(_destinationMarkerPrefab, targetPosition, Quaternion.identity);
            Destroy(marker, 2f);
        }

        // Prepare data for the next network tick
        _pendingTargetPosition = targetPosition;
        _hasPendingTarget = true;
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

        // Iterate over all registered units and relay their data to clients.
        foreach (var unit in UnitRegistry.Units.Values)
        {
            unit.RPC_RelaySpawnedUnitInfo(unit.Object.Id, unit.name, unit.materialIndex);
        }
    }
}
