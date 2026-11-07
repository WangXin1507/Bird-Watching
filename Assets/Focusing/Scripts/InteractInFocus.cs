using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class InteractInFocus : MonoBehaviour
{
    [SerializeField] private FocusingInteract focusableManager;
    [SerializeField] private GameObject appearOnFocus;

    private void Update()
    {
        appearOnFocus.SetActive(focusableManager.isFocused);
    }
    
    public void Interact()
    {
        if (focusableManager != null && focusableManager.isFocused)
        {
            InteractWhileFocus();
        }
    }
    
    public void InteractWhileFocus()
    {
        Debug.Log("Interact test: Clicked " + gameObject.name);
    }
}
