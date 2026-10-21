using UnityEngine;

/// <summary>
/// Drives the bird's Animator from PlayerMovement. Lives on the rig root next to the Animator
/// and never touches physics.
///
/// One float, Flight (0 = grounded, 1 = airborne), feeds the root of a nested blend tree;
/// Speed and Climb pick the pose inside each half.
///
/// Takeoff eases Flight up over takeOffBlendTime. Landing is not a timed blend at all: while
/// descending, Flight is read straight off PlayerMovement.TimeToLanding (seconds until the ground
/// check fires) divided by landBlendTime. It therefore reaches 0 on the frame the bird lands at
/// any height or speed, and corrects itself if the player changes speed mid-descent.
/// </summary>
public class PlayerVisuals : MonoBehaviour
{
    // Animator parameter names.
    public const string FlightParam = "Flight";
    public const string SpeedParam = "Speed";
    public const string ClimbParam = "Climb";

    private static readonly int FlightId = Animator.StringToHash(FlightParam);
    private static readonly int SpeedId = Animator.StringToHash(SpeedParam);
    private static readonly int ClimbId = Animator.StringToHash(ClimbParam);

    [Tooltip("Optional. Found on this object or its children when left empty.")]
    [SerializeField] private Animator animator;

    [Header("Ground / air blend")]
    [Tooltip("Seconds to blend from the ground pose to the flying pose after leaving the ground.")]
    [SerializeField] private float takeOffBlendTime = 0.2f;
    [Tooltip("How long before touchdown the landing blend starts. The blend finishes exactly as the bird lands.")]
    [SerializeField] private float landBlendTime = 0.3f;
    [Tooltip("Fastest the blend may fall toward the ground pose, in seconds for a full 1 to 0. Covers landings that " +
             "weren't predicted (too slow to anticipate, or ground appearing suddenly, like flying onto a ledge).")]
    [SerializeField] private float landCatchUpTime = 0.1f;
    [Tooltip("Shapes the blend. Both axes run 0..1.")]
    [SerializeField] private AnimationCurve blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Landing anticipation")]
    [Tooltip("Start the landing blend before touchdown. Off = blend only after landing, at landCatchUpTime.")]
    [SerializeField] private bool anticipateLanding = true;
    [Tooltip("Only anticipate while descending at least this fast (m/s), so drifting near the ground doesn't dip the pose.")]
    [SerializeField] private float minDescentSpeed = 0.3f;

    [Header("Parameter smoothing")]
    [Tooltip("Damp time for Speed and Climb, so velocity noise doesn't show up as twitching.")]
    [SerializeField] private float parameterDampTime = 0.1f;

    private PlayerMovement movement;
    private float progress; // linear 0..1; eased through blendCurve before it reaches the Animator
    private bool warnedNoController;

    /// <summary>The eased ground/air blend currently sent to the Animator.</summary>
    public float Flight => blendCurve.Evaluate(progress);

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;
        movement = GetComponentInParent<PlayerMovement>();
    }

    private void Start()
    {
        if (movement == null) movement = PlayerID.playerMovement;
        progress = movement != null && movement.IsFlying ? 1f : 0f;
    }

    private void Update()
    {
        if (!CanDrive()) return;

        float target = TargetProgress();

        // Up toward flight is paced by takeOffBlendTime. Down toward the ground normally follows the
        // landing prediction (which already moves at 1/landBlendTime); landCatchUpTime only caps it.
        float seconds = target > progress ? takeOffBlendTime : landCatchUpTime;
        progress = seconds > 0f
            ? Mathf.MoveTowards(progress, target, Time.deltaTime / seconds)
            : target;

        animator.SetFloat(FlightId, Flight);

        float speed01 = movement.GroundMaxSpeed > 0f ? movement.HorizontalSpeed / movement.GroundMaxSpeed : 0f;
        animator.SetFloat(SpeedId, Mathf.Clamp01(speed01), parameterDampTime, Time.deltaTime);
        animator.SetFloat(ClimbId, Climb(movement.VerticalSpeed), parameterDampTime, Time.deltaTime);
    }

    /// <summary>
    /// 0 on the ground, 1 in the air -- except while descending toward ground in range, where it is
    /// the predicted time to touchdown as a fraction of landBlendTime. Holding Rise or levelling
    /// off sends it back to 1, so an aborted landing recovers on its own.
    /// </summary>
    private float TargetProgress()
    {
        if (!movement.IsFlying) return 0f;
        if (!anticipateLanding || landBlendTime <= 0f || movement.RiseHeld) return 1f;
        if (-movement.VerticalSpeed < minDescentSpeed) return 1f;

        return Mathf.Clamp01(movement.TimeToLanding / landBlendTime); // Infinity (no ground in range) -> 1
    }

    /// <summary>Vertical speed mapped to -1 (full dive) .. 0 (level) .. 1 (full climb).</summary>
    private float Climb(float vertical)
    {
        if (vertical >= 0f)
            return movement.MaxRiseSpeed > 0f ? Mathf.Clamp01(vertical / movement.MaxRiseSpeed) : 0f;

        return movement.MaxDiveSpeed > 0f ? -Mathf.Clamp01(-vertical / movement.MaxDiveSpeed) : 0f;
    }

    private bool CanDrive()
    {
        if (movement == null) movement = PlayerID.playerMovement;
        if (movement == null || animator == null) return false;

        if (animator.runtimeAnimatorController == null)
        {
            if (!warnedNoController)
            {
                Debug.LogWarning("PlayerVisuals: the Animator has no controller assigned.", this);
                warnedNoController = true;
            }
            return false;
        }

        return true;
    }

    private void OnValidate()
    {
        takeOffBlendTime = Mathf.Max(0f, takeOffBlendTime);
        landBlendTime = Mathf.Max(0f, landBlendTime);
        landCatchUpTime = Mathf.Max(0f, landCatchUpTime);
        minDescentSpeed = Mathf.Max(0f, minDescentSpeed);
        parameterDampTime = Mathf.Max(0f, parameterDampTime);
    }
}
