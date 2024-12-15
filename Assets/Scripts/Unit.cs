using Fusion;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Windows;

public class Unit : NetworkBehaviour
{
    public GameObject body;
    public GameObject selectedIndicator;

    public float speed = 5;

    private float lastPredictedTimestamp = -1f; // ��������� ������������� ����

    [Networked] private Vector3 TargetPosition { get; set; } = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
    [Networked] private bool HasTarget { get; set; } = false;
    [Networked] public float LastCommandTimestamp { get; set; } // ��������� ������������ ������


    private bool _selected;
    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            selectedIndicator?.SetActive(_selected);
        }
    }

    // Unit owner
    [Networked] public PlayerRef Owner { get; private set; }

    public override void Spawned()
    {
        // ����� ����� �������� ������ ��� ������ ��������� ��������,
        // �� �������� ��������������� ����� SetOwner �����.
        Debug.Log($"Unit {gameObject.name} spawned. Awaiting owner assignment.");
    }


    public void SetOwner(PlayerRef newOwner)
    {
        Owner = newOwner;
    }

    public bool IsOwnedBy(PlayerRef player)
    {
        return Owner == player;
    }

    private NetworkCharacterController _cc;

    private void Awake()
    {
        if (body == null)
        {
            Debug.LogError("Body is not set.");
            return;
        }

        selectedIndicator?.SetActive(false);
        _cc = GetComponent<NetworkCharacterController>();
    }

    /*  // Old Manual moves
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            _cc.Move(5 * data.direction * NetRunner.DeltaTime);
        }
    }
    */

    // ������� ����������� ������ unit ���������� � input 
    public static List<Unit> GetUnitsInInput(NetworkInputData input)
    {
        List<Unit> units = new();
        for (int i = 0; i < input.unitCount; i++)
        {
            // ����� �� ���� �������� � �����, � ������������ NetworkObject � ���������� �� Id � Id �� input
            foreach (var obj in FindObjectsByType<NetworkObject>(FindObjectsSortMode.None))
            {
                if (obj.Id.Raw == input.unitIds[i])
                {
                    var unit = obj.GetComponent<Unit>();
                    if (unit != null)
                    {
                        units.Add(unit);
                    }
                }
            }
        }
        return units;
    }


    // ������� ������� unitTargetPosition, � ������ �������� ���� ������������ ������ ������ ������
    public Vector3 GetUnitTargetPosition(Vector3 center, Vector3 bearingTargetPosition)
    {
        Vector3 offset = center - transform.position;
        // ��� ����� ���������� offset, ����� ����� �� ���� ������� ������ ���� �� �����
        // ������ ������������ ������� ��� Target ����� �����, � ������ ����, ��� �� ������ ������������ ������ ���������� ������
        Vector3 unitTargetPosition = bearingTargetPosition - offset;
        return unitTargetPosition;
    }

    public override void FixedUpdateNetwork()
    {
        Vector3 direction = Vector3.zero;

        // 1. ��������� �����
        if (GetInput(out NetworkInputData input))
        {
            // ������ ��������� ������������
            if (Object.HasInputAuthority && BasicSpawner.Instance.HasPendingTarget) 
            {
                // find center of selected units
                var center = BasicSpawner.Instance.GetCenterOfUnits( BasicSpawner.Instance.SelectionManagerLink.SelectedUnits );
                Vector3 unitTargetPosition = GetUnitTargetPosition(center, input.targetPosition);

                // ������ ������������� �������� ��� ����������� �� HasTarget
                if (CheckStop(unitTargetPosition))
                {
                    Debug.Log($"Client predicts stop for unit {gameObject.name}");
                    direction = Vector3.zero; // ������������ ���������
                }
                else
                {
                 //   if (HasTarget)
                    {
                        direction = PredictClientDirection(input, unitTargetPosition);
                    }
                }
            }
        }

        // 2. �������� ��� �����
        if (Object.HasStateAuthority && HasTarget)
        {
            if (CheckStop(TargetPosition))
            {
                ClearTarget();
                direction = Vector3.zero; // ��������� ��� �����
            }
            else
            {
                direction = (TargetPosition - transform.position).normalized;
            }
        }

        // 3. ������ ����� Move
        if (direction != Vector3.zero)
        {
            _cc.Move(direction * speed * Runner.DeltaTime);
        }
    }

    private Vector3 PredictClientDirection(NetworkInputData input, Vector3 unitTragetPosition)
    {
        if (input.timestamp > lastPredictedTimestamp) // ���������, ����� �� ��� ����
        {
            lastPredictedTimestamp = input.timestamp; // ��������� ����� �������
            Debug.Log($"Client predicting movement for unit {gameObject.name}: input.targetPosition = {input.targetPosition}, unitTragetPosition = {unitTragetPosition} at {input.timestamp}");
            return (unitTragetPosition - transform.position).normalized;
        }

        return Vector3.zero; // �������� ��������, ���� ���� �� �����
    }

    private bool CheckStop(Vector3 target)
    {
        // ��������� ���������� ���� (���������� ������)
        float distance = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(target.x, target.z)
        );

        return distance < 1.0f;
    }

    public void HostSetTarget(Vector3 targetPosition, float timestamp)
    {
        if (Object.HasStateAuthority && (timestamp > LastCommandTimestamp))
        {
            TargetPosition = targetPosition;
            HasTarget = true;
            LastCommandTimestamp = timestamp;

            Debug.Log($"Unit {gameObject.name} received new target at {timestamp}");
        }
    }

    private void ClearTarget()
    {
        Debug.Log($"Unit {gameObject.name} reached target: {TargetPosition}");
        TargetPosition = Vector3.zero; // ���������� ������ � ����
        HasTarget = false; // ���������� ����
    }


}
