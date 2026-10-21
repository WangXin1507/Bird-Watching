using UnityEngine;
using UnityEngine.Splines;

public class MainMenu : UIElement
{
    /// <summary>
    /// Name of the play scene
    /// </summary>
    public string PlayScene;
    public FadeStyle fadeOutStyle;
    public FadeStyle fadeInStyle;

    [Header("References")]
    public GameObject birb;
    public SplineContainer spline;

    public void Play()
    {
        Hide();
        ScreenFader.LoadScene(PlayScene, fadeOutStyle, fadeInStyle).Forget();
    }

    public void Quit()
    {
        Hide();
        Application.Quit();
    }
}
