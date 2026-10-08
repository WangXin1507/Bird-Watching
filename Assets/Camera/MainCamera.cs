using Sirenix.OdinInspector;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using PrimeTween;

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

        /// <summary>Snapshot of the camera values a profile drives, read from a BirdCameraData or another CinemachineCamera.</summary>
        struct CameraProfile
        {
            public float verticalFov;
            public bool hasDistance;
            public float cameraDistance;
            public bool hasComposition;
            public Vector2 screenPosition;
            public bool deadZoneEnabled;
            public Vector2 deadZoneSize;
            public Vector3 aimTargetOffset;

            public static CameraProfile FromData(BirdCameraData data)
            {
                return new CameraProfile
                {
                    verticalFov = data.vertcialFov,
                    hasDistance = true,
                    cameraDistance = data.cameraDistance,
                    hasComposition = true,
                    screenPosition = data.screenPosition,
                    deadZoneEnabled = data.deadZoneEnabled,
                    deadZoneSize = data.deadZoneSize,
                    aimTargetOffset = data.aimTargetOffset,
                };
            }

            public static CameraProfile FromCamera(CinemachineCamera camera)
            {
                CameraProfile profile = new() { verticalFov = camera.Lens.FieldOfView };

                CinemachineOrbitalFollow sourceOrbital = camera.GetComponent<CinemachineOrbitalFollow>();
                if (sourceOrbital != null)
                {
                    profile.hasDistance = true;
                    profile.cameraDistance = sourceOrbital.Radius;
                }

                CinemachineRotationComposer sourceComposer = camera.GetComponent<CinemachineRotationComposer>();
                if (sourceComposer != null)
                {
                    profile.hasComposition = true;
                    profile.screenPosition = sourceComposer.Composition.ScreenPosition;
                    profile.deadZoneEnabled = sourceComposer.Composition.DeadZone.Enabled;
                    profile.deadZoneSize = sourceComposer.Composition.DeadZone.Size;
                    profile.aimTargetOffset = sourceComposer.TargetOffset;
                }

                return profile;
            }
        }

        public void UpdateCameraProfile(BirdCameraData newProfile, float duration = 3f)
        {
            if (newProfile == null)
            {
                Debug.LogWarning("BirdCameraData profile is missing.", this);
                return;
            }

            if (!EnsureLiveCamera()) return;

            ApplyProfile(CameraProfile.FromData(newProfile), Application.isPlaying ? duration : 0f);
        }

        /// <summary>Tweens the live camera to match another CinemachineCamera's settings. The live camera stays active.</summary>
        public void MatchCamera(CinemachineCamera source, float duration = 1f)
        {
            if (source == null)
            {
                Debug.LogWarning("Source CinemachineCamera is missing.", this);
                return;
            }

            if (!EnsureLiveCamera() || source == Instance) return;

            ApplyProfile(CameraProfile.FromCamera(source), Application.isPlaying ? duration : 0f);
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
            SelectLiveCamera();
        }

        void OnDisable()
        {
            if (Application.isPlaying) Tween.StopAll(onTarget: this);
        }

        void OnDestroy()
        {
            if (Instance != null && Instance.transform != null && Instance.transform.IsChildOf(transform))
            {
                Instance = null;
            }
        }

        bool EnsureLiveCamera()
        {
            if (Instance == null || !Instance.transform.IsChildOf(transform))
            {
                SelectLiveCamera();
            }

            if (Instance == null)
            {
                Debug.LogWarning("No CinemachineCamera found under MainCamera.", this);
                return false;
            }

            return true;
        }

        /// <summary>The first CinemachineCamera child is always the one we look through; profiles only change its values.</summary>
        void SelectLiveCamera()
        {
            CinemachineCamera[] cameras = GetComponentsInChildren<CinemachineCamera>(true);
            if (cameras.Length == 0)
            {
                Instance = null;
                orbital = null;
                composer = null;
                return;
            }

            Instance = cameras[0];
            orbital = Instance.GetComponent<CinemachineOrbitalFollow>();
            composer = Instance.GetComponent<CinemachineRotationComposer>();

            for (int i = 0; i < cameras.Length; i++)
            {
                SetPriority(cameras[i], i == 0 ? 20 : 0);
            }
        }

        static void SetPriority(CinemachineCamera camera, int value)
        {
            PrioritySettings priority = camera.Priority;
            priority.Enabled = true;
            priority.Value = value;
            camera.Priority = priority;
        }

        void ApplyProfile(CameraProfile profile, float duration)
        {
            if (Application.isPlaying) Tween.StopAll(onTarget: this);

            if (duration <= 0f || !Application.isPlaying)
            {
                ApplyProfileImmediate(profile);
                return;
            }

            if (orbital != null && profile.hasDistance)
            {
                Tween.Custom(this, orbital.Radius, profile.cameraDistance, duration,
                    (target, radius) => target.orbital.Radius = radius);
            }

            Tween.Custom(this, Instance.Lens.FieldOfView, profile.verticalFov, duration,
                (target, fov) => target.SetFieldOfView(fov));

            if (composer == null || !profile.hasComposition) return;

            ScreenComposerSettings composition = composer.Composition;
            ScreenComposerSettings.DeadZoneSettings deadZone = composition.DeadZone;
            deadZone.Enabled = profile.deadZoneEnabled;
            composition.DeadZone = deadZone;
            composer.Composition = composition;

            Tween.Custom(this, composer.Composition.ScreenPosition, profile.screenPosition, duration,
                (target, screenPosition) =>
                {
                    ScreenComposerSettings settings = target.composer.Composition;
                    settings.ScreenPosition = screenPosition;
                    target.composer.Composition = settings;
                });

            Tween.Custom(this, composer.Composition.DeadZone.Size, profile.deadZoneSize, duration,
                (target, size) =>
                {
                    ScreenComposerSettings settings = target.composer.Composition;
                    ScreenComposerSettings.DeadZoneSettings zone = settings.DeadZone;
                    zone.Size = size;
                    settings.DeadZone = zone;
                    target.composer.Composition = settings;
                });

            Tween.Custom(this, composer.TargetOffset, profile.aimTargetOffset, duration,
                (target, offset) => target.composer.TargetOffset = offset);
        }

        void ApplyProfileImmediate(CameraProfile profile)
        {
            SetFieldOfView(profile.verticalFov);

            if (orbital != null && profile.hasDistance)
            {
                orbital.Radius = profile.cameraDistance;
            }

            if (composer != null && profile.hasComposition)
            {
                ScreenComposerSettings composition = composer.Composition;
                composition.ScreenPosition = profile.screenPosition;
                ScreenComposerSettings.DeadZoneSettings deadZone = composition.DeadZone;
                deadZone.Enabled = profile.deadZoneEnabled;
                deadZone.Size = profile.deadZoneSize;
                composition.DeadZone = deadZone;
                composer.Composition = composition;
                composer.TargetOffset = profile.aimTargetOffset;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(Instance);
                if (orbital != null) UnityEditor.EditorUtility.SetDirty(orbital);
                if (composer != null) UnityEditor.EditorUtility.SetDirty(composer);
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
                SelectLiveCamera();
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
