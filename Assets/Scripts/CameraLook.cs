using UnityEngine;

/// <summary>
/// Third-person orbit camera. Attach to the player camera and assign the target to follow.
/// Reads the look vector from <see cref="InputManager"/> to orbit around the target,
/// clamping pitch (and optionally yaw) to the configured bounds.
/// </summary>
public class CameraLook : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [Tooltip("Offset from the target's pivot that the camera looks at, e.g. head/chest height.")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Distance")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 15f;

    [Header("Sensitivity")]
    [SerializeField] private float horizontalSensitivity = 0.1f;
    [SerializeField] private float verticalSensitivity = 0.1f;
    [SerializeField] private bool invertY = false;

    [Tooltip("Leave off for mouse delta (already frame-relative). Turn on for gamepad sticks, which report a constant value while held.")]
    [SerializeField] private bool scaleByDeltaTime = false;

    [Header("Pitch bounds (degrees)")]
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Yaw bounds (degrees)")]
    [SerializeField] private bool clampYaw = false;
    [SerializeField] private float minYaw = -90f;
    [SerializeField] private float maxYaw = 90f;

    [Header("Smoothing")]
    [Tooltip("Seconds to catch up to the target position. 0 = rigid follow.")]
    [SerializeField] private float positionSmoothTime = 0.05f;

    [Header("Collision")]
    [SerializeField] private bool avoidGeometry = true;
    [SerializeField] private LayerMask collisionMask = ~0;
    [Tooltip("How far the camera stays off any surface it would otherwise clip into.")]
    [SerializeField] private float collisionRadius = 0.2f;

    private float pitch;
    private float yaw;
    private Vector3 positionVelocity;

    /// <summary>Current camera pitch in degrees. Negative looks up at the target, positive looks down at it.</summary>
    public float Pitch => pitch;

    /// <summary>Current camera yaw in degrees, in the -180..180 range.</summary>
    public float Yaw => yaw;

    /// <summary>Full camera forward, pitch included. Use for movement that follows the look angle in 3D.</summary>
    public Vector3 Forward => transform.forward;

    /// <summary>Full camera right, pitch included.</summary>
    public Vector3 Right => transform.right;

    /// <summary>Camera up -- the normal of the plane Forward and Right span.</summary>
    public Vector3 Up => transform.up;

    /// <summary>Camera forward flattened onto the horizontal plane. Use this to make movement camera-relative.</summary>
    public Vector3 PlanarForward
    {
        get
        {
            Vector3 forward = transform.forward;
            forward.y = 0f;
            return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        }
    }

    /// <summary>Camera right flattened onto the horizontal plane.</summary>
    public Vector3 PlanarRight
    {
        get
        {
            Vector3 right = transform.right;
            right.y = 0f;
            return right.sqrMagnitude > 0.0001f ? right.normalized : Vector3.right;
        }
    }

    private void Awake()
    {
        PlayerID.cameraLook = this;
    }
    
    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning($"{nameof(CameraLook)} on '{name}' has no target assigned.", this);
        }

        Vector3 euler = transform.eulerAngles;
        pitch = Mathf.Clamp(NormalizeAngle(euler.x), minPitch, maxPitch);
        yaw = NormalizeAngle(euler.y);
        if (clampYaw) yaw = Mathf.Clamp(yaw, minYaw, maxYaw);

        ApplyTransform(true);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        if (InputManager.Instance != null)
        {
            Vector2 look = InputManager.Instance.lookVector;
            if (look.sqrMagnitude > Mathf.Epsilon)
            {
                float scale = scaleByDeltaTime ? Time.deltaTime : 1f;

                yaw += look.x * horizontalSensitivity * scale;
                pitch += (invertY ? look.y : -look.y) * verticalSensitivity * scale;

                pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                if (clampYaw) yaw = Mathf.Clamp(yaw, minYaw, maxYaw);
                else yaw = Mathf.Repeat(yaw + 180f, 360f) - 180f;
            }
        }

        ApplyTransform(false);
    }

    private void ApplyTransform(bool snap)
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target != null ? target.position + targetOffset : transform.position;

        float desiredDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        Vector3 direction = rotation * Vector3.back;

        if (avoidGeometry && Physics.SphereCast(pivot, collisionRadius, direction,
                out RaycastHit hit, desiredDistance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            desiredDistance = Mathf.Max(minDistance, hit.distance);
        }

        Vector3 desiredPosition = pivot + direction * desiredDistance;

        transform.position = (snap || positionSmoothTime <= 0f)
            ? desiredPosition
            : Vector3.SmoothDamp(transform.position, desiredPosition, ref positionVelocity, positionSmoothTime);

        transform.rotation = rotation;
    }

    /// <summary>Converts a 0..360 Euler angle into the -180..180 range.</summary>
    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f) angle -= 360f;
        return angle;
    }

    private void OnValidate()
    {
        minPitch = Mathf.Clamp(minPitch, -89f, 89f);
        maxPitch = Mathf.Clamp(maxPitch, minPitch, 89f);
        if (maxYaw < minYaw) maxYaw = minYaw;

        minDistance = Mathf.Max(0f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
        collisionRadius = Mathf.Max(0.01f, collisionRadius);
    }

    public float GetPitch()
    {
        return pitch;
    }
    
    public float GetYaw()
    {
        return yaw;
    }
}
