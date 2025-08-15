// Handles UI for starting or quitting the game.
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;
using VContainer;

// This directive is needed because Application.Quit() doesn't stop play mode in the editor.
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This class is responsible for the main menu UI, allowing the user to
/// host a game, join a game, or exit the application. It communicates with the
/// connection service via dependency injection.
/// </summary>
public class GameLauncher : MonoBehaviour
{
    #region GUI
    [Header("Menu")]
    [SerializeField] private float buttonSpacing = 2f;
    [SerializeField] private float buttonWidth = 200f;
    [SerializeField] private float buttonHeight = 40f;
    [SerializeField] private Color buttonColor = new Color(1f, 1f, 1f, 0.717f);
    [SerializeField] private Vector2 menuOffset = new Vector2(5f, 5f); // Offset from top-left corner

    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button exitButton;
    #endregion GUI

    private IConnectionService _connectionService;
    // Called by VContainer to inject the dependency immediately upon its creation.
    [Inject]
    public void Construct(IConnectionService connectionService)
    {
        Log($"{GetLogCallPrefix(GetType())} VContainer Inject!");
        this._connectionService = connectionService;
    }

    private void Start()
    {
        if (_connectionService.IsNullOrDestroyed())
        {
            LogError($"{GetLogCallPrefix(GetType())} Connection service NIL!");
            enabled = false;
            return;
        }

        StartMenu();
    }

    private void StartMenu()
    {
        /*
        var canvasGo = new GameObject("CanvasMenu");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        _hostButton = CreateButton(canvas.transform, new Vector2(menuOffset.x, -menuOffset.y), "Host");
        _joinButton = CreateButton(canvas.transform, new Vector2(menuOffset.x, -(menuOffset.y + buttonHeight + buttonSpacing)), "Join");
        _exitButton = CreateButton(canvas.transform, new Vector2(menuOffset.x, -(menuOffset.y + 2 * (buttonHeight + buttonSpacing))), "Exit");
        */

        if (hostButton == null || joinButton == null || exitButton == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} Buttons are not assigned in the inspector!");
            return;
        }

        hostButton.onClick.AddListener(async () => await _connectionService.StartGame(GameMode.Host));
        joinButton.onClick.AddListener(async () => await _connectionService.StartGame(GameMode.Client));
        exitButton.onClick.AddListener(QuitGame);
    }

    private Button CreateButton(Transform parent, Vector2 anchoredPos, string text)
    {
        var go = new GameObject(text + "Button");
        go.transform.SetParent(parent);
        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(buttonWidth, buttonHeight);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = anchoredPos;

        var image = go.AddComponent<Image>();

        image.color = buttonColor;

        var button = go.AddComponent<Button>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var label = textGo.AddComponent<Text>();
        label.text = text;
        label.alignment = TextAnchor.MiddleCenter;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.color = Color.black;

        return button;
    }

    private void Update()
    {
        if (_connectionService.IsNullOrDestroyed())
            return;

        bool selectionActive = SelectionManager.IsSelecting;
        bool runnerMissing = _connectionService.Runner.IsNullOrDestroyed();

        hostButton.gameObject.SetActive(runnerMissing);
        joinButton.gameObject.SetActive(runnerMissing);

        bool interactable = !_connectionService.IsConnecting && !selectionActive;
        hostButton.interactable = interactable;
        joinButton.interactable = interactable;
        exitButton.interactable = !selectionActive;
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        // This stops the editor play mode.
        EditorApplication.isPlaying = false;
#else
        // This quits the built application.
        Application.Quit();
#endif
    }
}
