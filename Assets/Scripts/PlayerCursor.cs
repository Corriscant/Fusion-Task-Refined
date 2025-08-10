using Fusion;
using UnityEngine;

/// <summary>
/// Networked cursor data for a player.
/// </summary>
public class PlayerCursor : NetworkBehaviour
{
    [Networked] public Vector3 CursorPosition { get; set; }
    [Networked] public int MaterialIndex { get; set; }

    private MeshRenderer _meshRenderer;

    public override void Spawned()
    {
        base.Spawned();
        PlayerCursorRegistry.Register(Object.InputAuthority, this);
        ApplyMaterial(MaterialIndex);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        PlayerCursorRegistry.Unregister(Object.InputAuthority);
        base.Despawned(runner, hasState);
    }

    private void ApplyMaterial(int index)
    {
        _meshRenderer ??= GetComponentInChildren<MeshRenderer>();
        var material = Resources.Load<Material>($"Materials/UnitPayer{index}_Material");
        if (_meshRenderer != null && material != null)
        {
            _meshRenderer.material = material;
        }
    }
}
