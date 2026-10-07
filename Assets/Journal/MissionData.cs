using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewMission", menuName = "Bird Watching/Mission")]
public class MissionData : ScriptableObject
{
    [SerializeField] private string missionName;
    [SerializeField] private Sprite missionPicture;
    [SerializeField] private Sprite trinket;
    [SerializeField] private List<ATranscriptEntry> transcripts = new List<ATranscriptEntry>();
    
    public string MissionName => missionName;
    public IReadOnlyList<ATranscriptEntry> Transcripts => transcripts;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Give every transcript a unique ID. Also catches duplicates, since
        // duplicating a list element in the inspector copies its ID.
        var seen = new HashSet<string>();
        bool changed = false;

        foreach (var entry in transcripts)
        {
            if (entry == null) continue;

            if (string.IsNullOrEmpty(entry.TranscriptId) || !seen.Add(entry.TranscriptId))
            {
                entry.AssignNewId();
                entry.mission = this;
                seen.Add(entry.TranscriptId);
                changed = true;
            }
        }

        if (changed)
            UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}