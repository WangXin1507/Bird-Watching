using UnityEngine;

/// <summary>
/// Interactable that opens the inspection menu for its InspectableData.
/// </summary>
public class InspectInteractable : Interactable
{
    [SerializeField] private InspectableData inspectable;

    public override void Interact()
    {
        InspectionManager.Instance.Open(inspectable);
        Journal.Instance.UnlockTranscript(inspectable);
    }
}
