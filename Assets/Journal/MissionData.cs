using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TranscriptEntry
{
    [Tooltip("Stable ID used for saving. Auto-generated; don't edit by hand.")]
    [SerializeField] private string transcriptId;

    [SerializeField] private string title;

    [TextArea(10, 40)]
    [SerializeField] private string text;

    public string TranscriptId => transcriptId;
    public string Title => title;
    public string Text => text;
    
    internal void AssignNewId() => transcriptId = Guid.NewGuid().ToString();
}

[CreateAssetMenu(fileName = "NewMission", menuName = "Bird Watching/Mission")]
public class MissionData : ScriptableObject
{
    [SerializeField] private string missionName;
    [SerializeField] private Sprite missionPicture;
    [SerializeField] private Sprite trinket;
    [SerializeField] private List<TranscriptEntry> transcripts = new List<TranscriptEntry>();
    
    public string MissionName => missionName;
    public IReadOnlyList<TranscriptEntry> Transcripts => transcripts;

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
                seen.Add(entry.TranscriptId);
                changed = true;
            }
        }

        if (changed)
            UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}