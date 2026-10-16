using System.Collections;
using UnityEngine;

/// <summary>
/// "Journal Updated" popup. Singleton: call JournalUpdateUI.Instance.Show() (or JournalUpdateUI.ShowPopup()) from anywhere.
/// Slides from hiddenPos -> shownPos, waits, then slides back.
/// Calling Show() again while it's visible just resets the hold timer (no re-slide/jitter).
/// </summary>
public class JournalUpdateUI : MonoBehaviour
{
    public static JournalUpdateUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private RectTransform panel;          // the thing that moves (defaults to this object)

    [Header("Positions (anchoredPosition)")]
    [SerializeField] private Vector2 shownPos;
    [SerializeField] private Vector2 hiddenPos;

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
        // DontDestroyOnLoad(transform.root.gameObject); // uncomment if the canvas should persist across scenes

        if (panel == null) panel = (RectTransform)transform;
        panel.anchoredPosition = hiddenPos;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Static convenience so callers don't need a null check.</summary>
    public static void ShowPopup(float holdTime = -1f)
    {
        if (Instance != null) Instance.Show(holdTime);
        else Debug.LogWarning("JournalUpdateUI: no instance in scene.");
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

    /// <summary>Hide early (e.g. player opened the journal).</summary>
    public void HideNow()
    {
        if (routine != null) StopCoroutine(routine);
        isHolding = false;
        routine = StartCoroutine(Slide(panel.anchoredPosition, hiddenPos));
    }

    private IEnumerator ShowRoutine(float hold)
    {
        // Start from wherever it currently is (handles being called mid slide-out)
        yield return Slide(panel.anchoredPosition, shownPos);

        isHolding = true;
        holdTimer = hold;
        while (holdTimer > 0f)
        {
            holdTimer -= Time.deltaTime;
            yield return null;
        }
        isHolding = false;

        yield return Slide(shownPos, hiddenPos);
        routine = null;
    }

    private IEnumerator Slide(Vector2 from, Vector2 to)
    {
        // Scale duration by remaining distance so interrupted slides keep a consistent speed
        float fullDist = Vector2.Distance(shownPos, hiddenPos);
        float dur = fullDist > 0.01f ? slideDuration * (Vector2.Distance(from, to) / fullDist) : 0f;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime / dur;
            panel.anchoredPosition = Vector2.LerpUnclamped(from, to, ease.Evaluate(Mathf.Clamp01(t / dur)));
            yield return null;
        }
        panel.anchoredPosition = to;
    }
}