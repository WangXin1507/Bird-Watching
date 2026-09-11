using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    // TODO: slow player movement & force rotation when dragging (maybe stick to ground)
    //       figure out mouth positioning
    //       flight grabbing

    [Header("Detection")]
    [SerializeField] private LayerMask mask = ~0;
    [SerializeField] private float focusRadius = 30.0f;
    [SerializeField] private int maxTargets = 5;

    [Header("Mouth Holding")]
    [SerializeField] private Transform mouthPos;

    [Header("Dragging")]
    [SerializeField] private float dragSpring = 500.0f;
    [SerializeField] private float dragDamper = 50.0f;
    [SerializeField] private Transform holding;

    private IFocusable focusedTarget;
    private Collider[] targets;
    private Rigidbody heldRb;
    private bool isDragging = false;

    private bool interactSubscribed;
    private bool holdSubscribed;


    private void Awake()
    {
        PlayerID.playerInteraction = this;
        targets = new Collider[maxTargets];
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

    private void FixedUpdate()
    {
        if (holding != null && heldRb != null && !isDragging)
        {
            heldRb.position = mouthPos.position;
            heldRb.rotation = mouthPos.rotation;
        }
    }

    private void CheckProximity()
    {
        if (holding)
        {
            focusedTarget?.LoseFocus();
            focusedTarget = null;
            return;
        }

        int count = Physics.OverlapSphereNonAlloc(transform.position, focusRadius, targets, mask);

        IFocusable closest = null;
        float minDistance = float.MaxValue;

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
            LetGo();
        }
        else if (focusedTarget is IGrabbable grabbable)
        {
            holding = grabbable.Grab(); // return Transform of grabbable

            if (grabbable.isDragged)
            {
                isDragging = true;

                if (holding.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    // Create a joint for the player to drag

                    SpringJoint dragJoint = gameObject.AddComponent<SpringJoint>();
                    dragJoint.connectedBody = rb;
                    dragJoint.autoConfigureConnectedAnchor = false;

                    // Closest point of the object to attach the joint to
                    Vector3 worldClosestPoint = Vector3.zero;
                    if (holding.TryGetComponent<Collider>(out Collider col))
                    {
                        worldClosestPoint = col.ClosestPoint(transform.position);
                    }
                    else
                    {
                        worldClosestPoint = rb.position;
                    }

                    dragJoint.anchor = Vector3.zero;
                    dragJoint.connectedAnchor = rb.transform.InverseTransformPoint(worldClosestPoint);
                    dragJoint.spring = dragSpring;
                    dragJoint.damper = dragDamper;
                }
            }
            else // Not draggable; hold in mouth
            {
                if (holding.TryGetComponent<Rigidbody>(out heldRb))
                {
                    heldRb.isKinematic = true;
                    heldRb.detectCollisions = false;
                }

                // Clear local positioning of object
                holding.transform.localPosition = Vector3.zero;
                holding.transform.localRotation = Quaternion.identity;

                // Position for holding is set in FixedUpdate, I didn't want to mess with reparenting
            }
            
        }
    }

    private void LetGo()
    {
        if (holding == null) return;

        if (isDragging)
        {
            Destroy(gameObject.GetComponent<SpringJoint>());
            isDragging = false;
        }
        else
        {
            if (holding.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.isKinematic = false;
                rb.detectCollisions = true;

                if (TryGetComponent<Rigidbody>(out Rigidbody playerRb))
                {
                    rb.linearVelocity = playerRb.linearVelocity;
                }
            }

        }

        heldRb = null;
        holding = null;
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
