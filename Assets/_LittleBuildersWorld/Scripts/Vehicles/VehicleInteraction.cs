using System;
using LittleBuildersWorld.CameraSystem;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.Input;
using LittleBuildersWorld.Audio;
using UnityEngine;

namespace LittleBuildersWorld.Vehicles
{
    /// <summary>
    /// Getting in and out. Not driving = "on foot": the camera frames the area and each nearby vehicle shows a big
    /// Drive bubble. Entering hands input to that vehicle and swings the camera behind it; exiting stops it safely
    /// where it is. The yard selector can also call any vehicle back to its parking bay and climb in.
    /// A vehicle already being driven cannot be entered again.
    /// </summary>
    public sealed class VehicleInteraction : MonoBehaviour
    {
        [SerializeField] private VehicleCameraController m_Camera;
        [SerializeField] private TouchInputController m_Input;
        [SerializeField] private VehicleActionController m_Actions;
        [Tooltip("Overview spot used at start and by the Home button.")] [SerializeField] private Transform m_YardView;
        [Tooltip("Vehicles within this distance of the overview focus show Drive bubbles.")] [SerializeField] private float m_BubbleRange = 32f;

        private Vehicle m_Current;
        private Vector3 m_FocusPoint;
        private float m_FocusYaw;

        public Vehicle Current => m_Current;
        public bool IsDriving => m_Current != null;
        public Vector3 FocusPoint => m_FocusPoint;
        public float BubbleRange => m_BubbleRange;

        public event Action<Vehicle> Entered;
        public event Action<Vehicle> Exited;

        private void Awake()
        {
            if (m_Camera == null || m_Input == null || m_Actions == null)
                Debug.LogError("[LBW] VehicleInteraction needs Camera, Input and Actions references.", this);
        }

        private void OnEnable()
        {
            if (m_Input != null) m_Input.InteractPressed += OnInteractKey;
        }

        private void OnDisable()
        {
            if (m_Input != null) m_Input.InteractPressed -= OnInteractKey;
        }

        private void Start()
        {
            GoToYard(true);
        }

        public bool CanEnter(Vehicle v)
        {
            if (v == null || v.IsOccupied || v == m_Current) return false;
            GameServices s = GameServices.Instance;
            return s == null || s.Progression.IsVehicleAvailable(v.Profile);
        }

        public bool Enter(Vehicle v)
        {
            if (!CanEnter(v)) return false;
            if (m_Current != null) ExitInternal(false);
            m_Current = v;
            v.Controller.SetInputSource(m_Input);
            GameServices s = GameServices.Instance;
            if (s != null)
            {
                v.Controller.SetSteerSensitivity(s.Save.Data.controls.steerSensitivity);
                s.Progression.SelectedVehicle = v.Id;
                s.State.SetMode(GameMode.Driving);
                s.Audio.Play(SfxId.EngineStart, v.Profile != null ? v.Profile.engineIdlePitch + 0.2f : 1f);
            }
            v.SetOccupied(true);
            m_Input.ReleaseAll();
            m_Actions.SetVehicle(v);
            m_Camera.Follow(v);
            Entered?.Invoke(v);
            return true;
        }

        public void Exit() => ExitInternal(true);

        private void ExitInternal(bool toOverview)
        {
            if (m_Current == null) return;
            Vehicle v = m_Current;
            m_Input.ReleaseAll();
            v.SetOccupied(false);
            v.Controller.SetInputSource(null);
            m_Current = null;
            m_Actions.SetVehicle(null);
            if (toOverview)
            {
                m_FocusPoint = v.transform.position;
                m_FocusYaw = m_Camera.transform.eulerAngles.y;
                m_Camera.Overview(m_FocusPoint, m_FocusYaw);
                GameServices.Instance?.State.SetMode(GameMode.OnFoot);
                GameServices.Instance?.Save.RequestSave();
            }
            Exited?.Invoke(v);
        }

        /// <summary>Home button: overview of the construction yard (autosaves — a safe point).</summary>
        public void GoToYard(bool snap)
        {
            ExitInternal(false);
            m_FocusPoint = m_YardView != null ? m_YardView.position : Vector3.zero;
            m_FocusYaw = m_YardView != null ? m_YardView.eulerAngles.y : 0f;
            m_Camera.Overview(m_FocusPoint, m_FocusYaw);
            if (snap) m_Camera.Snap();
            GameServices.Instance?.State.SetMode(GameMode.OnFoot);
            GameServices.Instance?.Save.RequestSave();
        }

        /// <summary>Vehicle selector: bring the vehicle back to its bay if it is far away, then drive it.</summary>
        public bool Select(Vehicle v)
        {
            if (!CanEnter(v)) return false;
            if ((v.transform.position - m_FocusPoint).sqrMagnitude > m_BubbleRange * m_BubbleRange)
            {
                v.ReturnHome();
                GameServices.Instance?.Audio.Play(SfxId.Whoosh);
            }
            return Enter(v);
        }

        public bool IsNearFocus(Vehicle v) =>
            v != null && (v.transform.position - m_FocusPoint).sqrMagnitude <= m_BubbleRange * m_BubbleRange;

        private void OnInteractKey()
        {
            if (GameServices.Instance != null && GameServices.Instance.State.IsPaused) return;
            if (m_Current != null)
            {
                Exit();
                return;
            }
            Vehicle best = null;
            float bestSq = m_BubbleRange * m_BubbleRange;
            foreach (Vehicle v in Vehicle.All)
            {
                if (!CanEnter(v)) continue;
                float sq = (v.transform.position - m_FocusPoint).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = v; }
            }
            if (best != null) Enter(best);
        }
    }
}
