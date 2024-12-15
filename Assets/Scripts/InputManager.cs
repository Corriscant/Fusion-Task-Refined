using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera; // Камера для обработки кликов
    [SerializeField] private SelectionManager selectionManager; // Менеджер выделения юнитов

    private void Update()
    {
        // Работа с рамкой выделения
        HandleSelectionInput();

        // Проверяем нажатие правой кнопки мыши
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 targetPosition = hit.point;

                // Передаём данные в BasicSpawner через синглтон
                BasicSpawner.Instance.HandleDestinationInput(targetPosition);
            }
        }
    }

    private void HandleSelectionInput()
    {
        // Начало выделения
        if (Input.GetMouseButtonDown(0))
        {
            selectionManager.StartSelection(Input.mousePosition);
        }

        // Изменение рамки выделения
        if (Input.GetMouseButton(0))
        {
            selectionManager.UpdateSelection(Input.mousePosition);
        }

        // Завершение выделения
        if (Input.GetMouseButtonUp(0))
        {
            selectionManager.EndSelection();
        }
    }
}
