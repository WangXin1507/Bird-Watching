using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Scene manager mapping each FlowerType to the prefab of its flower mesh.
/// Fill the list in the Inspector; it is turned into a dictionary on Awake.
/// </summary>

public enum FlowerType
{
    RedFlower,
    BlueFlower,
    GreenFlower,
}

public class FlowerRegistry : MonoBehaviour
{
    public static FlowerRegistry Instance { get; private set; }

    [Serializable]
    public struct FlowerEntry
    {
        public FlowerType type;
        public GameObject prefab;
    }

    [SerializeField] private List<FlowerEntry> flowers = new List<FlowerEntry>();

    private readonly Dictionary<FlowerType, GameObject> lookup = new Dictionary<FlowerType, GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate FlowerRegistry found, destroying " + gameObject.name);
            Destroy(this);
            return;
        }
        Instance = this;

        lookup.Clear();
        foreach (FlowerEntry entry in flowers)
        {
            if (entry.prefab == null)
            {
                Debug.LogWarning($"FlowerRegistry: no prefab assigned for {entry.type}");
                continue;
            }
            if (!lookup.TryAdd(entry.type, entry.prefab))
                Debug.LogWarning($"FlowerRegistry: duplicate entry for {entry.type}, keeping the first one");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public bool TryGetPrefab(FlowerType type, out GameObject prefab)
    {
        return lookup.TryGetValue(type, out prefab);
    }

    public GameObject GetPrefab(FlowerType type)
    {
        if (lookup.TryGetValue(type, out GameObject prefab)) return prefab;
        Debug.LogError($"FlowerRegistry: no prefab registered for {type}");
        return null;
    }

    /// <summary>Spawns the flower mesh for a type under a parent, snapped to its origin.</summary>
    public GameObject Spawn(FlowerType type, Transform parent)
    {
        GameObject prefab = GetPrefab(type);
        if (prefab == null) return null;
        return Instantiate(prefab, parent.position, parent.rotation, parent);
    }
}
