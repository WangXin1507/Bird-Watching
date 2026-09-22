using Unity.VisualScripting;
using UnityEngine;

namespace BirdWatching.Quests.Util
{
    public class TriggerEnterCondition : IQuestConditionStrategy
    {
        [Tooltip("Assign the collider that will trigger this condition. It must be set as a trigger.")]
        public Collider colliderSource;

        [Tooltip("If true, the condition will deactivate when the player exits the trigger.")]
        public bool deactivateOnExit = true;

        bool inTrigger = false;
        ColliderListener colliderListener;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (colliderSource == null)
            {
                Debug.LogError("Collider source is not assigned in TriggerEnterCondition.");
            }
            if (!colliderSource.isTrigger)
            {
                Debug.LogError("Collider source must be set as a trigger in TriggerEnterCondition.");
            }
            colliderListener = colliderSource.GetOrAddComponent<ColliderListener>();
            colliderListener.onTriggerEnter += OnTriggerEnter;
            colliderListener.onTriggerExit += OnTriggerExit;
        }

        public override bool Evaluate()
        {
            return inTrigger;
        }

        public override bool StopIfTriggered() => false;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.gameObject.CompareTag("Player"))
            {
                inTrigger = false;
                return;
            }

            inTrigger = true;

            Broadcast(Broadcaster);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!deactivateOnExit)
            {
                return;
            }

            if (!other.gameObject.CompareTag("Player"))
            {
                return;
            }

            inTrigger = false;

            Broadcast(Broadcaster);
        }
    }
}