using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDiary", menuName = "Bird Watching/Diary")]
public class DiaryData : ScriptableObject
{
    [TextArea(10, 40)]
    [SerializeField] public string leftTextBox;
    [TextArea(10, 40)]
    [SerializeField] public string rightTextBox;
    [SerializeField] public Sprite imageToShow;
    [Tooltip("350x400 max")]
    [SerializeField] public Vector2 imageSize;
}