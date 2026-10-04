using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using FMODUnity;
using FMOD.Studio;
using UnityEngine.Events;

// NOTE TO SELF: TODO Create global param in fmod

namespace BirdWatching.Conversation
{
    public class ConversationPlayer : MonoBehaviour
    {
        public static ConversationPlayer Instance;

        public List<ConversationObject> conversations = new();
        public ConversationCharacter character;

        [HideInInspector] public UnityEvent<ConversationObject> OnConversationFinishes = new();

        Coroutine subtitleRoutine = null;
        ConversationObject curConvo;
        int lineInConvo = 0;
        EventInstance currentConvoAudioInstance;

        public void PlayConversation(ConversationObject convo)
        {
            StopCurrentConversation();

            currentConvoAudioInstance = RuntimeManager.CreateInstance(convo.audio);
            subtitleRoutine = StartCoroutine(StartConversationSequence(convo));
        }

        public void SkipConversationLine()
        {
            if (subtitleRoutine == null)
            {
                return;
            }
            StopCoroutine(subtitleRoutine);
            subtitleRoutine = StartCoroutine(StartConversationSequence(curConvo, ++lineInConvo));
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

            character.HideCharacterSpeech();
            subtitleRoutine = null;
        }

        IEnumerator StartConversationSequence(ConversationObject newConvo, int startingIndex = 0)
        {
            if (newConvo.startDelay > 0)
            {
                yield return new WaitForSeconds(newConvo.startDelay);
            }

            if (currentConvoAudioInstance.isValid())
            {
                currentConvoAudioInstance.start();
            }

            float lastTime = 0;
            curConvo = newConvo;
            lineInConvo = startingIndex;

            for (; lineInConvo < curConvo.lines.Count; lineInConvo++)
            {
                var line = curConvo.lines[lineInConvo];
                character.SetCharacterSpeech(null, line.text);

                RuntimeManager.StudioSystem.setParameterByName("PhoneVoice", line.isFromPhone ? 1 : 0);

                yield return new WaitForSeconds(line.timeStamp - lastTime);
                lastTime = line.timeStamp;
            }

            var finished = curConvo;
            StopCurrentConversation();
            OnConversationFinishes.Invoke(finished);
        }

        void Awake()
        {
            if (Instance != null)
            {
                return;
            }
            Instance = this;

            character.HideCharacterSpeech();
        }
    }
}