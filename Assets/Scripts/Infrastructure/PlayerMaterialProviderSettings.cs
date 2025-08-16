using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FusionTask.Infrastructure
{
    /// <summary>
    /// Provides configuration for <see cref="PlayerMaterialProvider"/>.
    /// Contains an Addressables reference to the list of player materials.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerMaterialProviderSettings", menuName = "Config/Player Material Provider Settings")]
    public class PlayerMaterialProviderSettings : ScriptableObject
    {
        [SerializeField] private AssetReferenceT<PlayerMaterialList> _materialListReference;

        /// <summary>
        /// Addressable reference to the <see cref="PlayerMaterialList"/> asset.
        /// </summary>
        public AssetReferenceT<PlayerMaterialList> MaterialListReference => _materialListReference;
    }
}
