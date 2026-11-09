using System;
using System.Collections;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.Audio;
using LittleBuildersWorld.Vehicles;
using UnityEngine;

namespace LittleBuildersWorld.Activities
{
    /// <summary>
    /// A multi-stage project (the house, the roadworks). Each stage is a child activity opened in turn. When a stage
    /// finishes, its pre-authored parts pop into place one by one, a star is awarded, and the next stage opens.
    /// </summary>
    public class ProjectActivity : ActivityBase
    {
        [Serializable]
        public sealed class StageReveal
        {
            [Tooltip("Shown, with a bouncy snap, when this stage is finished.")] public GameObject[] show = new GameObject[0];
            [Tooltip("Hidden when this stage is finished (scaffolding, potholes...).")] public GameObject[] hide = new GameObject[0];
        }

        [SerializeField] private ActivityBase[] m_Stages = new ActivityBase[0];
        [SerializeField] private StageReveal[] m_Reveals = new StageReveal[0];
        [SerializeField] private int m_StageStars = 1;
        [SerializeField] private int m_ProjectBonus = 3;
        [Tooltip("Unlock id that opens this project (an Area unlock). Empty = open from the start.")] [SerializeField] private string m_RequiredUnlock;
        [Tooltip("Shown while the project is still locked (a barrier and sign at the gate).")] [SerializeField] private GameObject[] m_LockedVisuals = new GameObject[0];

        public bool IsLocked
        {
            get
            {
                if (string.IsNullOrEmpty(m_RequiredUnlock)) return false;
                GameServices s = GameServices.Instance;
                return s != null && !s.Stars.IsUnlocked(m_RequiredUnlock);
            }
        }

        public int StageCount => m_Stages.Length;
        public ActivityBase CurrentStage => Progress != null && !Progress.IsComplete ? m_Stages[Progress.Stage] : null;

        public event Action<ProjectActivity, int> StageFinished;

        protected override void Awake()
        {
            m_StageGoals = new int[Mathf.Max(1, m_Stages.Length)];
            for (int i = 0; i < m_StageGoals.Length; i++) m_StageGoals[i] = 1;
            base.Awake();
            foreach (ActivityBase child in m_Stages)
            {
                if (child == null) Debug.LogError($"[LBW] Project '{Id}' has an empty stage slot.", this);
                else child.SetParent(this);
            }
        }

        protected override void Start()
        {
            if (GameServices.Instance != null) GameServices.Instance.Stars.Unlocked += OnUnlocked;
            foreach (GameObject g in m_LockedVisuals) if (g != null) g.SetActive(IsLocked);
            RestoreFromSave();
            for (int i = 0; i < m_Stages.Length; i++)
            {
                if (m_Stages[i] == null) continue;
                if (i < Progress.Stage) m_Stages[i].ForceComplete();
                else m_Stages[i].RestoreFromSave();
            }
        }

        protected virtual void OnDestroy()
        {
            if (GameServices.Instance != null) GameServices.Instance.Stars.Unlocked -= OnUnlocked;
        }

        private void OnUnlocked(Progression.UnlockDefinition u)
        {
            if (u == null || u.id != m_RequiredUnlock) return;
            foreach (GameObject g in m_LockedVisuals)
            {
                if (g == null || !g.activeSelf) continue;
                Tween.Scale(g.transform, Vector3.zero, 0.35f, Ease.InBack, () => g.SetActive(false));
            }
            foreach (ActivityBase child in m_Stages) if (child != null) child.RefreshMarker();
        }

        public bool IsChildOpen(ActivityBase child) =>
            Progress != null && !Progress.IsComplete && !IsLocked && m_Stages[Progress.Stage] == child;

        public ActivityBase StageActivity(int index) => index >= 0 && index < m_Stages.Length ? m_Stages[index] : null;

        internal void OnChildCompleted(ActivityBase child)
        {
            int index = Array.IndexOf(m_Stages, child);
            if (index != Progress.Stage) return; // stale or duplicate notification
            int finished = Progress.AddProgress(1);
            if (finished < 0) return;
            bool completed = Progress.IsComplete;
            GameServices s = GameServices.Instance;
            s?.Progression.Commit(Id, Progress.Stage, 0, completed);
            Vector3 at = FocusPosition;
            if (s != null)
            {
                s.Stars.Award($"{Id}.stage{finished}", m_StageStars, at);
                if (completed) s.Stars.Award($"{Id}.complete", m_ProjectBonus, at);
            }
            StartCoroutine(RevealRoutine(finished, completed));
            if (!completed) m_Stages[Progress.Stage].RefreshMarker();
            StageFinished?.Invoke(this, finished);
            ActivityEvents.RaiseStageFinished(this, finished, completed);
        }

        private IEnumerator RevealRoutine(int stage, bool projectDone)
        {
            yield return new WaitForSeconds(0.6f);
            StageReveal r = stage < m_Reveals.Length ? m_Reveals[stage] : null;
            if (r != null)
            {
                foreach (GameObject h in r.hide)
                {
                    if (h == null) continue;
                    Transform t = h.transform;
                    Tween.Scale(t, Vector3.zero, 0.25f, Ease.InBack, () => h.SetActive(false));
                }
                foreach (GameObject part in r.show)
                {
                    if (part == null) continue;
                    PopIn(part);
                    GameServices.Instance?.Audio.Play(SfxId.Snap, UnityEngine.Random.Range(0.9f, 1.2f));
                    yield return new WaitForSeconds(0.22f);
                }
            }
            ActivityEvents.RaiseCelebrate(FocusPosition, projectDone ? CelebrationSize.Project : CelebrationSize.Stage);
            OnStageRevealed(stage, projectDone);
        }

        private void PopIn(GameObject part)
        {
            Transform t = part.transform;
            Vector3 rest = RestScale(part);
            Vector3 restPos = t.localPosition;
            part.SetActive(true);
            t.localScale = rest * 0.01f;
            t.localPosition = restPos + Vector3.up * 2.5f;
            Tween.MoveLocal(t, restPos, 0.4f, Ease.OutBounce);
            Tween.Scale(t, rest, 0.35f, Ease.OutBack);
        }

        private static Vector3 RestScale(GameObject part)
        {
            var marker = part.GetComponent<RestScaleMarker>();
            if (marker == null)
            {
                marker = part.AddComponent<RestScaleMarker>();
                marker.Scale = part.transform.localScale == Vector3.zero ? Vector3.one : part.transform.localScale;
            }
            return marker.Scale;
        }

        protected override void ApplyState(bool instant)
        {
            // Order-independent: a part can be shown by one stage and hidden by a later one (the dirt to dig appears
            // after clearing and goes after digging). Start from "nothing done", then replay finished stages in order.
            var initial = new System.Collections.Generic.HashSet<GameObject>();
            for (int i = 0; i < m_Reveals.Length; i++)
            {
                StageReveal r = m_Reveals[i];
                if (r == null) continue;
                foreach (GameObject s in r.show) if (s != null && initial.Add(s)) { RestScale(s); s.SetActive(false); }
                foreach (GameObject h in r.hide) if (h != null && initial.Add(h)) { RestScale(h); h.SetActive(true); }
            }
            for (int i = 0; i < m_Reveals.Length && i < Progress.Stage; i++)
            {
                StageReveal r = m_Reveals[i];
                if (r == null) continue;
                foreach (GameObject s in r.show) if (s != null) s.SetActive(true);
                foreach (GameObject h in r.hide) if (h != null) h.SetActive(false);
            }
            OnStageRevealed(Progress.Stage - 1, Progress.IsComplete, instant);
        }

        /// <summary>Hook for project-specific flourishes (residents, barriers). stage -1 = nothing finished.</summary>
        protected virtual void OnStageRevealed(int stage, bool projectDone, bool instant = false) { }

        public override bool TryGetGuidance(Vehicle vehicle, out Vector3 position, out int priority)
        {
            position = default;
            priority = 0;
            return false; // children guide
        }
    }

    /// <summary>Remembers a part's authored scale so pop-in animations always return to it.</summary>
    public sealed class RestScaleMarker : MonoBehaviour
    {
        public Vector3 Scale = Vector3.one;
    }
}
