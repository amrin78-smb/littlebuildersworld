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

        private void Update()
        {
            if (Time.timeScale <= 0f) return;
            m_Elapsed += Time.unscaledDeltaTime;
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
