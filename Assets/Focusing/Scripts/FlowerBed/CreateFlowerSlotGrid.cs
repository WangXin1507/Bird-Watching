using UnityEngine;

public class CreateFlowerSlotGrid : MonoBehaviour
{
    [SerializeField] private GameObject FlowerSlot;
    [SerializeField] private GameObject flowerSlotParent;
    [SerializeField] private Vector3 bounds;
    [SerializeField] private Vector3 offset;
    [SerializeField] private FlowerBed flowerBed;
    [SerializeField] private FocusingInteract focusingInteract;
    [SerializeField] private Vector3 numberOfSlots;

    private void OnValidate()
    {
        focusingInteract = GetComponent<FocusingInteract>();
        flowerBed = GetComponent<FlowerBed>();
    }
    
    [ContextMenu("Create Flower Slot")]
    private void CreateFlowerSlot()
    {
        for (int i = flowerSlotParent.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(flowerSlotParent.transform.GetChild(i).gameObject);
        }
        flowerBed.slots.Clear();
        
        for (int i = 0; i < numberOfSlots.x; i++)
        {
            for (int j = 0; j < numberOfSlots.y; j++)
            {
                for (int h = 0; h < numberOfSlots.z; h++)
                {
                    GameObject flowerSlot = Instantiate(FlowerSlot, flowerSlotParent.transform);
                    float tx = i / Mathf.Max((numberOfSlots.x - 1), 1);
                    float ty = j / Mathf.Max((numberOfSlots.y - 1), 1);
                    float tz = h / Mathf.Max((numberOfSlots.z - 1), 1);
                    flowerSlot.transform.position = new Vector3(Mathf.Lerp(-bounds.x, bounds.x, tx), Mathf.Lerp(-bounds.y, bounds.y, ty), Mathf.Lerp(-bounds.z, bounds.z, tz)) + offset + transform.position;
                    FlowerSlot fSlot = flowerSlot.GetComponent<FlowerSlot>();
                    fSlot.focusableManager = focusingInteract;
                    if (flowerBed != null)
                        flowerBed.slots.Add(flowerSlot.GetComponent<FlowerSlot>());
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.darkRed;
        Vector3 newOffset = new Vector3(offset.x * transform.localScale.x, offset.y * transform.localScale.y, offset.z * transform.localScale.z);
        Vector3 newBounds = new Vector3(bounds.x * transform.localScale.x, bounds.y * transform.localScale.y, bounds.z * transform.localScale.z);
        Gizmos.DrawWireCube(transform.position + newOffset, newBounds * 2);
    }
}
