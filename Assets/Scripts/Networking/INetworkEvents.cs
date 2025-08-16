using System;
using Fusion;

namespace FusionTask.Networking
{
    /// <summary>
    /// Exposes network-related events for player connections and input handling.
    /// </summary>
    public interface INetworkEvents
    {
        /// <summary>
        /// Triggered when a player joins the session.
        /// </summary>
        event Action<NetworkRunner, PlayerRef> PlayerJoined;

        /// <summary>
        /// Triggered when a player leaves the session.
        /// </summary>
        event Action<NetworkRunner, PlayerRef> PlayerLeft;

        /// <summary>
        /// Triggered to collect input data for the network simulation.
        /// </summary>
        event OnInputHandler Input;
    }
}
