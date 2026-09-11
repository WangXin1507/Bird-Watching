using UnityEngine;

public interface IGrabbable : IFocusable
{
    public bool isDragged { get; }
    public Transform Grab();
}
