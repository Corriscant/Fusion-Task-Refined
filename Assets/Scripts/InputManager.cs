using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    [SerializeField] private Camera mainCamera; // Camera for handling clicks

    public static event Action<Vector2> OnPrimaryMouseDown; 
    public static event Action<Vector2> OnPrimaryMouseDrag; 
    public static event Action OnPrimaryMouseUp; 

    public static event System.Action<Vector3> OnMoveCommand;

    private void Awake()
    {
        if (mainCamera != null && mainCamera.GetComponent<CameraMovement>() == null)
        {
            mainCamera.gameObject.AddComponent<CameraMovement>();
        }
    }

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

                // Pass data to NetworkGameManager via singleton
                OnMoveCommand?.Invoke(targetPosition);
            }
        }
    }

    private void HandleSelectionInput()
    {
        // Start selection
        if (Input.GetMouseButtonDown(0))
        {
            // Send "LMB pressed" event, passing mouse position
            OnPrimaryMouseDown?.Invoke(Input.mousePosition);
        }

        // Update selection box
        if (Input.GetMouseButton(0))
        {
            // Send "LMB held and moving" event
            OnPrimaryMouseDrag?.Invoke(Input.mousePosition);
        }

        // End selection
        if (Input.GetMouseButtonUp(0))
        {
            // Send "LMB released" event
            OnPrimaryMouseUp?.Invoke();
        }
    }
}
