using UnityEngine;
using BirdWatching.Quests;

[System.Serializable]
public class TranscriptLocation
{
    public MissionData missionData;
    public int index;
}

public class JournalUnlockExecution : IQuestExecutionStrategy
{
    [Tooltip("This transcript will be unlocked when the quest is completed")]
    public TranscriptLocation transcriptToUnlock;

    protected override void OnInitialize()
    {
        Journal.Instance.UnlockTranscript(transcriptToUnlock.missionData, transcriptToUnlock.index);
    }
}
