// Handles UI for starting or quitting the game.
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;

// This directive is needed because Application.Quit() doesn't stop play mode in the editor.
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This class is responsible for the main menu UI, allowing the user to
/// host a game, join a game, or exit the application. It communicates with the
/// NetworkGameManager (soon to be ConnectionManager) via its singleton instance.
/// </summary>
public class GameLauncher : MonoBehaviour
{
    #region GUI
    [SerializeField] private float buttonSpacing = 2f;
    [SerializeField] private float buttonWidth = 200f;
    [SerializeField] private float buttonHeight = 40f;
    [SerializeField] private Color buttonColor = new Color(1f, 1f, 1f, 0.717f);

    private Button _hostButton;
    private Button _joinButton;
    private Button _exitButton;
    private Button[] _buttons;
    #endregion GUI

    private NetworkGameManager _networkManager;

    private void Start()
    {
        _networkManager = NetworkGameManager.Instance;
        if (_networkManager == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} NetworkGameManager NIL!");
            enabled = false;
            return;
        }

        CreateMenu();
    }

    private void CreateMenu()
    {
        var canvasGo = new GameObject("CanvasMenu");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        _hostButton = CreateButton(canvas.transform, new Vector2(0f, 0f), "Host");
        _joinButton = CreateButton(canvas.transform, new Vector2(0f, -(buttonHeight + buttonSpacing)), "Join");
        _exitButton = CreateButton(canvas.transform, new Vector2(0f, -2 * (buttonHeight + buttonSpacing)), "Exit");

        _buttons = new[] { _hostButton, _joinButton, _exitButton };

        _hostButton.onClick.AddListener(() => _networkManager.StartGamePublic(GameMode.Host));
        _joinButton.onClick.AddListener(() => _networkManager.StartGamePublic(GameMode.Client));
        _exitButton.onClick.AddListener(QuitGame);
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
        if (_networkManager == null)
            return;

        bool selectionActive = SelectionManager.IsSelecting;
        bool runnerMissing = _networkManager.NetRunner == null;

        _hostButton.gameObject.SetActive(runnerMissing);
        _joinButton.gameObject.SetActive(runnerMissing);

        bool interactable = !_networkManager.IsConnecting && !selectionActive;
        _hostButton.interactable = interactable;
        _joinButton.interactable = interactable;
        _exitButton.interactable = !selectionActive;

        UpdateMenuLayout();
    }

    /// <summary>
    /// Repositions active buttons so that there are no empty gaps in the menu.
    /// </summary>
    private void UpdateMenuLayout()
    {
        float offset = 0f;
        foreach (var button in _buttons)
        {
            var rect = button.GetComponent<RectTransform>();
            if (button.gameObject.activeSelf)
            {
                rect.anchoredPosition = new Vector2(0f, -offset);
                offset += buttonHeight + buttonSpacing;
            }
        }
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
