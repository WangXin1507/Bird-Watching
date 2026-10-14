using FMODUnity;
using System;
using UnityEngine;

public class Interactable : Focusable, IInteractable
{
    public EventReference sfx;

    private void Start()
    {
        gameObject.layer = 7;
    }

    public virtual void Interact()
    {
        Debug.Log("Interacted");
    }
}
