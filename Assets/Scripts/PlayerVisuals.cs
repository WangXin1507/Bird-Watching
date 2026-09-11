using UnityEngine;

public class PlayerVisuals : MonoBehaviour
{
    [SerializeField] private GameObject flyingModel;
    [SerializeField] private GameObject groundModel;
    
    private void Update()
    {
        if (PlayerID.playerMovement.GetState() == PlayerMovement.MovementState.Flying)
        {
            flyingModel.SetActive(true);
            groundModel.SetActive(false);
        }
        else
        {
            flyingModel.SetActive(false);
            groundModel.SetActive(true);
        }
    }
}