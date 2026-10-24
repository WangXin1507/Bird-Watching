using FMODUnity;
using UnityEngine;

public class AnimationSound : MonoBehaviour
{
    public void PlaySoundEffect(string path)
    {
        if (GetComponent<Animator>().GetFloat("Speed") <= 0.5f)
        {
            return;
        }
        EventReference audio = EventReference.Find(path);
        AudioManager.PlayOneShot(audio, transform.position);
    }
}
