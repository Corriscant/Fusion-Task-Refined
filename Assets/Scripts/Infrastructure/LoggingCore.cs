using System;

namespace Corris.Loggers
{
    /// <summary>
    /// Internal core class that handles the actual writing of logs to the Unity console.
    /// </summary>
    internal static class LoggingCore
    {
        /// <summary>
        /// Time prefix for logging, to compare time of the log in different instances (Host and Client)
        /// </summary>
        public static string TimePrefix() => $"{DateTime.Now:HH:mm:ss.fff}";

        public static void Log(string messageFull, LogMessageType messageType = LogMessageType.Info)
        {
            switch (messageType)
            {
                case LogMessageType.Info:
                    UnityEngine.Debug.Log(messageFull);
                    break;
                case LogMessageType.Warning:
                    UnityEngine.Debug.LogWarning(messageFull);
                    break;
                case LogMessageType.Error:
                    UnityEngine.Debug.LogError(messageFull);
                    break;
                default:
                    UnityEngine.Debug.LogWarning($"Unknown message type: {messageType}:   {messageFull}");
                    break;
            }
        }
    }
}
