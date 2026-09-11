using UnityEngine;

public interface IGrabbable : IFocusable
{
    public bool isDragged { get; }
    public float mass { get; }

    public Transform Grab();
}
