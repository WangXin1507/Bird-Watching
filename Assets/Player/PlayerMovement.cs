using UnityEngine;
using BirdWatchingCamera;
using UnityEngine.Events;

/// <summary>
/// State-machine movement for the bird. Attach to the player root alongside a Rigidbody
/// and a Collider. WASD steers in the horizontal plane only; height is Rise (space) and
/// Dive (shift), each with its own acceleration and speed cap. Gravity applies only when
/// nothing at all is held -- a bird under power keeps its altitude.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    public UnityEvent OnTakeFlight;
    public UnityEvent OnLand;

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
    [Tooltip("Degrees per second the nose eases toward its target pitch. Also how fast it levels out after landing.")]
    [SerializeField] private float pitchSpeed = 360f;
    [Tooltip("Horizontal speed treated as the floor when working out pitch, so a slow straight-down sink doesn't tip the nose fully over.")]
    [SerializeField] private float minPitchAirspeed = 2f;
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

    [Header("Landing probe")]
    [Tooltip("How far below the bird to look for ground while airborne. Visuals read GroundDistance from this to start the landing blend before touchdown.")]
    [SerializeField] private float landingProbeDistance = 8f;

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
    private float headingYaw;
    private float pitchAngle;
    private float bankAngle;
    private CameraLook cameraLook;
    private bool controlSuspended;
    private bool kinematicBeforeSuspend;

    // ---------------------------------------------------------------- read-only state
    // Exposed for visuals, audio and camera. Nothing outside this class can write these.

    public MovementState State => state;
    public bool IsGrounded => isGrounded;
    public bool IsFlying => state == MovementState.Flying;
    public bool IsControlSuspended => controlSuspended;
    public bool RiseHeld => riseHeld;
    public bool DiveHeld => diveHeld;

    /// <summary>World-space velocity of the body.</summary>
    public Vector3 Velocity => rb != null ? rb.linearVelocity : Vector3.zero;
    public float HorizontalSpeed
    {
        get
        {
            Vector3 v = Velocity;
            return new Vector2(v.x, v.z).magnitude;
        }
    }
    public float VerticalSpeed => Velocity.y;

    /// <summary>Camera-relative steering input on the ground plane, magnitude 0..1.</summary>
    public Vector3 MoveDirection => moveDirection;

    /// <summary>Current roll in degrees.</summary>
    public float BankAngle => bankAngle;

    /// <summary>
    /// Height of the pivot above the ground. 0 while grounded, PositiveInfinity when nothing
    /// is within landingProbeDistance.
    /// </summary>
    public float GroundDistance { get; private set; }

    /// <summary>
    /// How much further the bird has to descend before the ground check fires -- i.e. before
    /// IsGrounded turns true. Uses the same sphere as the ground check, so it is measured to
    /// "landed" rather than to the pivot. 0 while grounded, PositiveInfinity when out of range.
    /// </summary>
    public float DistanceToLanding { get; private set; }

    /// <summary>
    /// Seconds until IsGrounded is predicted to turn true at the current descent rate.
    /// 0 while grounded, PositiveInfinity while not descending or out of range.
    /// </summary>
    public float TimeToLanding { get; private set; }

    // Speed caps, so readers can normalise without duplicating tuning values.
    public float GroundMaxSpeed => groundMaxSpeed;
    public float AirMaxSpeed => airMaxSpeed;
    public float MaxRiseSpeed => maxRiseSpeed;
    public float MaxDiveSpeed => maxDiveSpeed;

    private CameraLook ActiveCamera => cameraLook != null ? cameraLook : (cameraLook = PlayerID.cameraLook);

    private void Awake()
    {
        PlayerID.playerMovement = this;

        rb = GetComponent<Rigidbody>();
        headingYaw = transform.eulerAngles.y;
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    // Start runs after every Awake, so InputManager.Instance exists by then. OnEnable also
    // tries, to cover this component being toggled back on later; the flag stops double-subscribing.
    private void Start()
    {
        SubscribeToInput();
        rb.position = SaveManager.Instance.data.birdPosition;
        Debug.Log(SaveManager.Instance.data.birdPosition);
        Debug.Log(transform.position);
    }

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
        if (controlSuspended) return;

        float dt = Time.fixedDeltaTime;

        // The grace window keeps the ground check from re-grounding the bird on the frame it launches.
        isGrounded = Time.time >= groundCheckSuppressedUntil && CheckGrounded();
        UpdateLandingPrediction();

        if (isGrounded && !wasGrounded) OnLand?.Invoke();

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
        
        OnTakeFlight?.Invoke();
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
    /// Input rotated into the rendered camera's view. W is always the direction the camera
    /// is looking, flattened onto the ground plane. Pitch is discarded so looking up or down
    /// never moves the bird vertically -- height is Rise and Dive.
    /// </summary>
    private Vector3 ReadMoveDirection()
    {
        if (InputManager.Instance == null) return Vector3.zero;

        Vector2 input = InputManager.Instance.movementVector;
        if (input.sqrMagnitude < 0.0001f) return Vector3.zero;

        CameraLook cam = ActiveCamera;
        Vector3 forward;
        Vector3 right;
        if (cam != null)
        {
            forward = cam.PlanarForward;
            right = cam.PlanarRight;
        }
        else if (Camera.main != null)
        {
            forward = Camera.main.transform.forward;
            forward.y = 0f;
            forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            right = Camera.main.transform.right;
            right.y = 0f;
            right = right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;
        }
        else
        {
            return Vector3.zero;
        }

        Vector3 direction = forward * input.y + right * input.x;
        return Vector3.ClampMagnitude(direction, 1f);
    }

    /// <summary>
    /// Turns the body toward its own velocity, eased so single-frame wobble doesn't show up as
    /// jitter. Heading (yaw) and pitch are tracked separately: heading only steers when there is
    /// enough horizontal speed to read a direction from, but pitch is updated every frame. That
    /// split is what stops a steep dive leaving the bird stuck nose-down after it lands -- with
    /// almost no horizontal speed the old single rotation was never updated at all.
    /// </summary>
    private void FaceVelocity(float dt)
    {
        Vector3 velocity = rb.linearVelocity;
        Vector3 planar = new Vector3(velocity.x, 0f, velocity.z);
        float airspeed = planar.magnitude;

        // Heading. Below minTurnSpeed it is left alone so the bird doesn't chase noise while nearly stopped.
        float turnRemaining = 0f;
        if (airspeed >= minTurnSpeed)
        {
            float targetYaw = Quaternion.LookRotation(planar / airspeed, Vector3.up).eulerAngles.y;
            turnRemaining = Mathf.DeltaAngle(headingYaw, targetYaw);
            headingYaw = Mathf.MoveTowardsAngle(headingYaw, targetYaw, turnSpeed * dt);
        }

        // Pitch. Always level on the ground; in the air, from how steeply the bird is travelling.
        // minPitchAirspeed keeps a slow vertical sink from reading as a nosedive. Positive X = nose down.
        float targetPitch = 0f;
        if (maxPitchAngle > 0f && !isGrounded)
        {
            float pitchAirspeed = Mathf.Max(airspeed, minPitchAirspeed);
            targetPitch = Mathf.Clamp(-Mathf.Atan2(velocity.y, pitchAirspeed) * Mathf.Rad2Deg, -maxPitchAngle, maxPitchAngle);
        }
        pitchAngle = Mathf.MoveTowards(pitchAngle, targetPitch, pitchSpeed * dt);

        // Roll leans into the turn still to go and unwinds as it closes.
        ApplyBank(isGrounded ? 0f : -turnRemaining * bankPerDegree, dt);
    }

    /// <summary>Eases the roll toward a target and writes heading, pitch and roll to the Rigidbody.</summary>
    private void ApplyBank(float targetBank, float dt)
    {
        targetBank = Mathf.Clamp(targetBank, -maxBankAngle, maxBankAngle);
        bankAngle = Mathf.MoveTowards(bankAngle, targetBank, bankSpeed * dt);

        // Yaw, then pitch about the body's right axis, then roll about its forward axis.
        Quaternion facing = Quaternion.Euler(0f, headingYaw, 0f) * Quaternion.Euler(pitchAngle, 0f, 0f);
        rb.MoveRotation(facing * Quaternion.AngleAxis(bankAngle, Vector3.forward));
    }

    private bool CheckGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * groundCheckOffset;
        return Physics.SphereCast(origin, groundCheckRadius, Vector3.down,
            out _, groundCheckOffset + groundCheckDistance, groundMask, QueryTriggerInteraction.Ignore);
    }

    /// <summary>
    /// Sweeps the ground-check sphere further down than the check itself, so the prediction
    /// agrees exactly with when isGrounded will flip, whatever the height or speed.
    /// </summary>
    private void UpdateLandingPrediction()
    {
        if (isGrounded)
        {
            GroundDistance = 0f;
            DistanceToLanding = 0f;
            TimeToLanding = 0f;
            return;
        }

        GroundDistance = float.PositiveInfinity;
        DistanceToLanding = float.PositiveInfinity;
        TimeToLanding = float.PositiveInfinity;

        Vector3 origin = transform.position + Vector3.up * groundCheckOffset;
        float groundedReach = groundCheckOffset + groundCheckDistance;
        if (!Physics.SphereCast(origin, groundCheckRadius, Vector3.down, out RaycastHit hit,
                groundedReach + landingProbeDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        GroundDistance = Mathf.Max(0f, transform.position.y - hit.point.y);
        DistanceToLanding = Mathf.Max(0f, hit.distance - groundedReach);

        float descent = -rb.linearVelocity.y;
        if (descent > 0.0001f) TimeToLanding = DistanceToLanding / descent;
    }

    private void OnValidate()
    {
        landingProbeDistance = Mathf.Max(0f, landingProbeDistance);

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
        pitchSpeed = Mathf.Max(0f, pitchSpeed);
        minPitchAirspeed = Mathf.Max(0.01f, minPitchAirspeed);
        bankPerDegree = Mathf.Max(0f, bankPerDegree);
        maxBankAngle = Mathf.Clamp(maxBankAngle, 0f, 89f);
        bankSpeed = Mathf.Max(0f, bankSpeed);
    }

    public MovementState GetState()
    {
        return State;
    }

    /// <summary>
    /// Hands the body to a scripted move. Physics and input steering stay off until this is cleared,
    /// and the kinematic flag is put back the way it was.
    /// </summary>
    public void SetControlSuspended(bool suspended)
    {
        if (suspended == controlSuspended) return;

        controlSuspended = suspended;
        takeoffQueued = false;
        riseHeld = false;
        diveHeld = false;

        if (suspended)
        {
            kinematicBeforeSuspend = rb.isKinematic;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
        else
        {
            rb.isKinematic = kinematicBeforeSuspend;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Puts the bird at a pose and treats that rotation as its facing, so steering does not
    /// snap it back to the heading it had before the scripted move.
    /// </summary>
    public void Place(Vector3 position, Quaternion rotation)
    {
        headingYaw = rotation.eulerAngles.y;
        pitchAngle = 0f;
        bankAngle = 0f;
        rb.position = position;
        rb.rotation = rotation;
    }
}
