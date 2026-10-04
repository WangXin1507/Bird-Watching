using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace BirdWatching.Cutscenes
{
    [TrackColor(0.22f, 0.55f, 0.72f)]
    [TrackClipType(typeof(CinemachineDollyClip))]
    [DisplayName("Cutscene/Cinemachine Dolly")]
    public class CinemachineDollyTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<CinemachineDollyMixer>.Create(graph, inputCount);
        }
    }
}
