using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class SplineFollower : MonoBehaviour
{
    private float progress = 0.0f;
    private float randomX;
    private float randomY;
    private float randomZ;

    [Header("Spline Variables")]
    [SerializeField] private SplineContainer spline;
    [SerializeField] private float followSpeed = 8.0f;
    [SerializeField] private float randomDeviation = 1.0f;
    [SerializeField] private float driftRadius = 1.5f;

    private void Start()
    {
        randomX = UnityEngine.Random.Range(0f, 100f);
        randomY = UnityEngine.Random.Range(0f, 100f);
        randomZ = UnityEngine.Random.Range(0f, 100f);
    }

    private void Update()
    {
        FollowSpline();
    }

    void FollowSpline()
    {
        if (spline == null) return;

        progress += followSpeed * Time.deltaTime;
        if (progress > 1f) progress = 0f;

        float3 pos = spline.EvaluatePosition(0, progress);
        float3 tan = math.normalize(spline.EvaluateTangent(0, progress));

        float time = Time.time * randomDeviation;
        float noiseX = (Mathf.PerlinNoise(time + randomX, 0f) * 2f) - 1f;
        float noiseY = (Mathf.PerlinNoise(time + randomY, 0f) * 2f) - 1f;
        float noiseZ = (Mathf.PerlinNoise(time + randomZ, 0f) * 2f) - 1f;

        Vector3 forward = tan;
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, forward).normalized;
        up = Vector3.Cross(forward, right).normalized;

        // Randomly drift in a cone shape
        Vector3 cone = (right * noiseX + up * noiseY + forward * noiseZ) * driftRadius;
        transform.position = (Vector3)pos + cone;
    }
}


