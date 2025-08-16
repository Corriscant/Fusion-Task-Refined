using Fusion;
using UnityEngine;
using static Corris.Loggers.Logger;
using static Corris.Loggers.LogUtils;

/// <summary>
/// Component that makes a unit selectable. Attach to a unit prefab to enable selection.
/// </summary>
public class Selectable : NetworkBehaviour, ISelectable
{
    [SerializeField] private bool isSelectable = true;
    [SerializeField] private GameObject selectedIndicator;

    private bool _selected;
    private Unit _unit;

    public bool IsSelectable => isSelectable;

    /// <summary>
    /// Indicates whether the unit is currently selected.
    /// </summary>
    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            if (selectedIndicator)
                selectedIndicator.SetActive(_selected);
        }
    }

    public override void Spawned()
    {
        _unit = GetComponent<Unit>();

        if (selectedIndicator)
        {
            selectedIndicator.SetActive(false); // Ensure the indicator is off by default
        }
        else
        {
            LogWarning($"{GetLogCallPrefix(GetType())} Selected indicator is not set. It will not be shown.");
        }
    }

    /// <summary>
    /// Determines if the unit can be selected by the specified player.
    /// </summary>
    /// <param name="player">Player attempting the selection.</param>
    /// <returns>True if the player is allowed to select the unit.</returns>
    public bool CanBeSelectedBy(PlayerRef player)
    {
        if (_unit == null)
        {
            return false;
        }

        // Currently selection is allowed only for the owner. Extend this logic to include allies if needed.
        return _unit.PlayerOwner == player;
    }
}
