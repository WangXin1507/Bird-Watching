using UnityEngine;

namespace QuestSystem
{
    /** <summary>An asset with a name to show.</summary> */
    public interface IDisplayNamed
    {
        /** <summary>The name to show.</summary> */
        string DisplayName { get; }
    }

    /** <summary>The one place that asks what role a named asset plays.</summary> */
    public static class DisplayNames
    {
        /** <summary>The asset as a <typeparamref name="T" />; null when it isn't one.</summary> */
        public static T As<T>(Object asset) where T : class, IDisplayNamed => asset != null ? asset as T : null;

        /** <summary>True for no asset, or one that is a <typeparamref name="T" />.</summary> */
        public static bool Fits<T>(Object asset) where T : class, IDisplayNamed => asset == null || asset is T;
    }
}
