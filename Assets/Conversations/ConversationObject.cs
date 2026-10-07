using FMODUnity;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace BirdWatching.Conversation
{
    [CreateAssetMenu(fileName = "ConversationObject", menuName = "Scriptable Objects/ConversationObject")]
    public class ConversationObject : ScriptableObject
    {
        public List<ConversationLine> lines;
        public EventReference audio;
        public float startDelay;
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