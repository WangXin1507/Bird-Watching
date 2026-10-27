using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : Interactable
{
    [SerializeField] private GameObject itemBase;
    [SerializeField] private ItemData item;
    [SerializeField] private int maxItems = 10;
    private List<GameObject> spawnedItems = new List<GameObject>();

    public override void Interact()
    {
        Debug.Log("Interacted");

        if (!PlayerID.playerInteraction.holding)
        {
            spawnedItems.RemoveAll(item => item == null);

            if (spawnedItems.Count >= maxItems)
            {
                GameObject oldest = spawnedItems[0];
                spawnedItems.RemoveAt(0);

                if (oldest != null)
                {
                    Destroy(oldest);
                }
            }

            GameObject newItem = Instantiate(itemBase);
            newItem.transform.localScale = item.itemScale;
            newItem.name = item.itemName;

            if (item.itemMesh)
            {
                Bounds bounds = GetTightBounds(item.itemMesh);

                MeshFilter meshFilter = newItem.GetComponent<MeshFilter>();
                meshFilter.sharedMesh = item.itemMesh;

                meshFilter.mesh = item.itemMesh;

                Vector3 meshSize = bounds.size;
                Vector3 targetSize = item.itemScale;

                float x = targetSize.x / meshSize.x;
                float y = targetSize.y / meshSize.y;
                float z = targetSize.z / meshSize.z;
                float scale = Mathf.Min(x, y, z);

                newItem.transform.localScale = Vector3.one * scale;

                BoxCollider boxCollider = newItem.GetComponent<BoxCollider>();
                boxCollider.center = bounds.center;
                boxCollider.size = bounds.size;
            }

            if (item.itemMaterial)
            {
                MeshRenderer meshRenderer = newItem.GetComponent<MeshRenderer>();
                meshRenderer.material = item.itemMaterial;

                Focusable focusable = newItem.GetComponent<Focusable>();
                if (focusable) focusable.RefreshMaterials();
            }

            ItemComponent itemComponent = newItem.GetComponent<ItemComponent>();
            itemComponent.data = item;

            AudioManager.PlayOneShot(interactSound, transform.position);
            PlayerID.playerInteraction.ForceHold(newItem.transform);

            spawnedItems.Add(newItem);
        }
    }

    private Bounds GetTightBounds(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        bool foundVertex = false;
        Bounds bounds = new Bounds();

        for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
        {
            int[] indices = mesh.GetIndices(submesh);

            foreach (int index in indices)
            {
                if (!foundVertex)
                {
                    bounds = new Bounds(vertices[index], Vector3.zero);
                    foundVertex = true;
                }
                else
                {
                    bounds.Encapsulate(vertices[index]);
                }
            }
        }

        return bounds;
    }
}
