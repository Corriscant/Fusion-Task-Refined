using Fusion;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Windows;

public class Unit : NetworkBehaviour
{
    public GameObject body;
    public GameObject selectedIndicator;

    public float speed = 5;

    private float lastPredictedTimestamp = -1f; // Последний предсказанный ввод

    [Networked] private Vector3 TargetPosition { get; set; } = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
    [Networked] private bool HasTarget { get; set; } = false;
    [Networked] public float LastCommandTimestamp { get; set; } // Последний обработанный приказ


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

    /*  // Old Manual moves
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            _cc.Move(5 * data.direction * NetRunner.DeltaTime);
        }
    }
    */

    // функция формирующая список unit затронутых в input 
    public static List<Unit> GetUnitsInInput(NetworkInputData input)
    {
        List<Unit> units = new();
        for (int i = 0; i < input.unitCount; i++)
        {
            // бежим по всем объектам в сцене, с компонентами NetworkObject и сравниваем их Id с Id из input
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


    // функция находит unitTargetPosition, с учетом смещения себя относительно центра группы юнитов
    public Vector3 GetUnitTargetPosition(Vector3 center, Vector3 bearingTargetPosition)
    {
        Vector3 offset = center - transform.position;
        // тут можно ограничить offset, чтобы юниты не были слишком далеко друг от друга
        // найдем персональную позицию для Target этого юнита, с учетом того, что он смещен относительно центра выделенных юнитов
        Vector3 unitTargetPosition = bearingTargetPosition - offset;
        return unitTargetPosition;
    }

    public override void FixedUpdateNetwork()
    {
        Vector3 direction = Vector3.zero;

        // 1. Обработка ввода
        if (GetInput(out NetworkInputData input))
        {
            // Клиент выполняет предсказание
            if (Object.HasInputAuthority && BasicSpawner.Instance.HasPendingTarget) 
            {
                // find center of selected units
                var center = BasicSpawner.Instance.GetCenterOfUnits( BasicSpawner.Instance.SelectionManagerLink.SelectedUnits );
                Vector3 unitTargetPosition = GetUnitTargetPosition(center, input.targetPosition);

                // Клиент предсказывает движение без зависимости от HasTarget
                if (CheckStop(unitTargetPosition))
                {
                    Debug.Log($"Client predicts stop for unit {gameObject.name}");
                    direction = Vector3.zero; // Предсказание остановки
                }
                else
                {
                 //   if (HasTarget)
                    {
                        direction = PredictClientDirection(input, unitTargetPosition);
                    }
                }
            }
        }

        // 2. Движение для хоста
        if (Object.HasStateAuthority && HasTarget)
        {
            if (CheckStop(TargetPosition))
            {
                ClearTarget();
                direction = Vector3.zero; // Остановка для хоста
            }
            else
            {
                direction = (TargetPosition - transform.position).normalized;
            }
        }

        // 3. Единый вызов Move
        if (direction != Vector3.zero)
        {
            _cc.Move(direction * speed * Runner.DeltaTime);
        }
    }

    private Vector3 PredictClientDirection(NetworkInputData input, Vector3 unitTragetPosition)
    {
        if (input.timestamp > lastPredictedTimestamp) // Проверяем, новый ли это ввод
        {
            lastPredictedTimestamp = input.timestamp; // Обновляем метку времени
            Debug.Log($"Client predicting movement for unit {gameObject.name}: input.targetPosition = {input.targetPosition}, unitTragetPosition = {unitTragetPosition} at {input.timestamp}");
            return (unitTragetPosition - transform.position).normalized;
        }

        return Vector3.zero; // Никакого движения, если ввод не новый
    }

    private bool CheckStop(Vector3 target)
    {
        // Проверяем достижение цели (игнорируем высоту)
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

            Debug.Log($"Unit {gameObject.name} received new target at {timestamp}");
        }
    }

    private void ClearTarget()
    {
        Debug.Log($"Unit {gameObject.name} reached target: {TargetPosition}");
        TargetPosition = Vector3.zero; // Сбрасываем данные о цели
        HasTarget = false; // Сбрасываем флаг
    }


}
