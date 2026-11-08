using System.Collections.Generic;
using LittleBuildersWorld.Core;
using UnityEngine;

namespace LittleBuildersWorld.World
{
    /// <summary>
    /// A rock, log or crate the bulldozer can push. Moved by script only (no rigidbody), so it slides predictably,
    /// can never launch or roll away, and never blocks a vehicle. Returns home if it leaves its activity area.
    /// </summary>
    public sealed class PushableDebris : MonoBehaviour
    {
        private static readonly List<PushableDebris> s_All = new List<PushableDebris>();
        public static IReadOnlyList<PushableDebris> All => s_All;

        [SerializeField] private float m_Radius = 0.8f;

        private Vector3 m_HomePosition;
        private Quaternion m_HomeRotation;
        private Vector3 m_RestScale;

        public float Radius => m_Radius;
        public bool IsCleared { get; private set; }
        public bool IsPushable => !IsCleared && gameObject.activeInHierarchy;
        public float LastPushedTime { get; private set; } = -10f;

        private void Awake()
        {
            m_HomePosition = transform.position;
            m_HomeRotation = transform.rotation;
            m_RestScale = transform.localScale;
        }

        private void OnEnable() => s_All.Add(this);
        private void OnDisable() => s_All.Remove(this);

        public void PushTo(Vector3 position)
        {
            if (IsCleared) return;
            Vector3 delta = position - transform.position;
            delta.y = 0f;
            transform.position = new Vector3(position.x, m_HomePosition.y, position.z);
            // A light wobble sells the push without any rolling.
            float wobble = Mathf.Sin(Time.time * 14f) * 2f;
            transform.rotation = m_HomeRotation * Quaternion.Euler(0f, wobble, 0f);
            if (delta.sqrMagnitude > 0.0001f) LastPushedTime = Time.time;
        }

        /// <summary>Tidies the debris into the skip/clear area.</summary>
        public void Clear(Vector3 slot, bool instant)
        {
            IsCleared = true;
            if (instant)
            {
                transform.position = slot;
                transform.localScale = m_RestScale * 0.7f;
                return;
            }
            Tween.Jump(transform, slot, 1.5f, 0.45f);
            Tween.Scale(transform, m_RestScale * 0.7f, 0.45f, Ease.OutBack);
        }

        /// <summary>Back to where it started (activity reset or it left the play area).</summary>
        public void ResetHome(bool animate)
        {
            IsCleared = false;
            transform.rotation = m_HomeRotation;
            if (!animate)
            {
                transform.position = m_HomePosition;
                transform.localScale = m_RestScale;
                return;
            }
            transform.localScale = Vector3.zero;
            transform.position = m_HomePosition;
            Tween.Scale(transform, m_RestScale, 0.4f, Ease.OutBack);
        }

        public Vector3 HomePosition => m_HomePosition;
    }
}
