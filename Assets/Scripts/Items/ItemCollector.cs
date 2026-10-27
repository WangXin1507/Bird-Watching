using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemCollector : Interactable
{
    [SerializeField] private List<ItemData> acceptableItems;
    [SerializeField] private List<ItemData> desiredItems;
    [SerializeField] private MissionData mission;
    [SerializeField] private Transform slotContainer;
    
    private int totalItems = 0;
    private GameObject[] slots;
    private List<Transform> itemPositions = new List<Transform>();

    public void Start()
    {
        totalItems = desiredItems.Count;

        foreach (Transform child in slotContainer) {
            itemPositions.Add(child);
        }

        slots = new GameObject[totalItems];
    }

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
                int emptySlot = _FirstEmptySlotIndex();

                if (emptySlot != -1)
                {
                    playerItem.GetComponent<IFocusable>().enabled = false;
                    playerItem.SetParent(transform, true);
                    slots[emptySlot] = playerItem.gameObject;

                    Transform targetSlot = itemPositions.ElementAtOrDefault(emptySlot);
                    if (targetSlot != null)
                    {
                        playerItem.transform.position = targetSlot.position;
                        playerItem.transform.rotation = targetSlot.rotation;
                    }
                    else
                    {
                        playerItem.transform.position = transform.position;
                    }

                    PlayerID.playerInteraction.ForceRemove();

                    // Check if the items match
                    if (GetItemCount() >= totalItems) _EvaluateItems();
                }
            }
            else 
            {
                Debug.Log("Collector is full");
            }
        }
        else if (GetItemCount() > 0)
        {
            _GiveItem();
        }
    }

    private void _EvaluateItems()
    {
        if (_CompareItemLists())
        {
            
            Debug.Log("QUEST LOGIC GOES HERE");
            Journal.Instance.CompleteMission(mission);
        }
        else
        {
            Debug.Log("Content doesn't match");
            WrongWayUI.Instance.Show();
            DestroyItems();
        }
    }

    private int _FirstEmptySlotIndex()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null) return i;
        }
        return -1;
    }

    private int GetItemCount()
    {
        int count = 0;
        foreach (var obj in slots)
        {
            if (obj != null) count++;
        }
        return count;
    }

    private bool _CompareItemLists()
    {
        List<ItemData> currentItemData = new List<ItemData>();
        foreach (var obj in slots)
        {
            if (obj != null)
            {
                ItemComponent comp = obj.GetComponent<ItemComponent>();
                if (comp != null) currentItemData.Add(comp.data);
            }
        }

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

    private void DestroyItems()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
            {
                Destroy(slots[i]);
                slots[i] = null;
            }
        }
    }

    private void _GiveItem()
    {
        int lastIndex = -1;
        for (int i = slots.Length - 1; i >= 0; i--)
        {
            if (slots[i] != null)
            {
                lastIndex = i;
                break;
            }
        }

        if (lastIndex != -1)
        {
            GameObject givenItem = slots[lastIndex];
            slots[lastIndex] = null;

            givenItem.GetComponent<IFocusable>().enabled = true;
            PlayerID.playerInteraction.ForceHold(givenItem.transform);
        }
    }
}
