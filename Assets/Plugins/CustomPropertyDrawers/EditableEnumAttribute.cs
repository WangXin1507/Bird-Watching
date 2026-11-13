using UnityEngine;

/// <summary>
/// Draws an enum field with a "+" button next to the dropdown.
/// Clicking it lets you type a new value, which is appended to the enum's source file.
/// Usage: [SerializeField, EditableEnum] ItemType type;
/// </summary>
public class EditableEnumAttribute : PropertyAttribute { }
