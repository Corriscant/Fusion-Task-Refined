using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;
using VContainer;

/// <summary>
/// Unit class represents a controllable unit in the game.
/// </summary>
[RequireComponent(typeof(Selectable))]
public class Unit : NetworkBehaviour, IPositionable, ISelectableProvider
{
    public GameObject body;
    private NetworkCharacterController _cc;
    private MeshRenderer _meshRenderer; // Cache for MeshRenderer to avoid repeated lookups
    private IUnitRegistry _unitRegistry;

    /// <summary>
    /// Cached MeshRenderer component of the unit.
    /// </summary>
    public MeshRenderer MeshRenderer
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

    /// <summary>
    /// Material index, for passing to other clients via RPC
    /// </summary>
    public int materialIndex;

    #region IPositionable
    /// <summary>
    /// Implementation of IPositionable interface to provide position of the unit. (Used in ListExtensions.GetCenter)
    /// </summary>
    public virtual Vector3 Position => transform.position;
    #endregion IPositionable

    #region ISelectableProvider
    private ISelectable _selectable;
    /// <summary>
    /// Provides access to the Selectable component associated with this unit.
    /// </summary>
    public ISelectable Selectable
    {
        get
        {
            if (_selectable == null)
            {
                _selectable = GetComponent<Selectable>();
            }
            return _selectable;
        }
    }
    #endregion ISelectableProvider

    [Networked] public PlayerRef PlayerOwner { get; private set; }
    [Networked] private Vector3 TargetPosition { get; set; } = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
    [Networked] private bool HasTarget { get; set; } = false;

    // Last server tick for processed command (defense from "old commands" being processed)
    private float lastCommandServerTick;

    public override void Spawned()
    {
        base.Spawned();
        Log($"{GetLogCallPrefix(GetType())} Unit {gameObject.name} spawned.");

        // Instant, cached access. No scene search.
        VContainerBridge.Container.Inject(this);

        if (_unitRegistry == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} Unit registry injection failed.");
            return;
        }

        _unitRegistry.Register(Object.Id.Raw, this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Log($"{GetLogCallPrefix(GetType())} Unit {gameObject.name} despawned. HasState: {hasState}");
        _unitRegistry.Unregister(Object.Id.Raw);
        base.Despawned(runner, hasState);
    }

    [Inject]
    public void Construct(IUnitRegistry unitRegistry)
    {
        _unitRegistry = unitRegistry;
    }

    public void SetOwner(PlayerRef newOwner) => PlayerOwner = newOwner;

    public bool IsOwnedBy(PlayerRef player) => PlayerOwner == player;

    private void Awake()
    {
        if (body == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} Body is not set.");
            return;
        }

        _cc = GetComponent<NetworkCharacterController>();

    }

    /// <summary>
    /// Limits the offset magnitude so that the units are not too far apart.
    /// </summary>
    /// <param name="offset">Original offset from the group center.</param>
    /// <param name="allowedOffset">Maximum allowed distance.</param>
    public Vector3 ClampOffset(Vector3 offset, float allowedOffset)
    {
        // limit the offset to the allowed distance
        return Vector3.ClampMagnitude(offset, allowedOffset);
    }

    /// <summary>
    /// Finds the personal target position for this unit, taking into account that it is offset relative to the center of the selected units.
    /// </summary>
    /// <param name="offset">Offset relative to the group center.</param>
    /// <param name="bearingTargetPosition">Desired position for the group's center.</param>
    public Vector3 GetUnitTargetPosition(Vector3 offset, Vector3 bearingTargetPosition)
    {
        Vector3 unitTargetPosition = bearingTargetPosition - offset;
        return unitTargetPosition;
    }

    public override void FixedUpdateNetwork()
    {
        // Movement for the host
        if (Object.HasStateAuthority && HasTarget)
        {
            if (HasReachedTarget(TargetPosition))
            {
                Log($"{GetLogCallPrefix(GetType())} Unit {gameObject.name} reached target: {TargetPosition}");

                StopUnit();
            }
            else
            {
                MoveUnit();
            }
        }
    }

    private void StopUnit()
    {
        ClearTarget();
        _cc.Move(Vector3.zero);
    }

    private void MoveUnit()
    {
        Vector3 direction = (TargetPosition - transform.position).normalized;
        _cc.Move(direction);
    }

    /// <summary>
    /// Function checks if the unit has reached the target position.
    /// </summary>
    private bool HasReachedTarget(Vector3 target)
    {
        // Check if the target is reached (ignore height)
        float distance = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(target.x, target.z)
        );

        return distance < 1.0f;
    }

    public void HostSetTarget(Vector3 targetPosition, float serverTick)
    {
        if (Object.HasStateAuthority && (serverTick > lastCommandServerTick))
        {
            TargetPosition = targetPosition;
            HasTarget = true;
            lastCommandServerTick = serverTick;

            Log($"{GetLogCallPrefix(GetType())} Unit {gameObject.name} received new target at {serverTick}");
        }
    }

    private void ClearTarget()
    {
        TargetPosition = Vector3.zero; // Reset target data
        HasTarget = false; 
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, HostMode = RpcHostMode.SourceIsServer)]
    public void RPC_RelaySpawnedUnitInfo(NetworkId unitId, String unitName, int materialIndex)
    {
        Log($"{GetLogCallPrefix(GetType())} RPC_RelaySpawnedUnitInfo {unitName}");

        if (Runner.TryFindObject(unitId, out var networkObject))
        {
            if (networkObject.TryGetComponent<Unit>(out var unit))
            {
                unit.name = unitName;
                unit.materialIndex = materialIndex;
                MaterialApplier.ApplyMaterial(unit.MeshRenderer, materialIndex, "Unit");
            }
        }
        else
        {
            LogError($"{GetLogCallPrefix(GetType())} Failed to find the unit from unitId.");
        }
    }
}
