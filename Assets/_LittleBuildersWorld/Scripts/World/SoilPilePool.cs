using LittleBuildersWorld.Core;
using UnityEngine;

namespace LittleBuildersWorld.World
{
    /// <summary>
    /// Free-play soil heaps: dumping outside a work zone leaves a little pile. Pooled with a fixed cap; the oldest pile
    /// shrinks away when a new one is needed, so free play never accumulates clutter or cost.
    /// </summary>
    public sealed class SoilPilePool : MonoBehaviour
    {
        public static SoilPilePool Instance { get; private set; }

        [SerializeField] private GameObject m_PilePrefab;
        [SerializeField] private int m_Capacity = 8;
        [SerializeField] private Material[] m_KindMaterials = new Material[0];

        private GameObject[] m_Piles;
        private int m_Next;

        private void Awake()
        {
            Instance = this;
            m_Piles = new GameObject[Mathf.Max(1, m_Capacity)];
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Spawn(Vector3 position, int kind)
        {
            if (m_PilePrefab == null) return;
            GameObject pile = m_Piles[m_Next];
            if (pile == null)
            {
                pile = Instantiate(m_PilePrefab, transform);
                m_Piles[m_Next] = pile;
            }
            m_Next = (m_Next + 1) % m_Piles.Length;
            if (Physics.Raycast(position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 12f, 1, QueryTriggerInteraction.Ignore))
                position.y = hit.point.y;
            pile.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            var r = pile.GetComponentInChildren<Renderer>();
            if (r != null && kind >= 0 && kind < m_KindMaterials.Length && m_KindMaterials[kind] != null) r.sharedMaterial = m_KindMaterials[kind];
            pile.SetActive(true);
            pile.transform.localScale = Vector3.one * 0.1f;
            Tween.Scale(pile.transform, Vector3.one, 0.4f, Ease.OutBack);
        }
    }
}
