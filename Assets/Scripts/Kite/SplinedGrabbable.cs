using System.Reflection;
using UnityEditor.UI;
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

        Journal.Instance.OnTranscriptUnlocked += CheckKiteRequirements;
    }

    // Check if all the required transcipts are unlocked and enable the spline follower
    private void CheckKiteRequirements()
    {
        bool unlock = true;
        foreach (ATranscriptEntry transcript in requiredTranscripts)
        {
            if (!Journal.Instance.IsTranscriptUnlocked(transcript))
            {
                unlock = false;
            }
        }

        if (unlock && _splineFollower.enabled == false)
        {
            Debug.Log("Unlocked SplineFollower");
            _splineFollower.enabled = true;
            Journal.Instance.OnTranscriptUnlocked -= CheckKiteRequirements;
        }
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
