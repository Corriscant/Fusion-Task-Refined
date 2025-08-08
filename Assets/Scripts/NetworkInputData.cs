using Fusion;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using System.Collections;

public struct NetworkInputData : INetworkInput
{
    public Vector3 targetPosition;
    public UnitIdList unitIds;
    public int unitCount;

    // The mouse world position is used to show opponent's mouse position in the game world.
    public Vector3 mouseWorldPosition;
}

public struct UnitIdList : INetworkStruct
{
    public const int MaxUnits = 50;
    unsafe fixed uint _unitIds[MaxUnits];
    public int Length => MaxUnits;


    public uint this[int index]
    {
        get { unsafe { return _unitIds[index]; } }
        set { unsafe { _unitIds[index] = value; } }
    }

    public void Clear()
    {
        unsafe
        {
            for (int i = 0; i < MaxUnits; i++)
                _unitIds[i] = 0;
        }
    }
}


