using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Ties a bed together. Put it on the same object as FocusingInteract
/// (which already handles the Cinemachine camera switch). Collects the child slots
/// and reports when the arrangement changes, for checks like "is the bed full?".
/// </summary>
[RequireComponent(typeof(FocusingInteract))]
public class FlowerBed : MonoBehaviour
{
    public List<FlowerSlot> slots = new List<FlowerSlot>();

    public IReadOnlyList<FlowerSlot> Slots => slots;
    public FocusingInteract Focus { get; private set; }

    public event Action<FlowerBed> BedChanged;

    [SerializeField] private FlowerType[] neededFlowerTypes;
    [SerializeField] private MissionData missionData;

    public bool IsFull => slots.All(s => s.IsOccupied);
    public int PlantedCount => slots.Count(s => s.IsOccupied);

    private void Awake()
    {
        Focus = GetComponent<FocusingInteract>();
        if (slots.Count == 0)
            slots.AddRange(GetComponentsInChildren<FlowerSlot>());

        foreach (FlowerSlot slot in slots)
            slot.Changed += OnSlotChanged;
    }

    private void OnDestroy()
    {
        foreach (FlowerSlot slot in slots)
            if (slot != null) slot.Changed -= OnSlotChanged;
    }

    public int CountOf(FlowerType type) => slots.Count(s => s.IsOccupied && s.PlantedFlower == type);

    private void OnSlotChanged(FlowerSlot slot)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (neededFlowerTypes.Length <= i)
            {
                break;
            }
            if (slots[i].PlantedFlower != neededFlowerTypes[i])
            {
                break;
            }

            if (i == slots.Count - 1)
            {
                Journal.Instance.CompleteMission(missionData);
            }
        }
        BedChanged?.Invoke(this);
    }
}
