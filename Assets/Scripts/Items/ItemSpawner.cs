using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : Interactable
{
    [SerializeField] private GameObject itemBase;
    [SerializeField] private ItemData item;
    [SerializeField] private int maxItems = 10;
    private List<GameObject> spawnedItems = new List<GameObject>();

    private static readonly int BaseTextureID = Shader.PropertyToID("_BaseTexture");

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
            Texture tex = item.itemMaterial.mainTexture;
            if (!tex)
            {
                Texture2D colorTex = new Texture2D(1, 1);
                colorTex.SetPixel(0, 0, item.itemMaterial.color);
                colorTex.Apply();
                tex = colorTex;
            }

            Material focusMaterial = newItem.GetComponent<MeshRenderer>().material;
            focusMaterial.SetTexture(BaseTextureID, tex);

            ItemComponent itemComponent = newItem.GetComponent<ItemComponent>();
            itemComponent.data = item;

            AudioManager.PlayOneShot(interactSound, transform.position);
            PlayerID.playerInteraction.ForceHold(newItem.transform);

            spawnedItems.Add(newItem);
        }
    }
}
