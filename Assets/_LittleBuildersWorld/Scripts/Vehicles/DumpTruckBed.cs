using System;
using System.Collections;
using LittleBuildersWorld.Audio;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.World;
using UnityEngine;

namespace LittleBuildersWorld.Vehicles
{
    /// <summary>
    /// The dump truck's bed: shows how full it is, and tips with a scripted animation. Unloading anywhere is allowed
    /// (a small heap appears); a delivery zone offers a higher-priority Unload that counts toward its project.
    /// </summary>
    public sealed class DumpTruckBed : MonoBehaviour, IVehicleTool
    {
        public const int Capacity = 3;

        [SerializeField] private Vehicle m_Vehicle;
        [Tooltip("Hinge at the back of the bed. Rotating it negative about X lifts the front.")] [SerializeField] private Transform m_BedPivot;
        [SerializeField] private float m_TipAngle = 52f;
        [Tooltip("One visual per LoadKind (soil, gravel, bricks); scaled by load level.")] [SerializeField] private Transform[] m_LoadVisuals = new Transform[3];
        [SerializeField] private Transform m_DumpPoint;

        private int m_Level;
        private LoadKind m_Kind;
        private bool m_Busy;
        private Action m_PendingCommit;
        private Vector3[] m_LoadRest;

        public int Level => m_Level;
        public LoadKind Kind => m_Kind;
        public bool IsLoaded => m_Level > 0;
        public bool IsFull => m_Level >= Capacity;
        public bool IsBusy => m_Busy;
        public bool CanReceive => !m_Busy && m_Level < Capacity;
        public Vector3 BedCentre => m_LoadVisuals.Length > 0 && m_LoadVisuals[0] != null ? m_LoadVisuals[0].position : transform.position + Vector3.up * 2f;
        public Vector3 DumpPoint => m_DumpPoint != null ? m_DumpPoint.position : transform.position - transform.forward * 4f;

        public event Action<DumpTruckBed> LoadChanged;

        private void Awake()
        {
            if (m_Vehicle == null) m_Vehicle = GetComponent<Vehicle>();
            m_LoadRest = new Vector3[m_LoadVisuals.Length];
            for (int i = 0; i < m_LoadVisuals.Length; i++)
            {
                if (m_LoadVisuals[i] == null) continue;
                m_LoadRest[i] = m_LoadVisuals[i].localScale;
            }
            RefreshVisual(true);
        }

        /// <summary>Adds load (from the excavator or a loading bay). Returns false if there is no room.</summary>
        public bool AddLoad(LoadKind kind, int amount)
        {
            if (m_Level >= Capacity || amount <= 0) return false;
            if (m_Level == 0) m_Kind = kind;
            m_Level = Mathf.Min(Capacity, m_Level + amount);
            RefreshVisual(false);
            GameServices.Instance?.Audio.Play(SfxId.Load);
            LoadChanged?.Invoke(this);
            return true;
        }

        /// <summary>Tips the load out toward <paramref name="target"/>; <paramref name="onDumped"/> runs once it lands.</summary>
        public bool TipInto(Vector3 target, Action<LoadKind, int> onDumped)
        {
            if (m_Busy || m_Level <= 0) return false;
            StartCoroutine(TipRoutine(target, onDumped));
            return true;
        }

        private IEnumerator TipRoutine(Vector3 target, Action<LoadKind, int> onDumped)
        {
            m_Busy = true;
            LoadKind kind = m_Kind;
            int amount = m_Level;
            m_PendingCommit = () =>
            {
                m_Level = 0;
                RefreshVisual(true);
                LoadChanged?.Invoke(this);
                onDumped?.Invoke(kind, amount);
            };
            GameServices.Instance?.Audio.Play(SfxId.Hydraulic);
            yield return Rotate(m_BedPivot, 0f, -m_TipAngle, 0.7f);
            GameServices.Instance?.Audio.Play(SfxId.BedTip);
            Transform visual = VisualFor(kind);
            if (visual != null)
            {
                Vector3 start = visual.localPosition;
                for (float t = 0f; t < 0.45f; t += Time.deltaTime)
                {
                    float k = t / 0.45f;
                    visual.localPosition = start + new Vector3(0f, -0.3f * k, -1.6f * k);
                    visual.localScale = Vector3.Lerp(visual.localScale, visual.localScale * 0.6f, k * 0.5f);
                    yield return null;
                }
                visual.localPosition = start;
            }
            Action commit = m_PendingCommit;
            m_PendingCommit = null;
            commit?.Invoke();
            GameServices.Instance?.Audio.Play(SfxId.SoilDump);
            yield return new WaitForSeconds(0.25f);
            yield return Rotate(m_BedPivot, -m_TipAngle, 0f, 0.6f);
            m_Busy = false;
        }

        private static IEnumerator Rotate(Transform pivot, float from, float to, float seconds)
        {
            if (pivot == null) yield break;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Easing.Evaluate(Ease.InOutSine, t / seconds);
                pivot.localRotation = Quaternion.Euler(Mathf.Lerp(from, to, k), 0f, 0f);
                yield return null;
            }
            pivot.localRotation = Quaternion.Euler(to, 0f, 0f);
        }

        public void CancelToRest()
        {
            StopAllCoroutines();
            Action commit = m_PendingCommit;
            m_PendingCommit = null;
            commit?.Invoke(); // a tip that was under way still counts
            if (m_BedPivot != null) m_BedPivot.localRotation = Quaternion.identity;
            m_Busy = false;
            RefreshVisual(true);
        }

        private Transform VisualFor(LoadKind kind)
        {
            int i = (int)kind;
            return i < m_LoadVisuals.Length ? m_LoadVisuals[i] : null;
        }

        private void RefreshVisual(bool instant)
        {
            for (int i = 0; i < m_LoadVisuals.Length; i++)
            {
                Transform v = m_LoadVisuals[i];
                if (v == null) continue;
                bool show = m_Level > 0 && i == (int)m_Kind;
                if (!show)
                {
                    v.gameObject.SetActive(false);
                    v.localScale = m_LoadRest[i];
                    continue;
                }
                float fill = 0.45f + 0.55f * m_Level / Capacity;
                Vector3 target = new Vector3(m_LoadRest[i].x, m_LoadRest[i].y * fill, m_LoadRest[i].z);
                v.gameObject.SetActive(true);
                if (instant) v.localScale = target;
                else
                {
                    v.localScale = new Vector3(target.x, target.y * 0.6f, target.z);
                    Tween.Scale(v, target, 0.35f, Ease.OutBack);
                }
            }
        }

        // ---- IActionProvider: free unload anywhere (low priority; delivery zones outrank it)
        public bool TryGetOffer(Vehicle vehicle, out ActionOffer offer)
        {
            offer = default;
            if (vehicle != m_Vehicle || m_Busy || m_Level <= 0) return false;
            offer = new ActionOffer { Icon = ActionIcon.Unload, Priority = 1, Provider = this };
            return true;
        }

        public void Execute(Vehicle vehicle, int code)
        {
            Vector3 drop = DumpPoint;
            TipInto(drop, (kind, amount) => SoilPilePool.Instance?.Spawn(drop, (int)kind));
        }
    }
}
