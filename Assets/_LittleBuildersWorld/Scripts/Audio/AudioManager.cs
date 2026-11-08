using LittleBuildersWorld.Save;
using UnityEngine;

namespace LittleBuildersWorld.Audio
{
    /// <summary>
    /// Routes music, sound effects and voice on separate volumes taken from the save. Gentle by design: short pooled
    /// SFX with slight pitch variation, a per-clip volume cap and no overlapping voice lines. Fully optional: the game
    /// is playable muted.
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        private const int PoolSize = 8;
        private const float MinRepeatSeconds = 0.06f;

        private AudioLibrary m_Library;
        private SaveSystem m_Save;
        private AudioSource m_Music;
        private AudioSource m_Voice;
        private AudioSource[] m_Pool;
        private int m_Next;
        private readonly float[] m_LastPlayed = new float[64];

        public float SfxVolume => m_Save != null ? m_Save.Data.audio.sfx : 1f;
        public AudioLibrary Library => m_Library;

        public void Initialise(AudioLibrary library, SaveSystem save)
        {
            m_Library = library;
            m_Save = save;
            m_Music = gameObject.AddComponent<AudioSource>();
            m_Music.loop = true;
            m_Music.playOnAwake = false;
            m_Voice = gameObject.AddComponent<AudioSource>();
            m_Voice.playOnAwake = false;
            m_Pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
            {
                m_Pool[i] = gameObject.AddComponent<AudioSource>();
                m_Pool[i].playOnAwake = false;
            }
            ApplySettings();
        }

        public void ApplySettings()
        {
            if (m_Save == null) return;
            AudioSettingsData a = m_Save.Data.audio;
            m_Music.volume = a.music * 0.5f;
            m_Voice.volume = a.voicePrompts ? a.voice : 0f;
        }

        public void PlayMusic(AudioClip clip)
        {
            if (m_Music == null || clip == null) return;
            if (m_Music.clip == clip && m_Music.isPlaying) return;
            m_Music.clip = clip;
            m_Music.Play();
        }

        public void StopMusic() => m_Music?.Stop();

        public void Play(SfxId id, float pitch = 1f, float volumeScale = 1f)
        {
            if (m_Library == null || m_Pool == null || !m_Library.TryGet(id, out AudioLibrary.Entry e)) return;
            int idx = (int)id;
            if (idx < m_LastPlayed.Length)
            {
                if (Time.unscaledTime - m_LastPlayed[idx] < MinRepeatSeconds) return;
                m_LastPlayed[idx] = Time.unscaledTime;
            }
            AudioSource s = m_Pool[m_Next];
            m_Next = (m_Next + 1) % m_Pool.Length;
            s.pitch = pitch * Random.Range(0.96f, 1.04f);
            s.PlayOneShot(e.clip, Mathf.Clamp01((e.volume <= 0f ? 1f : e.volume) * volumeScale) * SfxVolume);
        }

        /// <summary>Optional spoken prompt. Skipped if one is still playing, so lines never pile up.</summary>
        public void PlayVoice(AudioClip clip)
        {
            if (clip == null || m_Voice == null || m_Voice.isPlaying) return;
            if (m_Save != null && !m_Save.Data.audio.voicePrompts) return;
            m_Voice.clip = clip;
            m_Voice.Play();
        }
    }
}
