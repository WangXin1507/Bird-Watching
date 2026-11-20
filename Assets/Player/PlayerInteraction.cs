using DG.Tweening;
using System;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private LayerMask mask = ~0;
    [SerializeField] private float focusRadius = 0.40f;
    [SerializeField] private int maxTargets = 5;

    [Header("Holding")]
    [SerializeField] private Transform mouthPos;

    private Collider playerCollider;
    private Rigidbody rb;

    private IFocusable focusedTarget;
    private Transform focusedTransform;
    private Collider[] targets;
    private int targetCount;

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

    private void FixedUpdate()
    {
    }

    private void Update()
    {
        CheckProximity(); // Check for any nearby Focusable objects
    }

    private void CheckProximity()
    {
        targetCount = Physics.OverlapSphereNonAlloc(transform.position, focusRadius, targets, mask);

        IFocusable closest = null;
        Transform closestTransform = null;
        float minDistance = float.MaxValue;
        float bias = 1.5f;

        // Calculate closest focusables
        for (int i = 0; i < targetCount; i++)
        {
            Collider col = targets[i];
            if (col == null) continue;

            if (holding != null && col.transform == holding) continue;
            if (!col.TryGetComponent<IFocusable>(out var focusable)) continue;
            if (!focusable.CanFocus()) continue;
            if (!focusable.enabled) continue;
            if (holding != null && focusable is IGrabbable) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (focusedTransform && col.transform == focusedTransform)
            {
                dist -= bias;
            }
            
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = focusable;
                closestTransform = col.transform;
            }
        }

        if (closestTransform != focusedTransform)
        {
            Debug.Log("Changing focused target");
            focusedTarget?.LoseFocus();
            focusedTarget = closest;
            focusedTransform = closestTransform;
            closest?.GainFocus();
        }
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

            _Hold(mouthPos);
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
        ItemComponent itemComponent = heldRb.GetComponent<ItemComponent>();

        Vector3 targetPos = pos.position;
        Quaternion targetRot = pos.localRotation;

        //To make calculations easier(like centering the kite on the corner) you can just move the handle around and set
        //overrideHandlePos to false.Otherwise it will just move the handle with the data offset.
        if (itemComponent && itemComponent.overrideHandlePos && itemComponent.data)
        {
            if (grabbable.grabHandle) grabbable.grabHandle.localRotation = Quaternion.Euler(itemComponent.data.grabRotation);
            if (grabbable.grabHandle) grabbable.grabHandle.localPosition = itemComponent.data.grabOffset;
        }

        if (grabbable.grabHandle)
        {
            heldRb.transform.SetParent(pos);
            heldRb.transform.localRotation = Quaternion.Inverse(grabbable.grabHandle.localRotation);

            Vector3 posOffset = heldRb.transform.position - grabbable.grabHandle.position;
            heldRb.transform.position = targetPos + posOffset;
        }
        else
        {
            heldRb.transform.SetParent(pos);
            heldRb.transform.localRotation = targetRot;
            heldRb.transform.position = targetPos;
        }
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
