using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class JournalUI : MonoBehaviour
{
    [Serializable]
    public class TranscriptSlot
    {
        public Button button;
        [Tooltip("Optional. Set to the button's label to show the transcript title on it.")]
        public TMP_Text buttonLabel;
    }
    
    [Header("Page Content")]
    [SerializeField] private TMP_Text missionNameText;
    [Tooltip("Slot 0 shows the mission's first transcript, slot 1 the second, and so on.")]
    [SerializeField] private List<TranscriptSlot> transcriptSlots = new List<TranscriptSlot>();
    [Tooltip("Optional. Shows something like '2 / 5'.")]
    [SerializeField] private TMP_Text pageNumberText;
    [Tooltip("Optional. Shown only when the journal has no pages yet.")]
    [SerializeField] private GameObject emptyJournalObject;

    [Header("Navigation")]
    [SerializeField] private Button leftArrow;
    [SerializeField] private Button rightArrow;

    [Header("Display Options")]
    [Tooltip("If on, only missions with at least one unlocked transcript get a page.")]
    [SerializeField] private bool onlyShowUnlockedMissions = true;

    private readonly List<MissionData> pages = new List<MissionData>();
    private int currentPage;
    private ATranscriptEntry currentOpen;
    private bool openedTranscript = false;

    private void Awake()
    {
        leftArrow.onClick.AddListener(PreviousPage);
        rightArrow.onClick.AddListener(NextPage);
        Journal.Instance.OnTranscriptUnlocked += Refresh;
        
        for (int i = 0; i < transcriptSlots.Count; i++)
        {
            int slotIndex = i; // capture a copy for the lambda
            transcriptSlots[i].button.onClick.AddListener(() => OpenTranscriptInSlot(slotIndex));
        }
    }

    private void Start()
    {
        InputManager.Instance.JournalClicked += OnJournalClicked;
    }

    private void OnDestroy()
    {
        leftArrow.onClick.RemoveListener(PreviousPage);
        rightArrow.onClick.RemoveListener(NextPage);

        foreach (var slot in transcriptSlots)
            slot.button.onClick.RemoveAllListeners();
    }

    public void OnJournalClicked()
    {
        if (openedTranscript)
        {
            currentOpen.CloseUI();
            return;
        }
        
        if (gameObject.GetComponent<Canvas>().enabled)
        {
            CloseJournal();
            return;
        }
        OpenJournal();
    }

    public void OpenJournal()
    {
        PlayerID.LockMovement();
        Cursor.lockState = CursorLockMode.None;
        
        gameObject.GetComponent<Canvas>().enabled = true;
        
        if (Journal.Instance.lastMissionChanged != null)
            GoToMission(Journal.Instance.lastMissionChanged);
    }
    
    public void CloseJournal()
    {
        PlayerID.UnlockMovement();
        
        Cursor.lockState = CursorLockMode.Locked;
        
        gameObject.GetComponent<Canvas>().enabled = false;
    }

    // ---------- Public API ----------

    /// <summary>
    /// Rebuilds the page list from the journal and redraws the current page.
    /// Hook this up to whatever event should update the journal.
    /// </summary>
    public void Refresh()
    {
        // Remember which mission we were on so a refresh doesn't jump pages.
        MissionData previousMission = HasPages ? pages[currentPage] : null;

        pages.Clear();
        foreach (MissionData mission in Journal.Instance.AllMissions)
            if (Journal.Instance.IsMissionUnlocked(mission)) pages.Add(mission);
        foreach (MissionData mission in Journal.Instance.AllMissions) 
            if (!Journal.Instance.IsMissionUnlocked(mission)) pages.Add(mission);

        int previousIndex = previousMission != null ? pages.IndexOf(previousMission) : -1;
        currentPage = previousIndex >= 0 ? previousIndex : Mathf.Clamp(currentPage, 0, Mathf.Max(0, pages.Count - 1));

        DrawPage();
    }

    public void NextPage() => GoToPage(currentPage + 1);
    public void PreviousPage() => GoToPage(currentPage - 1);

    public void GoToPage(int index)
    {
        if (!HasPages) return;

        int clamped = Mathf.Clamp(index, 0, pages.Count - 1);
        if (clamped == currentPage) return;

        currentPage = clamped;
        DrawPage();
    }

    /// <summary>Jump straight to a specific mission's page, if it has one.</summary>
    public void GoToMission(MissionData mission)
    {
        int index = pages.IndexOf(mission);
        if (index >= 0) GoToPage(index);
    }

    // ---------- Drawing ----------

    private bool HasPages => pages.Count > 0;

    private void DrawPage()
    {
        if (emptyJournalObject != null)
            emptyJournalObject.SetActive(!HasPages);

        if (!HasPages)
        {
            missionNameText.text = string.Empty;
            if (pageNumberText != null) pageNumberText.text = string.Empty;
            HideAllSlots();
            leftArrow.interactable = false;
            rightArrow.interactable = false;
            return;
        }

        MissionData mission = pages[currentPage];

        missionNameText.text = mission.MissionName;
        DrawSlots(mission);

        if (pageNumberText != null)
            pageNumberText.text = $"{currentPage + 1} / {pages.Count}";

        leftArrow.interactable = currentPage > 0;
        rightArrow.interactable = currentPage < pages.Count - 1;
    }

    private void DrawSlots(MissionData mission)
    {
        if (mission.Transcripts.Count > transcriptSlots.Count)
        {
            Debug.LogWarning($"[JournalUI] '{mission.MissionName}' has {mission.Transcripts.Count} transcripts " +
                             $"but only {transcriptSlots.Count} slots exist. Extra transcripts won't be shown.");
        }

        for (int i = 0; i < transcriptSlots.Count; i++)
        {
            TranscriptSlot slot = transcriptSlots[i];
            ATranscriptEntry transcript = i < mission.Transcripts.Count ? mission.Transcripts[i] : null;
            bool visible = transcript != null && Journal.Instance.IsTranscriptUnlocked(transcript);

            SetSlotVisible(slot, visible);
            if (!visible) continue;

            if (slot.buttonLabel != null)
                slot.buttonLabel.text = transcript.Title;
        }
    }

    private void HideAllSlots()
    {
        foreach (var slot in transcriptSlots)
            SetSlotVisible(slot, false);
    }

    private static void SetSlotVisible(TranscriptSlot slot, bool visible)
    {
        slot.button.gameObject.SetActive(visible);
    }
    
    private void OpenTranscriptInSlot(int slotIndex)
    {
        if (!HasPages) return;

        MissionData mission = pages[currentPage];
        if (slotIndex >= mission.Transcripts.Count) return;

        ATranscriptEntry transcript = mission.Transcripts[slotIndex];
        if (transcript == null || !Journal.Instance.IsTranscriptUnlocked(transcript)) return;

        transcript.OpenUI();
        openedTranscript = true;
        currentOpen = transcript;
        transcript.OnTranscriptClosed.AddListener(TranscriptClosed);
    }

    private void TranscriptClosed()
    {
        openedTranscript = false;
        currentOpen.OnTranscriptClosed.RemoveListener(TranscriptClosed);
    }
}