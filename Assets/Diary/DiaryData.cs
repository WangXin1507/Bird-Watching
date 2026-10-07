using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDiary", menuName = "Bird Watching/Diary")]
public class DiaryData : ATranscriptEntry
{
    [TextArea(10, 40)]
    [SerializeField] public string leftTextBox;
    [TextArea(10, 40)]
    [SerializeField] public string rightTextBox;
    [SerializeField] public Sprite imageToShow;
    [Tooltip("350x400 max")]
    [SerializeField] public Vector2 imageSize;
    
    
    public override void OpenUI()
    {
        DiaryUI.Instance.DrawAndOpenDiaryUI(this, false);
    }
    
    public override void CloseUI()
    {
        DiaryUI.Instance.CloseDiary();
    }
}