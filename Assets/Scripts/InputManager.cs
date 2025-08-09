// Generic input handler, decoupled from game logic.
using System;
using Unity.VisualScripting;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    // --- Primary (usually Left) Mouse Button Events ---
    public static event Action<Vector2> OnPrimaryMouseDown;
    public static event Action<Vector2> OnPrimaryMouseDrag;
    public static event Action OnPrimaryMouseUp;

    // --- Secondary (usually Right) Mouse Button Events ---
    // This event was created for the move command
    public static event Action<Vector3> OnSecondaryMouseClick_World;

    // --- Mouse movement  ---
    public static event Action<Vector3> OnMouseMove;

    // Camera used for raycasting
    [SerializeField] private Camera mainCamera;

    /// <summary>
    /// The main loop that polls for input each frame.
    /// </summary>
    private void LateUpdate()
    {
        // Process input for each button type
        ProcessPrimaryMouseInput();
        ProcessSecondaryMouseInput();
        ProcessMousePosition();
    }

    /// <summary>
    /// Processes all states of the primary mouse button.
    /// </summary>
    private void ProcessPrimaryMouseInput()
    {
        // Must be separate 'if' statements because GetMouseButtonDown(0) and GetMouseButton(0)
        // are both true on the first frame of a click.

        if (Input.GetMouseButtonDown(0))
        {
            OnPrimaryMouseDown?.Invoke(Input.mousePosition);
        }

        if (Input.GetMouseButton(0))
        {
            OnPrimaryMouseDrag?.Invoke(Input.mousePosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            OnPrimaryMouseUp?.Invoke();
        }
    }

    /// <summary>
    /// Handles clicks from the secondary mouse button.
    /// </summary>
    private void ProcessSecondaryMouseInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                OnSecondaryMouseClick_World?.Invoke(hit.point);
            }
        }
    }

    /// <summary>
    /// Publishes the current mouse position.
    /// </summary>
    private void ProcessMousePosition()
    {
        OnMouseMove?.Invoke(Input.mousePosition);
    }

}