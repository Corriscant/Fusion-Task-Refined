using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class NetworkGameManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // Singleton Instance
    public static NetworkGameManager Instance { get; private set; }

    private NetworkRunner _netRunner;
    public NetworkRunner NetRunner => _netRunner; // Giving access to runner from other scripts

    [SerializeField] public int unitCountPerPlayer = 5;
    /// <summary>
    /// The maximum allowed offset for a unit from the center of the group.
    /// </summary>
    [SerializeField] public int unitAllowedOffset = 3;

    [SerializeField] private NetworkPrefabRef _unitPrefab;
    [SerializeField] private GameObject _destinationMarkerPrefab;
    [SerializeField] private SelectionManager _selectionManager;
    [SerializeField] private HostManager _hostManagerPrefab;

    // Client request to send destination point to the host
    private Vector3 _pendingTargetPosition = Vector3.zero;
    private bool _hasPendingTarget = false;
    /// <summary>
    /// Flag indicating the presence of a destination point
    /// </summary>
    public bool HasPendingTarget => _hasPendingTarget;

    private Dictionary<PlayerRef, List<NetworkObject>> _spawnedPlayers = new();

    // Prevent multiple connection attempts
    private bool _isConnecting = false;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Destroy duplicate object
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Preserve between scenes
    }

    private async Task StartGame(GameMode mode)
    {
        // Test if _unitPrefab initialized
        if (_unitPrefab == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} Unit prefab is not set.");
            return;
        }

        // Create the Fusion runner and let it know that we will be providing user input
        _netRunner = gameObject.AddComponent<NetworkRunner>();
        _netRunner.ProvideInput = true;

        // Create the NetworkSceneInfo from the current scene
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        Log($"{GetLogCallPrefix(GetType())} StartGame initiated for {mode}");

        try
        {
            var result = await _netRunner.StartGame(new StartGameArgs()
            {
                GameMode = mode,
                SessionName = "TestRoom",
                Scene = scene,
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
            });

            #region LogAccessTo NetRunner.Tick
            // Provide the logger with a way to get the current server tick safely
            Corris.Loggers.Logger.LogPrefix = "";
            Corris.Loggers.Logger.GetCurrentServerTick = () => _netRunner.IsRunning ? _netRunner.Tick : -1;
            Log($"{GetLogCallPrefix(GetType())} StartGame result: Ok={result.Ok}, ShutdownReason={result.ShutdownReason}, ErrorMessage={result.ErrorMessage}");
            
            if (_netRunner.IsRunning)
            {
                Log($"{GetLogCallPrefix(GetType())} Starting game in {mode} mode. Current NetRunner tick: {_netRunner.Tick}");
            }
            else
            {
                Log($"{GetLogCallPrefix(GetType())} Starting game in {mode} mode. Runner is not running.");
            }
            #endregion
        }

        catch (Exception ex)
        {
            LogError($"{GetLogCallPrefix(GetType())} StartGame exception: {ex.Message}");
            return;
        }

    }

    private async void StartGameAsync(GameMode mode)
    {
        // Prevent multiple concurrent calls.
        if (_isConnecting) return;

        try
        {
            _isConnecting = true;
            Panel_Status.Instance.StartConnecting();
            await StartGame(mode);
            if (_netRunner != null && _netRunner.IsRunning)
            {
                Panel_Status.Instance.SetConnected();
            }
            else
            {
                Panel_Status.Instance.SetUnconnected();
            }
        }
        finally
        {
            _isConnecting = false;
        }
    }

    private void OnGUI()
    {
        GUI.enabled = !_isConnecting;

        float buttonY = 0f;

        if (_netRunner == null)
        {
            if (GUI.Button(new Rect(0, 0, 200, 40), "Host"))
            {
                StartGameAsync(GameMode.Host);
            }
            if (GUI.Button(new Rect(0, 40, 200, 40), "Join"))
            {
                StartGameAsync(GameMode.Client);
            }

            buttonY = 80f;
        }

        GUI.enabled = true;

        if (GUI.Button(new Rect(0, buttonY, 200, 40), "Exit"))
        {
            QuitGame();
        }
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false; // Simulate pressing the stop button in the editor
#else
        Application.Quit();
#endif
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Log($"{GetLogCallPrefix(GetType())} Player joined: {player}");

        if (runner.IsServer)
        {
            SpawnPlayerUnits(runner, player);
            // Synchronize unit data between players (names, materials). Delayed to allow spawning for everyone
            StartCoroutine(SyncUnitsData());
        }
    }

    private IEnumerator SyncUnitsData()
    {
        // Wait a bit to allow everything to spawn
        yield return new WaitForSeconds(0.5f);

        // Iterate through all units in the scene and send their data via RPC to all clients
        foreach (var unit in FindObjectsByType<Unit>(FindObjectsSortMode.None))
        {
            // Send unit data
            unit.RPC_RelaySpawnedUnitInfo(unit.Object.Id, unit.name, unit.materialIndex);
        }
    }

    // Number of spawned players
    private int spawnedPlayersCount;

    private void SpawnPlayerUnits(NetworkRunner runner, PlayerRef player)
    {
        // Create a unique center position for each player
        var playerSpawnCenterPosition = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 1, 0);

        var unitList = new List<NetworkObject>();

        spawnedPlayersCount += 1;

        // Spawn Player units on the field
        for (int i = 0; i < unitCountPerPlayer; i++)
        {
            // Place units in a circle around player center
            Vector3 spawnPosition = playerSpawnCenterPosition + new Vector3(Mathf.Cos(i * Mathf.PI * 2 / unitCountPerPlayer), 0, Mathf.Sin(i * Mathf.PI * 2 / unitCountPerPlayer));
            // Spawn unit
            var networkUnitObject = runner.Spawn(_unitPrefab, spawnPosition, Quaternion.identity, player);

            if (networkUnitObject == null)
            {
                LogError($"{GetLogCallPrefix(GetType())} Failed to spawn network unit object.");
                return;
            }

            var unit = networkUnitObject.GetComponent<Unit>();
            unit.SetOwner(player); // Set unit owner directly (in case it may be given to another player)
            // Set unit name with the current player index and unit index
            unit.name = $"Unit_{player.RawEncoded}_{i}";
            // Record material index so other clients also know
            unit.materialIndex = spawnedPlayersCount;

            unitList.Add(networkUnitObject);

        }
        // Keep track of the player avatars for easy access
        _spawnedPlayers.Add(player, unitList);
        // Debug.Log($"Spawned {unitList.Count} units for player: {player}  material name: {materialName}");
        Log($"{GetLogCallPrefix(GetType())} Spawned {unitList.Count} units for player: {player}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)  // Not in Phoron tutorial somehow, but looks like it is needed
        {
            if (_spawnedPlayers.TryGetValue(player, out var networkObjects))
            {
                // Despawn all player units
                foreach (var networkObject in networkObjects)
                {
                    runner.Despawn(networkObject);
                }

                // Delete the player from the list
                _spawnedPlayers.Remove(player);
            }
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        // If there is a new target
        if (HasPendingTarget)
        {
            data.targetPosition = _pendingTargetPosition;
            data.timestamp = Time.time;

            data.unitCount = Mathf.Min(_selectionManager.SelectedUnits.Count, UnitIdList.MaxUnits);
            for (int i = 0; i < data.unitCount; i++)
            {
                var networkObject = _selectionManager.SelectedUnits[i].GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    data.unitIds[i] = networkObject.Id.Raw;
                }
                else
                {
                    LogError($"{GetLogCallPrefix(GetType())} Unit {_selectionManager.SelectedUnits[i].name} is missing a NetworkObject!");
                }
            }
            _hasPendingTarget = false;
        }
        input.Set(data);
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // running host manager
        if (_netRunner.IsServer)
        {
            Log($"{GetLogCallPrefix(GetType())} Spawning HostManagerPrefab on server.");
            var result = _netRunner.Spawn(_hostManagerPrefab, Vector3.zero, Quaternion.identity, null);
            if (result == null)
            {
                LogError($"{GetLogCallPrefix(GetType())} Failed to spawn HostManagerPrefab.");
            }
            else
            {
                Log($"{GetLogCallPrefix(GetType())} HostManagerPrefab spawned successfully.");
            }
        }
    }

    #region INetworkRunnerCallbacks Implementation
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { Log($"{GetLogCallPrefix(GetType())} OnInputMissing triggered"); }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { Log($"{GetLogCallPrefix(GetType())} OnShutdown triggered"); }
    public void OnConnectedToServer(NetworkRunner runner) { Log($"{GetLogCallPrefix(GetType())} OnConnectedToServer triggered"); }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { Log($"{GetLogCallPrefix(GetType())} OnDisconnectedFromServer triggered"); }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { Log($"{GetLogCallPrefix(GetType())} OnConnectRequest triggered"); }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { Log($"{GetLogCallPrefix(GetType())} OnConnectFailed triggered"); }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { Log($"{GetLogCallPrefix(GetType())} OnUserSimulationMessage triggered"); }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { Log($"{GetLogCallPrefix(GetType())} OnSessionListUpdated triggered"); }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { Log($"{GetLogCallPrefix(GetType())} OnCustomAuthenticationResponse triggered"); }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { Log($"{GetLogCallPrefix(GetType())} OnHostMigration triggered"); }
    public void OnSceneLoadStart(NetworkRunner runner) { Log($"{GetLogCallPrefix(GetType())} OnSceneLoadStart triggered"); }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { Log($"{GetLogCallPrefix(GetType())} OnObjectExitAOI triggered"); }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { Log($"{GetLogCallPrefix(GetType())} OnObjectEnterAOI triggered"); }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { Log($"{GetLogCallPrefix(GetType())} OnReliableDataReceived triggered"); }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { Log($"{GetLogCallPrefix(GetType())} OnReliableDataProgress triggered"); }
    #endregion

    public void HandleDestinationInput(Vector3 targetPosition)
    {
        // if there is at least one selected object
        if (_selectionManager.SelectedUnits.Count == 0)
            return;

        // instantiate _destinationMarkerPrefab prefab at this point, and delete it after 2 seconds
        var marker = Instantiate(_destinationMarkerPrefab, targetPosition, Quaternion.identity);
        Destroy(marker, 2);

        Log($"{GetLogCallPrefix(GetType())} Received destination input: {targetPosition}");

        _pendingTargetPosition = targetPosition; // Save destination point
        _hasPendingTarget = true; // Set flag
    }

}

public class Command
{
    public PlayerRef Player;         // ID of the client from which the command came
    public NetworkInputData Input;  // Full data structure from NetworkInputData
}
