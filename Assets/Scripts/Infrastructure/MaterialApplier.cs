using System.Threading.Tasks;
using UnityEngine;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;

namespace FusionTask.Infrastructure
{
    /// <summary>
    /// Provides utility methods for applying materials to mesh renderers.
    /// </summary>
    public class MaterialApplier : IMaterialApplier
    {
        private readonly IPlayerMaterialProvider _playerMaterialProvider;

        /// <summary>
        /// Creates a new material applier.
        /// </summary>
        /// <param name="playerMaterialProvider">Provider for retrieving materials.</param>
        public MaterialApplier(IPlayerMaterialProvider playerMaterialProvider)
        {
            _playerMaterialProvider = playerMaterialProvider;
        }

        /// <summary>
        /// Applies a material to the given mesh renderer using the material index.
        /// Logs errors or success messages using the provided entity name.
        /// </summary>
        /// <param name="renderer">The MeshRenderer to apply the material to.</param>
        /// <param name="index">The material index used to retrieve the material.</param>
        /// <param name="entityName">Name of the entity for logging purposes.</param>
        /// <param name="propertyBlock">Optional property block for per-instance material properties.</param>
        /// <returns>True if the material was successfully applied, otherwise false.</returns>
        public bool ApplyMaterial(MeshRenderer renderer, int index, string entityName, MaterialPropertyBlock propertyBlock = null)
        {
            if (renderer == null)
            {
                LogError($"{GetLogCallPrefix(typeof(MaterialApplier))} MeshRenderer not found for {entityName} Index[{index}].");
                return false;
            }

            var material = _playerMaterialProvider.GetMaterial(index);

            if (material == null)
            {
                LogError($"{GetLogCallPrefix(typeof(MaterialApplier))} Material not found for {entityName} Index[{index}].");
                return false;
            }

            renderer.sharedMaterial = material;

            if (propertyBlock != null)
            {
                renderer.SetPropertyBlock(propertyBlock);
            }

            Log($"{GetLogCallPrefix(typeof(MaterialApplier))} Material successfully loaded and applied to {entityName} Index[{index}].");
            return true;
        }

        /// <summary>
        /// Asynchronously applies a material to the given mesh renderer using the material index.
        /// </summary>
        /// <param name="renderer">The MeshRenderer to apply the material to.</param>
        /// <param name="index">The material index used to retrieve the material.</param>
        /// <param name="entityName">Name of the entity for logging purposes.</param>
        /// <param name="propertyBlock">Optional property block for per-instance material properties.</param>
        /// <returns>True if the material was successfully applied, otherwise false.</returns>
        public async Task<bool> ApplyMaterialAsync(MeshRenderer renderer, int index, string entityName, MaterialPropertyBlock propertyBlock = null)
        {
            if (renderer == null)
            {
                LogError($"{GetLogCallPrefix(typeof(MaterialApplier))} MeshRenderer not found for {entityName} Index[{index}].");
                return false;
            }

            var material = await _playerMaterialProvider.GetMaterialAsync(index);

            if (material == null)
            {
                LogError($"{GetLogCallPrefix(typeof(MaterialApplier))} Material not found for {entityName} Index[{index}].");
                return false;
            }

            renderer.sharedMaterial = material;

            if (propertyBlock != null)
            {
                renderer.SetPropertyBlock(propertyBlock);
            }

            Log($"{GetLogCallPrefix(typeof(MaterialApplier))} Material successfully loaded and applied to {entityName} Index[{index}].");
            return true;
        }
    }
}
