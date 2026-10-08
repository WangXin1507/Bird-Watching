using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;

namespace BirdWatching.Cutscenes
{
    [CustomEditor(typeof(CinemachineDollyClip))]
    public class CinemachineDollyClipEditor : Editor
    {
        SerializedProperty cameraProperty;
        SerializedProperty startPositionProperty;
        SerializedProperty endPositionProperty;
        SerializedProperty easeProperty;

        void OnEnable()
        {
            cameraProperty = serializedObject.FindProperty("camera");
            startPositionProperty = serializedObject.FindProperty("startPosition");
            endPositionProperty = serializedObject.FindProperty("endPosition");
            easeProperty = serializedObject.FindProperty("ease");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(cameraProperty, new GUIContent("Camera"));
            EditorGUILayout.Slider(startPositionProperty, 0f, 1f, new GUIContent("Start Position"));
            EditorGUILayout.Slider(endPositionProperty, 0f, 1f, new GUIContent("End Position"));
            EditorGUILayout.PropertyField(easeProperty, new GUIContent("Ease"));

            DrawDollyWarning();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawDollyWarning()
        {
            CinemachineCamera camera = ResolveCamera();
            if (camera == null) return;

            if (camera.GetComponent<CinemachineSplineDolly>() == null)
            {
                EditorGUILayout.HelpBox(
                    $"'{camera.name}' has no CinemachineSplineDolly. Assign a camera that rides a dolly spline.",
                    MessageType.Warning);
            }
        }

        CinemachineCamera ResolveCamera()
        {
            var clip = (CinemachineDollyClip)target;
            PlayableDirector director = TimelineEditor.inspectedDirector;
            if (director != null)
                return clip.camera.Resolve(director);

            return cameraProperty.FindPropertyRelative("defaultValue").objectReferenceValue as CinemachineCamera;
        }
    }
}
