using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class ATranscriptEntry : ScriptableObject
{
    [Tooltip("Stable ID used for saving. Auto-generated; don't edit by hand.")]
    [SerializeField] private string transcriptId;
    [SerializeField] private string title;

    [HideInInspector] public MissionData mission;
    public string TranscriptId => transcriptId;
    public string Title => title;
    
    internal void AssignNewId() => transcriptId = Guid.NewGuid().ToString();

    [HideInInspector] public UnityEvent OnTranscriptClosed;

    public virtual void OpenUI()
    {
        
    }

    public virtual void CloseUI()
    {
        
    }
}

[CreateAssetMenu(fileName = "NewTranscript", menuName = "Bird Watching/Transcript")]
public class TranscriptEntry : ATranscriptEntry
{
    [TextArea(10, 40)]
    [SerializeField] private string text;
    
    public string Text => text;
    
    public override void OpenUI()
    {
        TranscriptUI.Instance.DrawAndOpenTranscriptUI(this, false);
    }

    public override void CloseUI()
    {
        TranscriptUI.Instance.CloseTranscript();
    }
}