using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class FocusingInteract : Interactable
{
    [SerializeField] CinemachineCamera cinemachineCamera;
    
    public bool isFocused = false;
    
    public override void Interact()
    {
        if (!isFocused)
        {
            Debug.Log("FocusingInteract");
            cinemachineCamera.Priority = 30;
            StartCoroutine(FocusedCountdown(0.1f));
        }
    }

    private void Start()
    {
        InputManager.Instance.InteractClicked += OnInteract;
    }
    
    private IEnumerator FocusedCountdown(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        isFocused = true;
        PlayerID.LockMovement();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    private IEnumerator UnFocusedCountdown(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        isFocused = false;
        PlayerID.UnlockMovement();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnInteract()
    {
        if (isFocused)
        {
            cinemachineCamera.Priority = 0;
            StartCoroutine(UnFocusedCountdown(0.1f));
        }
    }
}
