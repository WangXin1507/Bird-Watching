using UnityEngine;

/// <summary>
/// Describes something the player can pick up and look at in the inspection menu.
/// </summary>
[CreateAssetMenu(fileName = "NewInspectable", menuName = "Bird Watching/Inspectable")]
public class InspectableData : ATranscriptEntry
{
    [Header("Model")]
    [Tooltip("Prefab spawned on the inspection stage. Its scripts, colliders and rigidbodies are disabled on the spawned copy.")]
    public GameObject prefab;

    [Tooltip("Rotation (degrees) the object starts at when the menu opens.")]
    public Vector3 initialRotation;

    [Tooltip("1 = auto-framed to fill the view. Higher pushes the camera further back.")]
    [Min(0.1f)]
    public float framingDistance = 1f;

    public override void OpenUI()
    {
        base.OpenUI();
        InspectionManager.Instance.Open(this);
    }

    public override void CloseUI()
    {
        base.CloseUI();
        InspectionManager.Instance.Close();
    }
}
