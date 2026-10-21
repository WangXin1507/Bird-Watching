using UnityEngine;

/// <summary>
/// Drives the bird's Animator from PlayerMovement. Lives on the Visuals object (the parent of the
/// models) and never touches physics.
///
/// One float, Flight (0 = grounded, 1 = airborne), is eased over time and fed to a nested blend
/// tree: takeoff and landing are just that value crossing over. Speed and Climb pick the pose
/// inside each half of the tree.
///
/// While the bird is two static blockout meshes, the clips only toggle which mesh is active
/// (the swap lands around Flight = 0.5) and a squash-and-stretch pop hides the cut. When real
/// clips exist, drop them into the same blend trees and turn the pop off -- this script stays.
///
/// Build the Animator with: right-click this component's header > Build Bird Animator.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerVisuals : MonoBehaviour
{
    // Animator parameter names, shared with the editor builder so they can't drift apart.
    public const string FlightParam = "Flight";
    public const string SpeedParam = "Speed";
    public const string ClimbParam = "Climb";

    private static readonly int FlightId = Animator.StringToHash(FlightParam);
    private static readonly int SpeedId = Animator.StringToHash(SpeedParam);
    private static readonly int ClimbId = Animator.StringToHash(ClimbParam);

    [Header("Blockout models")]
    [Tooltip("Read by the Build Bird Animator tool to know which objects the swap clips toggle.")]
    [SerializeField] private GameObject flyingModel;
    [SerializeField] private GameObject groundModel;

    [Header("Ground / air blend")]
    [Tooltip("Seconds to blend from the ground pose to the flying pose after leaving the ground.")]
    [SerializeField] private float takeOffBlendTime = 0.2f;
    [Tooltip("Seconds to blend from the flying pose back to the ground pose.")]
    [SerializeField] private float landBlendTime = 0.15f;
    [Tooltip("Shapes the blend. Both axes run 0..1.")]
    [SerializeField] private AnimationCurve blendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Landing anticipation")]
    [Tooltip("Start the landing blend before touchdown, so the bird is already in its ground pose when it lands.")]
    [SerializeField] private bool anticipateLanding = true;
    [Tooltip("Only anticipate while descending at least this fast (m/s), so drifting near the ground doesn't flicker.")]
    [SerializeField] private float minDescentSpeed = 0.5f;

    [Header("Parameter smoothing")]
    [Tooltip("Damp time for Speed and Climb, so velocity noise doesn't show up as twitching.")]
    [SerializeField] private float parameterDampTime = 0.1f;

    [Header("Swap pop (blockout only)")]
    [Tooltip("Squash and stretch this object around the mesh swap. Turn off once real animations exist.")]
    [SerializeField] private bool popOnSwap = true;
    [Tooltip("Stretch at the peak: 0.25 = 25% taller and correspondingly thinner.")]
    [SerializeField] private float popStrength = 0.25f;
    [Tooltip("Pop amount (Y) across the blend (X). Peak it at 0.5, where the mesh swaps.")]
    [SerializeField] private AnimationCurve popCurve = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));

    private Animator animator;
    private PlayerMovement movement;
    private Vector3 baseScale;
    private float progress;          // linear 0..1; eased through blendCurve before it reaches the Animator
    private bool landingCommitted;   // latched so the anticipation can't flicker on and off near the ground
    private bool warnedNoController;

    public GameObject FlyingModel => flyingModel;
    public GameObject GroundModel => groundModel;

    /// <summary>The eased ground/air blend currently sent to the Animator.</summary>
    public float Flight => blendCurve.Evaluate(progress);

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator != null) animator.applyRootMotion = false;
        baseScale = transform.localScale;
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

        bool airborne = WantsFlyingPose();
        float duration = airborne ? takeOffBlendTime : landBlendTime;
        float target = airborne ? 1f : 0f;
        progress = duration > 0f
            ? Mathf.MoveTowards(progress, target, Time.deltaTime / duration)
            : target;

        animator.SetFloat(FlightId, Flight);

        float speed01 = movement.GroundMaxSpeed > 0f ? movement.HorizontalSpeed / movement.GroundMaxSpeed : 0f;
        animator.SetFloat(SpeedId, Mathf.Clamp01(speed01), parameterDampTime, Time.deltaTime);
        animator.SetFloat(ClimbId, Climb(movement.VerticalSpeed), parameterDampTime, Time.deltaTime);
    }

    private void LateUpdate()
    {
        if (!popOnSwap || popStrength <= 0f)
        {
            transform.localScale = baseScale;
            return;
        }

        // Taller and thinner at the swap, roughly volume-preserving, back to rest at either end.
        float stretch = popCurve.Evaluate(Flight) * popStrength;
        float thin = 1f / Mathf.Sqrt(1f + stretch);
        transform.localScale = Vector3.Scale(baseScale, new Vector3(thin, 1f + stretch, thin));
    }

    /// <summary>
    /// Flying pose while airborne, except when descending fast enough that touchdown will arrive
    /// within landBlendTime -- then the blend starts early. Once committed, only climbing, holding
    /// Rise, levelling off, or actually landing releases it.
    /// </summary>
    private bool WantsFlyingPose()
    {
        if (!movement.IsFlying)
        {
            landingCommitted = false;
            return false;
        }

        if (!anticipateLanding) return true;

        float descent = -movement.VerticalSpeed;

        if (landingCommitted)
        {
            // Half the threshold on the way out gives hysteresis against hovering at the edge.
            if (movement.RiseHeld || descent < minDescentSpeed * 0.5f) landingCommitted = false;
        }
        else if (!movement.RiseHeld && descent >= minDescentSpeed)
        {
            float timeToGround = movement.GroundDistance / descent; // Infinity when no ground in range
            landingCommitted = timeToGround <= landBlendTime;
        }

        return !landingCommitted;
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
                Debug.LogWarning("PlayerVisuals: no Animator Controller assigned. Right-click the PlayerVisuals " +
                                 "component > Build Bird Animator.", this);
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
        minDescentSpeed = Mathf.Max(0f, minDescentSpeed);
        parameterDampTime = Mathf.Max(0f, parameterDampTime);
        popStrength = Mathf.Max(0f, popStrength);
    }
}
