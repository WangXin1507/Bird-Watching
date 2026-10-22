using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Feeds the bird's Animator from PlayerMovement. All states and transitions live in the graph;
/// this script only sets parameters.
///
///   Speed    (float, 0..1)   horizontal speed against the ground speed cap
///   Climb    (float, -1..1)  full dive .. level .. full climb
///   IsFlying (bool)          true in the air; turns false landLeadTime before a predicted touchdown,
///                            so a Flying -> Grounded (or -> Land) transition finishes on the ground
///
/// Input Triggers fire Animator triggers from input actions, so you can punt out of Locomotion
/// (e.g. Any State -> Peck on the "Peck" trigger) and come back with Has Exit Time.
/// </summary>
public class PlayerVisuals : MonoBehaviour
{
    [Serializable]
    public class InputTrigger
    {
        [Tooltip("Input action as 'Map/Action' in InputSystem_Actions, e.g. 'BirdActions/Peck'. Use a separate " +
                 "action map, not 'Player' -- every 'Player' action needs an On...() method on InputManager.")]
        public string input = "BirdActions/Peck";
        [Tooltip("Animator trigger parameter to set.")]
        public string trigger = "Peck";

        [NonSerialized] internal InputAction bound;
        [NonSerialized] internal Action<InputAction.CallbackContext> handler;
    }

    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int ClimbId = Animator.StringToHash("Climb");
    private static readonly int IsFlyingId = Animator.StringToHash("IsFlying");

    [Tooltip("Optional. Found on this object or its children when left empty.")]
    [SerializeField] private Animator animator;

    [Header("Landing")]
    [Tooltip("Turn IsFlying off this many seconds before a predicted touchdown. Match it to your Flying -> Grounded " +
             "transition duration, or to the touchdown frame of a Land clip.")]
    [SerializeField] private float landLeadTime = 0.3f;
    [Tooltip("Off = IsFlying only turns false once the bird is actually on the ground.")]
    [SerializeField] private bool anticipateLanding = true;
    [Tooltip("Only anticipate while descending at least this fast (m/s), so drifting near the ground doesn't trigger it.")]
    [SerializeField] private float minDescentSpeed = 0.3f;

    [Header("Parameter smoothing")]
    [Tooltip("Damp time for Speed and Climb, so velocity noise doesn't show up as twitching.")]
    [SerializeField] private float parameterDampTime = 0.1f;

    [Header("Input triggers")]
    [SerializeField] private List<InputTrigger> inputTriggers = new List<InputTrigger>();
    [Tooltip("Seconds a trigger waits to be used before it is cleared, so a press the graph can't act on " +
             "right now (e.g. a ground-only action mid-air) doesn't fire later by surprise.")]
    [SerializeField] private float triggerBufferTime = 0.15f;

    private PlayerMovement movement;
    private bool landingCommitted;
    private bool inputBound;
    private readonly Dictionary<int, float> pendingTriggers = new Dictionary<int, float>();
    private readonly List<int> expired = new List<int>();

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
        movement = GetComponentInParent<PlayerMovement>();
    }

    // InputManager is created in Awake, so binding waits for Start; OnEnable covers re-enabling later.
    private void Start() => BindInput();
    private void OnEnable() => BindInput();
    private void OnDisable() => UnbindInput();

    private void Update()
    {
        if (movement == null) movement = PlayerID.playerMovement;
        if (movement == null || animator == null || animator.runtimeAnimatorController == null) return;

        float speed01 = movement.GroundMaxSpeed > 0f ? movement.HorizontalSpeed / movement.GroundMaxSpeed : 0f;
        animator.SetFloat(SpeedId, Mathf.Clamp01(speed01), parameterDampTime, Time.deltaTime);
        animator.SetFloat(ClimbId, Climb(movement.VerticalSpeed), parameterDampTime, Time.deltaTime);
        animator.SetBool(IsFlyingId, movement.IsFlying && !LandingAnticipated());

        ExpireTriggers();
    }

    /// <summary>Sets an Animator trigger, cleared again after triggerBufferTime if the graph doesn't use it.</summary>
    public void Trigger(string triggerName)
    {
        if (animator == null) return;

        int id = Animator.StringToHash(triggerName);
        animator.SetTrigger(id);
        pendingTriggers[id] = Time.time + triggerBufferTime;
    }

    /// <summary>
    /// Latched once touchdown is predicted within landLeadTime, so it doesn't flicker near the ground.
    /// Holding Rise or levelling off (half the trigger speed, for hysteresis) lets go of it.
    /// </summary>
    private bool LandingAnticipated()
    {
        if (!movement.IsFlying || !anticipateLanding)
        {
            landingCommitted = false;
            return false;
        }

        float descent = -movement.VerticalSpeed;

        if (landingCommitted)
        {
            if (movement.RiseHeld || descent < minDescentSpeed * 0.5f) landingCommitted = false;
        }
        else if (!movement.RiseHeld && descent >= minDescentSpeed)
        {
            landingCommitted = movement.TimeToLanding <= landLeadTime; // Infinity when no ground in range
        }

        return landingCommitted;
    }

    /// <summary>Vertical speed mapped to -1 (full dive) .. 0 (level) .. 1 (full climb).</summary>
    private float Climb(float vertical)
    {
        if (vertical >= 0f)
            return movement.MaxRiseSpeed > 0f ? Mathf.Clamp01(vertical / movement.MaxRiseSpeed) : 0f;

        return movement.MaxDiveSpeed > 0f ? -Mathf.Clamp01(-vertical / movement.MaxDiveSpeed) : 0f;
    }

    private void ExpireTriggers()
    {
        if (pendingTriggers.Count == 0) return;

        expired.Clear();
        foreach (KeyValuePair<int, float> pair in pendingTriggers)
            if (Time.time >= pair.Value) expired.Add(pair.Key);

        foreach (int id in expired)
        {
            animator.ResetTrigger(id);
            pendingTriggers.Remove(id);
        }
    }

    // ---------------------------------------------------------------- input

    private void BindInput()
    {
        if (inputBound || InputManager.Instance == null || InputManager.Instance.Actions == null) return;

        InputActionAsset asset = InputManager.Instance.Actions.asset;
        foreach (InputTrigger entry in inputTriggers)
        {
            if (string.IsNullOrWhiteSpace(entry.input) || string.IsNullOrWhiteSpace(entry.trigger)) continue;

            InputAction action = asset.FindAction(entry.input, throwIfNotFound: false);
            if (action == null)
            {
                Debug.LogError($"PlayerVisuals: input action '{entry.input}' isn't in InputSystem_Actions.", this);
                continue;
            }

            if (!action.actionMap.enabled) action.actionMap.Enable();

            string trigger = entry.trigger;
            entry.handler = _ => OnTriggerInput(trigger);
            entry.bound = action;
            action.performed += entry.handler;
        }

        inputBound = true;
    }

    private void UnbindInput()
    {
        if (!inputBound) return;

        foreach (InputTrigger entry in inputTriggers)
        {
            if (entry.bound != null && entry.handler != null) entry.bound.performed -= entry.handler;
            entry.bound = null;
            entry.handler = null;
        }

        inputBound = false;
    }

    private void OnTriggerInput(string trigger)
    {
        // Same gate the rest of the player's buttons respect (journal, dialogue, pause).
        if (InputManager.Instance != null && !InputManager.Instance.interactionInputEnabled) return;
        if (Time.timeScale <= 0f) return;
        Trigger(trigger);
    }

    private void OnValidate()
    {
        landLeadTime = Mathf.Max(0f, landLeadTime);
        minDescentSpeed = Mathf.Max(0f, minDescentSpeed);
        parameterDampTime = Mathf.Max(0f, parameterDampTime);
        triggerBufferTime = Mathf.Max(0f, triggerBufferTime);
    }
}
