using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

namespace BirdWatching.Quests
{
    public class CutsceneExecution : IQuestExecutionStrategy
    {
        [Tooltip("Insert your timeline here")]
        public PlayableDirector playableDirector;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            PlayCutscene().Forget();
        }

        protected override void OnDeactivate()
        {
            if (playableDirector != null && playableDirector.state == PlayState.Playing)
            {
                playableDirector.Stop();
            }

            EnableInput();
        }

        async UniTaskVoid PlayCutscene()
        {
            if (playableDirector == null || playableDirector.playableAsset == null)
            {
                Debug.LogError("PlayableDirector is not assigned on CutsceneExecution.");
                return;
            }

            DisableInput();
            try
            {
                playableDirector.time = 0;
                playableDirector.Play();
                await WaitUntilStopped(playableDirector);
            }
            finally
            {
                EnableInput();
            }
        }

        static UniTask WaitUntilStopped(PlayableDirector director)
        {
            var completion = new UniTaskCompletionSource();

            void OnStopped(PlayableDirector _)
            {
                director.stopped -= OnStopped;
                completion.TrySetResult();
            }

            director.stopped += OnStopped;
            return completion.Task;
        }

        void DisableInput()
        {
            if (InputManager.Instance == null) return;

            InputManager.Instance.LockInputs(false, false, false);
        }

        void EnableInput()
        {
            if (InputManager.Instance == null) return;

            InputManager.Instance.LockInputs(true, true, true);
        }
    }
}
