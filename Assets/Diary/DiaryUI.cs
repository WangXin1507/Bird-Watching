using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DiaryUI : MonoBehaviour
{
    public static DiaryUI Instance;

    [SerializeField] private TMP_Text leftText;
    [SerializeField] private TMP_Text rightText;
    [SerializeField] private Image rightImage;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void DrawAndOpenDiaryUI(DiaryData context)
    {
        PlayerID.LockMovement();
        Cursor.lockState = CursorLockMode.None;
        
        gameObject.GetComponent<Canvas>().enabled = true;
        
        leftText.text = context.leftTextBox;
        if (context.rightTextBox != "")
        {
            rightText.text = context.rightTextBox;
            rightImage.enabled = false;
            rightText.enabled = true;
        }
        else
        {
            rightImage.sprite = context.imageToShow;
            rightImage.rectTransform.sizeDelta = new Vector2(context.imageSize.x, context.imageSize.y);
            rightImage.enabled = true;
            rightText.enabled = false;
        }
    }
    
    public void CloseJournal()
    {
        PlayerID.UnlockMovement();
        
        Cursor.lockState = CursorLockMode.Locked;
        
        gameObject.GetComponent<Canvas>().enabled = false;
    }
}