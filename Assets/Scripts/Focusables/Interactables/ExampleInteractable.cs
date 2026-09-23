using UnityEngine;

public class ExampleInteractable : Focusable, IInteractable
{
    public void Interact()
    {
        Debug.Log("I have been waiting for this moment");
    }
}
