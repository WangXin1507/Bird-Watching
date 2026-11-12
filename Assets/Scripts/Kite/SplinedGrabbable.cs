using System.Reflection;
using UnityEngine;

public class SplinedGrabbable : Focusable, IGrabbable
{
    [SerializeField] private MissionData missionCompleteOnGrab;
    [SerializeField] private Transform _grabHandle;
    private Collider _collider;
    private SplineFollower _splineFollower;
    public Transform grabHandle => _grabHandle;

    private void Start()
    {
        _splineFollower = GetComponent<SplineFollower>();
        if (!_splineFollower) Debug.Log("No spline follower on SplinedGrabbable object");

        _collider = GetComponent<Collider>();
    }

    public Transform Grab()
    {
        if (_splineFollower.enabled)
        {
            if (_collider)
            {
                // Go from only focus to everything to avoid collision while following
                // if excluded in editor
                _collider.excludeLayers = 0;
            }

            Debug.Log("Completed mission");
            Journal.Instance.CompleteMission(missionCompleteOnGrab);

            _splineFollower.enabled = false;
        }
        return transform;
    }
}
