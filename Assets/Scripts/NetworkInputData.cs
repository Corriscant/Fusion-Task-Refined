using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector3 direction;       // Для движения
    public Vector3 targetPosition;  // Для точки назначения
    public float timestamp;         // Время/счётчик, чтобы отличить новый ввод
}

