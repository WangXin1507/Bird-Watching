using Cysharp.Threading.Tasks;
using PrimeTween;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class UIElement : MonoBehaviour
{
    public bool showOnAwake;

    CanvasGroup canvasGroup;
    int fadeId;

    public void Show(float fadeDuration = 0)
    {
        Fade(1f, fadeDuration).Forget();
    }

    public void Hide(float fadeDuration = 0)
    {
        Fade(0f, fadeDuration).Forget();
    }

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (showOnAwake)
        {
            ApplyImmediate(true);
        }
    }

    async UniTaskVoid Fade(float targetAlpha, float duration)
    {
        int id = ++fadeId;
        Tween.StopAll(onTarget: canvasGroup);

        bool show = targetAlpha > 0f;
        if (show) SetInteractable(true);

        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
        }
        else
        {
            await Tween.Alpha(canvasGroup, targetAlpha, duration, useUnscaledTime: true);
            if (id != fadeId) return;
        }

        if (!show) SetInteractable(false);
    }

    void ApplyImmediate(bool show)
    {
        Tween.StopAll(onTarget: canvasGroup);
        canvasGroup.alpha = show ? 1f : 0f;
        SetInteractable(show);
    }

    void SetInteractable(bool enabled)
    {
        canvasGroup.interactable = enabled;
        canvasGroup.blocksRaycasts = enabled;
    }
}
