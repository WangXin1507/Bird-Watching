using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Splines;

public class SplinedGrabbable : Focusable, IGrabbable
{
    [SerializeField] private Transform _grabHandle;
    private SplineFollower _splineFollower;
    public Transform grabHandle => _grabHandle;

    private void Start()
    {
        _splineFollower = GetComponent<SplineFollower>();
        if (!_splineFollower) Debug.Log("No spline follower on SplinedGrabbable object");
    }

    public Transform Grab()
    {
        _splineFollower.enabled = false;
        return transform;
    }
}
