using System;
using UnityEngine;

/// <summary>
/// Tracks the flower the player is currently holding (one at a time).
/// Put this on the player (or any object in the scene). Optional hand anchor shows the held mesh.
/// </summary>
public class PlayerFlowerHolder : MonoBehaviour
{
    public static PlayerFlowerHolder Instance { get; private set; }

    [Tooltip("Optional: where the held flower mesh is shown (e.g. a point in front of the camera).")]
    [SerializeField] private Transform handAnchor;

    public bool IsHolding { get; private set; }
    public FlowerType HeldFlower { get; private set; }

    public event Action<FlowerType> FlowerPickedUp;
    public event Action<FlowerType> FlowerReleased;

    private GameObject heldVisual;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate PlayerFlowerHolder found, destroying " + gameObject.name);
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Gives the player a flower. Fails if they are already holding one.</summary>
    public bool TryGive(FlowerType type)
    {
        if (IsHolding) return false;

        IsHolding = true;
        HeldFlower = type;

        if (handAnchor != null && FlowerRegistry.Instance != null)
            heldVisual = FlowerRegistry.Instance.Spawn(type, handAnchor);

        FlowerPickedUp?.Invoke(type);
        return true;
    }

    /// <summary>Takes the held flower away from the player. Fails if they have none.</summary>
    public bool TryTake(out FlowerType type)
    {
        type = HeldFlower;
        if (!IsHolding) return false;

        IsHolding = false;
        if (heldVisual != null) Destroy(heldVisual);
        heldVisual = null;

        FlowerReleased?.Invoke(type);
        return true;
    }
}
