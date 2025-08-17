using System.Threading.Tasks;
using UnityEngine;

namespace FusionTask.Infrastructure
{
    /// <summary>
    /// Applies materials to mesh renderers.
    /// </summary>
    public interface IMaterialApplier
    {
        /// <summary>
        /// Applies a material by index to the given renderer.
        /// </summary>
        /// <param name="renderer">Target mesh renderer.</param>
        /// <param name="index">Material index.</param>
        /// <param name="entityName">Name for logging.</param>
        /// <param name="propertyBlock">Optional property block.</param>
        /// <returns>True if material applied; otherwise false.</returns>
        bool ApplyMaterial(MeshRenderer renderer, int index, string entityName, MaterialPropertyBlock propertyBlock = null);

        /// <summary>
        /// Asynchronously applies a material by index to the given renderer.
        /// </summary>
        /// <param name="renderer">Target mesh renderer.</param>
        /// <param name="index">Material index.</param>
        /// <param name="entityName">Name for logging.</param>
        /// <param name="propertyBlock">Optional property block.</param>
        /// <returns>True if material applied; otherwise false.</returns>
        Task<bool> ApplyMaterialAsync(MeshRenderer renderer, int index, string entityName, MaterialPropertyBlock propertyBlock = null);
    }
}
