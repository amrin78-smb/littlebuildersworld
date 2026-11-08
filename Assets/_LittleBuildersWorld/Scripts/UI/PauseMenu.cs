using LittleBuildersWorld.Core;
using UnityEngine;

namespace LittleBuildersWorld.UI
{
    /// <summary>Adult pause (opened by holding the gear): resume, settings, or back to the main menu. Time stops while open.</summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject m_Panel;
        [SerializeField] private TapButton m_Resume;
        [SerializeField] private TapButton m_Settings;
        [SerializeField] private TapButton m_MainMenu;
        [SerializeField] private SettingsPanel m_SettingsPanel;

        private void Awake()
        {
            if (m_Resume != null) m_Resume.Pressed += Close;
            if (m_Settings != null) m_Settings.Pressed += () => m_SettingsPanel?.Open();
            if (m_MainMenu != null) m_MainMenu.Pressed += () => GameServices.Instance?.Scenes.Load(SceneFlow.MainMenu);
            if (m_Panel != null) m_Panel.SetActive(false);
        }

        public bool IsOpen => m_Panel != null && m_Panel.activeSelf;

        public void Open()
        {
            if (m_Panel != null) m_Panel.SetActive(true);
            GameServices.Instance?.State.SetPaused(true);
        }

        public void Close()
        {
            if (m_SettingsPanel != null && m_SettingsPanel.gameObject.activeSelf) m_SettingsPanel.Close();
            if (m_Panel != null) m_Panel.SetActive(false);
            GameServices.Instance?.State.SetPaused(false);
        }

        private void OnDestroy()
        {
            // Never leave time stopped if the scene unloads while paused.
            if (IsOpen) GameServices.Instance?.State.SetPaused(false);
        }
    }
}
