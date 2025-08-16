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
    [Header("Menu")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button exitButton;

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

    /// <summary>
    /// Parameters the buttons in MainMenu and sets up their click listeners.
    /// </summary>
    private void StartMenu()
    {
        if (hostButton == null || joinButton == null || exitButton == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} Buttons are not assigned in the inspector!");
            return;
        }

        hostButton.onClick.AddListener(async () => await _connectionService.StartGame(GameMode.Host));
        joinButton.onClick.AddListener(async () => await _connectionService.StartGame(GameMode.Client));
        exitButton.onClick.AddListener(QuitGame);
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
