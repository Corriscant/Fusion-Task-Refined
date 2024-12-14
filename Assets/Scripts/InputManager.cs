using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private SelectionManager selectionManager;

    private Vector2 _startMousePosition;

    void Update()
    {
        // Начало выделения
        if (Input.GetMouseButtonDown(0))
        {
            _startMousePosition = Input.mousePosition;
            selectionManager.StartSelection(_startMousePosition);
        }

        // Изменение выделения
        if (Input.GetMouseButton(0))
        {
            Vector2 currentMousePosition = Input.mousePosition;
            selectionManager.UpdateSelection(currentMousePosition);
        }

        // Завершение выделения
        if (Input.GetMouseButtonUp(0))
        {
            selectionManager.EndSelection();
        }
    }
}
