using System;

namespace LittleBuildersWorld.Activities
{
    public enum ActivityState
    {
        /// <summary>Waiting for the player; nothing done in the current stage yet.</summary>
        Available,
        /// <summary>Work has started in the current stage.</summary>
        Active,
        /// <summary>The last action finished a stage; the next stage is now open.</summary>
        StageCompleted,
        /// <summary>Every stage is done.</summary>
        Completed
    }

    /// <summary>
    /// Pure stage/progress bookkeeping for one activity: Available → Active → StageCompleted → … → Completed.
    /// Holds no Unity objects so it can be unit tested; <see cref="ActivityBase"/> persists it.
    /// </summary>
    public sealed class ActivityProgress
    {
        private readonly int[] m_StageGoals;

        public int Stage { get; private set; }
        public int Progress { get; private set; }
        public ActivityState State { get; private set; }

        public int StageCount => m_StageGoals.Length;
        public bool IsComplete => Stage >= m_StageGoals.Length;
        public int CurrentGoal => IsComplete ? 0 : m_StageGoals[Stage];
        public float StageFraction => IsComplete ? 1f : (float)Progress / Math.Max(1, CurrentGoal);

        public ActivityProgress(int[] stageGoals)
        {
            if (stageGoals == null || stageGoals.Length == 0) throw new ArgumentException("An activity needs at least one stage.");
            m_StageGoals = (int[])stageGoals.Clone();
            for (int i = 0; i < m_StageGoals.Length; i++) m_StageGoals[i] = Math.Max(1, m_StageGoals[i]);
            State = ActivityState.Available;
        }

        public int GoalOf(int stage) => stage >= 0 && stage < m_StageGoals.Length ? m_StageGoals[stage] : 0;

        /// <summary>Restores saved values, clamping anything out of range (older save, changed content).</summary>
        public void Restore(int stage, int progress)
        {
            Stage = Math.Max(0, Math.Min(stage, m_StageGoals.Length));
            Progress = IsComplete ? 0 : Math.Max(0, Math.Min(progress, CurrentGoal - 1));
            State = IsComplete ? ActivityState.Completed : Progress > 0 ? ActivityState.Active : ActivityState.Available;
        }

        /// <summary>
        /// Adds one unit of work to the current stage. Returns the index of the stage that was completed by this call,
        /// or -1. Calls after completion are ignored, so duplicate events cannot skip stages or pay twice.
        /// </summary>
        public int AddProgress(int amount = 1)
        {
            if (IsComplete || amount <= 0) return -1;
            Progress += amount;
            if (Progress < CurrentGoal)
            {
                State = ActivityState.Active;
                return -1;
            }
            int finished = Stage;
            Stage++;
            Progress = 0;
            State = IsComplete ? ActivityState.Completed : ActivityState.StageCompleted;
            return finished;
        }

        /// <summary>Starts a repeatable activity again from its first stage.</summary>
        public void Restart()
        {
            Stage = 0;
            Progress = 0;
            State = ActivityState.Available;
        }
    }
}
