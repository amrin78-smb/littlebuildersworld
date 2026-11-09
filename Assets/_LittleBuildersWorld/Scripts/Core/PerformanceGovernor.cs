using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LittleBuildersWorld.Core
{
    /// <summary>
    /// Keeps budget phones smooth: if the frame rate stays well below target, first lower the render scale, then turn
    /// off real-time shadows. Never raises quality again during a session (no oscillation). Logs what it did.
    /// </summary>
    public sealed class PerformanceGovernor : MonoBehaviour
    {
        private const float SampleSeconds = 4f;
        private const float LowFps = 40f;

        private float m_Elapsed;
        private int m_Frames;
        private int m_Step;
        private float m_WarmUpUntil;

        public float AverageFps { get; private set; } = 60f;
        public int Step => m_Step;

        private void Start() => m_WarmUpUntil = Time.unscaledTime + 6f;

        // Scene loads and returning from the background stall a few frames; never judge the phone by those.
        private void OnEnable() => UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode m) => Restart(4f);
        private void OnApplicationPause(bool paused) { if (!paused) Restart(4f); }
        private void OnApplicationFocus(bool focused) { if (focused) Restart(4f); }

        private void Restart(float warmUp)
        {
            m_Elapsed = 0f;
            m_Frames = 0;
            m_WarmUpUntil = Mathf.Max(m_WarmUpUntil, Time.unscaledTime + warmUp);
        }

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            float dt = Time.unscaledDeltaTime;
            if (dt > 0.25f) { m_Elapsed = 0f; m_Frames = 0; return; } // a hitch, not the steady frame rate
            m_Elapsed += dt;
            m_Frames++;
            if (m_Elapsed < SampleSeconds) return;
            AverageFps = m_Frames / m_Elapsed;
            m_Elapsed = 0f;
            m_Frames = 0;
            if (Time.unscaledTime < m_WarmUpUntil || Application.isEditor) return;
            if (AverageFps < LowFps && m_Step < 2) StepDown();
        }

        private void StepDown()
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null) return;
            m_Step++;
            if (m_Step == 1) urp.renderScale = Mathf.Min(urp.renderScale, 0.8f);
            else urp.shadowDistance = 0f;
            Debug.Log($"[LBW Perf] Average {AverageFps:0} FPS; quality step {m_Step} applied.");
        }
    }
}
