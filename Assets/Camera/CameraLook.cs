using UnityEngine;

namespace BirdWatchingCamera
{
    /// <summary>
    /// Look-orbit controller. Attach to the Camera Target so Cinemachine Third Person Follow
    /// sits behind this transform. Reads the look vector from <see cref="InputManager"/>,
    /// clamping pitch (and optionally yaw) to the configured bounds.
    /// </summary>
    public class CameraLook : MonoBehaviour
    {
        [Header("Sensitivity")]
        [SerializeField] private float horizontalSensitivity = 1.5f;
        [SerializeField] private float verticalSensitivity = 1.5f;
        [SerializeField] private bool invertY = false;

        [Tooltip("Leave off for mouse delta (already frame-relative). Turn on for gamepad sticks, which report a constant value while held.")]
        [SerializeField] private bool scaleByDeltaTime = false;

        [Header("Pitch bounds (degrees)")]
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Yaw bounds (degrees)")]
        [SerializeField] private bool clampYaw = false;
        [SerializeField] private float minYaw = -90f;
        [SerializeField] private float maxYaw = 90f;

        private float pitch;
        private float yaw;

        private Transform ViewTransform =>
            MainCamera.Instance != null ? MainCamera.Instance.transform : transform;

        /// <summary>Current camera pitch in degrees. Negative looks up at the target, positive looks down at it.</summary>
        public float Pitch => pitch;

        /// <summary>Current camera yaw in degrees, in the -180..180 range.</summary>
        public float Yaw => yaw;

        /// <summary>Full camera forward, pitch included. Use for movement that follows the look angle in 3D.</summary>
        public Vector3 Forward => ViewTransform.forward;

        /// <summary>Full camera right, pitch included.</summary>
        public Vector3 Right => ViewTransform.right;

        /// <summary>Camera up -- the normal of the plane Forward and Right span.</summary>
        public Vector3 Up => ViewTransform.up;

        /// <summary>Camera forward flattened onto the horizontal plane. Use this to make movement camera-relative.</summary>
        public Vector3 PlanarForward
        {
            get
            {
                Vector3 forward = ViewTransform.forward;
                forward.y = 0f;
                return forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
            }
        }

        /// <summary>Camera right flattened onto the horizontal plane.</summary>
        public Vector3 PlanarRight
        {
            get
            {
                Vector3 right = ViewTransform.right;
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
            Vector3 euler = transform.eulerAngles;
            pitch = Mathf.Clamp(NormalizeAngle(euler.x), minPitch, maxPitch);
            yaw = NormalizeAngle(euler.y);
            if (clampYaw) yaw = Mathf.Clamp(yaw, minYaw, maxYaw);

            ApplyRotation();
        }

        private void LateUpdate()
        {
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

            ApplyRotation();
        }

        private void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
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
}
