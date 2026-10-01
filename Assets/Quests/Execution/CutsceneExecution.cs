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
            playableDirector.Play();
        }
    }
}