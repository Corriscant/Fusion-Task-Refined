namespace FusionTask.Infrastructure
{
    /// <summary>
    /// Provides helpers to correctly check for null on any object that might be a Unity object.
    /// </summary>
    public static class UnityObjectExtensions
    {
        /// <summary>
        /// Determines whether the specified object is null or a destroyed <see cref="UnityEngine.Object"/>.
        /// This generic extension method can be called on any interface or class.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns><c>true</c> if the object is null or destroyed; otherwise, <c>false</c>.</returns>
        public static bool IsNullOrDestroyed(this object obj)
        {
            // If the object is a UnityEngine.Object, use the overloaded '==' operator.
            // This correctly checks if the underlying game object has been destroyed.
            if (obj is UnityEngine.Object unityObj)
            {
                return unityObj == null;
            }

            // For pure C# objects, perform a standard reference null check.
            return obj == null;
        }
    }
}
