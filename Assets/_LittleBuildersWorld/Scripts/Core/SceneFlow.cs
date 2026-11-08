using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LittleBuildersWorld.Core
{
    /// <summary>Scene names and soft fade transitions between them.</summary>
    public sealed class SceneFlow : MonoBehaviour
    {
        public const string Boot = "Boot";
        public const string MainMenu = "MainMenu";
        public const string Town = "ConstructionTown";

        private const float FadeSeconds = 0.35f;

        private CanvasGroup m_Fade;
        private bool m_Loading;

        public bool IsLoading => m_Loading;

        private void Awake()
        {
            var canvasGo = new GameObject("[Fade]", typeof(Canvas), typeof(CanvasGroup));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            m_Fade = canvasGo.GetComponent<CanvasGroup>();
            m_Fade.alpha = 0f;
            m_Fade.blocksRaycasts = false;
            var img = new GameObject("Colour", typeof(RectTransform), typeof(Image));
            img.transform.SetParent(canvasGo.transform, false);
            var rt = (RectTransform)img.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            img.GetComponent<Image>().color = new Color(1f, 0.93f, 0.78f, 1f);
        }

        public void Load(string sceneName)
        {
            if (m_Loading) return;
            StartCoroutine(LoadRoutine(sceneName));
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            m_Loading = true;
            m_Fade.blocksRaycasts = true;
            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                m_Fade.alpha = t / FadeSeconds;
                yield return null;
            }
            m_Fade.alpha = 1f;
            GameServices.Instance?.State.SetPaused(false);
            GameServices.Instance?.Save.SaveNow();
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (op != null && !op.isDone) yield return null;
            yield return null;
            for (float t = 0f; t < FadeSeconds; t += Time.unscaledDeltaTime)
            {
                m_Fade.alpha = 1f - t / FadeSeconds;
                yield return null;
            }
            m_Fade.alpha = 0f;
            m_Fade.blocksRaycasts = false;
            m_Loading = false;
        }
    }
}
