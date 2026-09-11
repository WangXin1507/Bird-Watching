using UnityEngine;

public class ExampleGrabbable : Focusable, IGrabbable
{
    [SerializeField] private bool _isDragged = false;
    public bool isDragged => _isDragged;

    public Transform Grab()
    {
        return transform;
    }
}
