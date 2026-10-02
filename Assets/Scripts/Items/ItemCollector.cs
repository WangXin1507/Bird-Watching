using BirdWatching.Quests;
using NUnit.Framework;
using Sirenix.Utilities;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemCollector : Interactable
{
    [SerializeField] private List<ItemData> acceptableItems;
    [SerializeField] private List<ItemData> desiredItems;
    [SerializeField] private int totalItems = 0;

    private List<GameObject> currentItems = new List<GameObject>();
    private List<ItemData> currentItemData = new List<ItemData>();

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

                        foreach (ItemData data in currentItemData)
                        {
                            Debug.Log(data);
                        }

                        playerItem.SetParent(transform, false);
                        //playerItem.localPosition = Vector3.zero;
                        //playerItem.localRotation = Quaternion.identity;

                        PlayerID.playerInteraction.ForceRemove();
                    }
                }
            }

            if (currentItems.Count == totalItems)
            {
                if (_compareItemLists())
                {
                    Debug.Log("QUEST LOGIC GOES HERE");
                }
                else
                {
                    Debug.Log("Content doesn't match");
                }
            }
        }
        else if (currentItems.Count > 0) // Pop the last currentItem
        {
            _GiveItem();
        }
    }

    private bool _compareItemLists()
    {
        if (currentItemData == desiredItems) return true;
        if (currentItemData == null || desiredItems == null) return false;
        if (currentItemData.Count != desiredItems.Count) return false;

        var counts = new Dictionary<ItemData, int>();

        foreach (ItemData item in currentItemData)
        {
            if (counts.ContainsKey(item)) counts[item]++;
            else counts[item] = 1;
        }

        foreach (ItemData item in desiredItems)
        {
            if (!counts.ContainsKey(item)) return false;

            counts[item]--;

            if (counts[item] < 0) return false;
        }

        return true;
    }

    private void _GiveItem()
    {
        GameObject givenItem = currentItems[currentItems.Count - 1];
        ItemData givenData = givenItem.GetComponent<ItemComponent>().data;
        currentItems.Remove(givenItem);
        currentItemData.Remove(givenData);

        foreach (ItemData data in currentItemData) {
            Debug.Log(data);
        }

        givenItem.GetComponent<IFocusable>().enabled = true;

        PlayerID.playerInteraction.ForceHold(givenItem.transform);
    }
}
