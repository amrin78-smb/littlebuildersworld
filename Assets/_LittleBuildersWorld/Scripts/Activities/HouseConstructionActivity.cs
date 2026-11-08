using UnityEngine;

namespace LittleBuildersWorld.Activities
{
    /// <summary>
    /// The first big project: clear the plot (bulldozer), dig the foundation (excavator), then deliver materials
    /// (dump truck) for the walls, the roof, the door and windows, and the garden. Parts snap into pre-authored spots.
    /// When finished, the family who will live there waves from the garden.
    /// </summary>
    public sealed class HouseConstructionActivity : ProjectActivity
    {
        [SerializeField] private ParticleSystem m_ChimneySmoke;

        protected override void OnStageRevealed(int stage, bool projectDone, bool instant = false)
        {
            if (!projectDone) return;
            if (m_ChimneySmoke != null && !m_ChimneySmoke.isPlaying) m_ChimneySmoke.Play();
        }
    }
}
