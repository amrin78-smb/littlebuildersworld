using System;
using System.Collections.Generic;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.Save;
using LittleBuildersWorld.Vehicles;
using UnityEngine;

namespace LittleBuildersWorld.Activities
{
    /// <summary>
    /// Base lifecycle for every construction activity: Available → Active → StageCompleted → Completed.
    /// Progress is saved after every unit of work, so the player can drive away at any moment and come back later.
    /// Rewards use one key per stage, so nothing pays twice. Activities inside a project are opened one at a time
    /// by their <see cref="ProjectActivity"/>, which also pays their rewards.
    /// </summary>
    public abstract class ActivityBase : MonoBehaviour, IGuidanceTarget
    {
        private static readonly List<ActivityBase> s_All = new List<ActivityBase>();
        public static IReadOnlyList<ActivityBase> All => s_All;

        [SerializeField] private string m_Id = "activity";
        [SerializeField] protected int[] m_StageGoals = { 1 };
        [SerializeField] private int m_StarsPerStage = 1;
        [SerializeField] private int m_CompletionBonus;
        [Tooltip("Starts over after completion (the reward is still paid only the first time).")] [SerializeField] private bool m_Repeatable;
        [SerializeField] private VehicleType m_Vehicle;
        [Tooltip("Where the floating marker sits and the guidance arrow points.")] [SerializeField] private Transform m_Focus;
        [SerializeField] private ActivityMarker m_Marker;
        [Tooltip("Helper line that says what to do here (key in Tools/voice/voice_lines.json). Empty = the activity id.")] [SerializeField] private string m_HintKey;

        private ProjectActivity m_Parent;
        private bool m_Restored;

        public string Id => m_Id;
        public ActivityProgress Progress { get; private set; }
        public VehicleType NeededVehicle => m_Vehicle;
        public Vector3 FocusPosition => m_Focus != null ? m_Focus.position : transform.position;
        public bool IsRepeatable => m_Repeatable;
        public ProjectActivity Parent => m_Parent;
        public string HintKey => string.IsNullOrEmpty(m_HintKey) ? m_Id : m_HintKey;

        /// <summary>Open for work right now (its project has reached it and it is not finished).</summary>
        public bool IsOpen => Progress != null && !Progress.IsComplete && (m_Parent == null || m_Parent.IsChildOpen(this));

        public event Action<ActivityBase> Changed;
        public event Action<ActivityBase> CompletedEvent;

        protected virtual void Awake()
        {
            Progress = new ActivityProgress(m_StageGoals);
        }

        protected virtual void OnEnable()
        {
            if (!s_All.Contains(this)) s_All.Add(this);
            GuidanceRegistry.Register(this);
        }

        protected virtual void OnDisable()
        {
            s_All.Remove(this);
            GuidanceRegistry.Unregister(this);
        }

        protected virtual void Start()
        {
            if (m_Parent == null) RestoreFromSave();
        }

        internal void SetParent(ProjectActivity parent) => m_Parent = parent;

        public void RestoreFromSave()
        {
            GameServices s = GameServices.Instance;
            if (s != null)
            {
                ActivityRecord r = s.Progression.GetRecord(m_Id);
                Progress.Restore(r.stage, r.progress);
                if (m_Repeatable && Progress.IsComplete)
                {
                    // Saved in the moment before it restarted: free play starts fresh (rewards are keyed, never twice).
                    Progress.Restart();
                    s.Progression.Commit(m_Id, 0, 0, false);
                }
            }
            m_Restored = true;
            ApplyState(true);
            RefreshMarker();
        }

        /// <summary>Called by a project for a child stage that is already behind it.</summary>
        internal void ForceComplete()
        {
            Progress.Restore(Progress.StageCount, 0);
            m_Restored = true;
            ApplyState(true);
            RefreshMarker();
        }

        /// <summary>Adds work. Saves, celebrates, rewards and notifies as needed.</summary>
        protected void AddProgress(int amount, Vector3 at)
        {
            if (!m_Restored || !IsOpen) return;
            int finished = Progress.AddProgress(amount);
            bool completed = Progress.IsComplete;
            GameServices s = GameServices.Instance;
            s?.Progression.Commit(m_Id, Progress.Stage, Progress.Progress, completed);

            if (finished < 0)
            {
                ActivityEvents.RaiseCelebrate(at, CelebrationSize.Small);
            }
            else
            {
                if (m_Parent == null && s != null)
                {
                    s.Stars.Award($"{m_Id}.stage{finished}", m_StarsPerStage, at);
                    if (completed && m_CompletionBonus > 0) s.Stars.Award($"{m_Id}.complete", m_CompletionBonus, at);
                }
                ActivityEvents.RaiseCelebrate(at, completed && m_Parent == null && !m_Repeatable ? CelebrationSize.Project : CelebrationSize.Stage);
                ActivityEvents.RaiseStageFinished(this, finished, completed && m_Parent == null && !m_Repeatable);
            }

            OnProgressChanged(finished);
            RefreshMarker();
            Changed?.Invoke(this);

            if (completed)
            {
                CompletedEvent?.Invoke(this);
                if (m_Parent != null) m_Parent.OnChildCompleted(this);
                if (m_Repeatable) Invoke(nameof(RestartRepeatable), 2.5f);
            }
        }

        private void RestartRepeatable()
        {
            Progress.Restart();
            GameServices.Instance?.Progression.Commit(m_Id, 0, 0, false);
            ApplyState(false);
            RefreshMarker();
            Changed?.Invoke(this);
        }

        /// <summary>Re-evaluates openness (e.g. after the parent project advanced).</summary>
        public void RefreshMarker()
        {
            if (m_Marker != null) m_Marker.SetVisible(IsOpen);
            OnOpenChanged(IsOpen);
        }

        /// <summary>Make the scene match <see cref="Progress"/>. instant = loading, no animation.</summary>
        protected abstract void ApplyState(bool instant);

        /// <summary>Hook for subclasses after progress was added; finishedStage is -1 unless a stage just ended.</summary>
        protected virtual void OnProgressChanged(int finishedStage) { }

        protected virtual void OnOpenChanged(bool open) { }

        public virtual string GuideHint(Vehicle vehicle) => HintKey;

        public virtual bool TryGetGuidance(Vehicle vehicle, out Vector3 position, out int priority)
        {
            position = FocusPosition;
            priority = 10;
            return IsOpen && vehicle.Type == m_Vehicle;
        }
    }
}
