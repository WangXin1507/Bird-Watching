using FMODUnity;
using System;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class Interactable : Focusable, IInteractable
{
    public EventReference interactSound;

    private void Start()
    {
        gameObject.layer = 7;
    }

    public virtual void Interact()
    {
        Debug.Log("Interacted");
    }
}
