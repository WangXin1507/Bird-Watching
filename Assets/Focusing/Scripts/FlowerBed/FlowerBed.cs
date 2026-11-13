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
    [SerializeField] private List<FlowerSlot> slots = new List<FlowerSlot>();

    public IReadOnlyList<FlowerSlot> Slots => slots;
    public FocusingInteract Focus { get; private set; }

    public event Action<FlowerBed> BedChanged;

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

    private void OnSlotChanged(FlowerSlot slot) => BedChanged?.Invoke(this);
}
