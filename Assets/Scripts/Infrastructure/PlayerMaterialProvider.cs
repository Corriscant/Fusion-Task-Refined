using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Provides player materials and caches them to avoid duplicate loading.
/// </summary>
public static class PlayerMaterialProvider
{
    private static readonly Dictionary<int, Material> _materials = new();
    private static PlayerMaterialList _materialList;

    /// <summary>
    /// Returns a material for the given index. The material is loaded once and then cached.
    /// </summary>
    public static Material GetMaterial(int index)
    {
        if (_materials.TryGetValue(index, out var material) && material != null)
        {
            return material;
        }

        if (_materialList == null)
        {
            _materialList = Resources.Load<PlayerMaterialList>("PlayerMaterialList");
            if (_materialList == null)
            {
                return null;
            }
        }

        if (index < 0 || index >= _materialList.Materials.Count)
        {
            return null;
        }

        material = _materialList.Materials[index];
        if (material != null)
        {
            _materials[index] = material;
        }
        return material;
    }
}
