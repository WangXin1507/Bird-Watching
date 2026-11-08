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
    [SerializeField] private Button closeButton;
    
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
        Time.timeScale = 0.0f;
        Cursor.lockState = CursorLockMode.None;
        
        gameObject.GetComponent<Canvas>().enabled = true;
        
        text.text = context.Text;
        
        _transcriptEntry = context;
        
        closeButton.Select();
    }
    
    public void CloseTranscript()
    {
        if (backToGame)
        {
            PlayerID.UnlockMovement();
            Time.timeScale = 1.0f;
        
            Cursor.lockState = CursorLockMode.Locked;
        }
        
        gameObject.GetComponent<Canvas>().enabled = false;
        
        _transcriptEntry.OnTranscriptClosed?.Invoke();
    }
}