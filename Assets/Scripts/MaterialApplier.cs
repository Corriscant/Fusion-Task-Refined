using UnityEngine;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;

/// <summary>
/// Provides utility methods for applying materials to mesh renderers.
/// </summary>
public static class MaterialApplier
{
    /// <summary>
    /// Applies a material to the given mesh renderer using the material index.
    /// Logs errors or success messages using the provided entity name.
    /// </summary>
    /// <param name="renderer">The MeshRenderer to apply the material to.</param>
    /// <param name="index">The material index used to retrieve the material.</param>
    /// <param name="entityName">Name of the entity for logging purposes.</param>
    /// <returns>True if the material was successfully applied, otherwise false.</returns>
    public static bool ApplyMaterial(MeshRenderer renderer, int index, string entityName)
    {
        if (renderer == null)
        {
            LogError($"{GetLogCallPrefix(typeof(MaterialApplier))} MeshRenderer not found for {entityName} Index[{index}].");
            return false;
        }

        var material = PlayerMaterialProvider.GetMaterial(index);

        if (material == null)
        {
            LogError($"{GetLogCallPrefix(typeof(MaterialApplier))} Material not found for {entityName} Index[{index}].");
            return false;
        }

        renderer.material = material;
        Log($"{GetLogCallPrefix(typeof(MaterialApplier))} Material successfully loaded and applied to {entityName} Index[{index}].");
        return true;
    }
}
