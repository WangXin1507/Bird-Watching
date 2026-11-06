using System;
using UnityEngine;

namespace QuestSystem
{
    /** <summary>Shows an asset field as a menu of only the assets of a type or interface, such as a quest's givers.</summary> */
    public sealed class AssetPickerAttribute : PropertyAttribute
    {
        /** <summary>The type or interface every asset offered is.</summary> */
        public Type Type { get; }

        /** <summary>Offers only the assets that are a <paramref name="type" />.</summary> */
        public AssetPickerAttribute(Type type) => Type = type;
    }
}
