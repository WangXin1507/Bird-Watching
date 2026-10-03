using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Collections;
using FMODUnity;
using FMOD.Studio;

// NOTE TO SELF: TODO Create global param in fmod

namespace BirdWatching.Audio
{

    public class ConversationPlayer : MonoBehaviour
    {
        public static ConversationPlayer Instance;

        public List<ConversationObject> conversations = new();

        [SerializeField] TextMeshProUGUI subtitles;

        Coroutine subtitleRoutine = null;
        ConversationObject curConvo;
        int lineInConvo = 0;
        EventInstance currentConvoAudioInstance;

        public void PlayConversation(ConversationObject convo)
        {
            StopCurrentConversation();

            currentConvoAudioInstance = RuntimeManager.CreateInstance(convo.audio);
            subtitleRoutine = StartCoroutine(StartSubtitles(convo));
        }

        public void SkipConversationLine()
        {
            if (subtitleRoutine == null)
            {
                return;
            }
            StopCoroutine(subtitleRoutine);
            subtitleRoutine = StartCoroutine(StartSubtitles(curConvo, ++lineInConvo));
        }

        public void StopCurrentConversation()
        {
            if (subtitleRoutine != null)
            {
                StopCoroutine(subtitleRoutine);
            }

            if (currentConvoAudioInstance.isValid())
            {
                currentConvoAudioInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                currentConvoAudioInstance.release();
            }

            RuntimeManager.StudioSystem.setParameterByName("PhoneVoice", 0);

            subtitles.text = "";
            subtitles.enabled = false;
            subtitleRoutine = null;
        }

        IEnumerator StartSubtitles(ConversationObject newConvo, int startingIndex = 0)
        {
            if (currentConvoAudioInstance.isValid())
            {
                currentConvoAudioInstance.start();
            }

            float lastTime = 0;
            curConvo = newConvo;
            lineInConvo = startingIndex;

            subtitles.enabled = true;

            for (; lineInConvo < curConvo.lines.Count; lineInConvo++)
            {
                var line = curConvo.lines[lineInConvo];
                subtitles.text = line.text;

                RuntimeManager.StudioSystem.setParameterByName("PhoneVoice", line.isFromPhone ? 1 : 0);

                yield return new WaitForSeconds(line.timeStamp - lastTime);
                lastTime = line.timeStamp;
            }

            StopCurrentConversation();
        }

        void Awake()
        {
            if (Instance != null)
            {
                return;
            }
            Instance = this;

            subtitles.text = "";
        }
    }
}