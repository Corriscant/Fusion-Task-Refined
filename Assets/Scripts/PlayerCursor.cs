using Fusion;
using UnityEngine;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;
using VContainer;

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
    private IPlayerCursorRegistry _playerCursorRegistry;

    /// <summary>
    /// Cached MeshRenderer component of the cursor.
    /// </summary>
    private MeshRenderer MeshRenderer
    {
        get
        {
            if (_meshRenderer == null)
            {
                _meshRenderer = GetComponentInChildren<MeshRenderer>();
            }
            return _meshRenderer;
        }
    }

    public override void Spawned()
    {
        base.Spawned();
        if (_playerCursorRegistry == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} Cursor registry injection failed.");
            return;
        }

        _playerCursorRegistry.Register(Object.InputAuthority, this);
        MaterialApplier.ApplyMaterial(MeshRenderer, MaterialIndex, "Cursor");
    }

    private void OnMaterialIndexChanged()
    {
        MaterialApplier.ApplyMaterial(MeshRenderer, MaterialIndex, "Cursor");
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        _playerCursorRegistry.Unregister(Object.InputAuthority);
        base.Despawned(runner, hasState);
    }

    [Inject]
    public void Construct(IPlayerCursorRegistry playerCursorRegistry)
    {
        _playerCursorRegistry = playerCursorRegistry;
    }
}
