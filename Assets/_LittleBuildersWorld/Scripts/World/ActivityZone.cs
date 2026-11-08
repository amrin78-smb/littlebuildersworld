using UnityEngine;

namespace LittleBuildersWorld.World
{
    /// <summary>
    /// A generous flat area (circle or box, tested on the ground plane). Activities ask whether a vehicle is inside.
    /// Polled for the one driven vehicle instead of physics triggers: deterministic, survives teleports and disabling.
    /// </summary>
    public sealed class ActivityZone : MonoBehaviour
    {
        public enum ZoneShape { Circle, Box }

        [SerializeField] private ZoneShape m_Shape = ZoneShape.Circle;
        [SerializeField] private float m_Radius = 5f;
        [SerializeField] private Vector2 m_Size = new Vector2(8f, 8f);

        public float Radius => m_Shape == ZoneShape.Circle ? m_Radius : Mathf.Max(m_Size.x, m_Size.y) * 0.5f;

        public void Configure(ZoneShape shape, float radius, Vector2 size)
        {
            m_Shape = shape;
            m_Radius = radius;
            m_Size = size;
        }

        public bool Contains(Vector3 world)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            if (m_Shape == ZoneShape.Circle) return local.x * local.x + local.z * local.z <= m_Radius * m_Radius;
            return Mathf.Abs(local.x) <= m_Size.x * 0.5f && Mathf.Abs(local.z) <= m_Size.y * 0.5f;
        }

        public float DistanceTo(Vector3 world)
        {
            Vector3 d = world - transform.position;
            d.y = 0f;
            return d.magnitude;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            if (m_Shape == ZoneShape.Circle) Gizmos.DrawWireSphere(Vector3.zero, m_Radius);
            else Gizmos.DrawWireCube(Vector3.zero, new Vector3(m_Size.x, 0.2f, m_Size.y));
        }
    }
}
