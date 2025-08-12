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
using UnityEngine.Windows;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This structure is used to encapsulate player input for network transmission.
/// </summary>
public delegate void OnInputHandler(ref NetworkInputData data);

/// <summary>
/// Manages the core network connection and session lifecycle using Photon Fusion.
/// This class is responsible for initializing the NetworkRunner, handling top-level network callbacks,
/// and delegating game-specific logic to specialized managers (e.g., PlayerManager).
/// It also collects input from other systems to provide it to the network simulation via OnInput.
/// </summary>
public class ConnectionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // --- Public Events ---
    public static event Action OnConnectingStarted;
    public static event Action OnConnected;
    public static event Action OnDisconnected;

    public static event Action<NetworkRunner, PlayerRef> On_PlayerJoined;
    public static event Action<NetworkRunner, PlayerRef> On_PlayerLeft;
    public static event OnInputHandler On_Input;

    // Singleton Instance
    public static ConnectionManager Instance { get; private set; }

    private NetworkRunner _netRunner;
    public NetworkRunner NetRunner => _netRunner; // Giving access to runner from other scripts

    [Header("Managers")]
    [SerializeField] public PlayerManager PlayerManager;

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
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnDestroy()
    {
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
            OnConnectingStarted?.Invoke();
            await StartGame(mode);
            if (_netRunner != null && _netRunner.IsRunning)
            {
                OnConnected?.Invoke();
            }
            else
            {
                OnDisconnected?.Invoke();
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


    // --- INetworkRunnerCallbacks Implementation ---

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData data = default;

        On_Input?.Invoke(ref data);   

        input.Set(data);
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Log($"{GetLogCallPrefix(GetType())} Player joined: {player}");

        On_PlayerJoined?.Invoke(runner, player);
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Log($"{GetLogCallPrefix(GetType())} Player left: {player}");

        On_PlayerLeft?.Invoke(runner, player);
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        // HostManager prefab is no longer required after the refactor.
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Log($"{GetLogCallPrefix(GetType())} OnShutdown triggered with reason: {shutdownReason}");
        UnitRegistry.Clear();
        PlayerCursorRegistry.Clear();
        OnDisconnected?.Invoke();
    }

    #region INetworkRunnerCallbacks Implementation
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { Log($"{GetLogCallPrefix(GetType())} OnInputMissing triggered"); }
    public void OnConnectedToServer(NetworkRunner runner) { Log($"{GetLogCallPrefix(GetType())} OnConnectedToServer triggered"); }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { Log($"{GetLogCallPrefix(GetType())} OnDisconnectedFromServer triggered"); }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { Log($"{GetLogCallPrefix(GetType())} OnConnectRequest triggered"); }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { Log($"{GetLogCallPrefix(GetType())} OnConnectFailed triggered"); }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { Log($"{GetLogCallPrefix(GetType())} OnUserSimulationMessage triggered"); }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { Log($"{GetLogCallPrefix(GetType())} OnSessionListUpdated triggered"); }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { Log($"{GetLogCallPrefix(GetType())} OnCustomAuthenticationResponse triggered"); }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { Log($"{GetLogCallPrefix(GetType())} OnHostMigration triggered"); }
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Log($"{GetLogCallPrefix(GetType())} OnSceneLoadStart triggered");
        UnitRegistry.Clear();
        PlayerCursorRegistry.Clear();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnitRegistry.Clear();
        PlayerCursorRegistry.Clear();
    }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { Log($"{GetLogCallPrefix(GetType())} OnObjectExitAOI triggered"); }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { Log($"{GetLogCallPrefix(GetType())} OnObjectEnterAOI triggered"); }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { Log($"{GetLogCallPrefix(GetType())} OnReliableDataReceived triggered"); }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { Log($"{GetLogCallPrefix(GetType())} OnReliableDataProgress triggered"); }
    #endregion

}
