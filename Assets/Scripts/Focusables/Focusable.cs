using System.Collections.Generic;
using UnityEngine;



public class Focusable : MonoBehaviour, IFocusable
{
    [SerializeField] private List<TranscriptLocation> requiredTranscripts = new List<TranscriptLocation>();
    
    public bool CanFocus()
    {
        bool canFocus = true;
        foreach (TranscriptLocation requiredTranscript in requiredTranscripts)
        {
            canFocus = canFocus && Journal.Instance.IsTranscriptUnlocked(requiredTranscript.missionData, requiredTranscript.index);
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
