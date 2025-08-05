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

public class ConnectionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // Singleton Instance
    public static ConnectionManager Instance { get; private set; }

    private NetworkRunner _netRunner;
    public NetworkRunner NetRunner => _netRunner; // Giving access to runner from other scripts

    /// <summary>
    /// The maximum allowed offset for a unit from the center of the group.
    /// </summary>
    [SerializeField] public int unitAllowedOffset = 3;

    [SerializeField] private GameObject _destinationMarkerPrefab;
    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private SelectionManager _selectionManager;
    [SerializeField] private HostManager _hostManagerPrefab;

    // Client request to send destination point to the host
    private Vector3 _pendingTargetPosition = Vector3.zero;
    private bool _hasPendingTarget = false;
    /// <summary>
    /// Flag indicating the presence of a destination point
    /// </summary>
    public bool HasPendingTarget => _hasPendingTarget;

    // Prevent multiple connection attempts
    private bool _isConnecting = false;
    public bool IsConnecting => _isConnecting;

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

        Log($"{GetLogCallPrefix(GetType())} ConnectionManager Instance!");

        // Subscribe to InputManager events
        InputManager.OnSecondaryMouseClick_World += HandleMoveCommand;
    }

    private void OnDestroy()
    {
        // Unsubscribe from InputManager events
        InputManager.OnSecondaryMouseClick_World -= HandleMoveCommand;

        // Cleanup singleton instance
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private async Task StartGame(GameMode mode)
    {
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

    public void StartGamePublic(GameMode mode)
    {
        // Public wrapper to be called from UI scripts.
        StartGameAsync(mode);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Log($"{GetLogCallPrefix(GetType())} Player joined: {player}");

        if (playerManager != null)
        {
            playerManager.HandlePlayerJoined(runner, player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Log($"{GetLogCallPrefix(GetType())} Player left: {player}");

        if (playerManager != null)
        {
            playerManager.HandlePlayerLeft(runner, player);
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

    // Handles input via an event, decoupling from InputManager.
    public void HandleMoveCommand(Vector3 targetPosition)
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
