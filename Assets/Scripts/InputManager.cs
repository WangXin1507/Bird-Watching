using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    public static InputManager Instance { get; private set; }
    public bool moveInputEnabled = true;
    public bool lookInputEnabled = true;
    public bool interactionInputEnabled = true;
    private Camera mainCamera;
    
    private InputSystem_Actions inputActions;
    private InputSystem_Actions.PlayerActions playerInput;

    public InputSystem_Actions Actions => inputActions;

    public Vector2 movementVector;
    public Vector2 lookVector;
    // Look input that ignores lookInputEnabled, for menus (e.g. inspection) that rotate things
    // while the player camera is locked. lookIsPointerDelta is true for mouse delta, false for sticks.
    public Vector2 rawLookVector;
    public bool lookIsPointerDelta;
    public Vector2 mousePos;

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

    public event UnityAction JournalClicked;
    public event UnityAction JournalReleased;
    
    public event UnityAction PauseClicked;
    public event UnityAction PauseReleased;

    public void LockInputs(bool moveInput, bool lookInput, bool interactInput)
    {
        bool moveWasEnabled = moveInputEnabled;
        moveInputEnabled = !moveInput;
        lookInputEnabled = !lookInput;
        interactionInputEnabled = !interactInput;

        if (!moveInputEnabled)
        {
            movementVector = Vector2.zero;
            if (moveWasEnabled)
            {
                RiseReleased?.Invoke();
                DiveReleased?.Invoke();
            }
        }

        if (!lookInputEnabled)
            lookVector = Vector2.zero;
    }

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

        LockInputs(false, false, false);
    }

    private void Start()
    {
        mainCamera = Camera.main;
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
        if (!moveInputEnabled)
        {
            return;
        }
        movementVector = context.ReadValue<Vector2>();
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        rawLookVector = context.ReadValue<Vector2>();
        lookIsPointerDelta = context.control?.device is Pointer;

        if (!lookInputEnabled)
        {
            return;
        }
        lookVector = context.ReadValue<Vector2>();
    }

    public void OnRise(InputAction.CallbackContext context)
    {
        if (!moveInputEnabled)
        {
            return;
        }
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
        if (!interactionInputEnabled)
        {
            return;
        }
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
        if (!interactionInputEnabled)
        {
            return;
        }
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
        if (!moveInputEnabled)
        {
            return;
        }
        if (context.performed)
        {
            DiveClicked?.Invoke();
        }
        else if (context.canceled)
        {
            DiveReleased?.Invoke();
        }
    }

    public void OnJournal(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            JournalClicked?.Invoke();
        }
        else if (context.canceled)
        {
            JournalReleased?.Invoke();
        }
    }

    public void OnPause(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PauseClicked?.Invoke();
        }
        else if (context.canceled)
        {
            PauseReleased?.Invoke();
        }
    }

    public void OnFocusClick(InputAction.CallbackContext context)
    {
        if (context.performed) {
            RaycastHit hitInfo;
            if (Physics.Raycast(mainCamera.ScreenPointToRay(mousePos), out hitInfo, Mathf.Infinity))
            {
                GameObject hitObject = hitInfo.collider.gameObject;
                InteractInFocus interactInFocus = hitObject.GetComponent<InteractInFocus>();
                if (interactInFocus != null) {
                    interactInFocus.Interact();
                }
                else
                {
                    Debug.Log("Not the ocrrect type");
                }
            }
        }
    }

    public void OnMousePos(InputAction.CallbackContext context)
    {
        mousePos = context.ReadValue<Vector2>();
    }

    private void OnDrawGizmos()
    {
        RaycastHit hitInfo;
        if (mainCamera != null && Physics.Raycast(mainCamera.ScreenPointToRay(mousePos), out hitInfo, Mathf.Infinity))
        {
            GameObject hitObject = hitInfo.collider.gameObject;
            Gizmos.DrawLine(mainCamera.transform.position, hitObject.transform.position);
        }
    }
}
