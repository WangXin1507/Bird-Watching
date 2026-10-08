using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace BirdWatching.Cutscenes
{
    [Serializable]
    public class CinemachineDollyClip : PlayableAsset, ITimelineClipAsset
    {
        public ExposedReference<CinemachineCamera> camera;

        [Range(0f, 1f)]
        public float startPosition;

        [Range(0f, 1f)]
        public float endPosition = 1f;

        public AnimationCurve ease = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        public ClipCaps clipCaps => ClipCaps.Blending | ClipCaps.Extrapolation;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<CinemachineDollyBehaviour>.Create(graph);
            CinemachineDollyBehaviour behaviour = playable.GetBehaviour();
            behaviour.camera = camera.Resolve(graph.GetResolver());
            behaviour.startPosition = startPosition;
            behaviour.endPosition = endPosition;
            behaviour.ease = ease;
            return playable;
        }
    }
}
