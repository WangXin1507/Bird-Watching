using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class InteractInFocus : MonoBehaviour
{
    [SerializeField] public FocusingInteract focusableManager;
    [SerializeField] private GameObject appearOnFocus;

    protected virtual void Update()
    {
        if (appearOnFocus != null && focusableManager != null)
            appearOnFocus.SetActive(focusableManager.isFocused);
    }
    
    public void Interact()
    {
        if (focusableManager != null && focusableManager.isFocused)
        {
            InteractWhileFocus();
        }
    }
    
    public virtual void InteractWhileFocus()
    {
        Debug.Log("Interact test: Clicked " + gameObject.name);
    }
}
