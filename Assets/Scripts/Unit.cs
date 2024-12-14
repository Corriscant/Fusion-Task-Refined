using Fusion;
using UnityEngine;

public class Unit : NetworkBehaviour
{
    public GameObject body;
    public GameObject selectedIndicator;

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

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            _cc.Move(5 * data.direction * Runner.DeltaTime);
        }
    }
}
