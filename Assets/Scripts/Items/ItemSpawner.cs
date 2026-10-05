using UnityEngine;

public class ItemSpawner : Interactable
{
    [SerializeField] private GameObject itemBase;
    [SerializeField] private ItemData item;

    public override void Interact()
    {
        Debug.Log("Interacted");

        if (!PlayerID.playerInteraction.holding)
        {
            GameObject newItem = Instantiate(itemBase);
            newItem.name = item.itemName;

            //MeshFilter meshFilter = newItem.GetComponent<MeshFilter>();
            //meshFilter.mesh = item.itemMesh;
            //newItem.GetComponent<MeshRenderer>().material = item.itemMaterial;

            ItemComponent itemComponent = newItem.GetComponent<ItemComponent>();
            itemComponent.data = item;

            PlayerID.playerInteraction.ForceHold(newItem.transform);
        }
    }
}
