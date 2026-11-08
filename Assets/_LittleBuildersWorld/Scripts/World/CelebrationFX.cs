using LittleBuildersWorld.Activities;
using LittleBuildersWorld.Audio;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.Vehicles;
using UnityEngine;

namespace LittleBuildersWorld.World
{
    /// <summary>World feedback: sparkles for each bit of work, confetti and a jingle for stages, a big burst for projects.</summary>
    public sealed class CelebrationFX : MonoBehaviour
    {
        [SerializeField] private ParticleSystem m_Sparkle;
        [SerializeField] private ParticleSystem m_Confetti;
        [SerializeField] private ParticleSystem m_Poof;

        private void OnEnable()
        {
            ActivityEvents.Celebrate += OnCelebrate;
            VehicleRecovery.Recovered += OnRecovered;
        }

        private void OnDisable()
        {
            ActivityEvents.Celebrate -= OnCelebrate;
            VehicleRecovery.Recovered -= OnRecovered;
        }

        private void OnCelebrate(Vector3 at, CelebrationSize size)
        {
            AudioManager audio = GameServices.Instance != null ? GameServices.Instance.Audio : null;
            switch (size)
            {
                case CelebrationSize.Small:
                    Emit(m_Sparkle, at + Vector3.up, 18);
                    audio?.Play(SfxId.Sparkle);
                    break;
                case CelebrationSize.Stage:
                    Emit(m_Confetti, at + Vector3.up * 2f, 60);
                    audio?.Play(SfxId.Celebration, 1f, 0.8f);
                    break;
                default:
                    Emit(m_Confetti, at + Vector3.up * 2f, 140);
                    Emit(m_Confetti, at + new Vector3(3f, 3f, 0f), 80);
                    Emit(m_Confetti, at + new Vector3(-3f, 3f, 0f), 80);
                    audio?.Play(SfxId.Celebration);
                    break;
            }
        }

        private void OnRecovered(Vehicle v, Vector3 from)
        {
            Emit(m_Poof, from + Vector3.up, 20);
            if (v != null) Emit(m_Poof, v.transform.position + Vector3.up, 20);
            GameServices.Instance?.Audio.Play(SfxId.Boing);
        }

        private static void Emit(ParticleSystem ps, Vector3 at, int count)
        {
            if (ps == null) return;
            ps.transform.position = at;
            ps.Emit(count);
        }
    }
}
