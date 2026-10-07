using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class Journal : MonoBehaviour
{
    [SerializeField] private List<MissionData> missions = new List<MissionData>();

    private readonly HashSet<string> unlockedTranscriptIds = new HashSet<string>();

    public event Action OnTranscriptUnlocked;

    [DoNotSerialize] public MissionData lastMissionChanged = null;
    public IReadOnlyList<MissionData> AllMissions => missions;
    public IEnumerable<MissionData> UnlockedMissions => missions.Where(IsMissionUnlocked);
    
    public static Journal Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        LoadUnlocks();
        OnTranscriptUnlocked?.Invoke();
    }

    public bool IsTranscriptUnlocked(ATranscriptEntry transcript)
    {
        return transcript != null && unlockedTranscriptIds.Contains(transcript.TranscriptId);
    }
    
    public bool IsTranscriptUnlocked(MissionData mission, int transcriptIndex)
    {
        ATranscriptEntry transcript = mission.Transcripts[transcriptIndex];
        return transcript != null && unlockedTranscriptIds.Contains(transcript.TranscriptId);
    }

    public bool IsMissionUnlocked(MissionData mission)
    {
        return mission != null && mission.Transcripts.Any(IsTranscriptUnlocked);
    }

    public IEnumerable<ATranscriptEntry> GetUnlockedTranscripts(MissionData mission)
    {
        if (mission == null) return Enumerable.Empty<TranscriptEntry>();
        return mission.Transcripts.Where(IsTranscriptUnlocked);
    }

    public bool UnlockTranscript(MissionData mission, int transcriptIndex)
    {
        if (!ValidateMission(mission)) return false;

        if (transcriptIndex < 0 || transcriptIndex >= mission.Transcripts.Count)
        {
            Debug.LogWarning($"[Journal] Transcript index {transcriptIndex} out of range for '{mission.MissionName}'.");
            return false;
        }

        if (!TryUnlock(mission, mission.Transcripts[transcriptIndex])) return false;

        SaveManager.Instance.data.unlockedTranscriptIds = unlockedTranscriptIds.ToList();
        return true;
    }

    public bool UnlockTranscript(MissionData mission, ATranscriptEntry transcript)
    {
        if (!ValidateMission(mission)) return false;

        if (transcript == null || !mission.Transcripts.Contains(transcript))
        {
            Debug.LogWarning($"[Journal] Transcript doesn't belong to '{mission.MissionName}'.");
            return false;
        }

        if (!TryUnlock(mission, transcript)) return false;

        SaveManager.Instance.data.unlockedTranscriptIds = unlockedTranscriptIds.ToList();
        return true;
    }
    
    public void UnlockTranscripts(MissionData mission, IEnumerable<int> transcriptIndices)
    {
        if (!ValidateMission(mission)) return;

        bool anyNew = false;
        foreach (int i in transcriptIndices)
        {
            if (i >= 0 && i < mission.Transcripts.Count)
                anyNew |= TryUnlock(mission, mission.Transcripts[i]);
        }

        if (anyNew) SaveManager.Instance.data.unlockedTranscriptIds = unlockedTranscriptIds.ToList();
    }

    public void UnlockAllTranscripts(MissionData mission)
    {
        if (!ValidateMission(mission)) return;

        bool anyNew = false;
        foreach (var transcript in mission.Transcripts)
            anyNew |= TryUnlock(mission, transcript);

        if (anyNew) SaveManager.Instance.data.unlockedTranscriptIds = unlockedTranscriptIds.ToList();
    }

    private bool ValidateMission(MissionData mission)
    {
        if (mission != null && missions.Contains(mission)) return true;

        Debug.LogWarning($"[Journal] Mission isn't in the journal: {mission}");
        return false;
    }

    private bool TryUnlock(MissionData mission, ATranscriptEntry transcript)
    {
        if (transcript == null || !unlockedTranscriptIds.Add(transcript.TranscriptId))
            return false;

        lastMissionChanged = mission;
        OnTranscriptUnlocked?.Invoke();
        return true;
    }

    private void LoadUnlocks()
    {
        unlockedTranscriptIds.Clear();

        var saved = SaveManager.Instance.data.unlockedTranscriptIds;
        if (saved == null) return;

        foreach (var id in saved)
            unlockedTranscriptIds.Add(id);
    }
}