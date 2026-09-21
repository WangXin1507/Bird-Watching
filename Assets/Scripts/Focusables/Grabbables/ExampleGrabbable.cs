using System.Reflection.Metadata;
using UnityEngine;

public class ExampleGrabbable : Focusable, IGrabbable
{
    //[SerializeField] private bool _isDragged = false;
    //[SerializeField] private float _mass = 0.0f;
    [SerializeField] private Transform _grabHandle;
    //public bool isDragged => _isDragged;
    //public float mass => _mass;
    public Transform grabHandle => _grabHandle;

    public Transform Grab()
    {
        return transform;
    }
}
