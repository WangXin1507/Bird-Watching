using System;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Mission-complete cutscene: lock the player, fade to black, cut to the mission's reward camera,
/// fade in, reveal the trinket bottom to top, hold, fade back to the player, unlock, then run onFinished
/// (the journal popup). Cutscenes queue rather than overlap.
/// </summary>
public static class TrinketRewardSequence
{
    private const int RewardCameraPriority = 100;
    private const float MaxCameraBlendWait = 3f;

    public static bool IsPlaying { get; private set; }

    public static async UniTaskVoid Play(MissionInScene entry, Action onFinished = null)
    {
        Trinket trinket = entry.reward;
        if (!trinket)
        {
            onFinished?.Invoke();
            return;
        }

        // A second mission finished mid-cutscene waits for the first one.
        await UniTask.WaitUntil(() => !IsPlaying);
        IsPlaying = true;

        CinemachineCamera rewardCam = entry.focusRewardCam;
        bool cameraRaised = false;
        bool cameraWasActive = false;
        PrioritySettings previousPriority = default;
        FadeStyle fade = new FadeStyle(FadeStyle.FadeType.FadeInAndOut, trinket.FadeDuration);

        try
        {
            SetPlayerLocked(true);

            await ScreenFader.FadeOut(fade);

            if (rewardCam)
            {
                cameraWasActive = rewardCam.gameObject.activeSelf;
                previousPriority = rewardCam.Priority;
                rewardCam.gameObject.SetActive(true);
                SetPriority(rewardCam, RewardCameraPriority);
                cameraRaised = true;
                await WaitForCameraBlend();
            }
            else
            {
                Debug.LogWarning($"[TrinketRewardSequence] No reward camera for '{entry.mission?.MissionName}'; revealing from the player camera.");
            }

            await ScreenFader.FadeIn(fade);
            await trinket.Reveal(trinket.GetCancellationTokenOnDestroy());
            await UniTask.Delay(TimeSpan.FromSeconds(trinket.HoldAfterReveal));

            await ScreenFader.FadeOut(fade);
            if (cameraRaised)
            {
                RestoreCamera(rewardCam, previousPriority, cameraWasActive);
                cameraRaised = false;
                await WaitForCameraBlend();
            }
            await ScreenFader.FadeIn(fade);
        }
        catch (OperationCanceledException)
        {
            // Trinket destroyed (e.g. scene unloaded) mid-cutscene; the finally block still cleans up.
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            if (cameraRaised) RestoreCamera(rewardCam, previousPriority, cameraWasActive);
            if (trinket && !trinket.IsRevealed) trinket.ShowRevealedInstant();
            if (ScreenFader.fadeCanvas) ScreenFader.SetAlpha(0f);

            SetPlayerLocked(false);
            IsPlaying = false;
            onFinished?.Invoke();
        }
    }

    private static void SetPlayerLocked(bool locked)
    {
        if (InputManager.Instance == null) return;
        InputManager.Instance.LockInputs(locked, locked, locked);
    }

    private static void SetPriority(CinemachineCamera camera, int value)
    {
        PrioritySettings priority = camera.Priority;
        priority.Enabled = true;
        priority.Value = value;
        camera.Priority = priority;
    }

    private static void RestoreCamera(CinemachineCamera camera, PrioritySettings priority, bool wasActive)
    {
        if (!camera) return;
        camera.Priority = priority;
        camera.gameObject.SetActive(wasActive);
    }

    // The screen is black while this runs, so any brain blend finishes off-screen and the shot reads as a cut.
    private static async UniTask WaitForCameraBlend()
    {
        await UniTask.NextFrame(PlayerLoopTiming.PostLateUpdate);

        Camera main = Camera.main;
        CinemachineBrain brain = main ? main.GetComponent<CinemachineBrain>() : null;
        if (!brain) return;

        float waited = 0f;
        while (brain && brain.IsBlending && waited < MaxCameraBlendWait)
        {
            waited += Time.unscaledDeltaTime;
            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate);
        }
    }
}
