using System;
using QuestSystem;
using UnityEngine;

namespace BirdWatching.Quests
{
    /** <summary>Lets a QuestTrigger count only the player (anything tagged Player, or with a Player-tagged rigidbody).</summary> */
    [Serializable]
    public class PlayerTriggerFilter : IQuestTriggerFilter
    {
        [Tooltip("The tag that counts as the player.")]
        public string playerTag = "Player";

        public bool Accepts(Collider other)
        {
            if (other.CompareTag(playerTag)) return true;
            return other.attachedRigidbody != null && other.attachedRigidbody.CompareTag(playerTag);
        }
    }
}
