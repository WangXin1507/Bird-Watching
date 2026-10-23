using PrimeTween;
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

    [Header("Birb Fly Away")]
    public float flyAwayDuration = 2f;
    public Ease flyAwayEase = Ease.InOutSine;

    [Header("References")]
    public GameObject birb;
    public SplineContainer splineContainer;

    public void Play()
    {
        Hide();
        var knot = splineContainer.Spline[0];
        knot.Position = splineContainer.transform.InverseTransformPoint(birb.transform.position);
        splineContainer.Spline[0] = knot;

        float knot1T = splineContainer.Spline.CurveToSplineT(1f);
        Tween.Custom(this, 0f, knot1T, flyAwayDuration,
            (menu, t) => menu.birb.transform.position = menu.splineContainer.EvaluatePosition(t),
            flyAwayEase);

        ScreenFader.LoadScene(PlayScene, fadeOutStyle, fadeInStyle).Forget();
    }

    public void Quit()
    {
        Hide();
        Application.Quit();
    }
}
