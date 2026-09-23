using UnityEngine;

[CreateAssetMenu(fileName = "BirdCameraData", menuName = "Scriptable Objects/BirdCameraData")]
public class BirdCameraData : ScriptableObject
{
    [Tooltip("CinemachineCamera vertical field of view.")]
    public float vertcialFov = 85f;

    [Tooltip("CinemachineOrbitalFollow sphere radius.")]
    public float cameraDistance = 0.6f;

    [Tooltip("CinemachineRotationComposer screen position for the target. 0 is screen center, ±0.5 is the screen edge.")]
    public Vector2 screenPosition = new(0f, 0.3f);

    [Tooltip("When enabled, CinemachineRotationComposer will not reframe while the target stays inside the dead zone.")]
    public bool deadZoneEnabled = false;

    [Tooltip("CinemachineRotationComposer dead zone size around the screen position. Larger means the target can drift farther before the camera recenters.")]
    public Vector2 deadZoneSize = new(0.2f, 0.2f);

    [Tooltip("CinemachineRotationComposer target offset in the tracked target's local space. Use this to aim at a point that is not the target origin.")]
    public Vector3 aimTargetOffset = new(0f, 0.05f, 0f);
}
