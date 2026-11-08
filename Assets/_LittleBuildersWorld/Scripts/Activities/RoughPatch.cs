using LittleBuildersWorld.Core;
using UnityEngine;

namespace LittleBuildersWorld.Activities
{
    /// <summary>One road patch: a pothole, then rough gravel once filled, then smooth tarmac once rolled.</summary>
    public sealed class RoughPatch : MonoBehaviour
    {
        public enum PatchState { Pothole, Rough, Smooth }

        [SerializeField] private GameObject m_Pothole;
        [SerializeField] private GameObject m_Rough;
        [SerializeField] private GameObject m_Smooth;
        [SerializeField] private float m_Radius = 3.2f;

        public PatchState State { get; private set; }
        public float Radius => m_Radius;
        public float RollTime { get; set; }

        public void SetState(PatchState state, bool instant)
        {
            State = state;
            Show(m_Pothole, state == PatchState.Pothole, instant);
            Show(m_Rough, state == PatchState.Rough, instant);
            Show(m_Smooth, state == PatchState.Smooth, instant);
        }

        private static void Show(GameObject go, bool on, bool instant)
        {
            if (go == null) return;
            if (go.activeSelf == on) return;
            go.SetActive(on);
            if (on && !instant)
            {
                Vector3 rest = go.transform.localScale;
                go.transform.localScale = new Vector3(rest.x * 0.6f, rest.y, rest.z * 0.6f);
                Tween.Scale(go.transform, rest, 0.35f, Ease.OutBack);
            }
        }
    }
}
