using FMODUnity;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static void PlayOneShot(EventReference audio, Vector3 position = default)
        => RuntimeManager.PlayOneShot(audio, position);

    public static void PlayOneShotAttached(EventReference audio, GameObject target)
        => RuntimeManager.PlayOneShotAttached(audio, target);
}
