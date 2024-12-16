using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera; // Camera for handling clicks
    [SerializeField] private SelectionManager selectionManager; // Manager for unit selection

    private void Update()
    {
        // Handling selection box
        HandleSelectionInput();

        // Check right mouse button click
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Vector3 targetPosition = hit.point;

                // Pass data to BasicSpawner via singleton
                BasicSpawner.Instance.HandleDestinationInput(targetPosition);
            }
        }
    }

    private void HandleSelectionInput()
    {
        // Start selection
        if (Input.GetMouseButtonDown(0))
        {
            selectionManager.StartSelection(Input.mousePosition);
        }

        // Update selection box
        if (Input.GetMouseButton(0))
        {
            selectionManager.UpdateSelection(Input.mousePosition);
        }

        // End selection
        if (Input.GetMouseButtonUp(0))
        {
            selectionManager.EndSelection();
        }
    }
}
