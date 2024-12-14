using UnityEngine;

public class Selectable : MonoBehaviour
{
    [SerializeField] private bool isSelectable = true;

    public bool IsSelectable => isSelectable;


    // Here to check if Player can select this object
}
