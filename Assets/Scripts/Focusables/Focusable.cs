using System.Collections.Generic;
using UnityEngine;



public class Focusable : MonoBehaviour, IFocusable
{
    [SerializeField] private List<ATranscriptEntry> requiredTranscripts = new List<ATranscriptEntry>();
    
    public bool CanFocus()
    {
        bool canFocus = true;
        foreach (ATranscriptEntry requiredTranscript in requiredTranscripts)
        {
            canFocus = canFocus && Journal.Instance.IsTranscriptUnlocked(requiredTranscript);
        }
        return canFocus;
    }
    
    public void LoseFocus()
    {
    }

    public void GainFocus()
    {
    }
}
