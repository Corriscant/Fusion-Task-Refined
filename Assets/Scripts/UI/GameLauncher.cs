// Handles UI for starting or quitting the game.
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;
using VContainer;
using FusionTask.Networking;
using FusionTask.Gameplay;
using FusionTask.Infrastructure;

// This directive is needed because Application.Quit() doesn't stop play mode in the editor.
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FusionTask.UI
{
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
    private ISelectionService _selectionService;
    // Called by VContainer to inject the dependency immediately upon its creation.
    [Inject]
    public void Construct(IConnectionService connectionService, ISelectionService selectionService)
    {
        Log($"{GetLogCallPrefix(GetType())} VContainer Inject!");
        _connectionService = connectionService;
        _selectionService = selectionService;
    }

    private void Start()
    {
        if (_connectionService.IsNullOrDestroyed())
        {
            LogError($"{GetLogCallPrefix(GetType())} Connection service NIL!");
            enabled = false;
            return;
        }
        if (_selectionService as UnityEngine.Object == null)
        {
            LogError($"{GetLogCallPrefix(GetType())} Selection service NIL!");
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

        bool selectionActive = _selectionService.IsSelecting;
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
}
