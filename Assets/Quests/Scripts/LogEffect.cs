using System;
using QuestSystem;
using UnityEngine;

namespace BirdWatching.Quests
{
    /** <summary>A QuestReaction effect that prints to the console, for testing. Replaces TestPrintExecution.</summary> */
    [Serializable]
    public class LogEffect : IQuestReactionEffect
    {
        [Tooltip("This text will be printed to console when the reaction turns on")]
        public string msgToPrint;

        public void Apply(bool on)
        {
            if (on) Debug.Log($"<color=green>LogEffect</color>: {msgToPrint}");
        }
    }
}
