using LittleBuildersWorld.Core;
using LittleBuildersWorld.Input;
using LittleBuildersWorld.Save;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LittleBuildersWorld.UI
{
    /// <summary>
    /// Grown-up settings: music, sound effects and voice volumes, voice prompts on/off, steering style, Easy Drive,
    /// steering sensitivity, and reset progress (behind the parent gate). Changes apply at once and are saved.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private Slider m_Music;
        [SerializeField] private Slider m_Sfx;
        [SerializeField] private Slider m_Voice;
        [SerializeField] private Toggle m_VoicePrompts;
        [SerializeField] private Toggle m_Joystick;
        [SerializeField] private Toggle m_EasyDrive;
        [SerializeField] private Slider m_Sensitivity;
        [SerializeField] private TapButton m_Reset;
        [SerializeField] private TapButton m_Close;
        [SerializeField] private ParentGate m_Gate;
        [SerializeField] private TouchInputController m_Input;

        private bool m_Binding;

        private void Awake()
        {
            m_Music?.onValueChanged.AddListener(_ => Store());
            m_Sfx?.onValueChanged.AddListener(_ => Store());
            m_Voice?.onValueChanged.AddListener(_ => Store());
            m_VoicePrompts?.onValueChanged.AddListener(_ => Store());
            m_Joystick?.onValueChanged.AddListener(_ => Store());
            m_EasyDrive?.onValueChanged.AddListener(_ => Store());
            m_Sensitivity?.onValueChanged.AddListener(_ => Store());
            if (m_Reset != null) m_Reset.Pressed += AskReset;
            if (m_Close != null) m_Close.Pressed += Close;
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Load();
        }

        public void Close()
        {
            GameServices.Instance?.Save.SaveNow();
            gameObject.SetActive(false);
        }

        private void Load()
        {
            GameServices s = GameServices.Instance;
            if (s == null) return;
            SaveData d = s.Save.Data;
            m_Binding = true;
            if (m_Music != null) m_Music.value = d.audio.music;
            if (m_Sfx != null) m_Sfx.value = d.audio.sfx;
            if (m_Voice != null) m_Voice.value = d.audio.voice;
            if (m_VoicePrompts != null) m_VoicePrompts.isOn = d.audio.voicePrompts;
            if (m_Joystick != null) m_Joystick.isOn = d.controls.steering == SteeringMode.Joystick;
            if (m_EasyDrive != null) m_EasyDrive.isOn = d.controls.easyDrive;
            if (m_Sensitivity != null) m_Sensitivity.value = d.controls.steerSensitivity;
            m_Binding = false;
        }

        private void Store()
        {
            if (m_Binding) return;
            GameServices s = GameServices.Instance;
            if (s == null) return;
            SaveData d = s.Save.Data;
            if (m_Music != null) d.audio.music = m_Music.value;
            if (m_Sfx != null) d.audio.sfx = m_Sfx.value;
            if (m_Voice != null) d.audio.voice = m_Voice.value;
            if (m_VoicePrompts != null) d.audio.voicePrompts = m_VoicePrompts.isOn;
            if (m_Joystick != null) d.controls.steering = m_Joystick.isOn ? SteeringMode.Joystick : SteeringMode.Buttons;
            if (m_EasyDrive != null) d.controls.easyDrive = m_EasyDrive.isOn;
            if (m_Sensitivity != null) d.controls.steerSensitivity = m_Sensitivity.value;
            d.Sanitise();
            s.Audio.ApplySettings();
            if (m_Input != null) m_Input.ApplySettings();
            s.Save.RequestSave();
        }

        private void AskReset()
        {
            if (m_Gate != null) m_Gate.Ask(DoReset);
        }

        private void DoReset()
        {
            GameServices s = GameServices.Instance;
            if (s == null) return;
            s.Save.ResetProgress();
            s.Stars.Refresh();
            // Reload so every activity and vehicle rebuilds from the fresh save.
            s.Scenes.Load(SceneManager.GetActiveScene().name);
        }
    }
}
