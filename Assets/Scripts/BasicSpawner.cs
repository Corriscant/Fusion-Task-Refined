using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Unity.Collections.Unicode;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{

    // Singleton Instance
    public static BasicSpawner Instance { get; private set; }

    private NetworkRunner _runner;
    public NetworkRunner Runner => _runner; // Giving access to runner from other scripts

    [SerializeField] public int unitCountPerPlayer = 5;

    [SerializeField] private NetworkPrefabRef _playerUnitPrefab;
    [SerializeField] private GameObject _DestinationMarkerPrefab;
    [SerializeField] private SelectionManager _selectionManager;

    // Client request to send destination point to the host
    private Vector3 _pendingTargetPosition = Vector3.zero;
    // Флаг наличия точки назначения
    private bool _hasPendingTarget = false; 
    public bool HasPendingTarget => _hasPendingTarget;

    private Dictionary<PlayerRef, List<NetworkObject>> _spawnedPlayers = new();

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

    async void StartGame(GameMode mode)
    {
        // Test if _playerUnitPrefab initialized
        if (_playerUnitPrefab == null)
        {
            Debug.LogError("Player unit prefab is not set.");
            return;
        }

        // Create the Fusion runner and let it know that we will be providing user input
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        // Create the NetworkSceneInfo from the current scene
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid)
        {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        // Start or join (depends on gamemode) a session with a specific name
        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "TestRoom",
            Scene = scene,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }
    private void OnGUI()
    {
        if (_runner == null)
        {
            if (GUI.Button(new Rect(0, 0, 200, 40), "Host"))
            {
                StartGame(GameMode.Host);
            }
            if (GUI.Button(new Rect(0, 40, 200, 40), "Join"))
            {
                StartGame(GameMode.Client);
            }
        }
    }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Player joined: {player}");
        if (runner.IsServer)
        {
            SpawnPlayerUnits(runner, player);
        }
    }

    private void SpawnPlayerUnits(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"Spawning units for player: {player}");
        // Create a unique center position for each player
        var playerSpawnCenterPosition = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 3, 1, 0);

        var unitList = new List<NetworkObject>();

        // Spawn Player units on the field
        for (int i = 0; i < unitCountPerPlayer; i++)
        {
            // place units in circle around player center
            Vector3 spawnPosition = playerSpawnCenterPosition + new Vector3(Mathf.Cos(i * Mathf.PI * 2 / unitCountPerPlayer), 0, Mathf.Sin(i * Mathf.PI * 2 / unitCountPerPlayer));
            // spawn unit
            var networkUnitObject = runner.Spawn(_playerUnitPrefab, spawnPosition, Quaternion.identity, player);

            if (networkUnitObject == null)
            {
                Debug.LogError("Failed to spawn network unit object.");
                return;
            }

            networkUnitObject.GetComponent<Unit>().SetOwner(player); // Set unit owner directly (in case it may be given to other player) 

            unitList.Add(networkUnitObject);

        }
        // Keep track of the player avatars for easy access
        _spawnedPlayers.Add(player, unitList);
        Debug.Log($"Spawned {unitList.Count} units for player: {player}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)  // not in Phoron turorial somehow, but looks like it needed
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

        // Обрабатываем направление
        if (Input.GetKey(KeyCode.W))
            data.direction += Vector3.forward;
        if (Input.GetKey(KeyCode.S))
            data.direction += Vector3.back;
        if (Input.GetKey(KeyCode.A))
            data.direction += Vector3.left;
        if (Input.GetKey(KeyCode.D))
            data.direction += Vector3.right;

        // Если есть новая цель
        if (_hasPendingTarget)
        {
            data.targetPosition = _pendingTargetPosition;
            data.timestamp = Time.time; // Метка времени
            _hasPendingTarget = false; // Сбрасываем флаг после передачи
        }

        input.Set(data);
    }



    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void HandleDestinationInput(Vector3 targetPosition)
    {
        // если есть хотя бы один выделенный объект
        if (_selectionManager.SelectedUnits.Count == 0)
            return;

        // делаем instantiate _DestinationMarkerPrefab префабу в этой точке, и удаляем его через 2 секунды
        var marker = Instantiate(_DestinationMarkerPrefab, targetPosition, Quaternion.identity);
        Destroy(marker, 2);
        
        Debug.Log($"Received destination input: {targetPosition}");
        _pendingTargetPosition = targetPosition; // Сохраняем точку назначения
        _hasPendingTarget = true; // Устанавливаем флаг
    }

    // Obsolete - Old way to set destination point
    public void SetDestinationPoint(Vector3 pointWorld)
    {

        // если есть хотя бы один выделенный объект
        if (_selectionManager.SelectedUnits.Count == 0)
            return;

        // делаем instantiate _DestinationMarkerPrefab префабу в этой точке, и удаляем его через 2 секунды
        var marker = Instantiate(_DestinationMarkerPrefab, pointWorld, Quaternion.identity);
        Destroy(marker, 2);

        Vector3 center = Vector3.zero;
        foreach (var unit in _selectionManager.SelectedUnits)
        {
            center += unit.transform.position;
        }
        center /= _selectionManager.SelectedUnits.Count;

        // Для каждого юнита устанавливаем цель с учётом смещения
        foreach (var unit in _selectionManager.SelectedUnits)
        {
            Vector3 offset = unit.transform.position - center;
         //   unit.SetTarget(pointWorld + offset);
        }

    }
}
