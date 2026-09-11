using UnityEngine;

/// <summary>
/// State-machine movement for the bird. Attach to the player root alongside a Rigidbody
/// and a Collider. WASD steers in the horizontal plane only; height is Rise (space) and
/// Dive (shift), each with its own acceleration and speed cap. Gravity applies only when
/// nothing at all is held -- a bird under power keeps its altitude.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public enum MovementState
    {
        GroundedIdle,
        GroundedMoving,
        Flying
    }

    [Header("Ground movement")]
    [SerializeField] private float groundAcceleration = 45f;
    [SerializeField] private float groundFriction = 35f;
    [SerializeField] private float groundMaxSpeed = 6f;

    [Header("Air movement (horizontal)")]
    [SerializeField] private float airAcceleration = 20f;
    [SerializeField] private float airFriction = 4f;
    [Tooltip("Horizontal speed cap while airborne. Vertical speed is capped separately by rise and dive.")]
    [SerializeField] private float airMaxSpeed = 9f;

    [Header("Rise (space, held)")]
    [Tooltip("How hard the climb builds toward the cap. Gravity is off entirely while rising, so this is the whole story.")]
    [SerializeField] private float riseAcceleration = 40f;
    [Tooltip("Upward speed cap while rising.")]
    [SerializeField] private float maxRiseSpeed = 8f;

    [Header("Dive (shift, held)")]
    [Tooltip("How hard the descent builds toward the cap. Replaces gravity rather than adding to it.")]
    [SerializeField] private float diveAcceleration = 60f;
    [Tooltip("Downward speed cap while diving.")]
    [SerializeField] private float maxDiveSpeed = 35f;

    [Tooltip("How hard vertical speed is pulled back to zero while flying under power with no rise or dive. High = releasing rise stops the climb dead.")]
    [SerializeField] private float verticalStopAcceleration = 60f;
    [Tooltip("Seconds the ground check is ignored after leaving the ground, so a takeoff isn't swallowed on the same frame.")]
    [SerializeField] private float takeoffGroundGrace = 0.15f;

    [Header("Falling (nothing held)")]
    [SerializeField] private float gravity = 24f;
    [SerializeField] private float maxFallSpeed = 20f;

    [Header("Facing")]
    [Tooltip("Turn the body to face the direction it is actually travelling, not the direction being pressed.")]
    [SerializeField] private bool rotateTowardVelocity = true;
    [Tooltip("Degrees per second the body turns toward its velocity. Smoothing this out is what kills the frame-to-frame jitter.")]
    [SerializeField] private float turnSpeed = 720f;
    [Tooltip("Below this speed the facing is left alone, so the bird doesn't chase noise while nearly stopped.")]
    [SerializeField] private float minTurnSpeed = 0.4f;
    [Tooltip("Nose up while rising and down while diving, in proportion to climb rate against airspeed. 0 = keep the body level.")]
    [SerializeField] private float maxPitchAngle = 45f;
    [Tooltip("Degrees of roll per degree of turn still to go.")]
    [SerializeField] private float bankPerDegree = 1f;
    [Tooltip("Hard cap on how far the body rolls. 0 = no banking.")]
    [SerializeField] private float maxBankAngle = 35f;
    [Tooltip("Degrees per second the roll eases in and back out. Lower = lazier wings.")]
    [SerializeField] private float bankSpeed = 180f;

    [Header("Ground check")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [Tooltip("Cast origin measured up from the player's pivot.")]
    [SerializeField] private float groundCheckOffset = 0.4f;
    [SerializeField] private float groundCheckDistance = 0.5f;

    private Rigidbody rb;
    private MovementState state = MovementState.GroundedIdle;
    private bool isGrounded;
    private Vector3 moveDirection;
    private bool takeoffQueued;
    private bool riseHeld;
    private bool diveHeld;
    private bool subscribed;
    private float groundCheckSuppressedUntil;
    private bool wasGrounded;
    private Quaternion facingRotation = Quaternion.identity;
    private float bankAngle;
    private CameraLook cameraLook;

    private MovementState State => state;
    private bool IsGrounded => isGrounded;

    private CameraLook ActiveCamera => cameraLook != null ? cameraLook : (cameraLook = PlayerID.cameraLook);

    private void Awake()
    {
        PlayerID.playerMovement = this;

        rb = GetComponent<Rigidbody>();
        facingRotation = transform.rotation;
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    // Start runs after every Awake, so InputManager.Instance exists by then. OnEnable also
    // tries, to cover this component being toggled back on later; the flag stops double-subscribing.
    private void Start() => SubscribeToInput();

    private void OnEnable() => SubscribeToInput();

    private void OnDisable()
    {
        UnsubscribeFromInput();
        takeoffQueued = false;
        riseHeld = false;
        diveHeld = false;
    }

    private void SubscribeToInput()
    {
        if (subscribed || InputManager.Instance == null) return;

        InputManager.Instance.RiseClicked += OnRisePressed;
        InputManager.Instance.RiseReleased += OnRiseReleased;
        
        InputManager.Instance.DiveClicked += OnDivePressed;
        InputManager.Instance.DiveReleased += OnDiveReleased;

        subscribed = true;
    }

    private void UnsubscribeFromInput()
    {
        if (!subscribed) return;

        if (InputManager.Instance != null)
        {
            InputManager.Instance.RiseClicked -= OnRisePressed;
            InputManager.Instance.RiseReleased -= OnRiseReleased;
            
            InputManager.Instance.DiveClicked -= OnDivePressed;
            InputManager.Instance.DiveReleased -= OnDiveReleased;
        }

        subscribed = false;
    }

    /// <summary>
    /// The press queues a takeoff -- acted on in FixedUpdate, since the event lands on a render
    /// frame -- and holding it keeps the climb going once airborne.
    /// </summary>
    private void OnRisePressed()
    {
        takeoffQueued = true;
        riseHeld = true;
    }

    private void OnRiseReleased() => riseHeld = false;

    private void OnDivePressed() => diveHeld = true;

    private void OnDiveReleased() => diveHeld = false;

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // The grace window keeps the ground check from re-grounding the bird on the frame it launches.
        isGrounded = Time.time >= groundCheckSuppressedUntil && CheckGrounded();

        if (takeoffQueued)
        {
            // Consumed either way: a press made mid-air must not fire on the next landing.
            takeoffQueued = false;
            if (isGrounded) BeginFlight(true);
        }

        moveDirection = ReadMoveDirection();

        // Leaving the ground any other way -- walking off a ledge -- starts flight on the same
        // terms as a Rise press, so there is never a state where the bird is falling uncontrolled.
        if (wasGrounded && !isGrounded && Time.time >= groundCheckSuppressedUntil) BeginFlight(false);
        wasGrounded = isGrounded;

        MovementState next = EvaluateState();
        if (next != state)
        {
            ExitState(state);
            state = next;
            EnterState(state);
        }

        switch (state)
        {
            case MovementState.GroundedIdle:
            case MovementState.GroundedMoving:
                TickGrounded(dt);
                break;
            case MovementState.Flying:
                TickFlying(dt);
                break;
        }

        if (rotateTowardVelocity) FaceVelocity(dt);
    }

    // ---------------------------------------------------------------- states

    private MovementState EvaluateState()
    {
        if (isGrounded)
        {
            return moveDirection.sqrMagnitude > 0.0001f
                ? MovementState.GroundedMoving
                : MovementState.GroundedIdle;
        }

        return MovementState.Flying;
    }

    /// <summary>
    /// The single entry point into flight. A Rise press launches with the rise cap -- there is
    /// no separate takeoff speed to keep in sync -- while walking off a ledge just starts falling.
    /// </summary>
    private void BeginFlight(bool launch)
    {
        if (launch)
        {
            Vector3 velocity = rb.linearVelocity;
            rb.linearVelocity = new Vector3(velocity.x, Mathf.Max(velocity.y, maxRiseSpeed), velocity.z);
        }

        isGrounded = false;
        wasGrounded = false;
        groundCheckSuppressedUntil = Time.time + takeoffGroundGrace;
    }

    private void EnterState(MovementState entered) { }

    private void ExitState(MovementState exited) { }

    private void TickGrounded(float dt)
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 horizontal = ApplyPlanarMotion(
            new Vector3(velocity.x, 0f, velocity.z),
            groundAcceleration, groundFriction, groundMaxSpeed, dt);

        // Small downward bias keeps the ground check honest on slopes and steps.
        float vertical = Mathf.Min(velocity.y, 0f) - 2f * dt;
        rb.linearVelocity = new Vector3(horizontal.x, vertical, horizontal.z);
    }

    private void TickFlying(float dt)
    {
        Vector3 velocity = rb.linearVelocity;

        // WASD is purely horizontal -- it never contributes height.
        Vector3 horizontal = ApplyPlanarMotion(
            new Vector3(velocity.x, 0f, velocity.z),
            airAcceleration, airFriction, airMaxSpeed, dt);

        float vertical = ApplyVerticalMotion(velocity.y, dt);

        rb.linearVelocity = new Vector3(horizontal.x, vertical, horizontal.z);
    }

    /// <summary>
    /// Height is entirely on the two buttons, and gravity only exists for a bird that has let
    /// go of everything. Under power the bird holds whatever altitude it is at: rise climbs,
    /// dive descends, and steering alone settles the vertical speed back to zero.
    /// </summary>
    private float ApplyVerticalMotion(float vertical, float dt)
    {
        // Rise wins a tie: holding both should climb rather than cancel to a confusing hover.
        // No gravity term at all -- the cap is what limits the climb, not a tug of war with it.
        if (riseHeld)
        {
            return Mathf.MoveTowards(vertical, maxRiseSpeed, riseAcceleration * dt);
        }

        if (diveHeld)
        {
            return Mathf.MoveTowards(vertical, -maxDiveSpeed, diveAcceleration * dt);
        }

        // Still flying, just not changing height: bleed the climb or descent off so releasing
        // rise stops the ascent instead of coasting on up to the apex.
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            return Mathf.MoveTowards(vertical, 0f, verticalStopAcceleration * dt);
        }

        // Hands off everything: the only place gravity exists.
        vertical -= gravity * dt;
        return Mathf.Max(vertical, -maxFallSpeed);
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>Accelerates toward the input direction, or bleeds speed off with friction when there is none.</summary>
    private Vector3 ApplyPlanarMotion(Vector3 horizontal, float acceleration, float friction, float maxSpeed, float dt)
    {
        if (moveDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 target = moveDirection * maxSpeed;
            horizontal = Vector3.MoveTowards(horizontal, target, acceleration * dt);
        }
        else
        {
            horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, friction * dt);
        }

        // Only clamp what the player is driving; momentum carried in from a dive is left alone.
        if (horizontal.magnitude > maxSpeed && moveDirection.sqrMagnitude > 0.0001f)
        {
            horizontal = Vector3.ClampMagnitude(horizontal, Mathf.Max(maxSpeed, horizontal.magnitude - friction * dt));
        }

        return horizontal;
    }

    /// <summary>
    /// Input rotated into camera space and flattened. W is always "away from the camera"
    /// whatever the yaw, and the camera's pitch is deliberately discarded -- height is the
    /// two buttons' job, so looking up or down never moves the bird vertically.
    /// </summary>
    private Vector3 ReadMoveDirection()
    {
        if (InputManager.Instance == null) return Vector3.zero;

        Vector2 input = InputManager.Instance.movementVector;
        if (input.sqrMagnitude < 0.0001f) return Vector3.zero;

        CameraLook cam = ActiveCamera;
        Vector3 forward = cam != null ? cam.PlanarForward : Vector3.forward;
        Vector3 right = cam != null ? cam.PlanarRight : Vector3.right;

        Vector3 direction = forward * input.y + right * input.x;
        return Vector3.ClampMagnitude(direction, 1f);
    }

    /// <summary>
    /// Turns the body toward its own velocity, eased at turnSpeed so single-frame wobble doesn't
    /// show up as jitter. Heading comes from horizontal velocity alone, so LookRotation can never
    /// degenerate; the climb is layered on afterwards as a local pitch, and the turn as a roll.
    /// </summary>
    private void FaceVelocity(float dt)
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 facing = new Vector3(velocity.x, 0f, velocity.z);

        if (facing.sqrMagnitude < minTurnSpeed * minTurnSpeed)
        {
            // Still unwind any roll left over from the last turn, even while drifting too
            // slowly to steer -- otherwise the body can be parked mid-bank.
            ApplyBank(0f, dt);
            return;
        }

        float airspeed = facing.magnitude;
        facing /= airspeed;

        // Nose angle from how steeply the bird is actually travelling. Negative X pitches up.
        float pitch = 0f;
        if (maxPitchAngle > 0f && !isGrounded)
        {
            pitch = Mathf.Clamp(-Mathf.Atan2(velocity.y, airspeed) * Mathf.Rad2Deg, -maxPitchAngle, maxPitchAngle);
        }

        Quaternion target = Quaternion.LookRotation(facing, Vector3.up) * Quaternion.Euler(pitch, 0f, 0f);

        // How far there is left to turn, and which way. The roll leans into that and unwinds
        // as it closes, so the body is upright again the moment it settles on the heading.
        Vector3 current = Vector3.ProjectOnPlane(facingRotation * Vector3.forward, Vector3.up);
        float turnRemaining = current.sqrMagnitude > 0.0001f
            ? Vector3.SignedAngle(current.normalized, facing, Vector3.up)
            : 0f;

        // Tracked unbanked so the roll never feeds back into the heading -- rotating toward a
        // target from an already-rolled rotation would spend the turn budget undoing the roll.
        facingRotation = Quaternion.RotateTowards(facingRotation, target, turnSpeed * dt);

        ApplyBank(isGrounded ? 0f : -turnRemaining * bankPerDegree, dt);
    }

    /// <summary>Eases the roll toward a target and writes the banked rotation to the Rigidbody.</summary>
    private void ApplyBank(float targetBank, float dt)
    {
        targetBank = Mathf.Clamp(targetBank, -maxBankAngle, maxBankAngle);
        bankAngle = Mathf.MoveTowards(bankAngle, targetBank, bankSpeed * dt);

        // Local Z is the body's forward axis, so rolling about it is the wing drop.
        rb.MoveRotation(facingRotation * Quaternion.AngleAxis(bankAngle, Vector3.forward));
    }

    private bool CheckGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * groundCheckOffset;
        return Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
            out _, groundCheckOffset + groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    private void OnValidate()
    {
        groundMaxSpeed = Mathf.Max(0f, groundMaxSpeed);
        airMaxSpeed = Mathf.Max(0f, airMaxSpeed);

        riseAcceleration = Mathf.Max(0f, riseAcceleration);
        maxRiseSpeed = Mathf.Max(0f, maxRiseSpeed);
        diveAcceleration = Mathf.Max(0f, diveAcceleration);
        maxDiveSpeed = Mathf.Max(0f, maxDiveSpeed);
        verticalStopAcceleration = Mathf.Max(0f, verticalStopAcceleration);

        gravity = Mathf.Max(0f, gravity);
        maxFallSpeed = Mathf.Max(0f, maxFallSpeed);

        takeoffGroundGrace = Mathf.Max(0f, takeoffGroundGrace);

        turnSpeed = Mathf.Max(0f, turnSpeed);
        minTurnSpeed = Mathf.Max(0f, minTurnSpeed);
        maxPitchAngle = Mathf.Clamp(maxPitchAngle, 0f, 89f);
        bankPerDegree = Mathf.Max(0f, bankPerDegree);
        maxBankAngle = Mathf.Clamp(maxBankAngle, 0f, 89f);
        bankSpeed = Mathf.Max(0f, bankSpeed);
    }

    public MovementState GetState()
    {
        return State;
    }
}
