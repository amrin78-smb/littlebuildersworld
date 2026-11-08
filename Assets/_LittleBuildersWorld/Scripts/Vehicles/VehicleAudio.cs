using LittleBuildersWorld.Audio;
using LittleBuildersWorld.Core;
using UnityEngine;

namespace LittleBuildersWorld.Vehicles
{
    /// <summary>Soft engine hum whose pitch follows speed. Quiet when parked; follows the SFX volume.</summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class VehicleAudio : MonoBehaviour
    {
        [SerializeField] private VehicleController m_Controller;
        [SerializeField] private VehicleProfile m_Profile;

        private AudioSource m_Source;
        private float m_Level;

        private void Awake()
        {
            m_Source = GetComponent<AudioSource>();
            m_Source.loop = true;
            m_Source.playOnAwake = false;
            m_Source.spatialBlend = 0.6f;
            m_Source.minDistance = 6f;
            m_Source.maxDistance = 40f;
            m_Source.rolloffMode = AudioRolloffMode.Linear;
            m_Source.dopplerLevel = 0f;
            m_Source.volume = 0f;
        }

        private void Start()
        {
            AudioClip clip = m_Profile != null && m_Profile.engineLoop != null ? m_Profile.engineLoop : GameServices.Instance?.Config.audio?.engineLoop;
            if (clip == null) return;
            m_Source.clip = clip;
        }

        private void Update()
        {
            if (m_Source.clip == null || m_Controller == null) return;
            bool driven = m_Controller.IsDriven;
            float sfx = GameServices.Instance != null ? GameServices.Instance.Audio.SfxVolume : 1f;
            float target = driven ? 0.22f : 0f;
            m_Level = Mathf.MoveTowards(m_Level, target, Time.deltaTime * 0.6f);
            if (m_Level > 0.001f && !m_Source.isPlaying) m_Source.Play();
            else if (m_Level <= 0.001f && m_Source.isPlaying) m_Source.Stop();
            float top = m_Profile != null ? m_Profile.tuning.MaxForwardSpeed : 7f;
            float t = Mathf.Clamp01(Mathf.Abs(m_Controller.Speed) / Mathf.Max(1f, top));
            float idle = m_Profile != null ? m_Profile.engineIdlePitch : 0.8f;
            float max = m_Profile != null ? m_Profile.engineTopPitch : 1.3f;
            m_Source.pitch = Mathf.Lerp(idle, max, t);
            m_Source.volume = m_Level * (0.7f + 0.3f * t) * sfx;
        }

        public void Horn()
        {
            float pitch = m_Profile != null ? m_Profile.hornPitch : 1f;
            GameServices.Instance?.Audio.Play(SfxId.Horn, pitch);
        }
    }
}
