using Fusion;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;

/// <summary>
/// Unit class represents a controllable unit in the game.
/// </summary>
public class Unit : NetworkBehaviour, IPositionable
{
    public GameObject body;
    public GameObject selectedIndicator;

    /// <summary>
    /// Material index, for passing to other clients via RPC
    /// </summary>
    public int materialIndex; 

    /// <summary>
    /// Implementation of IPositionable interface to provide position of the unit. (Used in ListExtensions.GetCenter)
    /// </summary>
    public virtual Vector3 Position => transform.position;

    [Networked] private Vector3 TargetPosition { get; set; } = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
    [Networked] private bool HasTarget { get; set; } = false;
    [Networked] public float LastCommandTimestamp { get; set; } // Last processed command

    private bool _selected;
    /// <summary>
    /// Selected property indicates whether the unit is currently selected.
    /// </summary>
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
        // Here you can leave logic for other initial actions,
        Log($"{GetLogCallPrefix(GetType())} Unit {gameObject.name} spawned.");
    }

    public void SetOwner(PlayerRef newOwner) => Owner = newOwner;

    public bool IsOwnedBy(PlayerRef player) => Owner == player;

    private NetworkCharacterController _cc;

    private void Awake()
    {
        if (body == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} Body is not set.");
            return;
        }

        selectedIndicator?.SetActive(false);
        _cc = GetComponent<NetworkCharacterController>();
    }

    // function forming a list of units affected in input
    public static List<Unit> GetUnitsInInput(NetworkInputData input)
    {
        List<Unit> units = new();
        for (int i = 0; i < input.unitCount; i++)
        {
            // iterate through all objects in the scene with NetworkObject components and compare their Ids with Ids from input
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

    /// <summary>
    /// function finds unitTargetPosition, taking into account the offset of itself relative to the center of the group of units
    /// </summary>
    public Vector3 GetUnitTargetPosition(Vector3 center, Vector3 bearingTargetPosition)
    {
        Vector3 offset = center - transform.position;
        // here you can limit the offset so that the units are not too far apart
        offset = Vector3.ClampMagnitude(offset, NetworkGameManager.Instance.unitAllowedOffset); // limit the offset to 5 meters
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
                Vector3 direction = (TargetPosition - transform.position).normalized;
                _cc.Move(direction);
            }
        }
    }

    private void StopUnit()
    {
        ClearTarget();
        _cc.Move(Vector3.zero);
    }

    /// <summary>
    /// function checks if the unit has reached the target position.
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

    public void HostSetTarget(Vector3 targetPosition, float timestamp)
    {
        if (Object.HasStateAuthority && (timestamp > LastCommandTimestamp))
        {
            TargetPosition = targetPosition;
            HasTarget = true;
            LastCommandTimestamp = timestamp;

            Log($"{GetLogCallPrefix(GetType())} Unit {gameObject.name} received new target at {timestamp}");
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
            var unit = networkObject.GetComponent<Unit>();
            if (unit != null)
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
