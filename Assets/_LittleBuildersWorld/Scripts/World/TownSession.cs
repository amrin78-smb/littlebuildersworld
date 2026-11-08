using LittleBuildersWorld.Core;
using LittleBuildersWorld.Vehicles;
using UnityEngine;

namespace LittleBuildersWorld.World
{
    /// <summary>
    /// Town scene start-up: town music, every vehicle parked in its bay (vehicle positions are not saved; parking in the
    /// yard on load is the safe, predictable restore), and the cosmetic yard decorations matching unlocked rewards.
    /// </summary>
    public sealed class TownSession : MonoBehaviour
    {
        [System.Serializable]
        public struct Decoration
        {
            public string unlockId;
            public GameObject root;
        }

        [SerializeField] private Decoration[] m_Decorations = new Decoration[0];

        private void Start()
        {
            GameServices s = GameServices.Instance;
            if (s == null) return;
            s.Audio.PlayMusic(s.Config.audio != null ? s.Config.audio.townMusic : null);
            s.Stars.Unlocked += OnUnlocked;
            foreach (Decoration d in m_Decorations)
            {
                if (d.root != null) d.root.SetActive(s.Stars.IsUnlocked(d.unlockId));
            }
            foreach (Vehicle v in Vehicle.All) v.Controller.Park();
        }

        private void OnDestroy()
        {
            if (GameServices.Instance != null) GameServices.Instance.Stars.Unlocked -= OnUnlocked;
        }

        private void OnUnlocked(Progression.UnlockDefinition u)
        {
            foreach (Decoration d in m_Decorations)
            {
                if (d.root == null || d.unlockId != u.id || d.root.activeSelf) continue;
                d.root.SetActive(true);
                Vector3 rest = d.root.transform.localScale;
                d.root.transform.localScale = Vector3.zero;
                Tween.Scale(d.root.transform, rest, 0.6f, Ease.OutBack);
            }
        }
    }
}
