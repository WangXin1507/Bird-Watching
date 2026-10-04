using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace BirdWatching.Cutscenes
{
    [Serializable]
    public class PlayerSplineOverrideClip : PlayableAsset, ITimelineClipAsset
    {
        public bool disableFreeLook;

        [NonSerialized]
        public PlayerSplinePositionOverride Target;

        [NonSerialized]
        public TimelineClip OwningClip;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<PlayerSplineOverrideBehaviour>.Create(graph);
            PlayerSplineOverrideBehaviour behaviour = playable.GetBehaviour();
            behaviour.target = Target;
            behaviour.disableFreeLook = disableFreeLook;
            behaviour.duration = OwningClip != null ? (float)OwningClip.duration : (float)duration;
            return playable;
        }
    }
}
