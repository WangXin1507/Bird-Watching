using BirdWatching.Conversation;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace BirdWatching.Quests
{
    public class CutsceneExecution : IQuestExecutionStrategy
    {
        [Tooltip("Insert your timeline here")]
        public PlayableDirector playableDirector;
        public ConversationObject conversationObject;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            PlayCutscene().Forget();
        }

        protected override void OnDeactivate()
        {
            StopCutscene();
            EnableInput();
        }

        async UniTaskVoid PlayCutscene()
        {
            if (conversationObject == null || ConversationPlayer.Instance == null)
            {
                Debug.LogError("ConversationObject / ConversationPlayer is not assigned on CutsceneExecution.");
                return;
            }

            DisableInput();

            UnityAction<ConversationObject> onConversationFinished = null;

            try
            {
                var conversationDone = new UniTaskCompletionSource();
                onConversationFinished = finished =>
                {
                    if (finished == conversationObject)
                    {
                        conversationDone.TrySetResult();
                    }
                };
                ConversationPlayer.Instance.OnConversationFinishes.AddListener(onConversationFinished);
                ConversationPlayer.Instance.PlayConversation(conversationObject);

                if (playableDirector != null && playableDirector.playableAsset != null)
                {
                    playableDirector.time = 0;
                    playableDirector.Play();
                }

                await conversationDone.Task;
            }
            finally
            {
                if (onConversationFinished != null && ConversationPlayer.Instance != null)
                {
                    ConversationPlayer.Instance.OnConversationFinishes.RemoveListener(onConversationFinished);
                }
                StopCutscene();
                EnableInput();
            }
        }

        void StopCutscene()
        {
            if (playableDirector != null && playableDirector.state == PlayState.Playing)
            {
                playableDirector.Stop();
            }

            if (ConversationPlayer.Instance != null)
            {
                ConversationPlayer.Instance.StopCurrentConversation();
            }
        }

        void DisableInput()
        {
            if (InputManager.Instance == null) return;

            // Leave move and look alone. PlayerSplinePositionOverride is what
            // takes over the bird; a camera-only cutscene must not freeze leftover velocity.
            InputManager.Instance.LockInputs(true, true, false);
        }

        void EnableInput()
        {
            if (InputManager.Instance == null) return;

            InputManager.Instance.LockInputs(true, true, true);
        }
    }
}
