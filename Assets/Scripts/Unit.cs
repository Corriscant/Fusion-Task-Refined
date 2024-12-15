using Fusion;
using UnityEngine;

public class Unit : NetworkBehaviour
{
    public GameObject body;
    public GameObject selectedIndicator;

    public float speed = 5;

    private float lastProcessedTimestamp = -1f; // ��������� ������������ ����
    private float lastPredictedTimestamp = -1f; // ��������� ������������� ����

    [Networked] private Vector3 TargetPosition { get; set; } = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
    [Networked] private bool HasTarget { get; set; } = false;


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
            _cc.Move(5 * data.direction * Runner.DeltaTime);
        }
    }
    */

    public override void FixedUpdateNetwork()
    {
        Vector3 direction = Vector3.zero;

        // 1. ��������� �����
        if (GetInput(out NetworkInputData input))
        {
            if (Object.HasStateAuthority) // ���� ������������ Input
            {
                ProcessHostInput(input);
            }

            if (Object.HasInputAuthority && BasicSpawner.Instance.HasPendingTarget) // ������ ��������� ������������
            {
                // ������ ������������� �������� ��� ����������� �� HasTarget
                if (CheckStop(input.targetPosition))
                {
                    Debug.Log($"Client predicts stop for unit {gameObject.name}");
                    direction = Vector3.zero; // ������������ ���������
                }
                else
                {
                    if (HasTarget)
                    {
                        direction = PredictClientDirection(input);
                    }
                }
            }
        }

        // 2. �������� ��� �����
        if (Object.HasStateAuthority && HasTarget)
        {
            direction = (TargetPosition - transform.position).normalized;

            if (CheckStop(TargetPosition))
            {
                ClearTarget();
                direction = Vector3.zero; // ��������� ��� �����
            }
        }

        // 3. ������ ����� Move
        if (direction != Vector3.zero)
        {
            _cc.Move(direction * speed * Runner.DeltaTime);
        }
    }


    private void ProcessHostInput(NetworkInputData input)
    {
        if (input.timestamp > 0 && input.timestamp > lastProcessedTimestamp) // ��������� ���������� ������
        {
            TargetPosition = input.targetPosition;
            HasTarget = true;
            lastProcessedTimestamp = input.timestamp;

            Debug.Log($"Host processed new target for unit {gameObject.name}: {TargetPosition} at {input.timestamp}");
        }
    }


    private Vector3 PredictClientDirection(NetworkInputData input)
    {
        if (input.timestamp > lastPredictedTimestamp) // ���������, ����� �� ��� ����
        {
            lastPredictedTimestamp = input.timestamp; // ��������� ����� �������
            Debug.Log($"Client predicting movement for unit {gameObject.name}: {input.targetPosition} at {input.timestamp}");
            return (input.targetPosition - transform.position).normalized;
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

    private void ClearTarget()
    {
        Debug.Log($"Unit {gameObject.name} reached target: {TargetPosition}");
        TargetPosition = Vector3.zero; // ���������� ������ � ����
        HasTarget = false; // ���������� ����
    }


}
