using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TranscriptUI : MonoBehaviour
{
    public static TranscriptUI Instance;

    [SerializeField] private TMP_Text text;
    
    private TranscriptEntry _transcriptEntry;
    private bool backToGame;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void DrawAndOpenTranscriptUI(TranscriptEntry context, bool back = true)
    {
        backToGame = back;
        
        PlayerID.LockMovement();
        Cursor.lockState = CursorLockMode.None;
        
        gameObject.GetComponent<Canvas>().enabled = true;
        
        text.text = context.Text;
        
        _transcriptEntry = context;
    }
    
    public void CloseTranscript()
    {
        if (backToGame)
        {
            PlayerID.UnlockMovement();
        
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        gameObject.GetComponent<Canvas>().enabled = false;
        
        _transcriptEntry.OnTranscriptClosed?.Invoke();
    }
}