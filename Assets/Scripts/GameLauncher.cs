// Handles UI for starting or quitting the game.
using UnityEngine;
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
    private void OnGUI()
    {
        // Get the singleton instance of the main network manager.
        var networkManager = NetworkGameManager.Instance;
        if (networkManager == null)
        {
            // If the manager doesn't exist yet, do nothing.
            LogError($"{GetLogCallPrefix(GetType())} NetworkGameManager NIL!");
            return;
        }

        float exitButtonY = 0f;
        bool selectionActive = SelectionManager.IsSelecting;

        // Only draw the Host and Join buttons if the network runner has not been created yet.
        if (networkManager.NetRunner == null)
        {
            // Disable buttons if a connection is currently in progress or a selection is active.
            GUI.enabled = !networkManager.IsConnecting;// && !selectionActive;

            if (GUI.Button(new Rect(0, 0, 200, 40), "Host"))
            {
                networkManager.StartGamePublic(GameMode.Host);
            }
            if (GUI.Button(new Rect(0, 40, 200, 40), "Join"))
            {
                networkManager.StartGamePublic(GameMode.Client);
            }

            // Adjust the Y position for the Exit button so it appears below.
            exitButtonY = 80f;
        }

        // Always show the Exit button but disable it during selection.
      //  GUI.enabled = !selectionActive;
        if (GUI.Button(new Rect(0, exitButtonY, 200, 40), "Exit"))
        {
            QuitGame();
        }
        GUI.enabled = true;
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