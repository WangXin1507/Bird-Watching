using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class CollectorSlot : Interactable
{
    // Slot for an SlottedItemCollector
    [SerializeField] private List<ItemData> acceptableItems;
    [SerializeField] private ItemData desiredItem;
    private SlottedItemCollector collector;
    public Transform currentItem;
    private ItemData currentItemData;
    private Transform itemPos;
    public bool correct;

    private void Start()
    {
        collector = GetComponentInParent<SlottedItemCollector>();
        if (!collector)
        {
            Debug.Log("PARENT OF CollectorSlot NEEDS TO BE AN OrderedItemCollector");
        }

        foreach (Transform child in transform)
        {
            itemPos = child.GetComponentInChildren<Transform>();
            if (itemPos) break;
        }

        if (!itemPos) itemPos = transform;
    }

    /*
     * Interactable behavior
     */ 
    public override void Interact()
    {
        Transform playerItem = PlayerID.playerInteraction.holding;

        if (playerItem)
        {
            ItemComponent itemComponent = playerItem.GetComponent<ItemComponent>();
            if (!itemComponent) return;
            ItemData itemData = itemComponent.data;

            // The item is allowed by the collector
            if (acceptableItems.Contains(itemData))
            {
                // The collector is empty
                if (!currentItem)
                {
                    playerItem.GetComponent<IFocusable>().enabled = false;
                    playerItem.SetParent(transform, true);
                    currentItem = playerItem.transform;
                    currentItemData = playerItem.GetComponent<ItemComponent>().data;

                    if (currentItemData == desiredItem) correct = true;

                    playerItem.transform.position = itemPos.position;
                    playerItem.transform.rotation = itemPos.rotation;

                    PlayerID.playerInteraction.ForceRemove();

                    collector.CheckItems();
                }
            }
        }
        else if (currentItem)
        {
            _GiveItem();
        }
    }

    /*
     * Give the currentItem to the Player
     */
    private void _GiveItem()
    {
        currentItem.GetComponent<IFocusable>().enabled = true;
        PlayerID.playerInteraction.ForceHold(currentItem);
        currentItem = null;
        currentItemData = null;
        correct = false;
    }

    /*
     *  Verifies that the current item is the desired item
     */
}
