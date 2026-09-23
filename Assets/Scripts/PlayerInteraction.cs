using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerInteraction : MonoBehaviour
{
    // TODO: slow player movement & force rotation when dragging (maybe stick to ground)
    //       figure out mouth positioning
    //       flight grabbing

    [Header("Detection")]
    [SerializeField] private LayerMask mask = ~0;
    [SerializeField] private float focusRadius = 30.0f;
    [SerializeField] private int maxTargets = 5;

    [Header("Holding")]
    [SerializeField] private Transform mouthPos;
    [SerializeField] private Transform feetPos;

    [Header("Dragging")]
    [SerializeField] private float dragSpring = 500.0f;
    [SerializeField] private float dragDamper = 50.0f;

    private Collider playerCollider;
    private Rigidbody rb;

    private IFocusable focusedTarget;
    private Collider[] targets;

    private Transform holding;
    private Rigidbody heldRb;
    private Collider heldCollider;

    private SpringJoint dragJoint;
    private bool isDragging = false;
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
        if (holding)
        {
            if (focusedTarget != null)
            {
                focusedTarget.LoseFocus();
                focusedTarget = null;
            }
            return;
        }

        int count = Physics.OverlapSphereNonAlloc(transform.position, focusRadius, targets, mask);

        IFocusable closest = null;
        float minDistance = float.MaxValue;

        // Calculate closest focusables
        for (int i = 0; i < count; i++)
        {
            if (targets[i] != null && targets[i].TryGetComponent<IFocusable>(out var focusable))
            {
                float dist = Vector3.Distance(transform.position, targets[i].transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closest = focusable;
                }
            }
        }

        if (closest != focusedTarget)
        {
            focusedTarget?.LoseFocus();
            focusedTarget = closest;
            focusedTarget?.GainFocus(); // Gain focus can handle user prompting, like a shader
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
            StopHolding();
            return;
        }
        else if (focusedTarget is IGrabbable grabbable)
        {
            holding = grabbable.Grab(); // return Transform of grabbable
            if (!holding) return;

            holding.TryGetComponent<Rigidbody>(out heldRb);
            holding.TryGetComponent<Collider>(out heldCollider);

            if (PlayerID.playerMovement.GetState() == PlayerMovement.MovementState.Flying)
            {
                if (!grabbable.isDragged)
                {
                    _Hold(mouthPos);
                }
            }
            else if (grabbable.isDragged)
            {
                _Drag();
            }
            else// Not draggable and is not flying, hold in feet
            {
                _Hold(feetPos);
            }
        }
    }

    void _Hold(Transform pos)
    {
        if (!heldRb /*|| !pos*/) return;

        IgnorePlayerCollision(true);

        heldRb.isKinematic = true;
        heldRb.useGravity = false;

        //heldRb.transform.SetParent(pos, false);
        heldRb.transform.SetParent(transform, false);
        heldRb.transform.localPosition = Vector3.zero;
        heldRb.transform.localRotation = Quaternion.identity;
    }

    private void _Drag()
    {
        if (!heldRb) return;

        isDragging = true;

        SpringJoint existingJoint = heldRb.GetComponent<SpringJoint>();
        if (existingJoint)
        {
            Destroy(existingJoint);
        }

        dragJoint = heldRb.gameObject.AddComponent<SpringJoint>();
        dragJoint.autoConfigureConnectedAnchor = false;

        Vector3 attachPoint = heldRb.position; 
        if (heldCollider)
        { 
            attachPoint = heldCollider.ClosestPoint(transform.position);
        }
        dragJoint.anchor = heldRb.transform.InverseTransformPoint(attachPoint);

        if (rb) {
            dragJoint.connectedBody = rb;
            dragJoint.connectedAnchor = rb.transform.InverseTransformPoint(transform.position);
        }

        dragJoint.spring = dragSpring;
        dragJoint.damper = dragDamper;
        dragJoint.minDistance = 0f;
        dragJoint.maxDistance = 0.25f;
    }

    private void StopHolding()
    {
        if (!holding) return;

        Vector3 releaseVelocity = Vector3.zero;
        if (rb) releaseVelocity = rb.linearVelocity;

        IgnorePlayerCollision(false);

        if (isDragging) // Stop dragging
        {
            if (dragJoint)
            {
                dragJoint.connectedBody = null;
                Destroy(dragJoint);
                dragJoint = null;
            }

            if (heldRb)
            {
                heldRb.linearVelocity = releaseVelocity;
            }
        }
        else if (heldRb) // Drop any held object
        {
            heldRb.transform.SetParent(null, true);

            heldRb.isKinematic = false;
            heldRb.useGravity = true;
            heldRb.linearVelocity = releaseVelocity;
        }

        isDragging = false;
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
