using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace BirdWatching.Cutscenes
{
    [TrackColor(0.78f, 0.48f, 0.22f)]
    [TrackClipType(typeof(PlayerSplineOverrideClip))]
    [TrackBindingType(typeof(PlayerSplinePositionOverride))]
    [DisplayName("Cutscene/Player Spline Override")]
    public class PlayerSplineOverrideTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var director = go.GetComponent<PlayableDirector>();
            var target = director != null
                ? director.GetGenericBinding(this) as PlayerSplinePositionOverride
                : null;

            foreach (TimelineClip clip in GetClips())
            {
                if (clip.asset is PlayerSplineOverrideClip playableAsset)
                {
                    playableAsset.Target = target;
                    playableAsset.OwningClip = clip;
                }
            }

            return ScriptPlayable<PlayerSplineOverrideMixer>.Create(graph, inputCount);
        }
    }
}
