using UnityEngine;

public class FlightSounds : MonoBehaviour
{
    int flaps = 0;

    public void TakeOff()
    {
        if (GetComponent<Animator>().GetFloat("Speed") <= 0.5f) return;
        AudioManager.PlayOneShot(AudioEvents.Bird.Bird_Takeoff_SFX, transform.position);
    }

    public void Flap()
    {
        if (PlayerID.playerMovement.VerticalSpeed < 0)
        {
            AudioManager.PlayOneShot(AudioEvents.Bird.Bird_Flap_SFX, transform.position);
        }
        else
        {
            // Cycle between flap with and without wind to keep it going

            if (flaps % 2 == 0)
            {
                AudioManager.PlayOneShot(AudioEvents.Bird.Bird_Flap_SFX, transform.position);
            }
            else
            {
                AudioManager.PlayOneShot(AudioEvents.Bird.Bird_Flap_SFX, transform.position);
                AudioManager.PlayOneShot(AudioEvents.Bird.Bird_Flight_SFX, transform.position);
            }
            flaps++;
        }
    }
}
