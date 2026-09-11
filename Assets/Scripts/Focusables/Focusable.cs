using UnityEngine;

public class Focusable : MonoBehaviour, IFocusable
{
    public void LoseFocus()
    {
        Debug.Log("Yay I'm focused");
    }

    public void GainFocus()
    {
        Debug.Log("I'm not focused");
    }
}
