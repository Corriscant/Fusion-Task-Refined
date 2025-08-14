using System;
using UnityEngine;

/// <summary>
/// Exports input events to consumers.
/// </summary>
public interface IInputService
{
    event Action<Vector2> OnPrimaryMouseDown;
    event Action<Vector2> OnPrimaryMouseDrag;
    event Action OnPrimaryMouseUp;
    event Action<Vector3> OnSecondaryMouseClick_World;
    event Action<Vector3> OnMouseMove;
}
