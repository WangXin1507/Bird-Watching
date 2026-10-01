using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using DG.Tweening;

namespace BirdWatchingCamera
{
    [DefaultExecutionOrder(-100000)]
    public class MainCamera : MonoBehaviour
    {
        public static CinemachineCamera Instance { get; private set; }

        [ShowInInspector]
        BirdCameraData CurrentCameraData => cameraDataList.Count > 0 ? cameraDataList[CurrentCameraDataIndex] : null;

        [SerializeField, Tooltip("List of pre-stored bird camera states"), ListDrawerSettings(OnEndListElementGUI = "DrawSwichProfileButton")]
        List<BirdCameraData> cameraDataList = new();

        public int CurrentCameraDataIndex => Mathf.Clamp(currentCameraDataIndex, 0, Mathf.Max(0, cameraDataList.Count - 1));
        private int currentCameraDataIndex = 0;

        private CinemachineOrbitalFollow orbital;
        private CinemachineRotationComposer composer;

        public void UpdateCameraProfile(BirdCameraData newProfile, float duration = 1f)
        {
            if (newProfile == null)
            {
                Debug.LogWarning("BirdCameraData profile is missing.", this);
                return;
            }

            SelectCamera(CurrentCameraDataIndex);
            if (Instance == null)
            {
                Debug.LogWarning("No CinemachineCamera found under MainCamera.", this);
                return;
            }

            ApplyProfile(newProfile, Application.isPlaying ? duration : 0f);
        }

        public void EnableFlightProfile()
        {
            if (cameraDataList.Count == 0) return;
            currentCameraDataIndex = 1;
            UpdateCameraProfile(cameraDataList[CurrentCameraDataIndex]);
        }

        public void EnableIdleProfile()
        {
            if (cameraDataList.Count == 0) return;
            currentCameraDataIndex = 0;
            UpdateCameraProfile(cameraDataList[CurrentCameraDataIndex]);
        }

        void Awake()
        {
            SelectCamera(CurrentCameraDataIndex);
        }

        void OnDisable()
        {
            DOTween.Kill(this);
        }

        void OnDestroy()
        {
            if (Instance != null && Instance.transform != null && Instance.transform.IsChildOf(transform))
            {
                Instance = null;
            }
        }

        void SelectCamera(int index)
        {
            CinemachineCamera[] cameras = GetComponentsInChildren<CinemachineCamera>(true);
            if (cameras.Length == 0)
            {
                Instance = null;
                orbital = null;
                composer = null;
                return;
            }

            index = Mathf.Clamp(index, 0, cameras.Length - 1);
            Instance = cameras[index];
            orbital = Instance.GetComponent<CinemachineOrbitalFollow>();
            composer = Instance.GetComponent<CinemachineRotationComposer>();

            for (int i = 0; i < cameras.Length; i++)
            {
                SetPriority(cameras[i], i == index ? 20 : 0);
            }
        }

        static void SetPriority(CinemachineCamera camera, int value)
        {
            PrioritySettings priority = camera.Priority;
            priority.Enabled = true;
            priority.Value = value;
            camera.Priority = priority;
        }

        void ApplyProfile(BirdCameraData profile, float duration)
        {
            DOTween.Kill(this);

            if (duration <= 0f || !Application.isPlaying)
            {
                ApplyProfileImmediate(profile);
                return;
            }

            if (orbital != null)
            {
                DOTween.To(() => orbital.Radius, x => orbital.Radius = x, profile.cameraDistance, duration)
                    .SetTarget(this);
            }

            DOTween.To(
                () => Instance.Lens.FieldOfView,
                fov => SetFieldOfView(fov),
                profile.vertcialFov,
                duration).SetTarget(this);

            if (composer == null) return;

            ScreenComposerSettings composition = composer.Composition;
            ScreenComposerSettings.DeadZoneSettings deadZone = composition.DeadZone;
            deadZone.Enabled = profile.deadZoneEnabled;
            composition.DeadZone = deadZone;
            composer.Composition = composition;

            DOTween.To(
                () => composer.Composition.ScreenPosition,
                screenPosition =>
                {
                    ScreenComposerSettings settings = composer.Composition;
                    settings.ScreenPosition = screenPosition;
                    composer.Composition = settings;
                },
                profile.screenPosition,
                duration).SetTarget(this);

            DOTween.To(
                () => composer.Composition.DeadZone.Size,
                size =>
                {
                    ScreenComposerSettings settings = composer.Composition;
                    ScreenComposerSettings.DeadZoneSettings zone = settings.DeadZone;
                    zone.Size = size;
                    settings.DeadZone = zone;
                    composer.Composition = settings;
                },
                profile.deadZoneSize,
                duration).SetTarget(this);

            DOTween.To(() => composer.TargetOffset, x => composer.TargetOffset = x, profile.aimTargetOffset, duration)
                .SetTarget(this);
        }

        void ApplyProfileImmediate(BirdCameraData profile)
        {
            SetFieldOfView(profile.vertcialFov);

            if (orbital != null)
            {
                orbital.Radius = profile.cameraDistance;
            }

            if (composer == null) return;

            ScreenComposerSettings composition = composer.Composition;
            composition.ScreenPosition = profile.screenPosition;
            ScreenComposerSettings.DeadZoneSettings deadZone = composition.DeadZone;
            deadZone.Enabled = profile.deadZoneEnabled;
            deadZone.Size = profile.deadZoneSize;
            composition.DeadZone = deadZone;
            composer.Composition = composition;
            composer.TargetOffset = profile.aimTargetOffset;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(Instance);
                if (orbital != null) UnityEditor.EditorUtility.SetDirty(orbital);
                UnityEditor.EditorUtility.SetDirty(composer);
            }
#endif
        }

        void SetFieldOfView(float fov)
        {
            LensSettings lens = Instance.Lens;
            lens.FieldOfView = fov;
            Instance.Lens = lens;
        }

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position, "Main Camera");
#endif
        }

#if UNITY_EDITOR

        void OnValidate()
        {
            if (Instance == null)
            {
                SelectCamera(CurrentCameraDataIndex);
            }
        }

        private void DrawSwichProfileButton(int index)
        {
            if (GUILayout.Button("Switch Profile"))
            {
                currentCameraDataIndex = index;
                if (index < 0 || index >= cameraDataList.Count) return;
                UpdateCameraProfile(cameraDataList[index], 0f);
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
