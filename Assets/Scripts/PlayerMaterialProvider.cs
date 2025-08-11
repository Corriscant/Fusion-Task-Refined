using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides player materials and caches them to avoid duplicate loading.
/// </summary>
public static class PlayerMaterialProvider
{
    private static readonly Dictionary<int, Material> _materials = new();

    /// <summary>
    /// Returns a material for the given index. The material is loaded once and then cached.
    /// </summary>
    public static Material GetMaterial(int index)
    {
        if (_materials.TryGetValue(index, out var material) && material != null)
        {
            return material;
        }

        string materialName = $"Materials/UnitPayer{index}_Material";
        material = Resources.Load<Material>(materialName);
        if (material != null)
        {
            _materials[index] = material;
        }
        return material;
    }
}
