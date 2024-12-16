using Fusion;
using System;
using Unity.VisualScripting;
using UnityEngine;

public class HostManager : NetworkBehaviour
{
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority)
            return;

        // �������� � �������� ��������� ������
        if (BasicSpawner.Instance != null)
        {
            BasicSpawner.Instance.HostProcessCommandsFromNetwork();
        }
    }

}
