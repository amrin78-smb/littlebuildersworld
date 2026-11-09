using LittleBuildersWorld.Core;
using LittleBuildersWorld.Input;
using LittleBuildersWorld.Vehicles;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBuildersWorld.UI
{
    /// <summary>
    /// Shows the driving controls while driving and the vehicle picker otherwise. Wires the big buttons to the
    /// systems they drive, and swaps the contextual action icon. Minimal by design: icons, no reading.
    /// </summary>
    public sealed class HUDController : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private VehicleInteraction m_Interaction;
        [SerializeField] private VehicleActionController m_Actions;
        [SerializeField] private TouchInputController m_Input;
        [SerializeField] private PauseMenu m_Pause;

        [Header("Panels")]
        [SerializeField] private GameObject m_DrivePanel;
        [SerializeField] private GameObject m_FootPanel;

        [Header("Driving")]
        [SerializeField] private GameObject m_ActionRoot;
        [SerializeField] private TapButton m_Action;
        [SerializeField] private Image m_ActionIcon;
        [SerializeField] private TapButton m_Exit;
        [SerializeField] private TapButton m_Horn;
        [SerializeField] private TapButton m_Reset;
        [SerializeField] private Pulse m_ResetPulse;

        [Header("On foot")]
        [SerializeField] private TapButton m_Home;
        [SerializeField] private TapButton m_Paint;

        [Header("Always")]
        [SerializeField] private HoldToConfirmButton m_PauseButton;

        private GameConfig m_Config;
        private VehicleRecovery m_Recovery;
        private float m_SwapAt = -10f;

        /// <summary>Exit and Home share a screen spot: a double tap on Exit must not also press Home.</summary>
        private bool JustSwapped => Time.unscaledTime - m_SwapAt < 0.45f;

        private void Awake()
        {
            if (m_Interaction == null || m_Actions == null || m_Input == null)
                Debug.LogError("[LBW] HUDController is missing system references.", this);
        }

        private void OnEnable()
        {
            m_Config = GameServices.Instance != null ? GameServices.Instance.Config : null;
            m_Interaction.Entered += OnEntered;
            m_Interaction.Exited += OnExited;
            m_Actions.OfferChanged += OnOfferChanged;
            m_Input.ActionPressed += OnAction;
            m_Input.HornPressed += OnHorn;
            m_Input.ResetPressed += OnReset;
            if (m_Action != null) m_Action.Pressed += OnAction;
            if (m_Exit != null) m_Exit.Pressed += OnExit;
            if (m_Horn != null) m_Horn.Pressed += OnHorn;
            if (m_Reset != null) m_Reset.Pressed += OnReset;
            if (m_Home != null) m_Home.Pressed += OnHome;
            if (m_Paint != null) m_Paint.Pressed += OnPaint;
            if (m_PauseButton != null) m_PauseButton.Confirmed += OnPause;
            if (GameServices.Instance != null) GameServices.Instance.Stars.TotalChanged += RefreshFoot;
        }

        private void OnDisable()
        {
            m_Interaction.Entered -= OnEntered;
            m_Interaction.Exited -= OnExited;
            m_Actions.OfferChanged -= OnOfferChanged;
            m_Input.ActionPressed -= OnAction;
            m_Input.HornPressed -= OnHorn;
            m_Input.ResetPressed -= OnReset;
            if (m_Action != null) m_Action.Pressed -= OnAction;
            if (m_Exit != null) m_Exit.Pressed -= OnExit;
            if (m_Horn != null) m_Horn.Pressed -= OnHorn;
            if (m_Reset != null) m_Reset.Pressed -= OnReset;
            if (m_Home != null) m_Home.Pressed -= OnHome;
            if (m_Paint != null) m_Paint.Pressed -= OnPaint;
            if (m_PauseButton != null) m_PauseButton.Confirmed -= OnPause;
            if (GameServices.Instance != null) GameServices.Instance.Stars.TotalChanged -= RefreshFoot;
        }

        private void Start()
        {
            ShowDriving(m_Interaction.IsDriving);
            OnOfferChanged(ActionIcon.None);
        }

        private void Update()
        {
            if (m_ResetPulse != null)
            {
                bool stuck = m_Recovery != null && m_Recovery.SeemsStuck;
                if (m_ResetPulse.Active != stuck)
                {
                    m_ResetPulse.Active = stuck;
                    if (!stuck) m_ResetPulse.transform.localScale = Vector3.one;
                }
            }
        }

        private void ShowDriving(bool driving)
        {
            if (m_FootPanel != null && m_FootPanel.activeSelf == driving) m_SwapAt = Time.unscaledTime;
            if (m_DrivePanel != null) m_DrivePanel.SetActive(driving);
            if (m_FootPanel != null) m_FootPanel.SetActive(!driving);
            RefreshFoot();
        }

        private void RefreshFoot()
        {
            if (m_Paint != null && GameServices.Instance != null)
                m_Paint.gameObject.SetActive(GameServices.Instance.Progression.AvailablePaintCount() > 1);
        }

        private void OnEntered(Vehicle v)
        {
            m_Recovery = v.GetComponent<VehicleRecovery>();
            ShowDriving(true);
        }

        private void OnExited(Vehicle v)
        {
            m_Recovery = null;
            ShowDriving(m_Interaction.IsDriving);
        }

        private void OnOfferChanged(ActionIcon icon)
        {
            bool show = icon != ActionIcon.None;
            if (m_ActionRoot != null) m_ActionRoot.SetActive(show);
            if (show && m_ActionIcon != null && m_Config != null) m_ActionIcon.sprite = m_Config.IconFor(icon);
        }

        private bool Blocked => GameServices.Instance != null && GameServices.Instance.State.IsPaused;

        private void OnAction()
        {
            if (!Blocked) m_Actions.Perform();
        }

        private void OnExit()
        {
            if (!Blocked && !JustSwapped) m_Interaction.Exit();
        }

        private void OnHorn()
        {
            if (Blocked || m_Interaction.Current == null) return;
            m_Interaction.Current.GetComponent<VehicleAudio>()?.Horn();
        }

        private void OnReset()
        {
            if (Blocked || m_Recovery == null) return;
            m_Recovery.Recover();
        }

        private void OnHome()
        {
            if (!Blocked && !JustSwapped) m_Interaction.GoToYard(false);
        }

        private void OnPaint()
        {
            if (Blocked || JustSwapped || GameServices.Instance == null) return;
            Vehicle target = NearestToFocus();
            if (target != null) GameServices.Instance.Progression.CyclePaint(target.Id);
            GameServices.Instance.Audio.Play(Audio.SfxId.Sparkle);
        }

        private Vehicle NearestToFocus()
        {
            Vehicle best = null;
            float bestSq = float.MaxValue;
            string selected = GameServices.Instance.Progression.SelectedVehicle;
            foreach (Vehicle v in Vehicle.All)
            {
                if (!m_Interaction.IsNearFocus(v)) continue;
                if (v.Id == selected) return v;
                float sq = (v.transform.position - m_Interaction.FocusPoint).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = v; }
            }
            return best;
        }

        private void OnPause()
        {
            if (m_Pause != null) m_Pause.Open();
        }
    }
}
