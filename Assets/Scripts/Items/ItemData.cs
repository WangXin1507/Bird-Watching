using UnityEngine;

public class ItemData : ScriptableObject
{
    [Header("Item Details")]
    public string itemName;
    public Mesh itemMesh;
    public Material itemMaterial;
    public Vector3 itemScale;

    [Header("Grab Positioning")]
    public Vector3 grabOffset;
    public Vector3 grabRotation;
}
