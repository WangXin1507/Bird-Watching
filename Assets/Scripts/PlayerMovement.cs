using UnityEngine;

/// <summary>
/// State-machine movement for the bird. Attach to the player root alongside a Rigidbody
/// and a Collider. Reads movement input from <see cref="InputManager"/>; climbing and
/// diving come from steering with the camera rather than from separate states.
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

    [Header("Air movement (shared)")]
    [SerializeField] private float airAcceleration = 20f;
    [SerializeField] private float airFriction = 4f;
    [Tooltip("Horizontal speed cap while airborne.")]
    [SerializeField] private float airMaxSpeed = 9f;
    [Tooltip("Multiplier on air acceleration for the vertical part of that input -- how sharply it climbs or noses over.")]
    [SerializeField] private float verticalFollowStrength = 1f;
    [SerializeField] private float gravity = 24f;
    [SerializeField] private float maxFallSpeed = 20f;

    [Header("Takeoff")]
    [Tooltip("Upward speed given by a single Rise press while grounded. This is the only way into the air.")]
    [SerializeField] private float takeoffSpeed = 8f;
    [Tooltip("Seconds the ground check is ignored after a takeoff, so the launch isn't swallowed on the same frame.")]
    [SerializeField] private float takeoffGroundGrace = 0.15f;
    [Tooltip("Upward speed when flight starts by walking off a ledge instead of by pressing Rise. 0 = just fall into it.")]
    [SerializeField] private float cliffLaunchSpeed = 0f;

    [Header("Facing")]
    [Tooltip("Turn the body to face the direction it is actually travelling, not the direction being pressed.")]
    [SerializeField] private bool rotateTowardVelocity = true;
    [Tooltip("Degrees per second the body turns toward its velocity. Smoothing this out is what kills the frame-to-frame jitter.")]
    [SerializeField] private float turnSpeed = 720f;
    [Tooltip("Below this speed the facing is left alone, so the bird doesn't chase noise while nearly stopped.")]
    [SerializeField] private float minTurnSpeed = 0.4f;
    [Tooltip("Roll the body into its turns, the way a bird drops a wing to bank.")]
    [SerializeField] private bool bankIntoTurns = true;
    [Tooltip("Degrees of roll per degree of turn still to go.")]
    [SerializeField] private float bankPerDegree = 1f;
    [Tooltip("Hard cap on how far the body rolls.")]
    [SerializeField] private float maxBankAngle = 35f;
    [Tooltip("Degrees per second the roll eases in and back out. Lower = lazier wings.")]
    [SerializeField] private float bankSpeed = 180f;
    [SerializeField] private bool bankWhileGrounded = false;
    [Tooltip("Airborne WASD follows the camera's pitch too, so looking up and holding W climbs. Off = movement stays flat.")]
    [SerializeField] private bool followCameraPitchInAir = true;

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
    private Vector3 planarMoveDirection;
    private bool takeoffQueued;
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
    }

    private void SubscribeToInput()
    {
        if (subscribed || InputManager.Instance == null) return;

        InputManager.Instance.RiseClicked += OnRisePressed;
        subscribed = true;
    }

    private void UnsubscribeFromInput()
    {
        if (!subscribed) return;

        if (InputManager.Instance != null) InputManager.Instance.RiseClicked -= OnRisePressed;
        subscribed = false;
    }

    /// <summary>Queued rather than acted on immediately -- the press lands on a render frame, the launch belongs in FixedUpdate.</summary>
    private void OnRisePressed() => takeoffQueued = true;

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // The grace window keeps the ground check from re-grounding the bird on the frame it launches.
        isGrounded = Time.time >= groundCheckSuppressedUntil && CheckGrounded();

        if (takeoffQueued)
        {
            // Consumed either way: a press made mid-air must not fire on the next landing.
            takeoffQueued = false;
            if (isGrounded) BeginFlight(takeoffSpeed);
        }

        moveDirection = ReadMoveDirection();
        planarMoveDirection = new Vector3(moveDirection.x, 0f, moveDirection.z);

        // Leaving the ground any other way -- walking off a ledge -- starts flight on the same
        // terms as a Rise press, so there is never a state where the bird is falling uncontrolled.
        if (wasGrounded && !isGrounded && Time.time >= groundCheckSuppressedUntil) BeginFlight(cliffLaunchSpeed);
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
    /// The single entry point into flight. A Rise press and walking off a ledge both come
    /// through here, so the two behave identically apart from the launch speed.
    /// </summary>
    private void BeginFlight(float launchSpeed)
    {
        if (launchSpeed > 0f)
        {
            Vector3 velocity = rb.linearVelocity;
            rb.linearVelocity = new Vector3(velocity.x, Mathf.Max(velocity.y, launchSpeed), velocity.z);
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
        Vector3 horizontal = ApplyPlanarMotion(
            new Vector3(velocity.x, 0f, velocity.z),
            airAcceleration, airFriction, airMaxSpeed, dt);

        // Powered flight: while there is input the bird drives its own vertical speed toward
        // whatever the camera is pointing at, and gravity is not applied at all. Gravity is
        // purely the hands-off baseline, so releasing everything is what makes it fall.
        float vertical = velocity.y;

        // Easing toward a target rather than adding acceleration means levelling the camera
        // out of a dive actually arrests the descent -- with no gravity there is nothing
        // else to bleed off that downward momentum.
        float targetVertical = moveDirection.y * airMaxSpeed;
        vertical = Mathf.MoveTowards(vertical, targetVertical, airAcceleration * verticalFollowStrength * dt);
        vertical -= gravity * dt * (5 - Mathf.Min(5, horizontal.magnitude));
        

        vertical = Mathf.Max(vertical, -maxFallSpeed);

        Vector3 result = new Vector3(horizontal.x, vertical, horizontal.z);

        // The per-axis caps above leave diagonal flight running at ~1.4x, so the combined
        // speed is clamped too -- Air Max Speed is top speed in any direction. Only clamped
        // while under power: a hands-off fall belongs to gravity and Max Fall Speed.
        if (moveDirection.sqrMagnitude > 0.0001f && result.magnitude > airMaxSpeed)
        {
            result = result.normalized * airMaxSpeed;
        }

        rb.linearVelocity = result;
    }

    // ---------------------------------------------------------------- helpers

    /// <summary>Accelerates toward the input direction, or bleeds speed off with friction when there is none.</summary>
    private Vector3 ApplyPlanarMotion(Vector3 horizontal, float acceleration, float friction, float maxSpeed, float dt)
    {
        // Only the flattened part of the input drives horizontal speed -- the vertical part is
        // applied separately, so looking steeply up trades ground speed for climb.
        if (planarMoveDirection.sqrMagnitude > 0.0001f)
        {
            Vector3 target = planarMoveDirection * maxSpeed;
            horizontal = Vector3.MoveTowards(horizontal, target, acceleration * dt);
        }
        else
        {
            horizontal = Vector3.MoveTowards(horizontal, Vector3.zero, friction * dt);
        }

        // Only clamp what the player is driving; momentum carried in from a dive is left alone.
        if (horizontal.magnitude > maxSpeed && planarMoveDirection.sqrMagnitude > 0.0001f)
        {
            horizontal = Vector3.ClampMagnitude(horizontal, Mathf.Max(maxSpeed, horizontal.magnitude - friction * dt));
        }

        return horizontal;
    }

    /// <summary>Input vector rotated into camera space.</summary>
    private Vector3 ReadMoveDirection()
    {
        if (InputManager.Instance == null) return Vector3.zero;

        Vector2 input = InputManager.Instance.movementVector;
        if (input.sqrMagnitude < 0.0001f) return Vector3.zero;

        // W/S run along the camera's forward axis, A/D along its right axis, so W is always
        // "away from the camera" whatever the yaw. Airborne, those axes keep the camera's
        // pitch, so looking up and holding W climbs; grounded, they stay flattened.
        CameraLook cam = ActiveCamera;

        Vector3 forward, right;
        if (cam == null)
        {
            forward = Vector3.forward;
            right = Vector3.right;
        }
        else if (followCameraPitchInAir && !isGrounded)
        {
            forward = cam.Forward;
            right = cam.Right;
        }
        else
        {
            forward = cam.PlanarForward;
            right = cam.PlanarRight;
        }

        Vector3 direction = forward * input.y + right * input.x;
        return Vector3.ClampMagnitude(direction, 1f);
    }

    /// <summary>
    /// Turns the body toward its own velocity, eased at turnSpeed so single-frame wobble in the
    /// velocity doesn't show up as jitter. The target is projected onto the camera plane -- the
    /// same plane WASD moves along -- and that plane's normal is used as the up vector, so the
    /// body can only rotate within it. Roll never enters the quaternion, which is what used to
    /// make near-vertical velocity snap the model around.
    /// </summary>
    private void FaceVelocity(float dt)
    {
        CameraLook cam = ActiveCamera;

        // Grounded movement is flat, so its plane is the ground; airborne it is the camera's own.
        Vector3 planeNormal = (cam != null && followCameraPitchInAir && !isGrounded)
            ? cam.Up
            : Vector3.up;

        Vector3 facing = Vector3.ProjectOnPlane(rb.linearVelocity, planeNormal);
        if (facing.sqrMagnitude < minTurnSpeed * minTurnSpeed)
        {
            // Still unwind any roll left over from the last turn, even while drifting too
            // slowly to steer -- otherwise the body can be parked mid-bank.
            ApplyBank(0f, dt);
            return;
        }

        facing.Normalize();
        Quaternion target = Quaternion.LookRotation(facing, planeNormal);

        // How far there is left to turn, and which way. The roll leans into that and unwinds
        // as it closes, so the body is upright again the moment it settles on the heading.
        Vector3 current = Vector3.ProjectOnPlane(facingRotation * Vector3.forward, planeNormal);
        float turnRemaining = current.sqrMagnitude > 0.0001f
            ? Vector3.SignedAngle(current.normalized, facing, planeNormal)
            : 0f;

        // Tracked unbanked so the roll never feeds back into the heading -- rotating toward a
        // target from an already-rolled rotation would spend the turn budget undoing the roll.
        facingRotation = Quaternion.RotateTowards(facingRotation, target, turnSpeed * dt);

        bool allowBank = bankIntoTurns && (bankWhileGrounded || !isGrounded);
        ApplyBank(allowBank ? -turnRemaining * bankPerDegree : 0f, dt);
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
        gravity = Mathf.Max(0f, gravity);
        maxFallSpeed = Mathf.Max(0f, maxFallSpeed);
        verticalFollowStrength = Mathf.Max(0f, verticalFollowStrength);
        takeoffSpeed = Mathf.Max(0f, takeoffSpeed);
        takeoffGroundGrace = Mathf.Max(0f, takeoffGroundGrace);
        cliffLaunchSpeed = Mathf.Max(0f, cliffLaunchSpeed);
        turnSpeed = Mathf.Max(0f, turnSpeed);
        minTurnSpeed = Mathf.Max(0f, minTurnSpeed);
        bankPerDegree = Mathf.Max(0f, bankPerDegree);
        maxBankAngle = Mathf.Clamp(maxBankAngle, 0f, 89f);
        bankSpeed = Mathf.Max(0f, bankSpeed);
    }

    public MovementState GetState()
    {
        return State;
    }
}
