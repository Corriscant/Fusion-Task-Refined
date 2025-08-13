using VContainer;
using VContainer.Unity;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;

/// <summary>
/// Registers application-level dependencies for VContainer.
/// </summary>
public class ProjectLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {

        Log($"{GetLogCallPrefix(GetType())} RegisterComponentInHierarchy!");

        builder.RegisterComponentInHierarchy<ConnectionManager>()
            .As<IConnectionService>()
            .AsSelf();
        // Note: RegisterComponentInHierarchy already registers as Singleton by default in VContainer.
        // The .WithLifetime(Lifetime.Singleton) call is not needed and causes CS1061.
    }
}
