using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Feeds the bird's Animator purely from how a transform moves -- the previous position against
/// the current one -- plus a downward ray for the ground. It doesn't know or care what moved the
/// bird: PlayerMovement, a spline, a Timeline or a hand-placed tween all animate the same way.
/// All states and transitions live in the graph; this script only sets parameters.
///
///   Speed    (float, 0..1)   horizontal speed against fullWalkSpeed
///   Climb    (float, -1..1)  full dive .. level .. full climb, relative to the ground below
///   IsFlying (bool)          true once the bird leaves the ground; turns false landLeadTime
///                            before a predicted touchdown, so the landing finishes on the ground
///   Turn     (float, -1..1)  optional -- yaw rate of the travel direction against fullTurnRate,
///                            for banking poses. Only set if the controller has the parameter.
///
/// Input Triggers fire Animator triggers from input actions (player only; leave it empty on
/// spline birds), and Trigger(name) does the same from code or spline events.
/// </summary>
[DefaultExecutionOrder(100)] // after movers that run in Update, so this frame's motion is sampled
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
    private static readonly int TurnId = Animator.StringToHash("Turn");

    // The ground ray starts this far above the tracked point so it still finds ground the bird is sunk into.
    private const float ProbeLift = 0.5f;
    private const int MaxProbeHits = 8;

    [Tooltip("Optional. Found on this object or its children when left empty.")]
    [SerializeField] private Animator animator;
    [Tooltip("Transform whose motion drives the animations. Defaults to this one.")]
    [SerializeField] private Transform tracked;

    [Header("Speed references")]
    [Tooltip("If a PlayerMovement sits above this object, copy its speed caps into the fields below on Start.")]
    [SerializeField] private bool useMovementCaps = true;
    [Tooltip("Horizontal speed (m/s) at which Speed reaches 1.")]
    [SerializeField] private float fullWalkSpeed = 6f;
    [Tooltip("Climb rate (m/s) at which Climb reaches 1.")]
    [SerializeField] private float fullClimbSpeed = 8f;
    [Tooltip("Descent rate (m/s) at which Climb reaches -1.")]
    [SerializeField] private float fullDiveSpeed = 35f;
    [Tooltip("Turn rate (degrees/s) at which Turn reaches +/-1.")]
    [SerializeField] private float fullTurnRate = 360f;

    [Header("Motion sampling")]
    [Tooltip("Seconds of smoothing on the sampled velocity. Evens out uneven frame times and spline steps.")]
    [SerializeField] private float velocitySmoothing = 0.05f;
    [Tooltip("Faster than this (m/s) between two frames is a teleport -- a spline loop, a respawn -- and is ignored.")]
    [SerializeField] private float teleportSpeed = 100f;

    [Header("Ground detection")]
    [SerializeField] private LayerMask groundMask = ~0;
    [Tooltip("Height of the tracked transform above the ground when the bird is standing. 0 if the pivot is at the feet.")]
    [SerializeField] private float standingHeight = 0f;
    [Tooltip("How far above standing height still counts as on the ground.")]
    [SerializeField] private float groundTolerance = 0.1f;
    [Tooltip("How far below the bird to look for ground. Landing can only be anticipated within this range.")]
    [SerializeField] private float probeDistance = 10f;
    [Tooltip("A frame-to-frame jump in ground height bigger than this is a ledge or step, not a slope, " +
             "and isn't counted as the bird climbing or sinking.")]
    [SerializeField] private float groundStepThreshold = 0.3f;

    [Header("Takeoff")]
    [Tooltip("While grounded, pulling away from the ground faster than this (m/s) counts as taking off -- " +
             "before the bird has even cleared Ground Tolerance. Measured against the ground, so walking uphill doesn't trip it.")]
    [SerializeField] private float takeOffRiseSpeed = 0.5f;

    [Header("Landing")]
    [Tooltip("Turn IsFlying off this many seconds before predicted touchdown. Match it to the Flying -> Land " +
             "transition, or to the second in the Land clip where the feet hit the ground.")]
    [SerializeField] private float landLeadTime = 0.3f;
    [Tooltip("Off = IsFlying only turns false once the bird is actually on the ground.")]
    [SerializeField] private bool anticipateLanding = true;
    [Tooltip("Only anticipate while closing on the ground at least this fast (m/s), so drifting low doesn't trigger it.")]
    [SerializeField] private float minDescentSpeed = 0.3f;

    [Header("Parameter smoothing")]
    [Tooltip("Damp time for Speed, Climb and Turn, so motion noise doesn't show up as twitching.")]
    [SerializeField] private float parameterDampTime = 0.1f;

    [Header("Input triggers")]
    [SerializeField] private List<InputTrigger> inputTriggers = new List<InputTrigger>();
    [Tooltip("Seconds a trigger waits to be used before it is cleared, so a press the graph can't act on " +
             "right now (e.g. a ground-only action mid-air) doesn't fire later by surprise.")]
    [SerializeField] private float triggerBufferTime = 0.15f;

    // Motion
    private bool hasPrevious;
    private Vector3 previousPosition;
    private Vector3 velocity;
    private bool hasHeading;
    private float previousHeading;
    private float turnRate;

    // Ground
    private Transform ignoreRoot;
    private readonly RaycastHit[] hits = new RaycastHit[MaxProbeHits];
    private bool hasGround;
    private float clearance = float.PositiveInfinity; // height above standing height
    private bool hadGroundLastFrame;
    private float previousGroundY;
    private float groundVerticalSpeed;

    // State
    private bool grounded = true;
    private bool landingCommitted;

    // Animator
    private bool parametersCached;
    private bool hasTurnParameter;
    private bool inputBound;
    private readonly Dictionary<int, float> pendingTriggers = new Dictionary<int, float>();
    private readonly List<int> expired = new List<int>();

    /// <summary>Smoothed world velocity sampled from the tracked transform.</summary>
    public Vector3 Velocity => velocity;
    public bool IsGrounded => grounded;
    /// <summary>The value sent to the Animator's IsFlying: airborne and not about to land.</summary>
    public bool IsFlying => !grounded && !landingCommitted;
    /// <summary>Height above standing height; PositiveInfinity with no ground in range.</summary>
    public float Clearance => clearance;
    /// <summary>Vertical speed relative to the ground below: positive pulling away, negative closing in.</summary>
    public float VerticalSpeedFromGround => velocity.y - groundVerticalSpeed;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
        if (tracked == null) tracked = transform;

        // Never let the ground ray land on the bird itself (or anything parented to its body, like a held item).
        Rigidbody body = GetComponentInParent<Rigidbody>();
        ignoreRoot = body != null ? body.transform : tracked;
    }

    private void Start()
    {
        if (useMovementCaps)
        {
            PlayerMovement movement = GetComponentInParent<PlayerMovement>();
            if (movement != null)
            {
                fullWalkSpeed = movement.GroundMaxSpeed;
                fullClimbSpeed = movement.MaxRiseSpeed;
                fullDiveSpeed = movement.MaxDiveSpeed;
            }
        }

        BindInput();
    }

    private void OnEnable()
    {
        hasPrevious = false; // don't read the gap while disabled as a burst of speed
        BindInput();
    }

    private void OnDisable() => UnbindInput();

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return; // paused

        SampleMotion(dt);
        SampleGround(dt);
        UpdateGrounded();
        UpdateLandingAnticipation();
        WriteParameters(dt);
        ExpireTriggers();
    }

    // ---------------------------------------------------------------- sampling

    private void SampleMotion(float dt)
    {
        Vector3 position = tracked.position;
        if (!hasPrevious)
        {
            previousPosition = position;
            hasPrevious = true;
            return;
        }

        Vector3 raw = (position - previousPosition) / dt;
        previousPosition = position;

        if (raw.sqrMagnitude > teleportSpeed * teleportSpeed)
        {
            velocity = Vector3.zero;
            hasHeading = false;
            hadGroundLastFrame = false;
            return;
        }

        float blend = Smoothing(dt);
        velocity = Vector3.Lerp(velocity, raw, blend);

        // Turn comes from the direction of travel, not the transform's rotation, so it works no
        // matter what rotates the bird (or whether anything does).
        Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);
        if (planar.sqrMagnitude > 0.01f)
        {
            float heading = Mathf.Atan2(planar.x, planar.z) * Mathf.Rad2Deg;
            float rate = hasHeading ? Mathf.DeltaAngle(previousHeading, heading) / dt : 0f;
            turnRate = Mathf.Lerp(turnRate, rate, blend);
            previousHeading = heading;
            hasHeading = true;
        }
        else
        {
            turnRate = Mathf.Lerp(turnRate, 0f, blend);
            hasHeading = false;
        }
    }

    /// <summary>
    /// Finds the ground straight below, and how fast that ground is rising or falling under the bird.
    /// Climb, takeoff and landing are all measured against it, so slopes read as walking, not flying.
    /// </summary>
    private void SampleGround(float dt)
    {
        Vector3 origin = tracked.position + Vector3.up * ProbeLift;
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, hits, ProbeLift + probeDistance,
            groundMask, QueryTriggerInteraction.Ignore);

        float nearest = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].collider.transform.IsChildOf(ignoreRoot)) continue;
            if (hits[i].distance < nearest) nearest = hits[i].distance;
        }

        hasGround = !float.IsPositiveInfinity(nearest);
        if (!hasGround)
        {
            clearance = float.PositiveInfinity;
            groundVerticalSpeed = Mathf.Lerp(groundVerticalSpeed, 0f, Smoothing(dt));
            hadGroundLastFrame = false;
            return;
        }

        float heightAboveGround = nearest - ProbeLift;
        clearance = heightAboveGround - standingHeight;

        float groundY = tracked.position.y - heightAboveGround;
        float groundRate = 0f;
        if (hadGroundLastFrame && Mathf.Abs(groundY - previousGroundY) <= groundStepThreshold)
            groundRate = (groundY - previousGroundY) / dt;

        groundVerticalSpeed = Mathf.Lerp(groundVerticalSpeed, groundRate, Smoothing(dt));
        previousGroundY = groundY;
        hadGroundLastFrame = true;
    }

    // ---------------------------------------------------------------- state

    private void UpdateGrounded()
    {
        float pullingAway = VerticalSpeedFromGround;

        if (grounded)
        {
            // Leaves the ground by clearing the tolerance (with a little hysteresis), or by
            // visibly pulling away from it -- which catches a takeoff on its first frames.
            bool cleared = !hasGround || clearance > groundTolerance * 1.5f;
            grounded = !(cleared || pullingAway > takeOffRiseSpeed);
        }
        else
        {
            grounded = hasGround && clearance <= groundTolerance && pullingAway <= takeOffRiseSpeed;
        }
    }

    /// <summary>
    /// Latched once touchdown is predicted within landLeadTime so it can't flicker near the ground;
    /// levelling off or climbing away (half the trigger speed, for hysteresis) lets go of it.
    /// </summary>
    private void UpdateLandingAnticipation()
    {
        if (grounded || !anticipateLanding || landLeadTime <= 0f)
        {
            landingCommitted = false;
            return;
        }

        float closing = -VerticalSpeedFromGround;

        if (landingCommitted)
        {
            if (closing < minDescentSpeed * 0.5f) landingCommitted = false;
        }
        else if (hasGround && closing >= minDescentSpeed)
        {
            float timeToLand = Mathf.Max(0f, clearance - groundTolerance) / closing;
            landingCommitted = timeToLand <= landLeadTime;
        }
    }

    // ---------------------------------------------------------------- animator

    private void WriteParameters(float dt)
    {
        if (animator == null || animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled) return;
        if (!parametersCached) CacheParameters();

        float horizontal = new Vector2(velocity.x, velocity.z).magnitude;
        float speed01 = fullWalkSpeed > 0f ? Mathf.Clamp01(horizontal / fullWalkSpeed) : 0f;
        animator.SetFloat(SpeedId, speed01, parameterDampTime, dt);
        animator.SetFloat(ClimbId, Climb(VerticalSpeedFromGround), parameterDampTime, dt);
        animator.SetBool(IsFlyingId, IsFlying);

        if (hasTurnParameter)
        {
            float turn01 = fullTurnRate > 0f ? Mathf.Clamp(turnRate / fullTurnRate, -1f, 1f) : 0f;
            animator.SetFloat(TurnId, turn01, parameterDampTime, dt);
        }
    }

    /// <summary>-1 (full dive) .. 0 (level) .. 1 (full climb).</summary>
    private float Climb(float vertical)
    {
        if (vertical >= 0f) return fullClimbSpeed > 0f ? Mathf.Clamp01(vertical / fullClimbSpeed) : 0f;
        return fullDiveSpeed > 0f ? -Mathf.Clamp01(-vertical / fullDiveSpeed) : 0f;
    }

    private void CacheParameters()
    {
        parametersCached = true;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.nameHash == TurnId && parameter.type == AnimatorControllerParameterType.Float)
                hasTurnParameter = true;
    }

    /// <summary>Sets an Animator trigger, cleared again after triggerBufferTime if the graph doesn't use it.</summary>
    public void Trigger(string triggerName)
    {
        if (animator == null) return;

        int id = Animator.StringToHash(triggerName);
        animator.SetTrigger(id);
        pendingTriggers[id] = Time.time + triggerBufferTime;
    }

    private void ExpireTriggers()
    {
        if (pendingTriggers.Count == 0 || animator == null) return;

        expired.Clear();
        foreach (KeyValuePair<int, float> pair in pendingTriggers)
            if (Time.time >= pair.Value) expired.Add(pair.Key);

        foreach (int id in expired)
        {
            animator.ResetTrigger(id);
            pendingTriggers.Remove(id);
        }
    }

    private float Smoothing(float dt) => velocitySmoothing > 0f ? 1f - Mathf.Exp(-dt / velocitySmoothing) : 1f;

    // ---------------------------------------------------------------- input

    private void BindInput()
    {
        if (inputBound || inputTriggers.Count == 0) return;
        if (InputManager.Instance == null || InputManager.Instance.Actions == null) return;

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

    // ---------------------------------------------------------------- editor

    private void OnValidate()
    {
        fullWalkSpeed = Mathf.Max(0f, fullWalkSpeed);
        fullClimbSpeed = Mathf.Max(0f, fullClimbSpeed);
        fullDiveSpeed = Mathf.Max(0f, fullDiveSpeed);
        fullTurnRate = Mathf.Max(0f, fullTurnRate);
        velocitySmoothing = Mathf.Max(0f, velocitySmoothing);
        teleportSpeed = Mathf.Max(0.01f, teleportSpeed);
        groundTolerance = Mathf.Max(0f, groundTolerance);
        probeDistance = Mathf.Max(0f, probeDistance);
        groundStepThreshold = Mathf.Max(0f, groundStepThreshold);
        takeOffRiseSpeed = Mathf.Max(0f, takeOffRiseSpeed);
        landLeadTime = Mathf.Max(0f, landLeadTime);
        minDescentSpeed = Mathf.Max(0f, minDescentSpeed);
        parameterDampTime = Mathf.Max(0f, parameterDampTime);
        triggerBufferTime = Mathf.Max(0f, triggerBufferTime);
    }

    /// <summary>Select the bird to see the ground probe: green on the ground, yellow landing, cyan flying.</summary>
    private void OnDrawGizmosSelected()
    {
        Transform t = tracked != null ? tracked : transform;
        Vector3 feet = t.position + Vector3.down * standingHeight;

        Gizmos.color = Color.gray;
        Gizmos.DrawLine(t.position + Vector3.up * ProbeLift, t.position + Vector3.down * probeDistance);

        Gizmos.color = !Application.isPlaying || grounded ? Color.green : landingCommitted ? Color.yellow : Color.cyan;
        Gizmos.DrawWireSphere(feet, 0.05f);
        Gizmos.DrawLine(feet, feet + Vector3.down * groundTolerance);
    }
}
