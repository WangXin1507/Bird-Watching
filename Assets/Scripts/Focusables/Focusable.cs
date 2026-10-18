using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;



public class Focusable : MonoBehaviour, IFocusable
{
    [SerializeField] private List<ATranscriptEntry> requiredTranscripts = new List<ATranscriptEntry>();

    // Highlight behavior
    [SerializeField] private Material highlightMaterial;
    private Renderer[] renderers;
    private List<Material> dynamicMaterials = new List<Material>(); // per renderer
    private static readonly int OutlineIntensityID = Shader.PropertyToID("_OutlineIntensity");
    private static readonly int BaseTextureID = Shader.PropertyToID("_BaseTexture");
    private float highlightStrength = 1.0f;

    private void Awake()
    {
        if (!highlightMaterial) return;
        renderers = GetComponentsInChildren<Renderer>();
        SetupDynamicMaterials();
        SetOutlineIntensity(0f);
    }

    private void SetupDynamicMaterials()
    {
        foreach (var rend in renderers)
        {
            Material originalMat = rend.sharedMaterial;
            if (originalMat != null)
            {
                Material dynamicMat = new Material(highlightMaterial);

                // Get the original texture
                Texture tex = null;
                if (originalMat.HasTexture("_BaseColorMap"))
                    tex = originalMat.GetTexture("_BaseColorMap");
                else if (originalMat.HasTexture("_MainTex"))
                    tex = originalMat.GetTexture("_MainTex");

                // If there is no default texture (color only material), then make it a texture
                if (tex == null && originalMat.HasColor("_BaseColor"))
                {
                    Color solidColor = originalMat.GetColor("_BaseColor");

                    Texture2D colorTex = new Texture2D(1, 1);
                    colorTex.SetPixel(0, 0, solidColor);
                    colorTex.Apply();
                    tex = colorTex;
                }

                // Give the original texture to the highlight material
                if (tex != null)
                {
                    dynamicMat.SetTexture(BaseTextureID, tex);
                }

                rend.material = dynamicMat;
                dynamicMaterials.Add(rend.material);
            }
        }
    }

    public bool CanFocus()
    {
        bool canFocus = true;
        foreach (ATranscriptEntry requiredTranscript in requiredTranscripts)
        {
            canFocus = canFocus && Journal.Instance.IsTranscriptUnlocked(requiredTranscript);
        }
        return canFocus;
    }
    
    public void LoseFocus()
    {
        if (!highlightMaterial) return;

        SetOutlineIntensity(0f);
    }

    public void GainFocus()
    {
        if (!highlightMaterial) return;

        SetOutlineIntensity(highlightStrength);
    }

    private void SetOutlineIntensity(float intensity)
    {
        foreach (var mat in dynamicMaterials)
        {
            mat.SetFloat(OutlineIntensityID, intensity);
        }
    }
}
