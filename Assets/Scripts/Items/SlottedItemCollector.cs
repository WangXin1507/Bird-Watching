using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;

public class SlottedItemCollector : MonoBehaviour
{
    [SerializeField] MissionData mission;
    [SerializeField] private CollectorSlot[] slots;

    public void CheckItems()
    {
        bool full = true;
        bool correct = true;

        Debug.Log("Checking items");
        foreach (CollectorSlot slot in slots)
        {
            if (!slot.currentItem) full = false;
            if (!slot.correct) correct = false;
        }

        if (full && correct)
        {
            Debug.Log("Completed mission");
            if (mission) Journal.Instance.CompleteMission(mission);
        }
        else if (full)
        {
            Debug.Log("Content doesn't match");
            WrongWayUI.Instance.Show();
        }
    }
}