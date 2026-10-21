using Cysharp.Threading.Tasks;
using PrimeTween;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
public struct FadeStyle
{
    public float duration;
    public float delay;
    public Ease easeStyle;

    public FadeStyle(float duration, float delay = 0, Ease easeStyle = Ease.Linear)
    {
        this.duration = duration;
        this.delay = delay;
        this.easeStyle = easeStyle;
    }
}

public static class ScreenFader
{
    public static CanvasGroup fadeCanvas;

    private static Tween t;

    public static async UniTaskVoid LoadScene(string sceneName, FadeStyle fadeOutStyle, FadeStyle fadeInStyle)
    {
        await FadeOut(fadeOutStyle);
        await SceneManager.LoadSceneAsync(sceneName);
        //await UniTask.NextFrame();
        //await UniTask.WaitUntil(() => Time.unscaledDeltaTime < 0.1f);
        await FadeIn(fadeInStyle);
    }

    public static void SetAlpha(float a)
    {
        t.Stop();
        fadeCanvas.alpha = a;
    }

    public static async UniTask FadeIn(FadeStyle fade)
    {
        SpawnCanvas();
        t.Stop();
        t = Tween.Alpha(fadeCanvas, endValue: 0, fade.duration, startDelay: fade.delay, ease: fade.easeStyle, useUnscaledTime: true);
        await t;
    }

    public static async UniTask FadeOut(FadeStyle fade)
    {
        SpawnCanvas();
        t.Stop();
        t = Tween.Alpha(fadeCanvas, endValue: 1, fade.duration, startDelay: fade.delay, ease: fade.easeStyle, useUnscaledTime: true);
        await t;
    }

    private static void SpawnCanvas()
    {
        if (fadeCanvas != null)
        {
            return;
        }

        fadeCanvas = new GameObject().AddComponent<CanvasGroup>();
        fadeCanvas.alpha = 0;
        var canvas = fadeCanvas.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        UnityEngine.Object.DontDestroyOnLoad(fadeCanvas);

        var img = fadeCanvas.gameObject.AddComponent<Image>();
        img.color = new(0, 0, 0);
    }
}
