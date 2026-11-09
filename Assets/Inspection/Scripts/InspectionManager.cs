using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UI;

/// <summary>
/// Spawns an InspectableData's prefab on a stage far outside the world, renders it with a
/// dedicated camera into a RenderTexture, shows that texture on a UI panel and lets the
/// player rotate the object with the look input.
/// </summary>
public class InspectionManager : MonoBehaviour
{
    public static InspectionManager Instance { get; private set; }

    [Header("Stage")]
    [Tooltip("Layer used only by inspected objects. The inspection camera renders only this layer. " +
             "Create a layer for it (e.g. 'Inspection') and remove it from the main camera's culling mask.")]
    [SerializeField] private int inspectionLayer = 31;

    [Header("Camera")]
    [SerializeField] private Camera inspectionCamera;
    [SerializeField] private Vector2Int renderSize = new Vector2Int(1024, 1024);
    //[SerializeField, Range(5f, 90f)] private float fieldOfView = 30f;
    [Tooltip("Extra room around the object's bounding sphere.")]
    [SerializeField, Min(1f)] private float framingPadding = 1.1f;
    [Tooltip("Alpha 0 gives a transparent background if the HDRP asset's color buffer format has alpha (R16G16B16A16).")]
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0f);

    [Header("UI")]
    [SerializeField] private Canvas panelRoot;
    [SerializeField] private RawImage renderImage;

    [Header("Rotation")]
    [Tooltip("Degrees per pixel of mouse movement.")]
    [SerializeField] private float mouseSensitivity = 0.25f;
    [Tooltip("Degrees per second at full stick deflection.")]
    [SerializeField] private float stickSpeed = 180f;
    [SerializeField] private bool invertY;

    [Header("Behaviour")]
    [Tooltip("Pressing Interact (E / gamepad West) again closes the menu.")]
    [SerializeField] private bool closeOnInteract = true;
    [Tooltip("Pressing Pause closes the menu (the pause menu ignores that press while inspecting).")]
    [SerializeField] private bool closeOnPause = true;
    [Tooltip("Set Time.timeScale to 0 while inspecting so the world (and the bird's physics) freezes.")]
    [SerializeField] private bool pauseWorld = true;
    [Tooltip("Show the cursor while open (e.g. for a close button). Mouse movement still rotates the object.")]
    [SerializeField] private bool unlockCursorWhileOpen;

    [Header("GameObjects")]
    [SerializeField] private Transform stageRoot;
    [SerializeField] private Transform pivot;
    [SerializeField] private Transform holdingArea;
    
    public bool IsOpen { get; private set; }
    public InspectableData Current { get; private set; }

    public event Action<InspectableData> Opened;
    public event Action Closed;
    
    private GameObject currentInstance;
    private RenderTexture renderTexture;

    private bool prevMoveEnabled, prevLookEnabled, prevInteractEnabled;
    private CursorLockMode prevCursorLock;
    private bool prevCursorVisible;
    private float prevTimeScale = 1f;
    private bool prevPlayerMovementEnabled, prevPlayerInteractionEnabled, prevCameraLookEnabled;
    private int openedFrame = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        BuildRenderTexture();
        SetupCamera();
        
        if (panelRoot) panelRoot.enabled = false;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        if (IsOpen) Close();

        if (renderTexture != null)
        {
            if (inspectionCamera) inspectionCamera.targetTexture = null;
            renderTexture.Release();
            Destroy(renderTexture);
        }
        if (stageRoot) Destroy(stageRoot.gameObject);

        Instance = null;
    }

    // ---------------------------------------------------------------- public API

    public void Open(InspectableData data)
    {
        if (data == null || data.prefab == null)
        {
            Debug.LogWarning("InspectionManager: InspectableData or its prefab is missing.", this);
            return;
        }

        if (IsOpen)
        {
            // Already open (e.g. swapping objects): keep the state saved on the first open,
            // otherwise we'd save the *locked* state and never unlock the bird.
            DestroyInstance();
        }
        else
        {
            EnterInspectState();
        }

        // The press that opened the menu is still "this frame"; don't let it close it again.
        openedFrame = Time.frameCount;
        Current = data;
        SpawnInstance(data);
        FrameCamera(data);

        inspectionCamera.enabled = true;
        if (panelRoot) panelRoot.enabled = true;

        IsOpen = true;
        Opened?.Invoke(data);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;

        inspectionCamera.enabled = false;
        if (panelRoot) panelRoot.enabled = false;
        DestroyInstance();
        ExitInspectState();

        Current = null;
        Closed?.Invoke();
    }

    public void Toggle(InspectableData data)
    {
        if (IsOpen && Current == data) Close();
        else Open(data);
    }

    // ---------------------------------------------------------------- rotation

    private void Update()
    {
        if (!IsOpen || pivot == null || InputManager.Instance == null) return;

        InputManager input = InputManager.Instance;

        // Polled here (after this frame's input callbacks) rather than via events:
        // InteractClicked is blocked while interaction is locked, and closing inside the
        // callback would unlock input mid-dispatch so the same E press re-opens the menu.
        if (Time.frameCount != openedFrame && input.Actions != null)
        {
            var player = input.Actions.Player;
            if ((closeOnInteract && player.Interact.WasPressedThisFrame()) ||
                (closeOnPause && player.Pause.WasPressedThisFrame()))
            {
                Close();
                return;
            }
        }

        // Mouse delta is already "per frame"; stick values are a rate and need deltaTime.
        // Unscaled so it still works if the game pauses time while the menu is open.
        float scale = input.lookIsPointerDelta ? mouseSensitivity : stickSpeed * Time.unscaledDeltaTime;
        Vector2 look = input.rawLookVector * scale;
        if (invertY) look.y = -look.y;
        if (look.sqrMagnitude < 0.000001f) return;

        // Rotate around the camera's axes so dragging always feels screen-relative.
        Transform cam = inspectionCamera.transform;
        pivot.Rotate(cam.up, -look.x, Space.World);
        pivot.Rotate(cam.right, look.y, Space.World);
    }

    // ---------------------------------------------------------------- setup

    private void BuildRenderTexture()
    {
        renderTexture = new RenderTexture(renderSize.x, renderSize.y, 24, RenderTextureFormat.ARGBHalf)
        {
            name = "InspectionRT"
        };
        renderTexture.Create();

        if (renderImage) renderImage.texture = renderTexture;
    }

    private void SetupCamera()
    {
        inspectionCamera.clearFlags = CameraClearFlags.SolidColor;
        inspectionCamera.backgroundColor = backgroundColor;
        inspectionCamera.targetTexture = renderTexture;

        var hd = inspectionCamera.GetComponent<HDAdditionalCameraData>();
        if (hd == null) hd = inspectionCamera.gameObject.AddComponent<HDAdditionalCameraData>();
        hd.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
        hd.backgroundColorHDR = backgroundColor;
        hd.antialiasing = HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
    }

    // ---------------------------------------------------------------- spawning

    private void SpawnInstance(InspectableData data)
    {
        pivot.localRotation = Quaternion.identity;

        // Spawn under an inactive parent so we can strip gameplay components before
        // OnEnable/Start run (e.g. Interactable.Start would reset the layer).
        currentInstance = Instantiate(data.prefab, holdingArea);
        currentInstance.name = data.prefab.name + " (Inspect)";

        // Keep the inspected object's own animation running while the world is frozen.
        if (pauseWorld)
            foreach (var anim in currentInstance.GetComponentsInChildren<Animator>(true))
                anim.updateMode = AnimatorUpdateMode.UnscaledTime;

        currentInstance.transform.SetParent(pivot, false);
        currentInstance.transform.localPosition = Vector3.zero;
        currentInstance.transform.localRotation = Quaternion.identity;
        SetLayerRecursively(currentInstance.transform, inspectionLayer);

        // Centre the visual bounds on the pivot so it spins around its middle.
        if (TryGetBounds(currentInstance, out Bounds bounds))
            currentInstance.transform.position += pivot.position - bounds.center;

        pivot.localRotation = Quaternion.Euler(data.initialRotation);
    }

    private void DestroyInstance()
    {
        if (currentInstance) Destroy(currentInstance);
        currentInstance = null;
    }

    private void FrameCamera(InspectableData data)
    {
        float radius = 0.5f;
        if (currentInstance && TryGetBounds(currentInstance, out Bounds bounds))
            radius = Mathf.Max(bounds.extents.magnitude, 0.01f); // bounding sphere: never clips while rotating

        float aspect = renderSize.x / (float)renderSize.y;
        float halfV = inspectionCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float halfH = Mathf.Atan(Mathf.Tan(halfV) * aspect);
        float halfFov = Mathf.Min(halfV, halfH);

        float distance = radius / Mathf.Sin(halfFov) * framingPadding * data.framingDistance;

        Transform cam = inspectionCamera.transform;
        cam.SetPositionAndRotation(pivot.position - Vector3.forward * distance, Quaternion.identity);
        inspectionCamera.nearClipPlane = Mathf.Max(0.01f, distance - radius * 1.5f);
        inspectionCamera.farClipPlane = distance + radius * 1.5f;
    }

    private static bool TryGetBounds(GameObject go, out Bounds bounds)
    {
        bounds = default;
        bool found = false;
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled || r is ParticleSystemRenderer) continue;
            if (!found) { bounds = r.bounds; found = true; }
            else bounds.Encapsulate(r.bounds);
        }
        return found;
    }

    private static void SetLayerRecursively(Transform t, int layer)
    {
        t.gameObject.layer = layer;
        for (int i = 0; i < t.childCount; i++)
            SetLayerRecursively(t.GetChild(i), layer);
    }

    // ---------------------------------------------------------------- input

    private void EnterInspectState()
    {
        prevCursorLock = Cursor.lockState;
        prevCursorVisible = Cursor.visible;

        InputManager input = InputManager.Instance;
        if (input != null)
        {
            prevMoveEnabled = input.moveInputEnabled;
            prevLookEnabled = input.lookInputEnabled;
            prevInteractEnabled = input.interactionInputEnabled;
            // Lock look too so the player camera stays still; we read rawLookVector instead.
            input.LockInputs(true, true, true);
        }

        // Zeroed input alone doesn't stop the bird: its rigidbody keeps its momentum.
        // Disable the player scripts outright (same idea as PlayerID.LockMovement).
        if (PlayerID.playerMovement)
        {
            prevPlayerMovementEnabled = PlayerID.playerMovement.enabled;
            PlayerID.playerMovement.enabled = false;
        }
        if (PlayerID.playerInteraction)
        {
            prevPlayerInteractionEnabled = PlayerID.playerInteraction.enabled;
            PlayerID.playerInteraction.enabled = false;
        }
        if ((object)PlayerID.cameraLook is Behaviour cameraLook && cameraLook)
        {
            prevCameraLookEnabled = cameraLook.enabled;
            cameraLook.enabled = false;
        }

        if (pauseWorld)
        {
            prevTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        if (unlockCursorWhileOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void ExitInspectState()
    {
        if (pauseWorld)
            Time.timeScale = prevTimeScale > 0f ? prevTimeScale : 1f;

        if (PlayerID.playerMovement) PlayerID.playerMovement.enabled = prevPlayerMovementEnabled;
        if (PlayerID.playerInteraction) PlayerID.playerInteraction.enabled = prevPlayerInteractionEnabled;
        if ((object)PlayerID.cameraLook is Behaviour cameraLook && cameraLook)
            cameraLook.enabled = prevCameraLookEnabled;

        InputManager input = InputManager.Instance;
        if (input != null)
            input.LockInputs(!prevMoveEnabled, !prevLookEnabled, !prevInteractEnabled);

        Cursor.lockState = prevCursorLock;
        Cursor.visible = prevCursorVisible;
    }
}
