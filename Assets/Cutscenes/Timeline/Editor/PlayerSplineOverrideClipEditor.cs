using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace BirdWatching.Cutscenes
{
    [CustomEditor(typeof(PlayerSplineOverrideClip))]
    public class PlayerSplineOverrideClipEditor : Editor
    {
        SerializedProperty disableFreeLookProperty;

        void OnEnable()
        {
            disableFreeLookProperty = serializedObject.FindProperty("disableFreeLook");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(disableFreeLookProperty, new GUIContent("Disable Free Look"));

            PlayerSplinePositionOverride overrideTarget = ResolveTarget();
            if (overrideTarget == null)
            {
                EditorGUILayout.HelpBox(
                    "Bind a PlayerSplinePositionOverride on this track.",
                    MessageType.Info);
            }
            else if (overrideTarget.spline == null)
            {
                EditorGUILayout.HelpBox(
                    $"'{overrideTarget.name}' has no spline assigned.",
                    MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties();
        }

        static PlayerSplinePositionOverride ResolveTarget()
        {
            PlayableDirector director = TimelineEditor.inspectedDirector;
            if (director == null || TimelineEditor.inspectedAsset == null)
                return null;

            foreach (var track in TimelineEditor.inspectedAsset.GetOutputTracks())
            {
                if (track is PlayerSplineOverrideTrack)
                    return director.GetGenericBinding(track) as PlayerSplinePositionOverride;
            }

            return null;
        }
    }
}
