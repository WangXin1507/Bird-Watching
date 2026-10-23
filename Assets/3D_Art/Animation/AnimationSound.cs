using FMODUnity;
using UnityEngine;

public class AnimationSound : MonoBehaviour
{
    public void PlaySoundEffect(string path)
    {
        //EventReference audio = EventReference.Find(path);
        AudioManager.PlayOneShot(AudioEvents.Bird.Bird_Footstep_SFX, transform.position);
        Debug.Log("?");
    }
}
