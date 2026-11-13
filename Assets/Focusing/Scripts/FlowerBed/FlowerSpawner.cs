using UnityEngine;

/// <summary>
/// Click while the bed is focused to receive a flower of the given type.
/// Inherits InteractInFocus, so it is clicked the same way as your other focus objects.
/// </summary>
public class FlowerSpawner : InteractInFocus
{
    [SerializeField] private FlowerType flowerType;

    [Tooltip("Optional: shows a display copy of the flower on the spawner.")]
    [SerializeField] private Transform displayAnchor;

    private void Start()
    {
        if (displayAnchor != null && FlowerRegistry.Instance != null)
            FlowerRegistry.Instance.Spawn(flowerType, displayAnchor);
    }

    public override void InteractWhileFocus()
    {
        if (PlayerFlowerHolder.Instance == null)
        {
            Debug.LogError("FlowerSpawner: no PlayerFlowerHolder in the scene");
            return;
        }

        if (PlayerFlowerHolder.Instance.TryGive(flowerType))
            Debug.Log($"Picked up {flowerType}");
        else
            Debug.Log($"Already holding {PlayerFlowerHolder.Instance.HeldFlower}");
    }
}
