using BirdWatchingCamera;
using UnityEngine;

public class PlayerID
{
    public static CameraLook cameraLook;
    public static PlayerMovement playerMovement;
    public static PlayerInteraction playerInteraction;

    private static bool paused = false;
    private static Vector3 velocity = Vector3.zero;
    
    public static void LockMovement()
    {
        //Time.timeScale = 0;
        playerMovement.enabled = false;
        playerInteraction.enabled = false;
        paused = true;
        velocity = playerMovement.GetComponent<Rigidbody>().linearVelocity;
        playerMovement.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
    }

    public static void UnlockMovement()
    {
        //Time.timeScale = 1;
        playerMovement.enabled = true;
        playerInteraction.enabled = true;
        if (paused)
        {
            playerMovement.GetComponent<Rigidbody>().linearVelocity = velocity;
        }
        paused = false;
    }
}