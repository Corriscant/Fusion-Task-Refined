// Generic input handler, decoupled from game logic.
using System;
using UnityEngine;

namespace FusionTask.Infrastructure
{
    /// <summary>
    /// Provides input events for other systems.
    /// </summary>
    public class InputManager : MonoBehaviour, IInputService
    {
    // --- Primary (usually Left) Mouse Button Events ---
    public event Action<Vector2> OnPrimaryMouseDown;
    public event Action<Vector2> OnPrimaryMouseDrag;
    public event Action OnPrimaryMouseUp;

    // --- Secondary (usually Right) Mouse Button Events ---
    // This event was created for the move command
    public event Action<Vector3> OnSecondaryMouseClick_World;

    // --- Mouse movement  ---
    public event Action<Vector3> OnMouseMove;
    public event Action OnRespawn;

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
        ProcessRespawnInput();
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

    /// <summary>
    /// Detects the spacebar press for the respawn command.
    /// </summary>
    private void ProcessRespawnInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnRespawn?.Invoke();
        }
    }

    }
}
