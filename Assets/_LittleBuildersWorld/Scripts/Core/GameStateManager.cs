using System;
using UnityEngine;

namespace LittleBuildersWorld.Core
{
    public enum GameMode
    {
        Menu,
        /// <summary>In the town, not driving: choose a vehicle.</summary>
        OnFoot,
        Driving
    }

    /// <summary>Tracks the current mode and the adult pause. Pausing stops time and audio; nothing is lost.</summary>
    public sealed class GameStateManager : MonoBehaviour
    {
        public GameMode Mode { get; private set; } = GameMode.Menu;
        public bool IsPaused { get; private set; }

        public event Action<GameMode> ModeChanged;
        public event Action<bool> PauseChanged;

        public void SetMode(GameMode mode)
        {
            if (Mode == mode) return;
            Mode = mode;
            ModeChanged?.Invoke(mode);
        }

        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            PauseChanged?.Invoke(paused);
        }

        private void OnApplicationPause(bool appPaused)
        {
            // The OS suspended us: keep audio quiet. Gameplay resumes untouched; the adult pause is left as it was.
            if (!IsPaused) AudioListener.pause = appPaused;
        }
    }
}
