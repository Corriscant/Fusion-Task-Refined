using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds a list of player materials configurable from the inspector.
/// </summary>
[CreateAssetMenu(fileName = "PlayerMaterialList", menuName = "Config/Player Material List")]
public class PlayerMaterialList : ScriptableObject
{
    [SerializeField] private List<Material> _materials = new();

    /// <summary>
    /// List of materials available for players.
    /// </summary>
    public IReadOnlyList<Material> Materials => _materials;
}
