using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using FusionTask.Gameplay;

namespace FusionTask.Infrastructure
{
    /// <summary>
    /// Creates and recycles networked objects through pooling.
    /// </summary>
    // TODO: Attach this component to a GameObject in the scene and assign prefab references.
    public class GameFactory : MonoBehaviour, IGameFactory, INetworkObjectPool
    {
        [Header("Prefabs")]
        [SerializeField] private NetworkObject _unitPrefab;
        [SerializeField] private NetworkObject _cursorPrefab;

        [Header("Warmup Settings")]
        [SerializeField] private int _unitWarmup = 10;
        [SerializeField] private int _cursorWarmup = 4;

        private AsyncObjectPool<NetworkObject> _unitPool;
        private AsyncObjectPool<NetworkObject> _cursorPool;

        private IConnectionService _connectionService;
        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IConnectionService connectionService, IObjectResolver resolver)
        {
            _connectionService = connectionService;
            _resolver = resolver;
        }

        private void Awake()
        {
            _unitPool = new AsyncObjectPool<NetworkObject>(() => CreateInstance(_unitPrefab));
            _cursorPool = new AsyncObjectPool<NetworkObject>(() => CreateInstance(_cursorPrefab));
        }

        /// <summary>
        /// Pre-warms the pools on startup.
        /// </summary>
        public async Task WarmupPools()
        {
            await _unitPool.Warmup(_unitWarmup);
            await _cursorPool.Warmup(_cursorWarmup);
        }

        private Task<NetworkObject> CreateInstance(NetworkObject prefab)
        {
            var instance = Instantiate(prefab);
            instance.gameObject.SetActive(false);
            if (_resolver != null)
            {
                _resolver.InjectGameObject(instance.gameObject);
            }
            return Task.FromResult(instance);
        }

        public async Task<Unit> CreateUnit(Vector3 position, Quaternion rotation, PlayerRef owner)
        {
            var obj = await _unitPool.Get();
            obj.transform.SetPositionAndRotation(position, rotation);
            var runner = _connectionService.Runner;
            runner.Spawn(obj, position, rotation, owner);
            var unit = obj.GetComponent<Unit>();
            unit.ResetState();
            return unit;
        }

        public async Task<PlayerCursor> CreateCursor(Vector3 position, Quaternion rotation, PlayerRef owner)
        {
            var obj = await _cursorPool.Get();
            obj.transform.SetPositionAndRotation(position, rotation);
            var runner = _connectionService.Runner;
            runner.Spawn(obj, position, rotation, owner);
            var cursor = obj.GetComponent<PlayerCursor>();
            cursor.ResetState();
            return cursor;
        }

        public void Release(NetworkObject obj)
        {
            if (obj == null)
            {
                return;
            }

            if (obj.TryGetComponent<Unit>(out _))
            {
                _unitPool.Release(obj);
            }
            else if (obj.TryGetComponent<PlayerCursor>(out _))
            {
                _cursorPool.Release(obj);
            }
            else
            {
                Destroy(obj.gameObject);
            }
        }

        NetworkObject INetworkObjectPool.AcquireInstance(NetworkRunner runner, NetworkPrefabInfo info)
        {
            if (info.Prefab == _unitPrefab)
            {
                return _unitPool.Get().GetAwaiter().GetResult();
            }

            if (info.Prefab == _cursorPrefab)
            {
                return _cursorPool.Get().GetAwaiter().GetResult();
            }

            var instance = Instantiate(info.Prefab);
            if (_resolver != null)
            {
                _resolver.InjectGameObject(instance.gameObject);
            }
            return instance;
        }

        void INetworkObjectPool.ReleaseInstance(NetworkRunner runner, NetworkObject instance, bool isSceneObject)
        {
            Release(instance);
        }
    }
}

