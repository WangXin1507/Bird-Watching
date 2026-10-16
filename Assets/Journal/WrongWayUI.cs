using System.Collections;
using UnityEngine;
using UnityEngine.Splines.Interpolators;
using UnityEngine.UI;

/// <summary>
/// "Journal Updated" popup. Singleton: call JournalUpdateUI.Instance.Show() (or JournalUpdateUI.ShowPopup()) from anywhere.
/// Slides from hiddenPos -> shownPos, waits, then slides back.
/// Calling Show() again while it's visible just resets the hold timer (no re-slide/jitter).
/// </summary>
public class WrongWayUI : MonoBehaviour
{
    public static WrongWayUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Image image;          // the thing that moves (defaults to this object)

    [Header("Timing")]
    [SerializeField] private float slideDuration = 0.35f;
    [SerializeField] private float defaultHoldTime = 2f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine routine;
    private float holdTimer;
    private bool isHolding;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Static convenience so callers don't need a null check.</summary>
    public static void ShowPopup(float holdTime = -1f)
    {
        if (Instance != null) Instance.Show(holdTime);
        else Debug.LogWarning("WrongWayUI: no instance in scene.");
    }

    /// <summary>Slide in, stay for holdTime seconds (default if &lt; 0), slide out.</summary>
    public void Show(float holdTime = -1f)
    {
        float hold = holdTime < 0f ? defaultHoldTime : holdTime;

        // Already sitting at the shown position: just extend the timer.
        if (isHolding)
        {
            holdTimer = hold;
            return;
        }

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(ShowRoutine(hold));
    }

    private IEnumerator ShowRoutine(float hold)
    {
        // Start from wherever it currently is (handles being called mid slide-out)
        yield return Slide(true);

        isHolding = true;
        holdTimer = hold;
        while (holdTimer > 0f)
        {
            holdTimer -= Time.deltaTime;
            yield return null;
        }
        isHolding = false;

        yield return Slide(false);
        routine = null;
    }

    private IEnumerator Slide(bool show)
    {
        float dur = slideDuration;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime / dur;
            float alpha = 0;
            if (show)
                alpha = Mathf.Lerp(0, 1, ease.Evaluate(Mathf.Clamp01(t / dur)));
            else
                alpha = Mathf.Lerp(1, 0, ease.Evaluate(Mathf.Clamp01(t / dur)));
            image.color = new Color(image.color.a, image.color.g, image.color.b, alpha);
            yield return null;
        }
        if (show)
            image.color = new Color(image.color.a, image.color.g, image.color.b, 1f);
        else
            image.color = new Color(image.color.a, image.color.g, image.color.b, 0f);
    }
}