using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FusionTask.Infrastructure
{
    /// <summary>
    /// Provides player materials via Addressables and caches them to avoid duplicate loading.
    /// </summary>
    public class PlayerMaterialProvider : IPlayerMaterialProvider
    {
        private readonly PlayerMaterialProviderSettings _settings;
        private AsyncOperationHandle<PlayerMaterialList> _handle;

        private readonly Dictionary<int, Material> _materials = new();
        private PlayerMaterialList _materialList;

        /// <summary>
        /// Initializes the provider with configuration settings.
        /// </summary>
        /// <param name="settings">Settings containing the Addressable reference to the material list.</param>
        public PlayerMaterialProvider(PlayerMaterialProviderSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Asynchronously loads and returns a material for the given index using Addressables.
        /// </summary>
        /// <param name="index">Index of the material in the list.</param>
        /// <returns>The loaded material or null if not found.</returns>
        public async Task<Material> GetMaterialAsync(int index)
        {
            if (_settings == null || _settings.MaterialListReference == null)
                return null;

            if (!_handle.IsValid())
                _handle = _settings.MaterialListReference.LoadAssetAsync();

            _materialList = await _handle.Task;
            if (_materialList == null || index < 0 || index >= _materialList.Materials.Count)
                return null;

            var material = _materialList.Materials[index];
            if (material != null)
                _materials[index] = material;
            return material;
        }

        /// <summary>
        /// Returns a material for the given index. The material is loaded once and then cached.
        /// </summary>
        public Material GetMaterial(int index)
        {
            if (_materials.TryGetValue(index, out var material) && material != null)
            {
                return material;
            }

            if (_settings == null || _settings.MaterialListReference == null)
            {
                return null;
            }

            if (!_handle.IsValid())
            {
                _handle = _settings.MaterialListReference.LoadAssetAsync();
            }

            _materialList = _handle.WaitForCompletion();

            if (_materialList == null || index < 0 || index >= _materialList.Materials.Count)
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

        /// <summary>
        /// Releases loaded Addressable resources and clears caches.
        /// </summary>
        public void Release()
        {
            if (_handle.IsValid())
            {
                Addressables.Release(_handle);
            }
            _materials.Clear();
            _materialList = null;
        }
    }
}
