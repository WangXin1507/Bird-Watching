using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    public static InputManager Instance { get; private set; }
    
    private InputSystem_Actions inputActions;
    private InputSystem_Actions.PlayerActions playerInput;

    public InputSystem_Actions Actions => inputActions;

    public Vector2 movementVector;
    public Vector2 lookVector;

    // Declared as events so subscribers can only use += / -=; a stray '=' assignment
    // would otherwise wipe every other listener without a compiler error.
    public event UnityAction RiseClicked;
    public event UnityAction RiseReleased;
    
    public event UnityAction InteractClicked;
    public event UnityAction InteractReleased;
    
    public event UnityAction HoldClicked;
    public event UnityAction HoldReleased;
    
    public event UnityAction DiveClicked;
    public event UnityAction DiveReleased;
    
    private void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        
        // Destroy the duplicate, not the manager that is already live and subscribed to.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        inputActions = new InputSystem_Actions();
        playerInput = inputActions.Player;
        playerInput.Enable();
        playerInput.AddCallbacks(this);
    }

    private void OnDestroy()
    {
        if (inputActions != null)
        {
            playerInput.RemoveCallbacks(this);
            playerInput.Disable();
            inputActions.Dispose();
        }
        if (Instance == this) Instance = null;
    }

    public void OnMovement(InputAction.CallbackContext context)
    {
        movementVector = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        lookVector = context.ReadValue<Vector2>();
    }

    public void OnRise(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            RiseClicked?.Invoke();
        }
        else if (context.canceled)
        {
            RiseReleased?.Invoke();
        }
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            InteractClicked?.Invoke();
        }
        else if (context.canceled)
        {
            InteractReleased?.Invoke();
        }
    }

    public void OnHold(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HoldClicked?.Invoke();
        }
        else if (context.canceled)
        {
            HoldReleased?.Invoke();
        }
    }
    
    public void OnDive(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            DiveClicked?.Invoke();
        }
        else if (context.canceled)
        {
            DiveReleased?.Invoke();
        }
    }
}
