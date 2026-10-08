using UnityEngine;
using UnityEngine.Playables;

namespace BirdWatching.Cutscenes
{
    public class PlayerSplineOverrideBehaviour : PlayableBehaviour
    {
        public PlayerSplinePositionOverride target;
        public bool disableFreeLook;
        public float duration;

        bool started;

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (!Application.isPlaying || target == null || started) return;

            started = true;
            float clipDuration = duration > 0f ? duration : (float)playable.GetDuration();
            target.BeginOverride(Mathf.Max(0f, clipDuration), disableFreeLook);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!started || target == null) return;

            started = false;
            target.EndOverride();
        }
    }
}
