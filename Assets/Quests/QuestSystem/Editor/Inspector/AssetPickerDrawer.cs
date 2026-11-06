using UnityEditor;
using UnityEngine;

namespace QuestSystem.EditorTools
{
    /**
     * <summary>Draws an <see cref="AssetPickerAttribute" /> field as a menu of only the assets that fit it, with a button
     * that shows the chosen asset in the Project window. An asset already set that doesn't fit is listed as such.</summary>
     */
    [CustomPropertyDrawer(typeof(AssetPickerAttribute))]
    public sealed class AssetPickerDrawer : PropertyDrawer
    {
        #region Private Fields

        /** <summary>The width of the button that shows the chosen asset.</summary> */
        private const float ShowWidth = 22f;

        private static GUIContent _show;

        #endregion

        #region Properties

        private static GUIContent ShowContent => _show ??= new GUIContent(EditorGUIUtility.IconContent("Search Icon").image, "Show the asset in the Project window");

        #endregion

        #region Public API

        /** <inheritdoc /> */
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var type = ((AssetPickerAttribute)attribute).Type;
            if (type == null || property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var choices = AssetChoices.Of(type);
            var chosenAsset = property.objectReferenceValue;
            var labels = new GUIContent[choices.Labels.Count + (choices.IndexOf(chosenAsset) < 0 ? 1 : 0)];
            for (var i = 0; i < choices.Labels.Count; i++) labels[i] = choices.Labels[i];
            var current = choices.IndexOf(chosenAsset);
            if (current < 0)
            {
                current = labels.Length - 1;
                labels[current] = new GUIContent($"{chosenAsset.name} (doesn't fit)");
            }

            label = EditorGUI.BeginProperty(position, label, property);
            var menu = new Rect(position.x, position.y, position.width - ShowWidth - 2f, position.height);
            var chosen = EditorGUI.Popup(menu, label, current, labels);
            if (chosen != current && chosen < choices.Assets.Count) property.objectReferenceValue = choices.Assets[chosen];
            EditorGUI.EndProperty();

            var show = new Rect(position.xMax - ShowWidth, position.y, ShowWidth, position.height);
            using (new EditorGUI.DisabledScope(chosenAsset == null))
                if (GUI.Button(show, ShowContent, EditorStyles.miniButton))
                    EditorGUIUtility.PingObject(chosenAsset);
        }

        #endregion
    }
}
