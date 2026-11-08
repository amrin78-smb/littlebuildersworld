using LittleBuildersWorld.Audio;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.Vehicles;
using UnityEngine;

namespace LittleBuildersWorld.Activities
{
    /// <summary>Road roller: drive over each rough patch to make it smooth. No precision: a wide radius and a short dwell.</summary>
    public sealed class RoadRollerActivity : ActivityBase
    {
        [SerializeField] private RoughPatch[] m_Patches = new RoughPatch[0];
        [SerializeField] private float m_RollSeconds = 0.35f;
        [Tooltip("Before this activity opens, patches show as potholes (true) or rough (false).")] [SerializeField] private bool m_PotholesUntilOpen = true;

        protected override void Awake()
        {
            m_StageGoals = new[] { Mathf.Max(1, m_Patches.Length) };
            base.Awake();
        }

        private void FixedUpdate()
        {
            if (!IsOpen) return;
            Vehicle roller = null;
            foreach (Vehicle v in Vehicle.All)
            {
                if (v.IsOccupied && v.Type == VehicleType.RoadRoller) { roller = v; break; }
            }
            if (roller == null || Mathf.Abs(roller.Controller.Speed) < 0.3f) return;
            Vector3 rp = roller.transform.position;
            foreach (RoughPatch patch in m_Patches)
            {
                if (patch == null || patch.State == RoughPatch.PatchState.Smooth) continue;
                Vector3 d = patch.transform.position - rp;
                d.y = 0f;
                if (d.sqrMagnitude > patch.Radius * patch.Radius) continue;
                patch.RollTime += Time.fixedDeltaTime;
                if (patch.RollTime < m_RollSeconds) continue;
                patch.SetState(RoughPatch.PatchState.Smooth, false);
                GameServices.Instance?.Audio.Play(SfxId.Sparkle);
                AddProgress(1, patch.transform.position);
                if (!IsOpen) break;
            }
        }

        protected override void ApplyState(bool instant)
        {
            int done = Progress.IsComplete ? m_Patches.Length : Progress.Progress;
            for (int i = 0; i < m_Patches.Length; i++)
            {
                if (m_Patches[i] == null) continue;
                RoughPatch.PatchState s = i < done ? RoughPatch.PatchState.Smooth
                    : (m_PotholesUntilOpen && !IsOpen ? RoughPatch.PatchState.Pothole : RoughPatch.PatchState.Rough);
                m_Patches[i].SetState(s, instant);
                m_Patches[i].RollTime = 0f;
            }
        }

        protected override void OnOpenChanged(bool open)
        {
            // When the fill stage finishes, the potholes become rough gravel ready to roll.
            if (open && Progress != null && !Progress.IsComplete)
            {
                for (int i = Progress.Progress; i < m_Patches.Length; i++)
                {
                    if (m_Patches[i] != null && m_Patches[i].State == RoughPatch.PatchState.Pothole)
                        m_Patches[i].SetState(RoughPatch.PatchState.Rough, false);
                }
            }
        }
    }
}
