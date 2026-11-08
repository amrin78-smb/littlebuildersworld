using System;
using UnityEngine;

namespace LittleBuildersWorld.World
{
    /// <summary>
    /// All the little background motion in one Update: windmill blades and beacons spin, trees sway, clouds drift,
    /// flags flutter, residents wave. One component instead of hundreds keeps it cheap on budget phones.
    /// </summary>
    public sealed class AmbientMotion : MonoBehaviour
    {
        public enum Kind { Spin, Sway, Drift, Bob, Wave }

        [Serializable]
        public struct Item
        {
            public Transform target;
            public Kind kind;
            public float speed;
            public float amount;
            [NonSerialized] public Vector3 restPos;
            [NonSerialized] public Quaternion restRot;
            [NonSerialized] public float phase;
        }

        [SerializeField] private Item[] m_Items = new Item[0];
        [Tooltip("Clouds wrap around within this half-size.")] [SerializeField] private float m_DriftWrap = 140f;

        public void SetItems(Item[] items) => m_Items = items;

        private void Awake()
        {
            for (int i = 0; i < m_Items.Length; i++)
            {
                if (m_Items[i].target == null) continue;
                m_Items[i].restPos = m_Items[i].target.localPosition;
                m_Items[i].restRot = m_Items[i].target.localRotation;
                m_Items[i].phase = (m_Items[i].target.position.x * 0.37f + m_Items[i].target.position.z * 0.21f) % 6.28f;
            }
        }

        private void Update()
        {
            float t = Time.time;
            float dt = Time.deltaTime;
            for (int i = 0; i < m_Items.Length; i++)
            {
                ref Item it = ref m_Items[i];
                Transform tr = it.target;
                if (tr == null) continue;
                switch (it.kind)
                {
                    case Kind.Spin:
                        tr.Rotate(0f, 0f, it.speed * dt, Space.Self);
                        break;
                    case Kind.Sway:
                        tr.localRotation = it.restRot * Quaternion.Euler(Mathf.Sin(t * it.speed + it.phase) * it.amount, 0f,
                            Mathf.Cos(t * it.speed * 0.7f + it.phase) * it.amount * 0.6f);
                        break;
                    case Kind.Drift:
                        Vector3 p = tr.localPosition + Vector3.right * (it.speed * dt);
                        if (p.x > m_DriftWrap) p.x = -m_DriftWrap;
                        tr.localPosition = p;
                        break;
                    case Kind.Bob:
                        tr.localPosition = it.restPos + Vector3.up * (Mathf.Sin(t * it.speed + it.phase) * it.amount);
                        break;
                    case Kind.Wave:
                        tr.localRotation = it.restRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * it.speed + it.phase) * it.amount);
                        break;
                }
            }
        }
    }
}
