using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds the blockout Animator for PlayerVisuals: two mesh-swap clips and a controller holding the
/// nested ground/air blend tree.
///
/// Use: open the Player prefab, select Visuals, right-click the PlayerVisuals component header >
/// Build Bird Animator.
///
/// Re-running rewrites the two swap clips (handy if the models are renamed or moved) but leaves an
/// existing controller alone, so real clips you've slotted into it survive. Delete the controller
/// to have it regenerated.
/// </summary>
public static class PlayerVisualsAnimatorBuilder
{
    private const string ParentFolder = "Assets/Player";
    private const string Folder = ParentFolder + "/Animation";
    private const string ControllerPath = Folder + "/BirdVisuals.controller";

    [MenuItem("CONTEXT/PlayerVisuals/Build Bird Animator")]
    private static void Build(MenuCommand command)
    {
        var visuals = (PlayerVisuals)command.context;
        Transform root = visuals.transform;

        if (!IsChild(visuals.GroundModel, root) || !IsChild(visuals.FlyingModel, root))
        {
            Debug.LogError("Build Bird Animator: assign Ground Model and Flying Model, and make sure both are " +
                           "children of the object holding PlayerVisuals.", visuals);
            return;
        }

        string groundPath = AnimationUtility.CalculateTransformPath(visuals.GroundModel.transform, root);
        string flyPath = AnimationUtility.CalculateTransformPath(visuals.FlyingModel.transform, root);

        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(ParentFolder, "Animation");

        AnimationClip groundClip = WriteSwapClip("Bird_Blockout_Ground", groundPath, flyPath, groundActive: true);
        AnimationClip flyClip = WriteSwapClip("Bird_Blockout_Fly", groundPath, flyPath, groundActive: false);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        bool created = controller == null;
        if (created) controller = CreateController(groundClip, flyClip);

        if (!visuals.TryGetComponent(out Animator animator))
            animator = Undo.AddComponent<Animator>(visuals.gameObject);

        Undo.RecordObject(animator, "Assign Bird Animator");
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        EditorUtility.SetDirty(animator);
        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);

        AssetDatabase.SaveAssets();
        Debug.Log($"Build Bird Animator: {(created ? "created" : "kept existing")} {ControllerPath}; " +
                  $"swap clips toggle '{groundPath}' and '{flyPath}'.", controller);
    }

    private static bool IsChild(GameObject model, Transform root) =>
        model != null && model.transform != root && model.transform.IsChildOf(root);

    private static AnimationClip WriteSwapClip(string name, string groundPath, string flyPath, bool groundActive)
    {
        string path = $"{Folder}/{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = name };
            AssetDatabase.CreateAsset(clip, path);
        }
        else
        {
            clip.ClearCurves();
        }

        SetActiveCurve(clip, groundPath, groundActive);
        SetActiveCurve(clip, flyPath, !groundActive);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static void SetActiveCurve(AnimationClip clip, string path, bool active)
    {
        // Same binding the Animation window records for the GameObject "active" checkbox.
        EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, typeof(GameObject), "m_IsActive");
        AnimationCurve curve = AnimationCurve.Constant(0f, 1f, active ? 1f : 0f);
        AnimationUtility.SetEditorCurve(clip, binding, curve);
    }

    /// <summary>
    /// Locomotion (1D on Flight)
    ///   0: Grounded (1D on Speed)  -- 0 idle, 1 walk
    ///   1: Flying   (1D on Climb)  -- -1 dive, 0 glide, 1 climb
    /// Every slot gets the matching blockout clip for now; swap in real clips per slot later.
    /// </summary>
    private static AnimatorController CreateController(AnimationClip ground, AnimationClip fly)
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter(PlayerVisuals.FlightParam, AnimatorControllerParameterType.Float);
        controller.AddParameter(PlayerVisuals.SpeedParam, AnimatorControllerParameterType.Float);
        controller.AddParameter(PlayerVisuals.ClimbParam, AnimatorControllerParameterType.Float);

        controller.CreateBlendTreeInController("Locomotion", out BlendTree root, 0);
        root.name = "Ground-Air";
        root.blendType = BlendTreeType.Simple1D;
        root.blendParameter = PlayerVisuals.FlightParam;
        root.useAutomaticThresholds = false;

        BlendTree grounded = root.CreateBlendTreeChild(0f);
        grounded.name = "Grounded";
        grounded.blendType = BlendTreeType.Simple1D;
        grounded.blendParameter = PlayerVisuals.SpeedParam;
        grounded.useAutomaticThresholds = false;
        grounded.AddChild(ground, 0f); // idle
        grounded.AddChild(ground, 1f); // walk

        BlendTree flying = root.CreateBlendTreeChild(1f);
        flying.name = "Flying";
        flying.blendType = BlendTreeType.Simple1D;
        flying.blendParameter = PlayerVisuals.ClimbParam;
        flying.useAutomaticThresholds = false;
        flying.AddChild(fly, -1f); // dive
        flying.AddChild(fly, 0f);  // glide
        flying.AddChild(fly, 1f);  // climb

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(grounded);
        EditorUtility.SetDirty(flying);
        EditorUtility.SetDirty(controller);
        return controller;
    }
}
