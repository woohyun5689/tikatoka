using System.Collections.Generic;
using UnityEngine;

namespace Tikatooka
{
    /// <summary>
    /// Keeps all UI and tabletop one-shots in one small, 2D audio layer.
    /// The controller builds the game at runtime, so clips are loaded from Resources
    /// instead of requiring scene references.
    /// </summary>
    internal sealed class TikatookaGameAudio : MonoBehaviour
    {
        private const string ResourcePrefix = "Audio/";
        private const float ButtonCooldown = 0.04f;

        private AudioSource uiSource;
        private AudioSource diceSource;
        private AudioSource cupShakeSource;

        private AudioClip[] buttonClickClips;
        private AudioClip[] menuOpenClips;
        private AudioClip[] menuCloseClips;
        private AudioClip[] confirmClips;
        private AudioClip[] errorClips;
        private AudioClip[] cupReadyClips;
        private AudioClip[] cupShakeClips;
        private AudioClip[] diceThrowClips;
        private AudioClip[] diceImpactClips;
        private AudioClip[] placementClips;
        private AudioClip[] attackClips;
        private AudioClip[] victoryClips;
        private AudioClip[] drawClips;
        private float nextButtonTime;
        private bool initialized;

        internal void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            uiSource = CreateSource("UI Sound Effects", 0.8f);
            diceSource = CreateSource("Dice Sound Effects", 0.9f);
            cupShakeSource = CreateSource("Dice Cup Shake", 0.28f);

            buttonClickClips = LoadClips("KenneyInterface/click_003");
            menuOpenClips = LoadClips("KenneyInterface/open_002");
            menuCloseClips = LoadClips("KenneyInterface/close_001");
            confirmClips = LoadClips("KenneyInterface/confirmation_003");
            errorClips = LoadClips("KenneyInterface/error_004");
            cupReadyClips = LoadClips(
                "KenneyCasino/dice-grab-1",
                "KenneyCasino/dice-grab-2");
            cupShakeClips = LoadClips(
                "KenneyCasino/dice-shake-1",
                "KenneyCasino/dice-shake-2",
                "KenneyCasino/dice-shake-3");
            diceThrowClips = LoadClips(
                "KenneyCasino/dice-throw-1",
                "KenneyCasino/dice-throw-2",
                "KenneyCasino/dice-throw-3");
            diceImpactClips = LoadClips(
                "KenneyCasino/die-throw-1",
                "KenneyCasino/die-throw-2",
                "KenneyCasino/die-throw-3",
                "KenneyCasino/die-throw-4");
            placementClips = LoadClips(
                "KenneyCasino/chip-lay-1",
                "KenneyCasino/chip-lay-2",
                "KenneyCasino/chip-lay-3");
            attackClips = LoadClips(
                "KenneyImpact/impactWood_light_000",
                "KenneyImpact/impactWood_light_001",
                "KenneyImpact/impactWood_light_002",
                "KenneyImpact/impactWood_light_003",
                "KenneyImpact/impactWood_light_004");
            victoryClips = LoadClips("KenneyJingles/jingles_SAX00");
            drawClips = LoadClips("KenneyJingles/jingles_STEEL00");
        }

        internal void PlayButtonClick()
        {
            EnsureInitialized();
            if (Time.unscaledTime < nextButtonTime)
            {
                return;
            }

            nextButtonTime = Time.unscaledTime + ButtonCooldown;
            PlayVariation(uiSource, buttonClickClips, 0.34f);
        }

        internal void PlayMenuOpen()
        {
            EnsureInitialized();
            PlayVariation(uiSource, menuOpenClips, 0.34f);
        }

        internal void PlayMenuClose()
        {
            EnsureInitialized();
            PlayVariation(uiSource, menuCloseClips, 0.24f);
        }

        internal void PlayConfirm()
        {
            EnsureInitialized();
            PlayVariation(uiSource, confirmClips, 0.42f);
        }

        internal void PlayError()
        {
            EnsureInitialized();
            PlayVariation(uiSource, errorClips, 0.3f);
        }

        internal void PlayCupReady()
        {
            EnsureInitialized();
            PlayVariation(diceSource, cupReadyClips, 0.42f);
        }

        internal void StartCupShake()
        {
            EnsureInitialized();
            if (cupShakeSource == null || cupShakeSource.isPlaying)
            {
                return;
            }

            var clip = Choose(cupShakeClips);
            if (clip == null)
            {
                return;
            }

            cupShakeSource.clip = clip;
            cupShakeSource.loop = true;
            cupShakeSource.volume = 0.23f;
            cupShakeSource.pitch = Random.Range(0.94f, 1.04f);
            cupShakeSource.Play();
        }

        internal void UpdateCupShake(float energy)
        {
            if (cupShakeSource == null || !cupShakeSource.isPlaying)
            {
                return;
            }

            var intensity = Mathf.InverseLerp(0.08f, 1.7f, energy);
            cupShakeSource.volume = Mathf.Lerp(0.17f, 0.37f, intensity);
            cupShakeSource.pitch = Mathf.Lerp(0.92f, 1.09f, intensity);
        }

        internal void StopCupShake()
        {
            if (cupShakeSource == null || !cupShakeSource.isPlaying)
            {
                return;
            }

            cupShakeSource.Stop();
            cupShakeSource.clip = null;
        }

        internal void PlayDiceThrow()
        {
            EnsureInitialized();
            PlayVariation(diceSource, diceThrowClips, 0.55f);
        }

        internal void PlayDiceTableImpact()
        {
            EnsureInitialized();
            PlayVariation(diceSource, diceImpactClips, 0.54f);
        }

        internal void PlayDiePlaced()
        {
            EnsureInitialized();
            PlayVariation(uiSource, placementClips, 0.46f);
        }

        internal void PlayAttackImpact()
        {
            EnsureInitialized();
            PlayVariation(diceSource, attackClips, 0.44f);
        }

        internal void PlayMatchVictory()
        {
            EnsureInitialized();
            PlayVariation(uiSource, victoryClips, 0.54f);
        }

        internal void PlayMatchDraw()
        {
            EnsureInitialized();
            PlayVariation(uiSource, drawClips, 0.38f);
        }

        internal void PlayMatchDefeat()
        {
            EnsureInitialized();
            PlayVariation(uiSource, errorClips, 0.4f);
        }

        private void OnDisable()
        {
            StopCupShake();
        }

        private void EnsureInitialized()
        {
            if (!initialized)
            {
                Initialize();
            }
        }

        private AudioSource CreateSource(string sourceName, float volume)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);
            var source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.dopplerLevel = 0f;
            source.volume = volume;
            return source;
        }

        private static AudioClip[] LoadClips(params string[] clipNames)
        {
            var clips = new List<AudioClip>(clipNames.Length);
            for (var index = 0; index < clipNames.Length; index++)
            {
                var clip = Resources.Load<AudioClip>(ResourcePrefix + clipNames[index]);
                if (clip != null)
                {
                    clips.Add(clip);
                }
            }

            return clips.ToArray();
        }

        private static AudioClip Choose(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return null;
            }

            return clips[Random.Range(0, clips.Length)];
        }

        private static void PlayVariation(AudioSource source, AudioClip[] clips, float volume)
        {
            if (source == null)
            {
                return;
            }

            var clip = Choose(clips);
            if (clip != null)
            {
                source.PlayOneShot(clip, volume);
            }
        }
    }
}
