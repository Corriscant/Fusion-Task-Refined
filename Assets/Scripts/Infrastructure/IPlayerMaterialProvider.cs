using System.Threading.Tasks;
using UnityEngine;

namespace FusionTask.Infrastructure
{
    /// <summary>
    /// Provides player materials and manages their lifecycle.
    /// </summary>
    public interface IPlayerMaterialProvider
    {
        /// <summary>
        /// Asynchronously retrieves a material by index.
        /// </summary>
        /// <param name="index">Index of the material in the list.</param>
        /// <returns>The material if found; otherwise null.</returns>
        Task<Material> GetMaterialAsync(int index);

        /// <summary>
        /// Retrieves a material by index, loading it if necessary.
        /// </summary>
        /// <param name="index">Index of the material in the list.</param>
        /// <returns>The material if found; otherwise null.</returns>
        Material GetMaterial(int index);

        /// <summary>
        /// Releases cached resources.
        /// </summary>
        void Release();
    }
}
