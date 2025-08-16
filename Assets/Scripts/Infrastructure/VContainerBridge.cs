// A static bridge to access the container for objects not created by VContainer.
using UnityEngine;
using VContainer;
using VContainer.Unity;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;

public static class VContainerBridge
{
    public static IObjectResolver Container { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        // Reset on domain reload
        Container = null;
    }

    public static void SetContainer(LifetimeScope scope)
    {
        // Store the container from the provided scope.
        Container = scope.Container;
        Log($"VContainer Bridge Initialized.");
    }
}