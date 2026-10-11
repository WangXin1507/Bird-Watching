using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : Interactable
{
    [SerializeField] private GameObject itemBase;
    [SerializeField] private ItemData item;
    [SerializeField] private int maxItems = 1;
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
            newItem.name = item.itemName;

            //MeshFilter meshFilter = newItem.GetComponent<MeshFilter>();
            //meshFilter.mesh = item.itemMesh;
            newItem.GetComponent<MeshRenderer>().material = item.itemMaterial;

            ItemComponent itemComponent = newItem.GetComponent<ItemComponent>();
            itemComponent.data = item;

            PlayerID.playerInteraction.ForceHold(newItem.transform);

            spawnedItems.Add(newItem);
        }
    }
}
