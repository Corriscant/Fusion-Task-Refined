using System;
using System.Threading.Tasks;
using Fusion;

/// <summary>
/// Provides access to network connection functionality.
/// </summary>
public interface IConnectionService
{
    /// <summary>
    /// Current Fusion runner instance.
    /// </summary>
    NetworkRunner Runner { get; }

    /// <summary>
    /// Indicates whether a connection attempt is in progress.
    /// </summary>
    bool IsConnecting { get; }

    /// <summary>
    /// Triggered when the connection process starts.
    /// </summary>
    event Action ConnectingStarted;

    /// <summary>
    /// Triggered when the connection has been successfully established.
    /// </summary>
    event Action Connected;

    /// <summary>
    /// Triggered when the connection has been lost or failed.
    /// </summary>
    event Action Disconnected;

    /// <summary>
    /// Starts a new game session in the specified mode.
    /// </summary>
    Task StartGame(GameMode mode);
}
