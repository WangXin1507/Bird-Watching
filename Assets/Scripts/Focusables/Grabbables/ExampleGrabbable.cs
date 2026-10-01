using UnityEngine;

public class ExampleGrabbable : Focusable, IGrabbable
{
    [SerializeField] private bool _isDragged = false;
    [SerializeField] private float _mass = 0.0f;
    public bool isDragged => _isDragged;
    public float mass => _mass;

    public Transform Grab()
    {
        return transform;
    }
}
