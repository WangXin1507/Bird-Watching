using System;
using System.Threading;
using BirdWatching.Conversation;
using Cysharp.Threading.Tasks;
using QuestSystem;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

namespace BirdWatching.Quests
{
    /**
     * <summary>
     * A QuestReaction effect that plays a conversation (and optionally a timeline) while its condition holds, with
     * input locked. When the conversation finishes it raises a signal, so the quest step that started the cutscene can
     * complete. Ported from the old CutsceneExecution.
     * </summary>
     * <remarks>
     * Use it with a reaction whose condition is "step active": the cutscene plays when the step starts, and plays
     * again if the game loads while that step is still active.
     * </remarks>
     */
    [Serializable]
    public class CutsceneEffect : IQuestReactionEffect
    {
        [Tooltip("Insert your timeline here")]
        public PlayableDirector playableDirector;
        public ConversationObject conversationObject;
        [Tooltip("Optional: unlocked in the journal when the conversation finishes.")]
        public TranscriptEntry transcriptEntry;

        [Tooltip("The quest runtime, to raise the finished signal through.")]
        public QuestRuntime runtime;
        [Tooltip("Raised when the conversation finishes. Count it in the quest step that plays this cutscene.")]
        public QuestSignal onFinished;

        private CancellationTokenSource playing;

        public void Apply(bool on)
        {
            if (on)
            {
                if (playing == null) PlayCutscene().Forget();
            }
            else if (playing != null)
            {
                playing.Cancel();
            }
        }

        async UniTaskVoid PlayCutscene()
        {
            playing = new CancellationTokenSource();
            var token = playing.Token;
            UnityAction<ConversationObject> onConversationFinished = null;
            bool finished = false;

            try
            {
                // reactions apply from Awake, possibly before the conversation player exists
                await UniTask.WaitUntil(() => ConversationPlayer.Instance != null, cancellationToken: token);

                if (conversationObject == null)
                {
                    Debug.LogError("ConversationObject is not assigned on CutsceneEffect.");
                    return;
                }

                DisableInput(); Cursor.lockState = CursorLockMode.Confined; Cursor.visible = false;

                var conversationDone = new UniTaskCompletionSource();
                onConversationFinished = done =>
                {
                    if (done == conversationObject)
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

                await conversationDone.Task.AttachExternalCancellation(token);
                finished = true;
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (onConversationFinished != null && ConversationPlayer.Instance != null)
                {
                    ConversationPlayer.Instance.OnConversationFinishes.RemoveListener(onConversationFinished);
                }
                if (onConversationFinished != null)
                {
                    StopCutscene();
                    EnableInput();
                }
                playing?.Dispose();
                playing = null;
            }

            if (finished)
            {
                if (transcriptEntry != null && Journal.Instance != null) Journal.Instance.UnlockTranscript(transcriptEntry);
                if (onFinished != null && runtime != null && runtime.Service != null) runtime.Service.Raise(onFinished);
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

            InputManager.Instance.LockInputs(true, true, false);
        }

        void EnableInput()
        {
            if (InputManager.Instance == null) return;

            InputManager.Instance.LockInputs(false, false, false);
        }
    }
}
