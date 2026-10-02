using UnityEngine;
using UnityEngine.Splines;

public class PlayerSplinePositionOverride : MonoBehaviour
{
    public SplineContainer spline;

    /// <summary>
    /// Disable player position control and interaction inputs.
    /// </summary>
    /// <param name="disableFreeLook">If player should still be allowed to rotate camera</param>
    public void BeginOverride(bool disableFreeLook)
    {
        if (PlayerID.inputManager == null)
        {
            Debug.LogError("Player not set or initialized");
            return;
        }

        PlayerID.inputManager.LockInputs(false, disableFreeLook, false);


    }
}
