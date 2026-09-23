using Unity.Cinemachine;
using UnityEngine;

namespace BirdWatchingCamera
{
    /// <summary>
    /// View-direction facade for the Cinemachine camera. Look orbit is driven by
    /// CinemachineInputAxisController on the virtual camera; this component exposes
    /// planar axes for movement.
    /// </summary>
    public class CameraLook : MonoBehaviour
    {
        private CinemachineOrbitalFollow orbital;

        private Transform ViewTransform
        {
            get
            {
                if (Camera.main != null) return Camera.main.transform;

                if (MainCamera.Instance != null) return MainCamera.Instance.transform;

                CinemachineCamera vcam = GetComponentInChildren<CinemachineCamera>(true);
                return vcam != null ? vcam.transform : transform;
            }
        }

        private CinemachineOrbitalFollow Orbital
        {
            get
            {
                if (orbital != null) return orbital;

                CinemachineCamera vcam = MainCamera.Instance != null
                    ? MainCamera.Instance
                    : GetComponentInChildren<CinemachineCamera>(true);
                if (vcam != null)
                {
                    orbital = vcam.GetComponent<CinemachineOrbitalFollow>();
                }

                return orbital;
            }
        }

        /// <summary>Orbital follow vertical axis, in degrees.</summary>
        public float Pitch => Orbital != null ? Orbital.VerticalAxis.Value : 0f;

        /// <summary>Orbital follow horizontal axis, in degrees.</summary>
        public float Yaw => Orbital != null ? Orbital.HorizontalAxis.Value : 0f;

        /// <summary>Full camera forward, pitch included.</summary>
        public Vector3 Forward => ViewTransform.forward;

        /// <summary>Full camera right, pitch included.</summary>
        public Vector3 Right => ViewTransform.right;

        /// <summary>Camera up.</summary>
        public Vector3 Up => ViewTransform.up;

        /// <summary>Camera forward flattened onto the horizontal plane. Use this to make movement camera-relative.</summary>
        public Vector3 PlanarForward => Flatten(ViewTransform.forward, ViewTransform.up);

        /// <summary>Camera right flattened onto the horizontal plane.</summary>
        public Vector3 PlanarRight
        {
            get
            {
                Vector3 right = Flatten(ViewTransform.right, Vector3.up);
                if (right.sqrMagnitude > 0.0001f) return right;
                return Vector3.Cross(Vector3.up, PlanarForward).normalized;
            }
        }

        private static Vector3 Flatten(Vector3 vector, Vector3 fallback)
        {
            vector.y = 0f;
            if (vector.sqrMagnitude > 0.0001f) return vector.normalized;

            fallback.y = 0f;
            return fallback.sqrMagnitude > 0.0001f ? fallback.normalized : Vector3.forward;
        }

        private void Awake()
        {
            PlayerID.cameraLook = this;
        }

        public float GetPitch()
        {
            return Pitch;
        }

        public float GetYaw()
        {
            return Yaw;
        }
    }
}
