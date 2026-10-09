using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask mask = ~0;
    [SerializeField] private float focusRadius = 0.25f;
    [SerializeField] private int maxTargets = 5;

    [Header("Holding")]
    [SerializeField] private Transform mouthPos;

    private Collider playerCollider;
    private Rigidbody rb;

    private IFocusable focusedTarget;
    private Collider[] targets;

    public Transform holding;
    private Rigidbody heldRb;
    private Collider heldCollider;

    private bool interactSubscribed;
    private bool holdSubscribed;


    private void Awake()
    {
        PlayerID.playerInteraction = this;
        targets = new Collider[maxTargets];
        playerCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        SubscribeToInteract();
        SubscribeToHold();
    }

    private void Update()
    {
        CheckProximity(); // Check for any nearby Focusable objects
    }

    private void CheckProximity()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, focusRadius, targets, mask);

        IFocusable closest = null;
        float minDistance = float.MaxValue;

        // Calculate closest focusables
        for (int i = 0; i < count; i++)
        {
            Collider col = targets[i];
            if (col == null) continue;

            if (holding != null && col.transform == holding) continue;
            if (!col.TryGetComponent<IFocusable>(out var focusable)) continue;
            if (!focusable.enabled) continue;
            if (holding != null && focusable is IGrabbable) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < minDistance && focusable.CanFocus())
            {
                minDistance = dist;
                closest = focusable;
            }
        }

        if (closest != focusedTarget)
        {
            focusedTarget?.LoseFocus();
            focusedTarget = closest;
        }

        for (int i = 0; i < targets.Length; i++) targets[i] = null;
    }

    private void TryInteract()
    {
        if (focusedTarget is IInteractable interactable)
        {
            interactable.Interact();
        }
    }

    private void TryHold()
    {
        // If there is an object in the mouth
        if (holding)
        {
            _StopHolding();
            return;
        }
        else if (focusedTarget is IGrabbable grabbable)
        {
            holding = grabbable.Grab(); // return Transform of grabbable
            if (!holding) return;

            holding.TryGetComponent<Rigidbody>(out heldRb);
            holding.TryGetComponent<Collider>(out heldCollider);

            if (!(PlayerID.playerMovement.GetState() == PlayerMovement.MovementState.Flying))
            {
                _Hold(mouthPos);
            }
        }
    }

    public bool ForceHold(Transform newHold)
    {
        if (holding) return false;

        holding = newHold;
        newHold.TryGetComponent<Rigidbody>(out heldRb);
        newHold.TryGetComponent<Collider>(out heldCollider);
        _Hold(mouthPos);
        return true;
    }

    public void ForceRemove()
    {
        heldCollider = null;
        heldRb = null;
        holding = null;
    }

    void _Hold(Transform pos)
    {
        if (!heldRb) return;

        IgnorePlayerCollision(true);

        heldRb.isKinematic = true;
        heldRb.useGravity = false;

        IGrabbable grabbable = heldRb.GetComponent<IGrabbable>();

        heldRb.transform.SetParent(transform, true);
        heldRb.transform.position = pos.position;
        heldRb.transform.localRotation = grabbable.grabHandle.localRotation;
    }

    private void _StopHolding()
    {
        if (!holding) return;

        Vector3 releaseVelocity = Vector3.zero;
        if (rb) releaseVelocity = rb.linearVelocity;

        IgnorePlayerCollision(false);

        if (heldRb) // Drop any held object
        {
            heldRb.transform.SetParent(null, true);

            heldRb.isKinematic = false;
            heldRb.useGravity = true;
            heldRb.linearVelocity = releaseVelocity;
        }

        heldCollider = null;
        heldRb = null;
        holding = null;
    }
    void IgnorePlayerCollision(bool value)
    {
        if (!playerCollider || !heldCollider) return;
        Physics.IgnoreCollision(playerCollider, heldCollider, value);
    }

    // Subscribe and unsubscribe from inputs, copied from PlayerMovement
    private void SubscribeToInteract()
    {
        if (interactSubscribed || InputManager.Instance == null) return;

        InputManager.Instance.InteractClicked += TryInteract;
        interactSubscribed = true;
    }

    private void UnsubscribeFromInteract()
    {
        if (!interactSubscribed) return;

        if (InputManager.Instance != null) InputManager.Instance.InteractClicked -= TryInteract;
        interactSubscribed = false;
    }

    private void SubscribeToHold()
    {
        if (holdSubscribed || InputManager.Instance == null) return;

        InputManager.Instance.HoldClicked += TryHold;
        holdSubscribed = true;
    }

    private void UnsubscribeFromHold()
    {
        if (!holdSubscribed) return;

        if (InputManager.Instance != null) InputManager.Instance.HoldClicked -= TryHold;
        holdSubscribed = false;
    }
}
