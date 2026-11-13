// Put this file in an Editor folder.
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

[CustomPropertyDrawer(typeof(EditableEnumAttribute))]
public class EditableEnumDrawer : PropertyDrawer
{
    const float ButtonWidth = 22f;
    internal const string PendingKey = "EditableEnum.Pending";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.Enum)
        {
            EditorGUI.LabelField(position, label.text, "[EditableEnum] only works on enums");
            return;
        }

        var fieldRect  = new Rect(position.x, position.y, position.width - ButtonWidth - 2, position.height);
        var buttonRect = new Rect(position.xMax - ButtonWidth, position.y, ButtonWidth, position.height);

        EditorGUI.PropertyField(fieldRect, property, label);

        if (GUI.Button(buttonRect, "+"))
        {
            var enumType = fieldInfo.FieldType.IsArray ? fieldInfo.FieldType.GetElementType()
                         : fieldInfo.FieldType.IsGenericType ? fieldInfo.FieldType.GetGenericArguments()[0]
                         : fieldInfo.FieldType;
            PopupWindow.Show(buttonRect, new AddEnumValuePopup(enumType, property));
        }
    }

    // The new value only exists after the recompile, so assign it once scripts reload.
    [DidReloadScripts]
    static void ApplyPending()
    {
        var data = SessionState.GetString(PendingKey, "");
        if (data == "") return;
        SessionState.EraseString(PendingKey);

        var parts = data.Split('|'); // targetId | propertyPath | enum type | value name
        if (parts.Length != 4 || !GlobalObjectId.TryParse(parts[0], out var id)) return;

        var target = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id);
        var type = Type.GetType(parts[2]);
        if (target == null || type == null || !Enum.IsDefined(type, parts[3])) return;

        var so = new SerializedObject(target);
        var prop = so.FindProperty(parts[1]);
        if (prop == null) return;

        prop.intValue = Convert.ToInt32(Enum.Parse(type, parts[3]));
        so.ApplyModifiedProperties();
    }
}

class AddEnumValuePopup : PopupWindowContent
{
    readonly Type enumType;
    readonly string targetId;
    readonly string propertyPath;
    string newName = "";
    string error;

    public AddEnumValuePopup(Type enumType, SerializedProperty property)
    {
        this.enumType = enumType;
        targetId = GlobalObjectId.GetGlobalObjectIdSlow(property.serializedObject.targetObject).ToString();
        propertyPath = property.propertyPath;
    }

    public override Vector2 GetWindowSize() => new Vector2(260, error == null ? 50 : 90);

    public override void OnGUI(Rect rect)
    {
        var e = Event.current;
        bool enter = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter);

        EditorGUIUtility.labelWidth = 80;
        GUI.SetNextControlName("EnumName");
        newName = EditorGUILayout.TextField($"New value", newName);
        EditorGUI.FocusTextInControl("EnumName");

        if (GUILayout.Button($"Add to {enumType.Name}") || enter)
        {
            TryAdd();
            e.Use();
        }

        if (error != null) EditorGUILayout.HelpBox(error, MessageType.Error);
    }

    void TryAdd()
    {
        newName = newName.Trim();
        if (!Regex.IsMatch(newName, @"^[A-Za-z_][A-Za-z0-9_]*$")) { error = "Not a valid C# identifier."; return; }
        if (Enum.GetNames(enumType).Contains(newName))            { error = "That value already exists."; return; }
        if (!EnumSourceWriter.Append(enumType, newName, out error)) return;

        SessionState.SetString(EditableEnumDrawer.PendingKey,
            string.Join("|", targetId, propertyPath, enumType.AssemblyQualifiedName, newName));
        editorWindow.Close();
    }
}

static class EnumSourceWriter
{
    public static bool Append(Type enumType, string newName, out string error)
    {
        error = null;
        var decl = $@"\benum\s+{enumType.Name}\b[^{{]*\{{(?<body>[^}}]*)\}}";

        var files = AssetDatabase.FindAssets("t:MonoScript", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".cs") && Regex.IsMatch(File.ReadAllText(p), decl))
            .ToList();

        if (files.Count == 0) { error = $"Couldn't find 'enum {enumType.Name}' under Assets/."; return false; }
        if (files.Count > 1)  { error = $"'enum {enumType.Name}' found in {files.Count} files; can't tell which one."; return false; }

        var path = files[0];
        var src  = File.ReadAllText(path);
        var nl   = src.Contains("\r\n") ? "\r\n" : "\n";
        var body = Regex.Match(src, decl).Groups["body"];

        var trimmed = body.Value.TrimEnd();
        var tail    = body.Value.Substring(trimmed.Length); // keeps the closing brace's indentation
        var code    = Regex.Replace(trimmed, @"//.*?$|/\*.*?\*/", "",
                                    RegexOptions.Multiline | RegexOptions.Singleline).TrimEnd();

        // Add a comma after the last value if there isn't one (skipping any trailing comment).
        if (code.Length > 0 && !code.EndsWith(","))
        {
            var last = Regex.Match(trimmed, @"[^\s,](?=(\s|//[^\n]*|/\*.*?\*/)*$)", RegexOptions.Singleline);
            trimmed = trimmed.Insert(last.Index + 1, ",");
        }

        // If the enum uses explicit values, give the new one the next number up.
        var entry = newName;
        if (code.Contains("="))
        {
            var next = Enum.GetValues(enumType).Cast<object>().Select(Convert.ToInt64).DefaultIfEmpty(-1).Max() + 1;
            entry = $"{newName} = {next}";
        }

        var indent = Regex.Match(trimmed, @"\n([ \t]*)\S[^\n]*$").Groups[1].Value;
        if (indent == "") indent = "    ";

        src = src.Remove(body.Index, body.Length)
                 .Insert(body.Index, $"{trimmed}{nl}{indent}{entry}{tail}");

        File.WriteAllText(path, src);
        AssetDatabase.ImportAsset(path); // triggers the recompile
        return true;
    }
}
