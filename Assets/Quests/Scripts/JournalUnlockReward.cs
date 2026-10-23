using System;
using Extensions.Logic;
using QuestSystem;
using UnityEngine;

namespace BirdWatching.Quests
{
    [Serializable]
    public class TranscriptLocation
    {
        public MissionData missionData;
        public int index;
    }

    /** <summary>A quest reward that unlocks a journal transcript. Granted once per completion. Replaces JournalUnlockExecution.</summary> */
    [Serializable]
    public class JournalUnlockReward : IQuestReward
    {
        [Tooltip("This transcript will be unlocked when the quest is completed")]
        public TranscriptLocation transcriptToUnlock = new();

        public RewardDescription Describe() =>
            new RewardDescription(transcriptToUnlock.missionData != null ? $"Journal: {transcriptToUnlock.missionData.name}" : "Journal entry");

        public void Grant(IFacts facts)
        {
            if (Journal.Instance == null || transcriptToUnlock.missionData == null) return;
            Journal.Instance.UnlockTranscript(transcriptToUnlock.missionData, transcriptToUnlock.index);
        }
    }
}
