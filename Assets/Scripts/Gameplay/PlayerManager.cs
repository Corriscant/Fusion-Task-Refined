// Manages player lifecycle events like joining and leaving.
using Fusion;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;
using FusionTask.Networking;
using FusionTask.Infrastructure;
using System.Linq;

namespace FusionTask.Gameplay
{
    /// <summary>
    /// Handles game logic related to players, such as spawning units when a player joins,
    /// cleaning up when they leave, and processing their commands via RPC while streaming cursor data through NetworkInput.
    /// </summary>
    public class PlayerManager : NetworkBehaviour
    {
        NetworkRunner NetRunner => !_connectionService.IsNullOrDestroyed() ? _connectionService.Runner : null;

    // --- Dependencies ---
    [Header("Dependencies")]
    [SerializeField] private SelectionManager _selectionManager;
    [SerializeField] private GameObject _destinationMarkerPrefab;
    [SerializeField] private Camera _mainCamera;

    [Header("Settings")]
    [SerializeField] private GameSettings _gameSettings; // TODO: Assign in the Inspector

    [Header("Unit Spawn Settings")]
    [SerializeField] private int unitCountPerPlayer = 5; // How many units to spawn for each player.

    /// <summary>
    /// The maximum allowed offset for a unit from the center of the group.
    /// </summary>
    private int unitAllowedOffset;

    /// <summary>
    /// Distance from the center for player spawn positions.
    /// </summary>
    private float playerSpawnDistance;

    // --- State for Network Input ---
    private Vector3 _currentMousePosition;

    // --- Private Fields ---
    // Tracks the spawned units for each player for easy cleanup.
    private Dictionary<PlayerRef, List<NetworkObject>> _spawnedPlayers = new();
    // Tracks material indices assigned to each player.
    private Dictionary<PlayerRef, int> _playerMaterialIndices = new();
    // Counter to assign a unique material index to each new player.
    private int _spawnedPlayersCount = 0;
    // Stores original spawn centers for players to support respawns.
    private Dictionary<PlayerRef, Vector3> _playerSpawnPositions = new();

    // --- Dependencies ---
    private IConnectionService _connectionService;
    private INetworkEvents _networkEvents;
    private IInputService _inputService;
    private IUnitRegistry _unitRegistry;
    private IPlayerCursorRegistry _playerCursorRegistry;
    private IMaterialApplier _materialApplier;
    private IGameFactory _gameFactory;

    [Inject]
    public void Construct(IConnectionService connectionService, INetworkEvents networkEvents, IInputService inputService, IUnitRegistry unitRegistry, IPlayerCursorRegistry playerCursorRegistry, IMaterialApplier materialApplier, IGameFactory gameFactory)
    {
        Log($"{GetLogCallPrefix(GetType())} VContainer Inject!");
        _connectionService = connectionService;
        _networkEvents = networkEvents;
        _inputService = inputService;
        _unitRegistry = unitRegistry;
        _playerCursorRegistry = playerCursorRegistry;
        _materialApplier = materialApplier;
        _gameFactory = gameFactory;
    }

    private void Awake()
    {
        if (_gameSettings == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} GameSettings not assigned!");
            return;
        }
        unitAllowedOffset = _gameSettings.unitAllowedOffset;
        playerSpawnDistance = _gameSettings.playerSpawnDistance;
    }

    // --- Unity & Event Subscription ---
    private void OnEnable()
    {
        // Subscriptions are handled in Spawned to ensure dependencies are injected.
    }

    private void Start()
    {
        if (_connectionService.IsNullOrDestroyed())
        {
            LogError($"{GetLogCallPrefix(GetType())} Connection service NIL!");
        }
        if (_inputService.IsNullOrDestroyed())
        {
            LogError($"{GetLogCallPrefix(GetType())} Input service NIL!");
        }
        if (_mainCamera.IsNullOrDestroyed())
        {
            LogError($"{GetLogCallPrefix(GetType())} Main camera is not assigned!");
        }
    }

    public override void Spawned()
    {
        if (!_inputService.IsNullOrDestroyed())
        {
            _inputService.OnSecondaryMouseClick_World += HandleMoveCommand;
            _inputService.OnMouseMove += CacheMousePosition;
            _inputService.OnRespawn += HandleRespawnRequest;
        }

        if (_networkEvents.IsNullOrDestroyed())
        {
            LogError($"{GetLogCallPrefix(GetType())} Network events NIL!");
            return;
        }

        _networkEvents.PlayerJoined += HandlePlayerJoined;
        _networkEvents.PlayerLeft += HandlePlayerLeft;
        _networkEvents.Input += TryGetNetworkInput;

        _ = _gameFactory.WarmupPools();
    }

    private void OnDisable()
    {
        if (!_inputService.IsNullOrDestroyed())
        {
            _inputService.OnSecondaryMouseClick_World -= HandleMoveCommand;
            _inputService.OnMouseMove -= CacheMousePosition;
            _inputService.OnRespawn -= HandleRespawnRequest;
        }
        if (!_networkEvents.IsNullOrDestroyed())
        {
            _networkEvents.PlayerLeft -= HandlePlayerLeft;
            _networkEvents.PlayerJoined -= HandlePlayerJoined;
            _networkEvents.Input -= TryGetNetworkInput;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
            HostUpdateCursorPositions();
    }

    private void HostUpdateCursorPositions()
    {
        // Get cursor data from all active players
        foreach (var player in NetRunner.ActivePlayers)
        {
            if (NetRunner.TryGetInputForPlayer<NetworkInputData>(player, out var input))
            {
                if (_playerCursorRegistry.TryGet(player, out var playerCursor))
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
        UpdateCursorsEcho();
    }

    private void UpdateCursorsEcho()
    {
        var cursors = _playerCursorRegistry.Cursors;
        for (int i = 0, count = cursors.Count; i < count; i++)
        {
            var cursor = cursors[i];
            cursor.transform.position = cursor.CursorPosition;
        }
    }

    private void CacheMousePosition(Vector3 position)
    {
        _currentMousePosition = position;
    }

    /// <summary>
    /// This uses a raycast from the camera to the mouse position to find the point in the world.
    /// </summary>
    private Vector3 GetMouseWorldPosition()
    {
        var ray = _mainCamera.ScreenPointToRay(_currentMousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.point;
        }
        return Vector3.zero; // Default to zero if no hit.
    }

    /// <summary>
    /// Generates network input data for the local player.
    /// Currently only the mouse world position is sent. Unit move
    /// commands are handled separately via RPC and are not included here.
    /// </summary>
    /// <param name="data">The generated input data.</param>
    private void TryGetNetworkInput(ref NetworkInputData data)
    {
        data = new NetworkInputData
        {
            mouseWorldPosition = GetMouseWorldPosition()
        };
    }

    /// <summary>
    /// Handles the logic for when a new player joins the game.
    /// This is called from the OnPlayerJoined callback in the ConnectionManager.
    /// </summary>
    public void HandlePlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            _ = SpawnPlayerUnitsAsync(runner, player);
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
                _playerMaterialIndices.Remove(player);
                _playerSpawnPositions.Remove(player);
            }

            if (_playerCursorRegistry.TryGet(player, out var cursor))
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

        // Build unit id array and invoke RPC immediately
        var selectedUnits = _selectionManager.SelectedUnits;
        var unitIds = new uint[selectedUnits.Count];

        for (int i = 0; i < selectedUnits.Count; i++)
        {
            var selectable = selectedUnits[i];
            if (selectable is NetworkBehaviour nb)
            {
                unitIds[i] = nb.Object.Id.Raw;
            }
            else if (selectable is Component component)
            {
                LogError($"{GetLogCallPrefix(GetType())} Unit {component.name} is missing a NetworkObject!");
            }
        }

        RPC_MoveUnits(targetPosition, unitIds);
    }

    /// <summary>
    /// Initiates a respawn request for the local player.
    /// </summary>
    private void HandleRespawnRequest()
    {
        if (_selectionManager != null)
        {
            _selectionManager.ClearSelection();
        }
        RPC_RequestRespawn();
    }

    /// <summary>
    /// RPC from clients requesting their units to be respawned.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsServer)]
    private void RPC_RequestRespawn(RpcInfo info = default)
    {
        var player = info.Source != PlayerRef.None ? info.Source : Runner.LocalPlayer;
        _ = RespawnPlayerUnitsAsync(player);
    }

    /// <summary>
    /// RPC called by clients to request movement for a group of units.
    /// Executed on the state authority to validate ownership and apply targets.
    /// </summary>
    [Rpc(RpcSources.All, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsServer)]
    private void RPC_MoveUnits(Vector3 targetPosition, uint[] unitIds, RpcInfo info = default)
    {
        var changedUnits = new List<Unit>();

        var player = info.Source != PlayerRef.None ? info.Source : Runner.LocalPlayer;

        foreach (var unitId in unitIds)
        {
            if (_unitRegistry.TryGet(unitId, out var unit) && unit.IsOwnedBy(player))
            {
                changedUnits.Add(unit);
            }
        }

        if (changedUnits.Count == 0)
            return;

        var center = changedUnits.GetCenter();

        foreach (var unit in changedUnits)
        {
            Vector3 offset = center - unit.transform.position;
            offset = unit.ClampOffset(offset, unitAllowedOffset);
            var unitTargetPosition = unit.GetUnitTargetPosition(offset, targetPosition);
            unit.HostSetTarget(unitTargetPosition, Runner.Tick);
        }
    }

    // --- Private Methods ---

    /// <summary>
    /// Spawns units and a cursor for the specified player using the factory.
    /// </summary>
    private async Task SpawnPlayerUnitsAsync(NetworkRunner runner, PlayerRef player)
    {
        Vector3 playerSpawnCenterPosition = GetPlayerSpawnCenter(player);
        await SpawnUnitsForPlayerAsync(runner, player, playerSpawnCenterPosition);
        SyncExistingUnitsToPlayer(player);
    }

    /// <summary>
    /// Returns the stored spawn center for a player or calculates a new one.
    /// </summary>
    private Vector3 GetPlayerSpawnCenter(PlayerRef player)
    {
        if (_playerSpawnPositions.TryGetValue(player, out var center))
        {
            return center;
        }

        Vector3 playerSpawnCenterPosition;
        switch (_playerSpawnPositions.Count)
        {
            case 0: // First player - bottom-left
                playerSpawnCenterPosition = new Vector3(-playerSpawnDistance, 1f, -playerSpawnDistance);
                break;
            case 1: // Second player - top-right
                playerSpawnCenterPosition = new Vector3(playerSpawnDistance, 1f, playerSpawnDistance);
                break;
            case 2: // Third player - bottom-right
                playerSpawnCenterPosition = new Vector3(playerSpawnDistance, 1f, -playerSpawnDistance);
                break;
            case 3: // Fourth player - top-left
                playerSpawnCenterPosition = new Vector3(-playerSpawnDistance, 1f, playerSpawnDistance);
                break;
            default: // Any subsequent players
                float randomX = Random.Range(-playerSpawnDistance, playerSpawnDistance);
                float randomZ = Random.Range(-playerSpawnDistance, playerSpawnDistance);
                playerSpawnCenterPosition = new Vector3(randomX, 1f, randomZ);
                break;
        }

        _playerSpawnPositions.Add(player, playerSpawnCenterPosition);
        return playerSpawnCenterPosition;
    }

    /// <summary>
    /// Removes existing units and respawns them at the original coordinates.
    /// </summary>
    private async Task RespawnPlayerUnitsAsync(PlayerRef player)
    {
        var runner = NetRunner;
        if (runner == null)
        {
            return;
        }

        if (_spawnedPlayers.TryGetValue(player, out var networkObjects))
        {
            for (int i = 0; i < networkObjects.Count; i++)
            {
                var networkObject = networkObjects[i];
                if (networkObject != null)
                {
                    runner.Despawn(networkObject);
                }
            }
            _spawnedPlayers.Remove(player);
        }

        Vector3 playerSpawnCenterPosition = GetPlayerSpawnCenter(player);
        await SpawnUnitsForPlayerAsync(runner, player, playerSpawnCenterPosition);
    }

    /// <summary>
    /// Creates player units at the given position and ensures color assignment.
    /// </summary>
    private async Task SpawnUnitsForPlayerAsync(NetworkRunner runner, PlayerRef player, Vector3 playerSpawnCenterPosition)
    {
        var unitList = new List<NetworkObject>();

        for (int i = 0; i < unitCountPerPlayer; i++)
        {
            Vector3 spawnPosition = playerSpawnCenterPosition + new Vector3(Mathf.Cos(i * Mathf.PI * 2 / unitCountPerPlayer), 0, Mathf.Sin(i * Mathf.PI * 2 / unitCountPerPlayer));
            var unit = _gameFactory.CreateUnit(runner, spawnPosition, Quaternion.identity, player);
            unit.name = $"Unit_{player.RawEncoded}_{i}";
            unitList.Add(unit.Object);
        }

        _spawnedPlayers[player] = unitList;

        if (!_playerCursorRegistry.TryGet(player, out _))
        {
            _gameFactory.CreateCursor(runner, Vector3.zero, Quaternion.identity, player);
        }

        int index;
        if (!_playerMaterialIndices.TryGetValue(player, out index))
        {
            index = _spawnedPlayersCount;
            _playerMaterialIndices[player] = index;
            _spawnedPlayersCount++;
        }

        await AssignPlayerColor(player, index);
    }

    /// <summary>
    /// Assigns a material index to the given player and applies it to all related objects.
    /// </summary>
    private async Task AssignPlayerColor(PlayerRef player, int index)
    {
        _playerMaterialIndices[player] = index;

        if (_playerCursorRegistry.TryGet(player, out var cursor))
        {
            cursor.MaterialIndex = index;
        }

        if (_spawnedPlayers.TryGetValue(player, out var objects))
        {
            foreach (var networkObject in objects)
            {
                if (networkObject != null && networkObject.TryGetComponent<Unit>(out var unit))
                {
                    unit.materialIndex = index;
                    await _materialApplier.ApplyMaterialAsync(unit.MeshRenderer, index, "Unit");
                    unit.RPC_RelaySpawnedUnitInfo(unit.name, index);
                }
            }
        }
    }

    /// <summary>
    /// Sends info about all spawned units to a newly joined player.
    /// </summary>
    /// <param name="targetPlayer">Player that should receive unit data.</param>
    private void SyncExistingUnitsToPlayer(PlayerRef targetPlayer)
    {
        foreach (var entry in _spawnedPlayers)
        {
            var owner = entry.Key;
            if (!_playerMaterialIndices.TryGetValue(owner, out var index))
            {
                continue;
            }

            var units = entry.Value;
            foreach (var networkObject in units)
            {
                if (networkObject != null && networkObject.TryGetComponent<Unit>(out var unit))
                {
                    unit.RPC_RelaySpawnedUnitInfoToPlayer(targetPlayer, unit.name, index);
                }
            }
        }
    }

    }
}
