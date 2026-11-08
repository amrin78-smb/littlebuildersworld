using System.Collections;
using System.Collections.Generic;
using LittleBuildersWorld.Audio;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LittleBuildersWorld.UI
{
    /// <summary>
    /// Builder Star counter plus the reward moment: stars pop out where the work happened, swoop to the counter,
    /// and the counter bounces. A new unlock shows a big picture card for a few seconds. Never blocks play.
    /// </summary>
    public sealed class StarCelebrationUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_Count;
        [SerializeField] private RectTransform m_CounterIcon;
        [SerializeField] private RectTransform m_FlyLayer;
        [SerializeField] private Image m_FlyingStarPrefab;
        [SerializeField] private Camera m_WorldCamera;
        [SerializeField] private GameObject m_UnlockCard;
        [SerializeField] private Image m_UnlockIcon;
        [SerializeField] private Image m_UnlockSwatch;

        private readonly Queue<UnlockDefinition> m_Unlocks = new Queue<UnlockDefinition>();
        private readonly List<Image> m_Pool = new List<Image>();
        private int m_Shown;
        private bool m_ShowingUnlock;

        private void OnEnable()
        {
            GameServices s = GameServices.Instance;
            if (s == null) return;
            s.Stars.StarsAwarded += OnAwarded;
            s.Stars.Unlocked += OnUnlocked;
            s.Stars.TotalChanged += OnTotalChanged;
            m_Shown = s.Stars.Total;
            SetCount(m_Shown);
            if (m_UnlockCard != null) m_UnlockCard.SetActive(false);
        }

        private void OnDisable()
        {
            GameServices s = GameServices.Instance;
            if (s == null) return;
            s.Stars.StarsAwarded -= OnAwarded;
            s.Stars.Unlocked -= OnUnlocked;
            s.Stars.TotalChanged -= OnTotalChanged;
        }

        private void OnTotalChanged()
        {
            // Resets and loads update immediately; awards count up as their stars land.
            int total = GameServices.Instance.Stars.Total;
            if (total < m_Shown) { m_Shown = total; SetCount(total); }
        }

        private void SetCount(int n)
        {
            if (m_Count != null) m_Count.text = n.ToString();
        }

        private void OnAwarded(int amount, int total, Vector3 world) => StartCoroutine(FlyStars(amount, world));

        private IEnumerator FlyStars(int amount, Vector3 world)
        {
            Vector2 start = Vector2.zero;
            if (m_WorldCamera != null)
            {
                Vector3 sp = m_WorldCamera.WorldToScreenPoint(world + Vector3.up * 3f);
                if (sp.z > 0f) RectTransformUtility.ScreenPointToLocalPointInRectangle(m_FlyLayer, sp, null, out start);
            }
            for (int i = 0; i < amount; i++)
            {
                StartCoroutine(FlyOne(start + Random.insideUnitCircle * 60f));
                yield return new WaitForSecondsRealtime(0.18f);
            }
        }

        private IEnumerator FlyOne(Vector2 from)
        {
            Image star = Take();
            var rt = (RectTransform)star.transform;
            Vector2 to = m_FlyLayer.InverseTransformPoint(m_CounterIcon.position);
            rt.anchoredPosition = from;
            rt.localScale = Vector3.zero;
            GameServices.Instance?.Audio.Play(SfxId.Star, Random.Range(0.95f, 1.15f));
            for (float t = 0f; t < 0.35f; t += Time.unscaledDeltaTime)
            {
                rt.localScale = Vector3.one * Easing.Evaluate(Ease.OutBack, t / 0.35f) * 1.6f;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.25f);
            Vector2 control = (from + to) * 0.5f + new Vector2(0f, 260f);
            for (float t = 0f; t < 0.7f; t += Time.unscaledDeltaTime)
            {
                float k = Easing.Evaluate(Ease.InOutSine, t / 0.7f);
                Vector2 a = Vector2.Lerp(from, control, k), b = Vector2.Lerp(control, to, k);
                rt.anchoredPosition = Vector2.Lerp(a, b, k);
                rt.localScale = Vector3.one * Mathf.Lerp(1.6f, 0.8f, k);
                rt.localRotation = Quaternion.Euler(0f, 0f, k * 360f);
                yield return null;
            }
            star.gameObject.SetActive(false);
            m_Shown = Mathf.Min(m_Shown + 1, GameServices.Instance != null ? GameServices.Instance.Stars.Total : m_Shown + 1);
            SetCount(m_Shown);
            if (m_CounterIcon != null) Tween.Squash(m_CounterIcon, Vector3.one, 0.3f, 0.35f);
        }

        private Image Take()
        {
            foreach (Image i in m_Pool)
            {
                if (!i.gameObject.activeSelf) { i.gameObject.SetActive(true); return i; }
            }
            Image img = Instantiate(m_FlyingStarPrefab, m_FlyLayer);
            img.gameObject.SetActive(true);
            m_Pool.Add(img);
            return img;
        }

        private void OnUnlocked(UnlockDefinition u)
        {
            m_Unlocks.Enqueue(u);
            if (!m_ShowingUnlock) StartCoroutine(ShowUnlocks());
        }

        private IEnumerator ShowUnlocks()
        {
            m_ShowingUnlock = true;
            yield return new WaitForSecondsRealtime(1.6f); // let the stars land first
            while (m_Unlocks.Count > 0)
            {
                UnlockDefinition u = m_Unlocks.Dequeue();
                if (m_UnlockCard != null)
                {
                    if (m_UnlockIcon != null) m_UnlockIcon.sprite = u.icon;
                    if (m_UnlockSwatch != null) m_UnlockSwatch.color = u.colour;
                    m_UnlockCard.SetActive(true);
                    m_UnlockCard.transform.localScale = Vector3.zero;
                    Tween.Scale(m_UnlockCard.transform, Vector3.one * 0.7f, 0.45f, Ease.OutBack);
                    GameServices.Instance?.Audio.Play(SfxId.Unlock);
                    yield return new WaitForSecondsRealtime(3f);
                    Tween.Scale(m_UnlockCard.transform, Vector3.zero, 0.3f, Ease.InBack);
                    yield return new WaitForSecondsRealtime(0.35f);
                    m_UnlockCard.SetActive(false);
                }
            }
            m_ShowingUnlock = false;
        }
    }
}
