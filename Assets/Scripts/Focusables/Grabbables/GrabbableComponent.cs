using FMODUnity;
using System.Collections.Generic;
using UnityEngine;

public class GrabbableComponent : Focusable, IGrabbable
{
    public EventReference grabSound;

    [SerializeField] private Transform _grabHandle;
    public Transform grabHandle => _grabHandle;

    public Transform Grab()
    {
        AudioManager.PlayOneShot(grabSound, transform.position);
        return transform;
    }
}
