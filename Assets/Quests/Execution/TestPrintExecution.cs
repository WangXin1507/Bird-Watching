using UnityEngine;
using BirdWatching.Quests;

public class TestPrintExecution : IQuestExecutionStrategy
{
    [Tooltip("This text will be printed to console when the quest is completed")]
    public string msgToPrint;

    protected override void OnInitialize()
    {
        Debug.Log($"<color=green>TestPrintExecution</color>: {msgToPrint}");
    }
}
