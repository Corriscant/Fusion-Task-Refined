using UnityEngine;
using VContainer;
using VContainer.Unity;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;
using FusionTask.Networking;
using FusionTask.Gameplay;
using FusionTask.UI;

namespace FusionTask.Infrastructure
{
    /// <summary>
    /// Registers application-level dependencies for VContainer.
    /// </summary>
    public class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private PlayerMaterialProviderSettings _playerMaterialSettings;
        private IPlayerMaterialProvider _playerMaterialProvider;

        protected override void Awake()
        {
            // Initialize the bridge as soon as the scope is awake.
            base.Awake();
            VContainerBridge.SetContainer(this);
        }

        protected override void Configure(IContainerBuilder builder)
        {

            Log($"{GetLogCallPrefix(GetType())} RegisterComponentInHierarchy!");

            builder.RegisterComponentInHierarchy<ConnectionManager>()
                .As<IConnectionService>()
                .As<INetworkEvents>()
                .AsSelf();
            // Note: RegisterComponentInHierarchy already registers as Singleton by default in VContainer.
            // The .WithLifetime(Lifetime.Singleton) call is not needed and causes CS1061.

            builder.RegisterComponentInHierarchy<InputManager>().As<IInputService>();
            builder.RegisterComponentInHierarchy<GameLauncher>();
            builder.RegisterComponentInHierarchy<Panel_Status>();
            builder.RegisterComponentInHierarchy<SelectionManager>();
            builder.RegisterComponentInHierarchy<PlayerManager>();
            builder.RegisterComponentInHierarchy<NetworkObjectInjector>();
            builder.RegisterComponentInHierarchy<SceneLoadHandler>();

            builder.Register<UnitRegistry>(Lifetime.Singleton).As<IUnitRegistry>();
            builder.Register<PlayerCursorRegistry>(Lifetime.Singleton).As<IPlayerCursorRegistry>();

            builder.RegisterInstance(_playerMaterialSettings);
            builder.Register<PlayerMaterialProvider>(Lifetime.Singleton).As<IPlayerMaterialProvider>();
            builder.Register<MaterialApplier>(Lifetime.Singleton).As<IMaterialApplier>();

            builder.RegisterBuildCallback(container =>
            {
                _playerMaterialProvider = container.Resolve<IPlayerMaterialProvider>();
            });
        }

        protected override void OnDestroy()
        {
            _playerMaterialProvider.Release();
            base.OnDestroy();
        }
    }
}
