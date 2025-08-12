using VContainer;
using VContainer.Unity;

/// <summary>
/// Registers application-level dependencies for VContainer.
/// </summary>
public class ProjectLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<ConnectionManager>()
            .As<IConnectionService>()
            .AsSelf()
            .WithLifetime(Lifetime.Singleton);
    }
}
