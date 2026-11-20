using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using Unity.Cinemachine;

public class Journal : SingletonBehaviour<Journal>
{
    [SerializeField] private List<MissionInScene> missions = new();

    private readonly HashSet<string> unlockedTranscriptIds = new HashSet<string>();
    private readonly HashSet<string> completedMissionIDs = new HashSet<string>();

    public event Action OnTranscriptUnlocked;
    public event Action<MissionData> OnMissionCompleted;

    [DoNotSerialize] public MissionData lastMissionChanged = null;
    public IReadOnlyList<MissionData> AllMissions => missions
        .Where(entry => entry.mission != null)
        .Select(entry => entry.mission)
        .ToList();

    //public IEnumerable<MissionData> UnlockedMissions => AllMissions.Where(IsMissionUnlocked);
    public IEnumerable<MissionData> UnlockedMissions => AllMissions;


    private bool afterLoad = false;

    private void Start()
    {
        LoadUnlocks();
        RestoreCompletedTrinkets();
        OnTranscriptUnlocked?.Invoke();
        afterLoad = true;
    }

    /// <summary>Finds the scene entry (trinket + reward camera) for a mission.</summary>
    public bool TryGetMissionInScene(MissionData mission, out MissionInScene entry)
    {
        foreach (MissionInScene candidate in missions)
        {
            if (candidate.mission != null && candidate.mission == mission)
            {
                entry = candidate;
                return true;
            }
        }

        entry = default;
        return false;
    }

    // Missions completed in an earlier session skip the cutscene: their trinket is simply there.
    private void RestoreCompletedTrinkets()
    {
        foreach (MissionInScene entry in missions)
        {
            if (entry.reward != null && IsMissionCompleted(entry.mission))
                entry.reward.ShowRevealedInstant();
        }
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
    
    public bool IsMissionCompleted(MissionData mission)
    {
        return mission != null && completedMissionIDs.Contains(mission.MissionID);
    }

    public IEnumerable<ATranscriptEntry> GetUnlockedTranscripts(MissionData mission)
    {
        if (mission == null) return Enumerable.Empty<TranscriptEntry>();
        return mission.Transcripts.Where(IsTranscriptUnlocked);
    }

    public bool CompleteMission(MissionData mission)
    {
        if (!ValidateMission(mission)) return false;
        
        if (!TryComplete(mission)) return false;

        SaveManager.Instance.data.completedMissionIds = completedMissionIDs.ToList();
        OnMissionCompleted?.Invoke(mission);

        if (!afterLoad) return true;

        // The reward cutscene plays first; the "journal updated" popup shows once it ends.
        if (TryGetMissionInScene(mission, out MissionInScene entry) && entry.reward != null)
            TrinketRewardSequence.Play(entry, ShowJournalPopup).Forget();
        else
            ShowJournalPopup();

        return true;
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

    public bool UnlockTranscript(ATranscriptEntry transcript)
    {
        return UnlockTranscript(transcript.mission, transcript);
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
        if (mission != null && missions.Any(entry => entry.mission == mission)) return true;

        Debug.LogWarning($"[Journal] Mission isn't in the journal: {mission}");
        return false;
    }

    private bool TryUnlock(MissionData mission, ATranscriptEntry transcript)
    {
        if (transcript == null || !unlockedTranscriptIds.Add(transcript.TranscriptId))
            return false;

        lastMissionChanged = mission;
        OnTranscriptUnlocked?.Invoke();
        if (afterLoad)
            JournalUpdateUI.Instance.Show();
        return true;
    }
    
    private bool TryComplete(MissionData mission)
    {
        // Already completed: no second reward, no second cutscene.
        if (!completedMissionIDs.Add(mission.MissionID)) return false;

        lastMissionChanged = mission;
        OnTranscriptUnlocked?.Invoke();
        return true;
    }

    private static void ShowJournalPopup()
    {
        if (JournalUpdateUI.Instance != null)
            JournalUpdateUI.Instance.Show();
    }

    private void LoadUnlocks()
    {
        unlockedTranscriptIds.Clear();

        var saved = SaveManager.Instance.data.unlockedTranscriptIds;
        if (saved == null) return;

        foreach (var id in saved)
            unlockedTranscriptIds.Add(id);
        
        var completed = SaveManager.Instance.data.completedMissionIds;
        if (completed == null) return;

        foreach (var id in completed)
            completedMissionIDs.Add(id);
        
    }
}

[Serializable]
public struct MissionInScene
{
    public MissionData mission;
    [Tooltip("Scene object that fades in when the mission is completed.")]
    public Trinket reward;
    [Tooltip("Camera that frames the trinket during the reward cutscene. Leave its priority at 0.")]
    public CinemachineCamera focusRewardCam;
}