using Fusion;
using UnityEngine;

public class Unit : NetworkBehaviour
{
    public GameObject body;
    public GameObject selectedIndicator;

    public float speed = 5;

    [Networked] private Vector3 TargetPosition { get; set; } // Позиция цели
    [Networked] private bool HasTarget { get; set; } // Флаг активности цели

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
        // Здесь можно оставить логику для других начальных действий,
        // но владелец устанавливается через SetOwner извне.
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
        Debug.Log($"Unit {gameObject.name} | TargetPosition: {TargetPosition}, HasTarget: {HasTarget}, Position: {transform.position}");

        if (HasTarget)
        {
            Debug.Log($"Unit {gameObject.name} is moving to target: {TargetPosition}");
            Vector3 direction = (TargetPosition - transform.position).normalized;
            _cc.Move(direction * speed * Runner.DeltaTime);

            // Если достигли цели (игнорируем высоту)
            if (Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(TargetPosition.x, TargetPosition.z)
                ) < 1.0f)
            {
                ClearTarget(); // Цель достигнута
            }
        }
    }

    public void SetTarget(Vector3 targetPosition)
    {
        Debug.Log($"Unit {gameObject.name} received new target: {targetPosition}");

        if (Object.HasInputAuthority)
        {
            Debug.Log("Unit has authority to set target.");
            TargetPosition = targetPosition;
            HasTarget = true; // Устанавливаем флаг
        }
    }

    private void ClearTarget()
    {
        TargetPosition = Vector3.zero; // Сбрасываем позицию цели (можно оставить это для удобства)
        HasTarget = false; // Сбрасываем флаг
    }

}
