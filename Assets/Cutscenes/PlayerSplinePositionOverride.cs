using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Splines;

[DefaultExecutionOrder(10)]
public class PlayerSplinePositionOverride : MonoBehaviour
{
    public SplineContainer spline;

    int runId;
    bool active;
    bool savedMove;
    bool savedLook;
    bool savedInteract;

    /// <summary>
    /// Disable player position control and interaction inputs.
    /// </summary>
    /// <param name="duration">Seconds to travel the spline from start to end.</param>
    /// <param name="disableFreeLook">If player should still be allowed to rotate camera</param>
    public void BeginOverride(float duration, bool disableFreeLook)
    {
        if (InputManager.Instance == null)
        {
            Debug.LogError("Input Manager not set or initialized", this);
            return;
        }

        if (spline == null)
        {
            Debug.LogError("Spline is not assigned on PlayerSplinePositionOverride.", this);
            return;
        }

        if (PlayerID.playerMovement == null)
        {
            Debug.LogError("Player movement is not initialized.", this);
            return;
        }

        var input = InputManager.Instance;
        if (!active)
        {
            savedMove = input.moveInputEnabled;
            savedLook = input.lookInputEnabled;
            savedInteract = input.interactionInputEnabled;
            active = true;
            input.movementVector = Vector2.zero;
            PlayerID.playerMovement.SetControlSuspended(true);
        }

        input.LockInputs(true, !disableFreeLook, true);
        FollowSpline(++runId, Mathf.Max(0f, duration)).Forget();
    }

    async UniTaskVoid FollowSpline(int id, float duration)
    {
        try
        {
            float elapsed = 0f;
            var token = this.GetCancellationTokenOnDestroy();

            while (id == runId)
            {
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                PlaceOnSpline(t);
                if (t >= 1f) break;

                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, token);
                elapsed += Time.fixedDeltaTime;
            }
        }
        finally
        {
            if (id == runId) EndOverride();
        }
    }

    void PlaceOnSpline(float t)
    {
        var movement = PlayerID.playerMovement;
        if (movement == null || spline == null) return;

        var position = spline.EvaluatePosition(t);
        var tangent = spline.EvaluateTangent(t);
        var up = spline.EvaluateUpVector(t);

        Quaternion rotation = movement.transform.rotation;
        var forward = new Vector3(tangent.x, tangent.y, tangent.z);
        if (forward.sqrMagnitude > 0.0001f)
        {
            var upVector = new Vector3(up.x, up.y, up.z);
            if (upVector.sqrMagnitude < 0.0001f) upVector = Vector3.up;
            rotation = Quaternion.LookRotation(forward.normalized, upVector);
        }

        movement.Place(new Vector3(position.x, position.y, position.z), rotation);
    }

    public void EndOverride()
    {
        runId++;
        if (!active) return;

        active = false;
        if (PlayerID.playerMovement != null)
            PlayerID.playerMovement.SetControlSuspended(false);

        if (InputManager.Instance != null)
            InputManager.Instance.LockInputs(!savedMove, !savedLook, !savedInteract);
    }

    void OnDisable()
    {
        EndOverride();
    }
}
