using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;
using FusionTask.Infrastructure;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;
using VContainer;

namespace FusionTask.Networking
{
    /// <summary>
    /// Handles clearing registries when scenes are loaded or network scenes change.
    /// </summary>
    public class SceneLoadHandler : NetworkRunnerCallbacksBase
    {
        private IUnitRegistry _unitRegistry;
        private IPlayerCursorRegistry _playerCursorRegistry;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _unitRegistry.Clear();
            _playerCursorRegistry.Clear();
        }

        public override void OnSceneLoadStart(NetworkRunner runner)
        {
            Log($"{GetLogCallPrefix(GetType())} OnSceneLoadStart triggered");
            _unitRegistry.Clear();
            _playerCursorRegistry.Clear();
        }

        public override void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Log($"{GetLogCallPrefix(GetType())} OnShutdown triggered with reason: {shutdownReason}");
            _unitRegistry.Clear();
            _playerCursorRegistry.Clear();
        }

        [Inject]
        public void Construct(IUnitRegistry unitRegistry, IPlayerCursorRegistry playerCursorRegistry)
        {
            _unitRegistry = unitRegistry;
            _playerCursorRegistry = playerCursorRegistry;
        }
    }
}
