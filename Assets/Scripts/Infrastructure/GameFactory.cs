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
    public class GameFactory : NetworkObjectProviderDefault, IGameFactory
    {
        [Header("Prefabs")]
        [SerializeField] private NetworkObject _unitPrefab;
        [SerializeField] private NetworkObject _cursorPrefab;

        [Header("Warmup Settings")]
        [SerializeField] private int _unitWarmup = 10;
        [SerializeField] private int _cursorWarmup = 4;

        private AsyncObjectPool<NetworkObject> _unitPool;
        private AsyncObjectPool<NetworkObject> _cursorPool;

        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
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

        /// <summary>
        /// Spawns a unit for the given owner at the specified position and rotation.
        /// </summary>
        public Unit CreateUnit(NetworkRunner runner, Vector3 position, Quaternion rotation, PlayerRef owner)
        {
            NetworkObject spawned = runner.Spawn(
                _unitPrefab,
                position,
                rotation,
                owner,
                (r, obj) =>
                {
                    var unit = obj.GetComponent<Unit>();
                    unit.ResetNetworkState();
                    unit.SetOwner(owner);
                }
            );

            return spawned.GetComponent<Unit>();
        }

        /// <summary>
        /// Asynchronously spawns a unit for the given owner at the specified position and rotation.
        /// Useful when assets require asynchronous loading.
        /// </summary>
        public Task<Unit> CreateUnitAsync(NetworkRunner runner, Vector3 position, Quaternion rotation, PlayerRef owner)
        {
            return Task.FromResult(CreateUnit(runner, position, rotation, owner));
        }

        /// <summary>
        /// Spawns a cursor for the given owner at the specified position and rotation.
        /// </summary>
        public PlayerCursor CreateCursor(NetworkRunner runner, Vector3 position, Quaternion rotation, PlayerRef owner)
        {
            NetworkObject spawned = runner.Spawn(
                _cursorPrefab,
                position,
                rotation,
                owner,
                (r, obj) =>
                {
                    var cursor = obj.GetComponent<PlayerCursor>();
                    cursor.ResetState();
                }
            );

            return spawned.GetComponent<PlayerCursor>();
        }

        /// <summary>
        /// Asynchronously spawns a cursor for the given owner at the specified position and rotation.
        /// Useful when assets require asynchronous loading.
        /// </summary>
        public Task<PlayerCursor> CreateCursorAsync(NetworkRunner runner, Vector3 position, Quaternion rotation, PlayerRef owner)
        {
            return Task.FromResult(CreateCursor(runner, position, rotation, owner));
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

        /// <summary>
        /// Provides an instance of the requested prefab from the appropriate pool.
        /// </summary>
        protected override NetworkObject InstantiatePrefab(NetworkRunner runner, NetworkObject prefab)
        {
            if (prefab == _unitPrefab)
            {
                var obj = _unitPool.Get().GetAwaiter().GetResult();
                obj.GetComponent<Unit>().ResetState();
                return obj;
            }

            if (prefab == _cursorPrefab)
            {
                var obj = _cursorPool.Get().GetAwaiter().GetResult();
                return obj;
            }

            var instance = Instantiate(prefab);
            if (_resolver != null)
            {
                _resolver.InjectGameObject(instance.gameObject);
            }
            return instance;
        }

        /// <summary>
        /// Returns the instance to the corresponding pool instead of destroying it.
        /// </summary>
        protected override void DestroyPrefabInstance(NetworkRunner runner, NetworkPrefabId prefabId, NetworkObject instance)
        {
            Release(instance);
        }
    }
}

