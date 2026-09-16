using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "BirdCameraData", menuName = "Scriptable Objects/BirdCameraData")]
public class BirdCameraData : ScriptableObject
{
    public float vertcialFov = 120f;
    public Vector3 offset = new(0, 1.5f, 0);
    public Vector3 damping = new(0.1f, 0.5f, 0.3f);
    public float cameraDistance = 2f;
}
