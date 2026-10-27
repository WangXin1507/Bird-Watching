using FMODUnity;
using UnityEngine;

public class AnimationSound : MonoBehaviour
{
    public void PlaySoundEffect(string path)
    {
        if (path == "" || GetComponent<Animator>().GetFloat("Speed") <= 0.5f)
        {
            return;
        }
        EventReference audio = EventReference.Find(path);
        if (!audio.IsNull) AudioManager.PlayOneShot(audio, transform.position);
    }
}
