using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

namespace BirdWatching.Cutscenes
{
    public class CinemachineDollyBehaviour : PlayableBehaviour
    {
        public CinemachineCamera camera;
        public float startPosition;
        public float endPosition = 1f;
        public AnimationCurve ease;

        public bool TryGetDolly(out CinemachineSplineDolly dolly)
        {
            dolly = camera != null ? camera.GetComponent<CinemachineSplineDolly>() : null;
            return dolly != null;
        }

        public float EvaluatePosition(float normalizedTime)
        {
            float t = Mathf.Clamp01(normalizedTime);
            if (ease != null && ease.length > 0)
                t = Mathf.Clamp01(ease.Evaluate(t));
            return Mathf.Lerp(startPosition, endPosition, t);
        }
    }
}
