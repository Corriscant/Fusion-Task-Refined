using Fusion;

namespace FusionTask.Networking
{
    /// <summary>
    /// A data structure that represents a command sent from a player to the host.
    /// </summary>
    public class Command
    {
        public PlayerRef Player;         // ID of the client from which the command came
        public NetworkInputData Input;  // Full data structure from NetworkInputData
    }
}
