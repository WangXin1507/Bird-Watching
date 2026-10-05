using UnityEngine;

public interface IFocusable
{
    bool enabled { get; set; }

    void GainFocus();
    void LoseFocus();
}
