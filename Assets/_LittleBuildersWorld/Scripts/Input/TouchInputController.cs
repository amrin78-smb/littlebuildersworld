using System;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.Save;
using LittleBuildersWorld.UI;
using LittleBuildersWorld.Vehicles;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LittleBuildersWorld.Input
{
    /// <summary>
    /// Merges every driving input into one <see cref="DriveInput"/>: the big HUD buttons (or the optional joystick),
    /// keyboard and gamepad for Editor testing, and a scripted override used by automated tests.
    /// Steering is only ever read from the steering controls, so touching other UI cannot steer.
    /// With Easy Drive on, holding a steering button alone also rolls forward gently: one finger is enough.
    /// </summary>
    public sealed class TouchInputController : MonoBehaviour, IDriveInputSource
    {
        [Header("Touch controls")]
        [SerializeField] private HoldButton m_Left;
        [SerializeField] private HoldButton m_Right;
        [SerializeField] private HoldButton m_Go;
        [SerializeField] private HoldButton m_Back;
        [SerializeField] private VirtualJoystick m_Joystick;
        [SerializeField] private GameObject m_ButtonSteering;
        [SerializeField] private GameObject m_JoystickSteering;

        [Header("Tuning")]
        [Tooltip("Throttle used by Easy Drive when only steering is held.")] [SerializeField] [Range(0f, 1f)] private float m_EasyDriveThrottle = 0.55f;

        private InputAction m_Move;
        private InputAction m_Action;
        private InputAction m_Interact;
        private InputAction m_Horn;
        private InputAction m_Reset;
        private bool m_HasScripted;
        private DriveInput m_Scripted;

        /// <summary>Contextual action (keyboard Space / gamepad South). Touch uses the HUD button directly.</summary>
        public event Action ActionPressed;
        /// <summary>Enter or exit (keyboard E / gamepad East).</summary>
        public event Action InteractPressed;
        public event Action HornPressed;
        public event Action ResetPressed;

        private void Awake()
        {
            m_Move = new InputAction("Move", InputActionType.Value);
            m_Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            m_Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            m_Move.AddBinding("<Gamepad>/leftStick");

            m_Action = Button("Action", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            m_Interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonEast");
            m_Horn = Button("Horn", "<Keyboard>/h", "<Gamepad>/buttonNorth");
            m_Reset = Button("Reset", "<Keyboard>/r", "<Gamepad>/select");

            m_Action.performed += _ => ActionPressed?.Invoke();
            m_Interact.performed += _ => InteractPressed?.Invoke();
            m_Horn.performed += _ => HornPressed?.Invoke();
            m_Reset.performed += _ => ResetPressed?.Invoke();
        }

        private static InputAction Button(string name, string key, string pad)
        {
            var a = new InputAction(name, InputActionType.Button);
            a.AddBinding(key);
            a.AddBinding(pad);
            return a;
        }

        private void OnEnable()
        {
            m_Move.Enable();
            m_Action.Enable();
            m_Interact.Enable();
            m_Horn.Enable();
            m_Reset.Enable();
            ApplySettings();
        }

        private void OnDisable()
        {
            m_Move.Disable();
            m_Action.Disable();
            m_Interact.Disable();
            m_Horn.Disable();
            m_Reset.Disable();
            ReleaseAll();
        }

        private void OnDestroy()
        {
            m_Move.Dispose();
            m_Action.Dispose();
            m_Interact.Dispose();
            m_Horn.Dispose();
            m_Reset.Dispose();
        }

        /// <summary>Shows buttons or joystick according to the saved control setting.</summary>
        public void ApplySettings()
        {
            ControlSettingsData c = GameServices.Instance != null ? GameServices.Instance.Save.Data.controls : new ControlSettingsData();
            bool joystick = c.steering == SteeringMode.Joystick;
            if (m_ButtonSteering != null) m_ButtonSteering.SetActive(!joystick);
            if (m_JoystickSteering != null) m_JoystickSteering.SetActive(joystick);
        }

        /// <summary>Automated tests and the demo: drive without touching anything. null = back to normal input.</summary>
        public void SetScriptedInput(DriveInput? input)
        {
            m_HasScripted = input.HasValue;
            m_Scripted = input ?? default;
        }

        public void ReleaseAll()
        {
            // Unity null checks (not ?.): during scene unload the buttons may already be destroyed.
            if (m_Left != null) m_Left.Release();
            if (m_Right != null) m_Right.Release();
            if (m_Go != null) m_Go.Release();
            if (m_Back != null) m_Back.Release();
            if (m_Joystick != null) m_Joystick.Release();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) ReleaseAll();
        }

        public DriveInput ReadDrive()
        {
            if (m_HasScripted) return m_Scripted;

            Vector2 keys = m_Move.ReadValue<Vector2>();
            float steer = keys.x;
            float throttle = Mathf.Max(0f, keys.y);
            float brake = Mathf.Max(0f, -keys.y);

            Gamepad pad = Gamepad.current;
            if (pad != null)
            {
                throttle = Mathf.Max(throttle, pad.rightTrigger.ReadValue());
                brake = Mathf.Max(brake, pad.leftTrigger.ReadValue());
            }

            bool joystickMode = m_JoystickSteering != null && m_JoystickSteering.activeInHierarchy;
            if (joystickMode && m_Joystick != null && m_Joystick.IsHeld)
            {
                Vector2 j = m_Joystick.Value;
                steer += j.x;
                if (j.y > 0.35f) throttle = Mathf.Max(throttle, j.y);
                if (j.y < -0.35f) brake = Mathf.Max(brake, -j.y);
            }
            else
            {
                if (m_Left != null && m_Left.IsHeld) steer -= 1f;
                if (m_Right != null && m_Right.IsHeld) steer += 1f;
            }
            if (m_Go != null && m_Go.IsHeld) throttle = 1f;
            if (m_Back != null && m_Back.IsHeld) brake = 1f;

            bool easy = GameServices.Instance == null || GameServices.Instance.Save.Data.controls.easyDrive;
            if (easy && Mathf.Abs(steer) > 0.1f && throttle < 0.01f && brake < 0.01f) throttle = m_EasyDriveThrottle;

            return new DriveInput(steer, throttle, brake);
        }
    }
}
