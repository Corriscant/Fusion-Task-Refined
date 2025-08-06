using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;

/// <summary>
/// Unit class represents a controllable unit in the game.
/// </summary>
public class Unit : NetworkBehaviour, IPositionable
{
    public GameObject body;
    private NetworkCharacterController _cc;

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

    [Networked] public PlayerRef PlayerOwner { get; private set; }
    [Networked] private Vector3 TargetPosition { get; set; } = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
    [Networked] private bool HasTarget { get; set; } = false;

    // Last server tick for processed command (defense from "old commands" being processed)
    private float lastCommandServerTick;

    public override void Spawned()
    {
        Log($"{GetLogCallPrefix(GetType())} Unit {gameObject.name} spawned.");
        UnitRegistry.Units[Object.Id.Raw] = this;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Log($"{GetLogCallPrefix(GetType())} Unit {gameObject.name} despawned. HasState: {hasState}");
        UnitRegistry.Units.Remove(Object.Id.Raw);
        base.Despawned(runner, hasState);
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
    /// function forming a list of units affected in input
    /// </summary>
    // Refactored to use UnitRegistry for performance.
    public static List<Unit> GetUnitsInInput(NetworkInputData input)
    {
        List<Unit> units = new();
        for (int i = 0; i < input.unitCount; i++)
        {
            // The key is now uint, so we can look it up directly.
            if (UnitRegistry.Units.TryGetValue(input.unitIds[i], out var unit))
            {
                units.Add(unit);
            }
        }
        return units;
    }

    /// <summary>
    /// Function finds unitTargetPosition, taking into account the offset of itself relative to the center of the group of units
    /// </summary>
    public Vector3 GetUnitTargetPosition(Vector3 center, Vector3 bearingTargetPosition)
    {
        Vector3 offset = center - transform.position;
        // here you can limit the offset so that the units are not too far apart
        offset = Vector3.ClampMagnitude(offset, ConnectionManager.Instance.PlayerManager.unitAllowedOffset); // limit the offset to 5 meters
                                                                                          // find the personal position for the Target of this unit, taking into account that it is offset relative to the center of the selected units
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
                string materialName = $"Materials/UnitPayer{materialIndex}_Material";
                Material material = Resources.Load<Material>(materialName);
                if (material != null)
                {
                    unit.GetComponentInChildren<MeshRenderer>().material = material;
                    Log($"{GetLogCallPrefix(GetType())} Material successfully loaded and applied.");
                }
            }
        }
        else
        {
            LogError($"{GetLogCallPrefix(GetType())} Failed to find the unit from unitId.");
        }
    }
}
