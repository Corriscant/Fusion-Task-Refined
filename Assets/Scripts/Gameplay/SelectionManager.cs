using Fusion; // for PlayerRef
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;
using VContainer;
using FusionTask.Networking;
using FusionTask.Infrastructure;

namespace FusionTask.Gameplay
{
    /// <summary>
    /// Manages drag-box selection of units: listens for mouse events,
    /// draws the selection rectangle and keeps track of the currently
    /// highlighted units.
    /// </summary>
    public class SelectionManager : MonoBehaviour
    {
    [SerializeField] private RectTransform selectionBox; // UI for selection box
    [SerializeField] private Camera mainCamera; // Camera for raycasting
    [SerializeField] private LayerMask selectableLayer; // Layer for units

    private Vector2 _startPosition;
    private RectTransform _canvasRect; // Canvas holding SelectionRect

    private readonly List<ISelectable> _selectedUnits = new();
    public List<ISelectable> SelectedUnits => _selectedUnits;

    // Indicates whether a frame selection is currently active
    public static bool IsSelecting { get; private set; }

    private IConnectionService _connectionService;
    private IInputService _inputService;
    private IUnitRegistry _unitRegistry;

    // Called by VContainer to inject the dependency immediately upon its creation.
    [Inject]
    public void Construct(IConnectionService connectionService, IInputService inputService, IUnitRegistry unitRegistry)
    {
        Log($"{GetLogCallPrefix(GetType())} VContainer Inject!");
        _connectionService = connectionService;
        _inputService = inputService;
        _unitRegistry = unitRegistry;
    }

    public void Start()
    {
        if (_connectionService.IsNullOrDestroyed())
        {
            LogError($"{GetLogCallPrefix(GetType())} Connection service NIL!");
        }
        if (_inputService.IsNullOrDestroyed())
        {
            LogError($"{GetLogCallPrefix(GetType())} Input service NIL!");
        }
        _canvasRect = selectionBox.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
    }

    public void OnEnable()
    {
        // Subscribe to input events
        if (!_inputService.IsNullOrDestroyed())
        {
            _inputService.OnPrimaryMouseDown += StartSelection;
            _inputService.OnPrimaryMouseDrag += UpdateSelection;
            _inputService.OnPrimaryMouseUp += EndSelection;
        }
    }

    public void OnDisable()
    {
        // Unsubscribe from input events
        if (!_inputService.IsNullOrDestroyed())
        {
            _inputService.OnPrimaryMouseDown -= StartSelection;
            _inputService.OnPrimaryMouseDrag -= UpdateSelection;
            _inputService.OnPrimaryMouseUp -= EndSelection;
        }
    }

    public void StartSelection(Vector2 startPosition)
    {
        if ((EventSystem.current != null) && EventSystem.current.IsPointerOverGameObject(PointerId.mousePointerId))
        {
            Log($"{GetLogCallPrefix(GetType())} Pointer is over UI element. Ignoring selection.");
            return; // Ignore if pointer is over UI elements
        }

        // Convert screen coordinates to local coordinates of the selection box
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, startPosition, mainCamera, out Vector2 localPoint);
        _startPosition = localPoint;

        selectionBox.gameObject.SetActive(true);
        IsSelecting = true;
    }

    public void UpdateSelection(Vector2 currentPosition)
    {
        if (!IsSelecting) return;
        // Convert current position to local coordinates of the Canvas
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, currentPosition, mainCamera, out Vector2 localPoint);

        // Calculate the size of the selection box
        Vector2 size = localPoint - _startPosition;
        selectionBox.sizeDelta = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        selectionBox.anchoredPosition = _startPosition + size / 2;
    }

    public void EndSelection()
    {
        if (!IsSelecting) return;
        selectionBox.gameObject.SetActive(false);

        SelectUnits();
        IsSelecting = false;
    }

    public void ClearSelection()
    {
        foreach (var selectable in _selectedUnits)
        {
            selectable.Selected = false;
        }
        _selectedUnits.Clear();
        Log($"{GetLogCallPrefix(GetType())} Selection cleared.");
    }

    private void SelectUnits()
    {
        if (_connectionService.Runner.IsNullOrDestroyed())
        {
            LogError($"{GetLogCallPrefix(GetType())} NetRunner is null. Cannot select units.");
            return;
        }
        if (selectionBox == null || mainCamera == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} Selection box or main camera is not set.");
            return;
        }

        PlayerRef localPlayer = _connectionService.Runner.LocalPlayer;

        // Clear previous selection
        ClearSelection();

        // Convert local coordinates of the selection box to screen coordinates (reverse action to what was done in StartSelection)
        var leftTop_Local = new Vector2(selectionBox.anchoredPosition.x - selectionBox.sizeDelta.x / 2, selectionBox.anchoredPosition.y - selectionBox.sizeDelta.y / 2);
        var rightBottom_Local = new Vector2(selectionBox.anchoredPosition.x + selectionBox.sizeDelta.x / 2, selectionBox.anchoredPosition.y + selectionBox.sizeDelta.y / 2);

        Vector2 leftTop_Screen = LocalToScreenPoint(mainCamera, _canvasRect, leftTop_Local);
        Vector2 rightBottom_Screeen = LocalToScreenPoint(mainCamera, _canvasRect, rightBottom_Local);
        Rect selectionBox_Screen = GetRectFromPoints(leftTop_Screen, rightBottom_Screeen);

        var units = _unitRegistry.Units;
        for (int i = 0; i < units.Count; i++)
        {
            Unit unit = units[i];
            if (unit is not ISelectableProvider { Selectable: { } selectable })
            {
                continue;
            }

            if (!selectable.CanBeSelectedBy(localPlayer))
            {
                continue;
            }

            Vector3 unitPosition_Screen = mainCamera.WorldToScreenPoint(unit.Position);

            if (selectionBox_Screen.Contains(unitPosition_Screen))
            {
                selectable.Selected = true;
                _selectedUnits.Add(selectable);

                Log($"{GetLogCallPrefix(GetType())} Unit selected: {unit.name}");
            }
        }
    }

    private Vector2 LocalToScreenPoint(Camera mainCamera, RectTransform rectTransform, Vector2 localPoint)
    {
        // Convert local point to world coordinates
        Vector3 worldPoint = rectTransform.TransformPoint(localPoint);

        // Convert world coordinates to screen coordinates
        return mainCamera.WorldToScreenPoint(worldPoint);
    }

    private Rect GetRectFromPoints(Vector2 point1, Vector2 point2)
    {
        float xMin = Mathf.Min(point1.x, point2.x);
        float yMin = Mathf.Min(point1.y, point2.y);
        float width = Mathf.Abs(point1.x - point2.x);
        float height = Mathf.Abs(point1.y - point2.y);

        return new Rect(xMin, yMin, width, height);
    }
    }
}
