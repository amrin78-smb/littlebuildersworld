using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleBuildersWorld.Core
{
    /// <summary>Easing curves used by every gentle animation in the game.</summary>
    public enum Ease
    {
        Linear,
        InQuad,
        OutQuad,
        InOutQuad,
        OutCubic,
        InOutSine,
        OutBack,
        InBack,
        OutElastic,
        OutBounce
    }

    public static class Easing
    {
        public static float Evaluate(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
                case Ease.OutBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
                }
                case Ease.InBack:
                {
                    const float c1 = 1.70158f;
                    const float c3 = c1 + 1f;
                    return c3 * t * t * t - c1 * t * t;
                }
                case Ease.OutElastic:
                {
                    if (t <= 0f || t >= 1f) return t;
                    const float c4 = 2f * Mathf.PI / 3f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * c4) + 1f;
                }
                case Ease.OutBounce:
                {
                    const float n1 = 7.5625f;
                    const float d1 = 2.75f;
                    if (t < 1f / d1) return n1 * t * t;
                    if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
                    if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
                    t -= 2.625f / d1;
                    return n1 * t * t + 0.984375f;
                }
                default: return t;
            }
        }
    }

    /// <summary>Handle returned by every tween so callers can cancel it.</summary>
    public sealed class TweenHandle
    {
        internal UnityEngine.Object Owner;
        internal float Duration;
        internal float Delay;
        internal float Elapsed;
        internal Ease Ease;
        internal Action<float> OnUpdate;
        internal Action OnComplete;
        internal bool Unscaled;
        internal bool Killed;
        internal bool Done;
        internal bool HasOwner;

        public bool IsActive => !Killed && !Done;

        public void Kill(bool complete = false)
        {
            if (!IsActive) return;
            if (complete)
            {
                SafeInvoke(OnUpdate, 1f);
                Done = true;
                SafeInvoke(OnComplete);
            }
            Killed = true;
        }

        internal static void SafeInvoke(Action<float> a, float v)
        {
            if (a == null) return;
            try { a(v); } catch (Exception e) { Debug.LogException(e); }
        }

        internal static void SafeInvoke(Action a)
        {
            if (a == null) return;
            try { a(); } catch (Exception e) { Debug.LogException(e); }
        }
    }

    /// <summary>
    /// Tiny dependency-free tween engine. All toy-like motion (hops, squashes, slides)
    /// goes through here so animations are consistent and cheap on mobile.
    /// Tweens are owned by a UnityEngine.Object and stop automatically when it is destroyed.
    /// </summary>
    public sealed class Tween : MonoBehaviour
    {
        private static Tween s_Runner;
        private readonly List<TweenHandle> m_Active = new List<TweenHandle>(64);
        private readonly List<TweenHandle> m_Pending = new List<TweenHandle>(16);

        private static Tween Runner
        {
            get
            {
                if (s_Runner == null)
                {
                    var go = new GameObject("[TweenRunner]");
                    go.hideFlags = HideFlags.HideInHierarchy;
                    if (Application.isPlaying) DontDestroyOnLoad(go);
                    s_Runner = go.AddComponent<Tween>();
                }
                return s_Runner;
            }
        }

        public static int ActiveCount => s_Runner == null ? 0 : s_Runner.m_Active.Count + s_Runner.m_Pending.Count;

        public static TweenHandle Value(UnityEngine.Object owner, float duration, Action<float> onUpdate,
            Ease ease = Ease.OutQuad, Action onComplete = null, float delay = 0f, bool unscaled = false)
        {
            var h = new TweenHandle
            {
                Owner = owner,
                HasOwner = owner != null,
                Duration = Mathf.Max(0.0001f, duration),
                Delay = Mathf.Max(0f, delay),
                Ease = ease,
                OnUpdate = onUpdate,
                OnComplete = onComplete,
                Unscaled = unscaled
            };
            Runner.m_Pending.Add(h);
            return h;
        }

        public static TweenHandle Delay(UnityEngine.Object owner, float seconds, Action action, bool unscaled = false)
        {
            return Value(owner, seconds, null, Ease.Linear, action, 0f, unscaled);
        }

        public static TweenHandle Move(Transform t, Vector3 to, float duration, Ease ease = Ease.OutQuad,
            Action onComplete = null, float delay = 0f)
        {
            Vector3 from = t.position;
            bool started = false;
            return Value(t, duration, k =>
            {
                if (!started) { from = t.position; started = true; }
                t.position = Vector3.LerpUnclamped(from, to, k);
            }, ease, onComplete, delay);
        }

        public static TweenHandle MoveLocal(Transform t, Vector3 to, float duration, Ease ease = Ease.OutQuad,
            Action onComplete = null, float delay = 0f)
        {
            Vector3 from = t.localPosition;
            bool started = false;
            return Value(t, duration, k =>
            {
                if (!started) { from = t.localPosition; started = true; }
                t.localPosition = Vector3.LerpUnclamped(from, to, k);
            }, ease, onComplete, delay);
        }

        /// <summary>Hop along a parabola to a world position (used for snapping food into place).</summary>
        public static TweenHandle Jump(Transform t, Vector3 to, float height, float duration,
            Action onComplete = null, float delay = 0f)
        {
            Vector3 from = t.position;
            bool started = false;
            return Value(t, duration, k =>
            {
                if (!started) { from = t.position; started = true; }
                Vector3 p = Vector3.Lerp(from, to, k);
                p.y += Mathf.Sin(k * Mathf.PI) * height;
                t.position = p;
            }, Ease.InOutSine, onComplete, delay);
        }

        public static TweenHandle Scale(Transform t, Vector3 to, float duration, Ease ease = Ease.OutBack,
            Action onComplete = null, float delay = 0f)
        {
            Vector3 from = t.localScale;
            bool started = false;
            return Value(t, duration, k =>
            {
                if (!started) { from = t.localScale; started = true; }
                t.localScale = Vector3.LerpUnclamped(from, to, k);
            }, ease, onComplete, delay);
        }

        public static TweenHandle RotateLocal(Transform t, Quaternion to, float duration, Ease ease = Ease.OutQuad,
            Action onComplete = null, float delay = 0f)
        {
            Quaternion from = t.localRotation;
            bool started = false;
            return Value(t, duration, k =>
            {
                if (!started) { from = t.localRotation; started = true; }
                t.localRotation = Quaternion.SlerpUnclamped(from, to, k);
            }, ease, onComplete, delay);
        }

        /// <summary>Squash-and-stretch "boing" around a known rest scale.</summary>
        public static TweenHandle Squash(Transform t, Vector3 restScale, float amount = 0.25f, float duration = 0.35f,
            Action onComplete = null)
        {
            return Value(t, duration, k =>
            {
                float wobble = Mathf.Sin(k * Mathf.PI * 2.5f) * (1f - k) * amount;
                t.localScale = new Vector3(restScale.x * (1f + wobble), restScale.y * (1f - wobble),
                    restScale.z * (1f + wobble));
            }, Ease.Linear, () =>
            {
                if (t != null) t.localScale = restScale;
                onComplete?.Invoke();
            });
        }

        /// <summary>Kills all tweens owned by the object.</summary>
        public static void Kill(UnityEngine.Object owner, bool complete = false)
        {
            if (s_Runner == null || owner == null) return;
            KillIn(s_Runner.m_Active, owner, complete);
            KillIn(s_Runner.m_Pending, owner, complete);
        }

        private static void KillIn(List<TweenHandle> list, UnityEngine.Object owner, bool complete)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i].Owner, owner)) list[i].Kill(complete);
            }
        }

        private void Update()
        {
            if (m_Pending.Count > 0)
            {
                m_Active.AddRange(m_Pending);
                m_Pending.Clear();
            }

            float dt = Time.deltaTime;
            float udt = Time.unscaledDeltaTime;
            for (int i = 0; i < m_Active.Count; i++)
            {
                TweenHandle h = m_Active[i];
                if (!h.IsActive) continue;
                if (h.HasOwner && h.Owner == null) { h.Killed = true; continue; }

                float step = h.Unscaled ? udt : dt;
                if (h.Delay > 0f) { h.Delay -= step; continue; }

                h.Elapsed += step;
                float t = Mathf.Clamp01(h.Elapsed / h.Duration);
                TweenHandle.SafeInvoke(h.OnUpdate, Easing.Evaluate(h.Ease, t));
                if (t >= 1f && !h.Killed)
                {
                    h.Done = true;
                    TweenHandle.SafeInvoke(h.OnComplete);
                }
            }

            m_Active.RemoveAll(h => !h.IsActive);
        }

        private void OnDestroy()
        {
            if (s_Runner == this) s_Runner = null;
        }
    }
}
