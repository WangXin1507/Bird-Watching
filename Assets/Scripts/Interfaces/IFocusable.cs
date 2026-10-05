using UnityEngine;

public interface IFocusable
{
    bool CanFocus();
    void GainFocus();
    void LoseFocus();
}
