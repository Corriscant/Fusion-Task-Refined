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
    /// <summary>
    /// We use a material index to determine which material to apply to the cursor. React on Networked changes to this index to update the cursor's appearance.
    /// </summary>
    [Networked, OnChangedRender(nameof(OnMaterialIndexChanged))]
    public int MaterialIndex { get; set; }

    private MeshRenderer _meshRenderer;

    public override void Spawned()
    {
        base.Spawned();
        PlayerCursorRegistry.Register(Object.InputAuthority, this);
        ApplyMaterial(MaterialIndex);
    }

    private void OnMaterialIndexChanged()
    {
       ApplyMaterial(MaterialIndex);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        PlayerCursorRegistry.Unregister(Object.InputAuthority);
        base.Despawned(runner, hasState);
    }

    private void ApplyMaterial(int index)
    {
        if (_meshRenderer == null)
        {
            _meshRenderer = GetComponentInChildren<MeshRenderer>();
        }

        if (_meshRenderer == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} MeshRenderer not found for Cursor Index[{index}].");
            return;
        }

        var material = PlayerMaterialProvider.GetMaterial(index);

        if (material == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} Material not found for Cursor Index[{index}].");
            return;
        }

        _meshRenderer.material = material;
        Log($"{GetLogCallPrefix(GetType())} Material successfully loaded and applied to Cursor Index[{index}].");
    }
}
