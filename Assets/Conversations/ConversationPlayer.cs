using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        bool skipRequested;
        Button skipButton;

        public void PlayConversation(ConversationObject convo)
        {
            StopCurrentConversation();

            if (convo == null)
            {
                return;
            }

            currentConvoAudioInstance = RuntimeManager.CreateInstance(convo.audio);
            subtitleRoutine = StartCoroutine(StartConversationSequence(convo));
        }

        public void SkipConversationLine()
        {
            if (subtitleRoutine == null)
            {
                return;
            }

            skipRequested = true;
        }

        public void StopCurrentConversation()
        {
            skipRequested = false;

            if (subtitleRoutine != null)
            {
                StopCoroutine(subtitleRoutine);
                subtitleRoutine = null;
            }

            CleanupConversationPlayback();
        }

        IEnumerator StartConversationSequence(ConversationObject newConvo)
        {
            curConvo = newConvo;
            skipRequested = false;

            if (newConvo.startDelay > 0)
            {
                yield return WaitForSecondsOrSkip(newConvo.startDelay);
                skipRequested = false;
            }

            if (currentConvoAudioInstance.isValid())
            {
                currentConvoAudioInstance.start();
            }

            float lastTime = 0;
            var lines = curConvo.lines;

            for (lineInConvo = 0; lines != null && lineInConvo < lines.Count; lineInConvo++)
            {
                var line = lines[lineInConvo];
                character.SetCharacterSpeech(null, line.text);

                RuntimeManager.StudioSystem.setParameterByName("PhoneVoice", line.isFromPhone ? 1 : 0);

                skipRequested = false;
                yield return WaitForSecondsOrSkip(line.timeStamp - lastTime);

                if (skipRequested)
                {
                    skipRequested = false;
                    bool hasNextLine = lineInConvo < lines.Count - 1;
                    if (hasNextLine)
                    {
                        SeekConversationAudio(line.timeStamp);
                    }
                }

                lastTime = line.timeStamp;
            }

            var finished = curConvo;
            subtitleRoutine = null;
            CleanupConversationPlayback();
            OnConversationFinishes.Invoke(finished);
        }

        void CleanupConversationPlayback()
        {
            skipRequested = false;

            if (currentConvoAudioInstance.isValid())
            {
                currentConvoAudioInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                currentConvoAudioInstance.release();
                currentConvoAudioInstance.clearHandle();
            }

            RuntimeManager.StudioSystem.setParameterByName("PhoneVoice", 0);

            if (character != null)
            {
                character.HideCharacterSpeech();
            }
        }

        IEnumerator WaitForSecondsOrSkip(float duration)
        {
            float remaining = Mathf.Max(0f, duration);
            while (remaining > 0f && !skipRequested)
            {
                remaining -= Time.deltaTime;
                yield return null;
            }
        }

        void SeekConversationAudio(float timeSeconds)
        {
            if (!currentConvoAudioInstance.isValid())
            {
                return;
            }

            int positionMs = Mathf.Max(0, Mathf.RoundToInt(timeSeconds * 1000f));
            currentConvoAudioInstance.setTimelinePosition(positionMs);

            if (currentConvoAudioInstance.getChannelGroup(out var group) == FMOD.RESULT.OK)
            {
                SeekChannelGroup(group, (uint)positionMs);
            }
        }

        static void SeekChannelGroup(FMOD.ChannelGroup group, uint positionMs)
        {
            if (!group.hasHandle())
            {
                return;
            }

            if (group.getNumChannels(out int channelCount) == FMOD.RESULT.OK)
            {
                for (int i = 0; i < channelCount; i++)
                {
                    if (group.getChannel(i, out var channel) == FMOD.RESULT.OK)
                    {
                        channel.setPosition(positionMs, FMOD.TIMEUNIT.MS);
                    }
                }
            }

            if (group.getNumGroups(out int nestedCount) == FMOD.RESULT.OK)
            {
                for (int i = 0; i < nestedCount; i++)
                {
                    if (group.getGroup(i, out var nested) == FMOD.RESULT.OK)
                    {
                        SeekChannelGroup(nested, positionMs);
                    }
                }
            }
        }

        void Awake()
        {
            if (Instance != null)
            {
                return;
            }
            Instance = this;

            if (character == null)
            {
                character = GetComponentInChildren<ConversationCharacter>(true);
            }

            skipButton = GetComponentInChildren<Button>(true);
            if (skipButton != null)
            {
                skipButton.onClick.AddListener(SkipConversationLine);
            }

            if (character != null)
            {
                character.HideCharacterSpeech();
            }
        }

        void Update()
        {
            if (subtitleRoutine == null || InputManager.Instance == null)
            {
                return;
            }

            // Cutscenes lock interaction, so read the action directly.
            if (InputManager.Instance.Actions.Player.Interact.WasPerformedThisFrame())
            {
                SkipConversationLine();
            }
        }

        void OnDestroy()
        {
            if (skipButton != null)
            {
                skipButton.onClick.RemoveListener(SkipConversationLine);
            }

            if (Instance == this)
            {
                StopCurrentConversation();
                Instance = null;
            }
        }
    }
}
