namespace LittleBuildersWorld.Vehicles
{
    /// <summary>One thing the contextual action button could do right now.</summary>
    public struct ActionOffer
    {
        public ActionIcon Icon;
        public int Priority;
        public IActionProvider Provider;
        public int Code;

        public bool IsValid => Provider != null && Icon != ActionIcon.None;
    }

    /// <summary>
    /// Implemented by vehicle tools (bucket, truck bed) and by activity zones (dig pile, loading bay, delivery spot).
    /// The highest-priority offer for the driven vehicle is shown on the action button.
    /// </summary>
    public interface IActionProvider
    {
        bool TryGetOffer(Vehicle vehicle, out ActionOffer offer);
        void Execute(Vehicle vehicle, int code);
    }

    /// <summary>A vehicle's working part. While busy, driving input is held so animations read clearly.</summary>
    public interface IVehicleTool : IActionProvider
    {
        bool IsBusy { get; }
        /// <summary>Called when the player leaves or the vehicle is reset: finish/cancel animations safely.</summary>
        void CancelToRest();
    }
}
