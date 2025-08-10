using Fusion;
using UnityEngine;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;

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
        _meshRenderer = _meshRenderer != null ? _meshRenderer : GetComponentInChildren<MeshRenderer>();

        if (_meshRenderer == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} MeshRenderer not found for Cursor Index[{index}].");
            return;
        }

        var material = Resources.Load<Material>($"Materials/UnitPayer{index}_Material");

        if (material == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} Material not found for Cursor Index[{index}].");
            return;
        }

        if (_meshRenderer != null && material != null)
        {
            _meshRenderer.material = material;
            Log($"{GetLogCallPrefix(GetType())} Material successfully loaded and applied to Cursor Index[{index}].");
        }
    }
}
