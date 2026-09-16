using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using DG.Tweening;

namespace BirdWatchingCamera
{
    [DefaultExecutionOrder(-100000)]
    public class MainCamera : MonoBehaviour
    {
        public static CinemachineCamera Instance { get; private set; }

        [ShowInInspector]
        BirdCameraData currentCameraData => cameraDataList.Count > 0 ? cameraDataList[CurrentCameraDataIndex] : null;
        [SerializeField, Tooltip("List of pre-stored bird camera states"), ListDrawerSettings(OnEndListElementGUI = "DrawSwichProfileButton")]
        List<BirdCameraData> cameraDataList = new();

        public int CurrentCameraDataIndex => Mathf.Clamp(currentCameraDataIndex, 0, cameraDataList.Count - 1);
        private int currentCameraDataIndex = 0;

        private CinemachineThirdPersonFollow follow;
        private CinemachineCameraOffset offset;

        public void UpdateCameraProfile(BirdCameraData newProfile, float degree = 1f)
        { 
            DOTween.To(() => Instance.Lens.FieldOfView, x => Instance.Lens.FieldOfView = x, newProfile.vertcialFov, degree);
            DOTween.To(() => offset.Offset, x => offset.Offset = x, newProfile.offset, degree);
            DOTween.To(() => follow.Damping, x => follow.Damping = x, newProfile.damping, degree);
            DOTween.To(() => follow.CameraDistance, x => follow.CameraDistance = x, newProfile.cameraDistance, degree);
        }

        public void EnableFlightProfile()
        {
            currentCameraDataIndex = 1;
            UpdateCameraProfile(cameraDataList[currentCameraDataIndex]);
        }

        public void EnableIdleProfile()
        {
            currentCameraDataIndex = 0;
            UpdateCameraProfile(cameraDataList[currentCameraDataIndex]);
        }

        void Awake()
        {
            Instance = GetComponentInChildren<CinemachineCamera>();
        }

        void OnDrawGizmos()
        {
            Handles.Label(transform.position, "Main Camera");
        }

#if UNITY_EDITOR

        void OnValidate()
        {
            if (Instance == null)
            {
                Instance = GetComponentInChildren<CinemachineCamera>();
            }
            if (Instance != null)
            {
                follow = Instance.GetComponentInChildren<CinemachineThirdPersonFollow>();
                offset = Instance.GetComponentInChildren<CinemachineCameraOffset>();
            }
        }

        private void DrawSwichProfileButton(int index)
        {
            if (GUILayout.Button("Switch Profile"))
            {
                if (Instance == null || follow == null || offset == null)
                {
                    Debug.LogWarning("CinemachineCamera, CinemachineThirdPersonFollow or CinemachineCameraOffset is not assigned.");
                    return;
                }

                currentCameraDataIndex = index;
                var data = cameraDataList[currentCameraDataIndex];
                
                UpdateCameraProfile(data);
            }
        }
#endif
    }
}
