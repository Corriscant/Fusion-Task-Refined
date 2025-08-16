using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FusionTask.Infrastructure
{
    /// <summary>
    /// Provides player materials and caches them to avoid duplicate loading.
    /// </summary>
    public static class PlayerMaterialProvider
    {
        private const string MaterialListAddress = "PlayerMaterialList.mat";
        private static AsyncOperationHandle<PlayerMaterialList> _handle;

        private static readonly Dictionary<int, Material> _materials = new();
        private static PlayerMaterialList _materialList;

        public static async Task<Material> GetMaterialAsync(int index)
        {
            if (!_handle.IsValid())
                _handle = Addressables.LoadAssetAsync<PlayerMaterialList>(MaterialListAddress);

            _materialList = await _handle.Task;
            if (_materialList == null || index < 0 || index >= _materialList.Materials.Count)
                return null;

            return _materialList.Materials[index];
        }

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
}
