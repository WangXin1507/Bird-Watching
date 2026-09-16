using Unity.Cinemachine;
using UnityEngine;

namespace BirdWatchingCamera
{
    public class CameraTarget : MonoBehaviour
    {
        void Awake()
        {
            CinemachineCamera camera = MainCamera.Instance;
            if (camera == null) return;

            camera.Follow = transform;
            camera.LookAt = transform;
        }

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position, "Camera Target");
#endif
        }
    }
}
