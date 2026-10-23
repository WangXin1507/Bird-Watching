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
        //EventReference audio = EventReference.Find(path);
        AudioManager.PlayOneShot(AudioEvents.Bird.Bird_Footstep_SFX, transform.position);
        Debug.Log("?");
    }
}
