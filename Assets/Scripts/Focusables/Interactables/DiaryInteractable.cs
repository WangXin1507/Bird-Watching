using UnityEngine;

public class DiaryInteractable : Interactable
{
    [SerializeField] DiaryData diaryData;

    public override void Interact()
    {
        DiaryUI.Instance.DrawAndOpenDiaryUI(diaryData);
    }
}
