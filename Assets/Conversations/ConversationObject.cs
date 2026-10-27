using FMODUnity;
using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace BirdWatching.Conversation
{
    [CreateAssetMenu(fileName = "ConversationObject", menuName = "Scriptable Objects/ConversationObject")]
    public class ConversationObject : SerializedScriptableObject
    {
        public List<ConversationLine> lines;
        public EventReference audio;
        public float startDelay;
        [OdinSerialize] public Dictionary<string, Sprite> charSprites;
    }

    [Serializable]
    public class ConversationLine
    {
        public string speaker;
        public string text;
        public bool isFromPhone;
        public float timeStamp;
    }
}