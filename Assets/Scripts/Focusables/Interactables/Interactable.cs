using UnityEngine;

public class Interactable : Focusable, IInteractable
{
    private void Start()
    {
        gameObject.layer = 7;
    }

    public virtual void Interact()
    {
        Debug.Log("Interacted");
    }
}
