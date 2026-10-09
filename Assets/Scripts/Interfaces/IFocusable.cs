using UnityEngine;

public interface IFocusable
{
    bool enabled { get; set; }

    bool CanFocus();
    void GainFocus();
    void LoseFocus();
}
