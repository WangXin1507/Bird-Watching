using FMODUnity;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class AudioManager : MonoBehaviour
{
    public List<EventReference> audioRefs;

    public void PlayeOneShot(EventReference audioRef)
    {
        RuntimeManager.PlayOneShot(audioRef);
    }

#if UNITY_EDITOR
    [Button]
    public void RefreshEventReferenceList()
    {
        foreach (var editorRefs in EventManager.Events)
        {
            audioRefs.Add(EventReference.Find(editorRefs.Path));
        }
    }
#endif
}