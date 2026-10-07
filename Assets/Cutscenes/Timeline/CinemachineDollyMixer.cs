using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace BirdWatching.Cutscenes
{
    public class CinemachineDollyMixer : PlayableBehaviour
    {
        const int LivePriority = 100;

        readonly List<CinemachineSplineDolly> dollies = new();
        readonly List<float> positionSums = new();
        readonly List<float> weightSums = new();
        readonly Dictionary<CinemachineCamera, PrioritySettings> originalPriorities = new();

        CinemachineCamera liveCamera;
        CinemachineBrain brain;
        CinemachineBlendDefinition savedBlend;
        bool blendOverridden;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            dollies.Clear();
            positionSums.Clear();
            weightSums.Clear();

            CinemachineCamera winningCamera = null;
            float winningWeight = 0f;

            int inputCount = playable.GetInputCount();
            for (int i = 0; i < inputCount; i++)
            {
                float weight = playable.GetInputWeight(i);
                if (weight <= 0f) continue;

                var input = (ScriptPlayable<CinemachineDollyBehaviour>)playable.GetInput(i);
                CinemachineDollyBehaviour behaviour = input.GetBehaviour();
                if (behaviour == null || !behaviour.TryGetDolly(out CinemachineSplineDolly dolly))
                    continue;

                float duration = (float)input.GetDuration();
                float time = (float)input.GetTime();
                float normalized = duration > 0.0001f ? Mathf.Clamp01(time / duration) : 1f;
                Accumulate(dolly, behaviour.EvaluatePosition(normalized), weight);

                if (behaviour.camera != null && weight > winningWeight)
                {
                    winningWeight = weight;
                    winningCamera = behaviour.camera;
                }
            }

            for (int i = 0; i < dollies.Count; i++)
            {
                float weight = weightSums[i];
                if (weight <= 0f) continue;
                SetDollyPosition(dollies[i], positionSums[i] / weight);
            }

            SetLiveCamera(winningCamera);
        }

        public override void OnGraphStop(Playable playable)
        {
            SetLiveCamera(null);
        }

        public override void OnPlayableDestroy(Playable playable)
        {
            SetLiveCamera(null);
        }

        void SetLiveCamera(CinemachineCamera camera)
        {
            if (liveCamera == camera) return;

            if (camera != null)
                ForceCutBlend();

            if (liveCamera != null)
                RestorePriority(liveCamera);

            liveCamera = camera;
            if (liveCamera != null)
                RaisePriority(liveCamera);
            else
                RestoreBlend();
        }

        void ForceCutBlend()
        {
            if (blendOverridden) return;

            brain = CinemachineBrain.ActiveBrainCount > 0
                ? CinemachineBrain.GetActiveBrain(0)
                : (Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null);
            if (brain == null) return;

            savedBlend = brain.DefaultBlend;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
            blendOverridden = true;
        }

        void RestoreBlend()
        {
            if (!blendOverridden || brain == null) return;

            brain.DefaultBlend = savedBlend;
            blendOverridden = false;
        }

        void RaisePriority(CinemachineCamera camera)
        {
            if (!originalPriorities.ContainsKey(camera))
                originalPriorities[camera] = camera.Priority;

            PrioritySettings priority = camera.Priority;
            priority.Enabled = true;
            priority.Value = LivePriority;
            camera.Priority = priority;
        }

        void RestorePriority(CinemachineCamera camera)
        {
            if (originalPriorities.TryGetValue(camera, out PrioritySettings original))
                camera.Priority = original;
        }

        void Accumulate(CinemachineSplineDolly dolly, float position, float weight)
        {
            int index = dollies.IndexOf(dolly);
            if (index < 0)
            {
                dollies.Add(dolly);
                positionSums.Add(position * weight);
                weightSums.Add(weight);
                return;
            }

            positionSums[index] += position * weight;
            weightSums[index] += weight;
        }

        static void SetDollyPosition(CinemachineSplineDolly dolly, float position)
        {
            SplineSettings settings = dolly.SplineSettings;
            settings.Position = position;
            dolly.SplineSettings = settings;
        }
    }
}
