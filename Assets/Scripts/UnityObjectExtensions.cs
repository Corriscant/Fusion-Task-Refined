// A helper to correctly check for null on any object that might be a Unity Object.
public static class UnityObjectExtensions
{
    // This generic extension method can be called on any interface or class.
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