using System.Reflection;
using UnityEngine;

public class SplinedGrabbable : Focusable, IGrabbable
{
    [SerializeField] private MissionData missionCompleteOnGrab;
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
        if (_splineFollower.enabled)
        {
            Debug.Log("Completed mission");
            Journal.Instance.CompleteMission(missionCompleteOnGrab);

            _splineFollower.enabled = false;
        }
        return transform;
    }
}
