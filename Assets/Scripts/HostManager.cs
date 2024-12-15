using Fusion;
using UnityEngine;

public class HostManager : NetworkBehaviour
{
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        // Вызываем у спавнера обработку команд
        if (BasicSpawner.Instance != null)
        {
            BasicSpawner.Instance.ProcessCommandsFromNetwork();
        }
    }
}
