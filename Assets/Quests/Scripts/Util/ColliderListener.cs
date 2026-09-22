using System;
using UnityEngine;

namespace BirdWatching.Quests.Util
{
    public class ColliderListener : MonoBehaviour
    {
        public Action<Collider> onTriggerEnter = delegate { };
        public Action<Collider> onTriggerStay = delegate { };
        public Action<Collider> onTriggerExit = delegate { };

        public void OnTriggerEnter(Collider other)
        {
            onTriggerEnter?.Invoke(other);
        }

        public void OnTriggerStay(Collider other)
        {
            onTriggerStay?.Invoke(other);
        }

        public void OnTriggerExit(Collider other)
        {
            onTriggerExit?.Invoke(other);
        }
    }
}
