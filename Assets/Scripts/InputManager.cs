using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera; // ������ ��� ��������� ������
    [SerializeField] private SelectionManager selectionManager; // �������� ��������� ������

    private void Update()
    {
        // ������ � ������ ���������
        HandleSelectionInput();

        // ��������� ������� ������ ������ ����
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 targetPosition = hit.point;

                // ������� ������ � BasicSpawner ����� ��������
                BasicSpawner.Instance.HandleDestinationInput(targetPosition);
            }
        }
    }

    private void HandleSelectionInput()
    {
        // ������ ���������
        if (Input.GetMouseButtonDown(0))
        {
            selectionManager.StartSelection(Input.mousePosition);
        }

        // ��������� ����� ���������
        if (Input.GetMouseButton(0))
        {
            selectionManager.UpdateSelection(Input.mousePosition);
        }

        // ���������� ���������
        if (Input.GetMouseButtonUp(0))
        {
            selectionManager.EndSelection();
        }
    }
}
