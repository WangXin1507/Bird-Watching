using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButton : UIElement, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Hover")]
    public float hoverScale = 1.1f;
    public float hoverDuration = 0.15f;
    public Ease hoverEase = Ease.OutQuad;

    protected Button button;
    Vector3 baseScale;

    protected virtual void Start()
    {
        button = GetComponent<Button>();
        baseScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.IsInteractable()) return;
        ScaleTo(baseScale * hoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ScaleTo(baseScale);
    }

    protected virtual void OnDisable()
    {
        Tween.StopAll(onTarget: transform);
        if (button != null) transform.localScale = baseScale;
    }

    void ScaleTo(Vector3 target)
    {
        Tween.StopAll(onTarget: transform);
        Tween.Scale(transform, target, hoverDuration, hoverEase, useUnscaledTime: true);
    }
}
