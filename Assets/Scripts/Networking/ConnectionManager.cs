using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using FusionTask.Infrastructure;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;
using VContainer;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FusionTask.Networking
{
    /// <summary>
    /// This structure is used to encapsulate player input for network transmission.
    /// </summary>
    public delegate void OnInputHandler(ref NetworkInputData data);

    /// <summary>
    /// Manages the core network connection and session lifecycle using Photon Fusion.
    /// This class is responsible for initializing the NetworkRunner, handling top-level network callbacks,
    /// and delegating game-specific logic to specialized managers (e.g., PlayerManager).
    /// It also collects input from other systems to provide it to the network simulation via OnInput.
    /// Exposes connection functionality through <see cref="IConnectionService"/> to allow dependency injection.
    /// </summary>
    public class ConnectionManager : NetworkRunnerCallbacksBase, IConnectionService, INetworkEvents
    {
    // --- Public Events ---
    public event Action ConnectingStarted;
    public event Action Connected;
    public event Action Disconnected;

    public event Action<NetworkRunner, PlayerRef> PlayerJoined;
    public event Action<NetworkRunner, PlayerRef> PlayerLeft;
    public event OnInputHandler Input;

    private NetworkRunner _netRunner;
    /// <summary>
    /// Current runner instance.
    /// </summary>
    public NetworkRunner Runner => _netRunner;

    // Prevent multiple connection attempts
    private bool _isConnecting = false;
    public bool IsConnecting => _isConnecting;

    private SceneLoadHandler _sceneLoadHandler;
    private NetworkObjectInjector _networkObjectInjector;
    private INetworkObjectProvider _networkObjectProvider;

    private void Awake()
    {
        // Avoid duplicates in the scene
        if (FindObjectsByType<ConnectionManager>(FindObjectsSortMode.None).Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject); // Preserve between scenes

        Log($"{GetLogCallPrefix(GetType())} ConnectionManager awake!");
    }

    // Removing the singleton pattern: no OnDestroy cleanup required

    private async Task StartGameInternal(GameMode mode)
    {
        // Create the Fusion runner and let it know that we will be providing user input
        _netRunner = gameObject.AddComponent<NetworkRunner>();
        _netRunner.AddCallbacks(this);
        if (!_networkObjectInjector.IsNullOrDestroyed())
        {
            _netRunner.AddCallbacks(_networkObjectInjector);
        }
        if (!_sceneLoadHandler.IsNullOrDestroyed())
        {
            _netRunner.AddCallbacks(_sceneLoadHandler);
        }
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
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
                ObjectProvider = _networkObjectProvider
            });

            #region LogAccessTo NetRunner.Tick
            // Provide the logger with a way to get the current server tick safely
            Corris.Loggers.Logger.LogPrefix = "";
            Corris.Loggers.Logger.GetCurrentServerTick = () => _netRunner != null && _netRunner.IsRunning ? _netRunner.Tick : -1;
            Log($"{GetLogCallPrefix(GetType())} StartGame result: Ok={result.Ok}, ShutdownReason={result.ShutdownReason}, ErrorMessage={result.ErrorMessage}");
            
            if ((_netRunner != null) && _netRunner.IsRunning)
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

    public async Task StartGame(GameMode mode)
    {
        // Prevent multiple concurrent calls.
        if (_isConnecting) return;

        if (_netRunner != null && !_netRunner.IsShutdown)
        {
            Log($"{GetLogCallPrefix(GetType())} StartGame called before previous runner shutdown");
            return;
        }

        try
        {
            _isConnecting = true;
            ConnectingStarted?.Invoke();
            await StartGameInternal(mode);
            if (_netRunner != null && _netRunner.IsRunning)
            {
                Connected?.Invoke();
            }
            else
            {
                Disconnected?.Invoke();
            }
        }
        catch (Exception ex)
        {
            LogError($"{GetLogCallPrefix(GetType())} StartGame exception: {ex.Message}");
            throw;
        }
        finally
        {
            _isConnecting = false;
        }
    }

    // --- NetworkRunnerCallbacksBase Overrides ---
    #region NetworkRunnerCallbacksBase Overrides

    public override void OnInput(NetworkRunner runner, NetworkInput input)
    {
        NetworkInputData data = default;

        Input?.Invoke(ref data);

        input.Set(data);
    }

    public override void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Log($"{GetLogCallPrefix(GetType())} Player joined: {player}");

        PlayerJoined?.Invoke(runner, player);
    }

    public override void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Log($"{GetLogCallPrefix(GetType())} Player left: {player}");

        PlayerLeft?.Invoke(runner, player);
    }

    public override void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Log($"{GetLogCallPrefix(GetType())} OnShutdown triggered with reason: {shutdownReason}");
        if (_netRunner != null)
        {
            Destroy(_netRunner);
            _netRunner = null;
        }
        Disconnected?.Invoke();
    }
    #endregion

    [Inject]
    public void Construct(SceneLoadHandler sceneLoadHandler, NetworkObjectInjector networkObjectInjector, INetworkObjectProvider networkObjectProvider)
    {
        _sceneLoadHandler = sceneLoadHandler;
        _networkObjectInjector = networkObjectInjector;
        _networkObjectProvider = networkObjectProvider;
    }
    }
}
