using Fusion;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FusionTask.Networking
{
    /// <summary>
    /// Injects dependencies into network-spawned objects before their spawn callbacks run.
    /// </summary>
    // TODO: Attach this component to a GameObject in the scene.
    public class NetworkObjectInjector : NetworkRunnerCallbacksBase
    {
        private IObjectResolver _resolver;

        [Inject]
        public void Construct(IObjectResolver resolver)
        {
            _resolver = resolver;
        }

        public override void OnBeforeSpawned(NetworkRunner runner, NetworkObject obj)
        {
            if (obj == null)
            {
                return;
            }

            if (_resolver == null)
            {
                return;
            }

            _resolver.InjectGameObject(obj.gameObject);
        }
    }
}
