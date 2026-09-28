using UnityEngine;
using System.Collections.Generic;

public class ItemCollector : Interactable
{
    [SerializeField] private ItemData[] acceptableItems;
    [SerializeField] private ItemData[] desiredItems;
    [SerializeField] private int totalItems = 0;


    private List<GameObject> currentItems = new List<GameObject>();
    private HashSet<ItemData> currentItemData = new HashSet<ItemData>();

    public override void Interact()
    {
        Transform playerItem = PlayerID.playerInteraction.holding;

        if (playerItem)
        {
            ItemComponent itemComponent = playerItem.GetComponent<ItemComponent>();
            if (!itemComponent) return;
            ItemData itemData = itemComponent.data;

            if (currentItems.Count < totalItems)
            {
                foreach (ItemData curr in acceptableItems)
                {
                    if (itemData == curr)
                    {
                        Debug.Log("Giving item to collector");

                        playerItem.GetComponent<IFocusable>().enabled = false;

                        currentItems.Add(playerItem.gameObject);
                        currentItemData.Add(itemData);

                        playerItem.SetParent(transform, false);
                        //playerItem.localPosition = Vector3.zero;
                        //playerItem.localRotation = Quaternion.identity;

                        PlayerID.playerInteraction.ForceRemove();
                    }
                }
            }

            if (currentItems.Count == totalItems)
            {
                HashSet<ItemData> desiredItemData = new HashSet<ItemData>();
                foreach (ItemData item in desiredItems)
                {
                    desiredItemData.Add(item);
                }

                // Order of items doesn't matter when comparing hash sets
                if (desiredItemData.SetEquals(currentItemData))
                {
                    Debug.Log("Content matches");
                }
                else
                {
                    Debug.Log("Incorrect content");
                }
            }
        }
        else if (currentItems.Count > 0) // Pop the last currentItem
        {
            _GiveItem();
        }
    }

    private void _GiveItem()
    {
        GameObject givenItem = currentItems[currentItems.Count - 1];
        ItemData givenData = givenItem.GetComponent<ItemComponent>().data;
        currentItems.Remove(givenItem);
        currentItemData.Remove(givenData);
            
        givenItem.GetComponent<IFocusable>().enabled = true;

        PlayerID.playerInteraction.ForceHold(givenItem.transform);
    }
}
