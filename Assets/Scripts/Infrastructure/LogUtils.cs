using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Corris.Loggers
{
    /// <summary>
    /// Collection of utility methods for logging.
    /// </summary>
    public static class LogUtils
    {
        /// <summary>
        /// Form prefix with Class.Method
        /// </summary>
        public static string GetLogCallPrefix(Type callerClass, [CallerMemberName] string callerName = "")
        {
            return $"<color=#144078>F:{Time.frameCount} [{callerClass.Name}.{callerName}]</color>";
        }

    }
}