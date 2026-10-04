using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    static EventInstance currentMusicTrack;

    public static void PlayOneShot(EventReference audio, Vector3 position = default) => RuntimeManager.PlayOneShot(audio, position);

    public static void PlayOneShotAttached(EventReference audio, GameObject target) => RuntimeManager.PlayOneShotAttached(audio, target);

    public static void PlayTrack(EventReference audio)
    {
        if (currentMusicTrack.isValid())
        {
            currentMusicTrack.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            currentMusicTrack.release();
        }

        var instance = RuntimeManager.CreateInstance(audio);
        instance.start();
    }
}