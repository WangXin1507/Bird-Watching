using System.Reflection.Metadata;
using UnityEngine;

public class GrabbableComponent : Focusable, IGrabbable
{
    [SerializeField] private Transform _grabHandle;
    public Transform grabHandle => _grabHandle;

    public Transform Grab()
    {
        return transform;
    }
}
