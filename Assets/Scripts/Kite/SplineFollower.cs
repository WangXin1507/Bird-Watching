using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class SplineFollower : MonoBehaviour
{
    [Header("Spline Variables")]
    [SerializeField] private SplineContainer spline;
    [SerializeField] private float followSpeed = 0.02f;

    private Rigidbody rb;
    private float progress = 0.0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        transform.position = (Vector3)spline.EvaluatePosition(0, 0);
    }

    private void FixedUpdate()
    {
        FollowSpline();
    }

    void FollowSpline()
    {
        if (spline == null) return;

        progress += followSpeed * Time.deltaTime;
        if (progress > 1f) progress = 0f;

        float3 pos = spline.EvaluatePosition(0, progress);

        if (rb)
        {
            rb.MovePosition((Vector3)pos);
        }
        else
        {
            transform.position = (Vector3)pos;
        }
    }
}


