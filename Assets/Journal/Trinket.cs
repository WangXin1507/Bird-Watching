using System.Threading;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Mission reward that sits in the scene from the start, hidden by the reveal shader (_progress = 0).
/// Reveal() renders it bottom to top; once done it switches to its normal materials, hands them to
/// Focusable for the highlight, and turns collision, physics and interaction on.
/// Runs before Focusable so Focusable never swaps the materials at Awake.
/// </summary>
[DefaultExecutionOrder(-100)]
public class Trinket : SerializedMonoBehaviour
{
    [Title("Reveal")]
    [Tooltip("Material made from the 'Trinket Mat' shader graph. Each renderer gets its own copy, " +
             "with the renderer's own texture copied into _BaseColorMap.")]
    [SerializeField] private Material revealMaterial;
    [SerializeField, Min(0.05f)] private float revealDuration = 2.5f;
    [SerializeField] private AnimationCurve revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Title("Cutscene")]
    [SerializeField, Min(0f)] private float fadeDuration = 0.6f;
    [Tooltip("Seconds the camera stays on the trinket after it has fully appeared.")]
    [SerializeField, Min(0f)] private float holdAfterReveal = 1.2f;

    public float FadeDuration => fadeDuration;
    public float HoldAfterReveal => holdAfterReveal;
    public bool IsRevealed { get; private set; }

    private static readonly int ProgressID = Shader.PropertyToID("_progress");
    private static readonly int RevealBaseMapID = Shader.PropertyToID("_BaseColorMap");
    private static readonly int RevealBaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly string[] SourceTextureNames = { "_BaseColorMap", "_DiffuseColorMap", "_MainTex", "_BaseMap" };

    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Material[][] revealInstances;
    private Collider[] colliders;
    private Rigidbody body;
    private bool bodyWasKinematic;
    private Focusable focusable;
    private MaterialPropertyBlock block;
    private bool initialized;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;

        block = new MaterialPropertyBlock();
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        body = GetComponent<Rigidbody>();
        focusable = GetComponent<Focusable>();
        if (!focusable) focusable = GetComponentInChildren<Focusable>(true);

        // Focusable must not replace the materials before the reveal; we call RefreshMaterials() at the end.
        if (focusable) focusable.SetupMaterialsOnAwake = false;

        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
            originalMaterials[i] = renderers[i].sharedMaterials;

        Hide();
    }

    /// <summary>Hidden state: reveal material at _progress 0, no collision, no physics, no interaction.</summary>
    private void Hide()
    {
        IsRevealed = false;
        SetInteractable(false);

        if (!revealMaterial)
        {
            Debug.LogWarning($"[Trinket] '{name}' has no reveal material; it will pop in instead of fading.", this);
            foreach (Renderer rend in renderers) rend.enabled = false;
            return;
        }

        revealInstances = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] originals = originalMaterials[i];
            Material[] instances = new Material[originals.Length];
            for (int m = 0; m < originals.Length; m++)
                instances[m] = CreateRevealInstance(originals[m]);

            revealInstances[i] = instances;
            renderers[i].sharedMaterials = instances;
        }

        SetProgress(0f);
    }

    private Material CreateRevealInstance(Material source)
    {
        Material instance = new Material(revealMaterial) { name = $"{revealMaterial.name} ({name})" };
        // Make sure HDRP's alpha-clip keywords/passes match the material settings (_AlphaCutoffEnable).
        HDMaterial.ValidateMaterial(instance);
        if (!source) return instance;

        foreach (string textureName in SourceTextureNames)
        {
            if (source.HasTexture(textureName) && source.GetTexture(textureName))
            {
                instance.SetTexture(RevealBaseMapID, source.GetTexture(textureName));
                break;
            }
        }

        if (source.HasColor("_BaseColor"))
            instance.SetColor(RevealBaseColorID, source.GetColor("_BaseColor"));
        else if (source.HasColor("_Color"))
            instance.SetColor(RevealBaseColorID, source.GetColor("_Color"));

        return instance;
    }

    /// <summary>Plays the bottom-to-top reveal, then switches to the final, interactable state.</summary>
    public async UniTask Reveal(CancellationToken cancellationToken = default)
    {
        Initialize();
        if (IsRevealed) return;

        if (revealMaterial)
        {
            float elapsed = 0f;
            while (elapsed < revealDuration)
            {
                cancellationToken.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                SetProgress(revealCurve.Evaluate(Mathf.Clamp01(elapsed / revealDuration)));
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            SetProgress(1f);
        }

        ShowRevealedInstant();
    }

    /// <summary>Final state with no animation: normal materials + highlight, collision and interaction on.</summary>
    public void ShowRevealedInstant()
    {
        Initialize();
        if (IsRevealed) return;
        IsRevealed = true;

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer rend = renderers[i];
            rend.SetPropertyBlock(null);
            rend.sharedMaterials = originalMaterials[i];
            rend.enabled = true;
        }
        DestroyRevealInstances();

        // Focusable reads each renderer's original texture and swaps in its highlight material instances.
        if (focusable) focusable.RefreshMaterials();

        SetInteractable(true);
    }

    private void SetProgress(float progress)
    {
        foreach (Renderer rend in renderers)
        {
            rend.GetPropertyBlock(block);
            block.SetFloat(ProgressID, progress);
            rend.SetPropertyBlock(block);
        }
    }

    private void SetInteractable(bool interactable)
    {
        foreach (Collider col in colliders)
            col.enabled = interactable;

        if (body)
        {
            if (!interactable)
            {
                bodyWasKinematic = body.isKinematic;
                body.isKinematic = true; // colliders are off, so keep it from falling through the floor
            }
            else
            {
                body.isKinematic = bodyWasKinematic;
            }
        }

        if (focusable) focusable.enabled = interactable;
    }

    private void DestroyRevealInstances()
    {
        if (revealInstances == null) return;
        foreach (Material[] instances in revealInstances)
            foreach (Material instance in instances)
                if (instance) Destroy(instance);
        revealInstances = null;
    }

    private void OnDestroy()
    {
        DestroyRevealInstances();
    }
}
