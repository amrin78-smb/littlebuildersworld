using LittleBuildersWorld.Core;
using UnityEngine;

namespace LittleBuildersWorld.UI
{
    /// <summary>Main menu: one huge Play button and a held gear for grown-up settings. No links, no store.</summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private TapButton m_Play;
        [SerializeField] private HoldToConfirmButton m_SettingsGear;
        [SerializeField] private SettingsPanel m_Settings;

        private void Awake()
        {
            if (m_Play != null) m_Play.Pressed += Play;
            if (m_SettingsGear != null) m_SettingsGear.Confirmed += () => m_Settings?.Open();
            if (m_Settings != null) m_Settings.gameObject.SetActive(false);
        }

        private void Start()
        {
            GameServices s = GameServices.Instance;
            if (s == null) return;
            s.State.SetMode(GameMode.Menu);
            s.Audio.PlayMusic(s.Config.audio != null ? s.Config.audio.menuMusic : null);
        }

        public void Play() => GameServices.Instance?.Scenes.Load(SceneFlow.Town);
    }
}
