using UnityEngine;

namespace QuestSystem
{
    /** <summary>An asset with a name to show, such as the person who gives a quest or the place it is given.</summary> */
    public interface IDisplayNamed
    {
        /** <summary>The name to show.</summary> */
        string DisplayName { get; }
    }

    /** <summary>Someone who can give a quest: a quest's giver is one (a game's person, a guild).</summary> */
    public interface IQuestGiverAsset : IDisplayNamed
    {
    }

    /** <summary>Somewhere a quest can be given: a quest's place is one (a game's town, a shrine).</summary> */
    public interface IQuestPlaceAsset : IDisplayNamed
    {
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
