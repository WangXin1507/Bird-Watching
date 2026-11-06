using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace QuestSystem.EditorTools
{
    /**
     * <summary>The assets a picker offers for a type or interface: None first, then every asset in the project that is
     * one, by its display name (the asset's name added where two share one, the kind of asset first where kinds mix).
     * Kept until the project changes.</summary>
     */
    public sealed class AssetChoices
    {
        #region Private Fields

        private static readonly Dictionary<Type, AssetChoices> Cached = new Dictionary<Type, AssetChoices>();
        private readonly List<Object> _assets = new List<Object> { null };
        private readonly List<GUIContent> _labels = new List<GUIContent> { new GUIContent("None") };

        #endregion

        #region Properties

        /** <summary>The assets offered, None (null) first.</summary> */
        public IReadOnlyList<Object> Assets => _assets;

        /** <summary>Each asset's label, in the same order.</summary> */
        public IReadOnlyList<GUIContent> Labels => _labels;

        #endregion

        #region Public API

        static AssetChoices() => EditorApplication.projectChanged += Cached.Clear;

        /** <summary>The choices for <paramref name="type" />.</summary> */
        public static AssetChoices Of(Type type)
        {
            if (!Cached.TryGetValue(type, out var choices)) Cached[type] = choices = new AssetChoices(type);
            return choices;
        }

        /** <summary>Where <paramref name="asset" /> stands among the choices (None for no asset or a destroyed one); -1 when it isn't one.</summary> */
        public int IndexOf(Object asset) => asset == null ? 0 : _assets.IndexOf(asset);

        /** <summary>The asset, or a true null for none or a destroyed one, so every empty reference reads alike.</summary> */
        public static Object Real(Object asset) => asset != null ? asset : null;

        #endregion

        #region Helpers

        private AssetChoices(Type type)
        {
            var found = new List<(Object Asset, Type Kind)>();
            foreach (var kind in Kinds(type))
                foreach (var guid in AssetDatabase.FindAssets("t:" + kind.Name))
                {
                    var asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), kind);
                    if (asset != null && kind.IsInstanceOfType(asset) && !found.Exists(entry => entry.Asset == asset)) found.Add((asset, kind));
                }
            var mixed = found.Exists(entry => entry.Kind != found[0].Kind);
            var names = new Dictionary<string, int>();
            foreach (var entry in found)
            {
                var name = NameOf(entry.Asset);
                names[name] = names.TryGetValue(name, out var count) ? count + 1 : 1;
            }
            found.Sort((a, b) => string.Compare(Label(a, mixed, names), Label(b, mixed, names), StringComparison.OrdinalIgnoreCase));
            foreach (var entry in found)
            {
                _assets.Add(entry.Asset);
                _labels.Add(new GUIContent(Label(entry, mixed, names), AssetDatabase.GetAssetPath(entry.Asset)));
            }
        }

        /** <summary>The asset types that are <paramref name="type" />, the type itself included.</summary> */
        private static IEnumerable<Type> Kinds(Type type)
        {
            if (!type.IsAbstract && !type.IsInterface && typeof(ScriptableObject).IsAssignableFrom(type)) yield return type;
            foreach (var kind in TypeCache.GetTypesDerivedFrom(type))
                if (!kind.IsAbstract && typeof(ScriptableObject).IsAssignableFrom(kind)) yield return kind;
        }

        private static string Label((Object Asset, Type Kind) entry, bool mixed, Dictionary<string, int> names)
        {
            var name = NameOf(entry.Asset);
            if (names[name] > 1) name = $"{name} ({entry.Asset.name})";
            return mixed ? $"{ObjectNames.NicifyVariableName(entry.Kind.Name)}/{name}" : name;
        }

        /** <summary>Its display name, slashes kept from opening submenus.</summary> */
        private static string NameOf(Object asset)
        {
            var named = DisplayNames.As<IDisplayNamed>(asset);
            var name = named != null && !string.IsNullOrWhiteSpace(named.DisplayName) ? named.DisplayName : asset.name;
            return name.Replace('/', '∕');
        }

        #endregion
    }
}
