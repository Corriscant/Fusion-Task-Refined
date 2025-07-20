using Fusion;
using System;
namespace Corris.Loggers
{
    /// <summary>
    /// Logger class for logging messages with a prefix, time, ServerTick and color coding.
    /// </summary> 
    public static class Logger
    {
        /// <summary>
        /// Delegate that returns the current server tick.
        /// </summary>
        /// <remarks>
        /// <b>Example</b>
        /// <code>
        /// NetworkBugsLogger.GetCurrentServerTick = () =>
        /// {
        ///     if (this != null &amp;&amp; this.Object?.IsValid == true
        ///         &amp;&amp; Runner?.IsRunning == true)
        ///         return this.NET_ServerTick;
        ///
        ///     return -1; // Tick.None.Raw
        /// };
        /// </code>
        /// </remarks>
        public static Func<Tick> GetCurrentServerTick { get; set; }

        public static string LogPrefix = "Logger: ";
        private static string PrefixColor = "#144078";

        [System.Diagnostics.Conditional("ENABLE_LOGS")]
        public static void Log(string message, LogMessageType messageType = LogMessageType.Info)
        {
            Tick serverTick = -1; // Default value if the provider is not set
            if (GetCurrentServerTick != null)
            {
                serverTick = GetCurrentServerTick();
            }

            string color = messageType switch
            {
                LogMessageType.Info => PrefixColor,
                LogMessageType.Warning => "yellow",
                LogMessageType.Error => "red",
                _ => "white"
            };

            string messageFull = $"<color={color}>{LoggingCore.TimePrefix()} {(serverTick == -1 ? "" : $"N{serverTick}")} {LogPrefix}</color> {message}";

            LoggingCore.Log(messageFull, messageType);
        }

        [System.Diagnostics.Conditional("ENABLE_LOGS")]
        public static void LogError(string message) => Log(message, LogMessageType.Error);

        [System.Diagnostics.Conditional("ENABLE_LOGS")]
        public static void LogWarning(string message) => Log(message, LogMessageType.Warning);
    }
}