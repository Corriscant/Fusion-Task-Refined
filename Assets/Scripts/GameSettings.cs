using UnityEngine;

/// <summary>
/// Global game configuration settings.
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "Config/Game Settings")]
public class GameSettings : ScriptableObject
{
    /// <summary>
    /// The maximum allowed offset for a unit from the center of the group.
    /// </summary>
    public int unitAllowedOffset = 1;
}
