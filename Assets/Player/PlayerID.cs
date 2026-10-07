using BirdWatchingCamera;
using UnityEngine;

public class PlayerID
{
    public static CameraLook cameraLook;
    public static PlayerMovement playerMovement;
    public static PlayerInteraction playerInteraction;

    public static void LockMovement()
    {
        Time.timeScale = 0;
        playerMovement.enabled = false;
        playerInteraction.enabled = false;
    }

    public static void UnlockMovement()
    {
        Time.timeScale = 1;
        playerMovement.enabled = true;
        playerInteraction.enabled = true;
    }
}