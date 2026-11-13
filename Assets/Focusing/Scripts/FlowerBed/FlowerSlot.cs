using System;
using UnityEngine;

/// <summary>
/// A spot in the flower bed. Click while focused to plant the flower the player is holding.
/// If allowPickup is on, clicking a filled slot with empty hands picks the flower back up.
/// </summary>
public class FlowerSlot : InteractInFocus
{
    [Tooltip("Where the planted flower mesh spawns. Defaults to this transform.")]
    [SerializeField] private Transform plantAnchor;
    [SerializeField] private bool allowPickup = true;

    public bool IsOccupied { get; private set; }
    public FlowerType PlantedFlower { get; private set; }

    /// <summary>Fired whenever this slot is planted or emptied.</summary>
    public event Action<FlowerSlot> Changed;

    private GameObject plantedVisual;

    private void Awake()
    {
        if (plantAnchor == null) plantAnchor = transform;
    }

    public override void InteractWhileFocus()
    {
        PlayerFlowerHolder holder = PlayerFlowerHolder.Instance;
        if (holder == null)
        {
            Debug.LogError("FlowerSlot: no PlayerFlowerHolder in the scene");
            return;
        }

        if (!IsOccupied)
        {
            if (holder.TryTake(out FlowerType type))
                Plant(type);
        }
        else if (allowPickup && !holder.IsHolding)
        {
            FlowerType type = PlantedFlower;
            Clear();
            holder.TryGive(type);
        }
    }

    public void Plant(FlowerType type)
    {
        if (IsOccupied) Clear();

        IsOccupied = true;
        PlantedFlower = type;
        if (FlowerRegistry.Instance != null)
            plantedVisual = FlowerRegistry.Instance.Spawn(type, plantAnchor);

        Changed?.Invoke(this);
    }

    public void Clear()
    {
        if (!IsOccupied) return;

        IsOccupied = false;
        if (plantedVisual != null) Destroy(plantedVisual);
        plantedVisual = null;

        Changed?.Invoke(this);
    }
}
