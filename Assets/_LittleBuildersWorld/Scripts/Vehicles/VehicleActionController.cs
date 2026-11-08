using System;
using UnityEngine;

namespace LittleBuildersWorld.Vehicles
{
    /// <summary>
    /// Decides what the single contextual action button does for the driven vehicle, every frame: asks the vehicle's
    /// tool and the world's zones for offers and keeps the highest priority. One action at a time, never a menu.
    /// </summary>
    public sealed class VehicleActionController : MonoBehaviour
    {
        private Vehicle m_Vehicle;
        private ActionOffer m_Current;

        public ActionOffer Current => m_Current;
        public bool HasAction => m_Current.IsValid;

        /// <summary>Raised when the offered icon changes (including to None).</summary>
        public event Action<ActionIcon> OfferChanged;
        public event Action<ActionIcon> ActionPerformed;

        public void SetVehicle(Vehicle vehicle)
        {
            m_Vehicle = vehicle;
            Evaluate();
        }

        private void Update() => Evaluate();

        private void Evaluate()
        {
            ActionOffer best = default;
            if (m_Vehicle != null && m_Vehicle.IsOccupied && (m_Vehicle.Tool == null || !m_Vehicle.Tool.IsBusy))
            {
                if (m_Vehicle.Tool != null && m_Vehicle.Tool.TryGetOffer(m_Vehicle, out ActionOffer own)) best = own;
                var providers = ActionProviderRegistry.Providers;
                for (int i = 0; i < providers.Count; i++)
                {
                    if (providers[i].TryGetOffer(m_Vehicle, out ActionOffer o) && (!best.IsValid || o.Priority > best.Priority)) best = o;
                }
            }
            ActionIcon before = m_Current.Icon;
            m_Current = best;
            if (before != best.Icon) OfferChanged?.Invoke(best.Icon);
        }

        /// <summary>Runs the current offer. Re-checks first, so a stale button press can never do the wrong thing.</summary>
        public bool Perform()
        {
            Evaluate();
            if (!m_Current.IsValid || m_Vehicle == null) return false;
            ActionOffer offer = m_Current;
            offer.Provider.Execute(m_Vehicle, offer.Code);
            ActionPerformed?.Invoke(offer.Icon);
            Evaluate();
            return true;
        }
    }
}
