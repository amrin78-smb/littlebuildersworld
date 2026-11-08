using UnityEngine;

namespace LittleBuildersWorld.Vehicles
{
    public enum VehicleType
    {
        Excavator,
        DumpTruck,
        Bulldozer,
        RoadRoller
    }

    /// <summary>Icons the contextual action button can show. Mapped to sprites in <see cref="Core.GameConfig"/>.</summary>
    public enum ActionIcon
    {
        None,
        Scoop,
        Dump,
        Load,
        Unload
    }

    /// <summary>All per-vehicle tuning in one asset. Gameplay scripts read handling from here, never hardcode it.</summary>
    [CreateAssetMenu(menuName = "Little Builders World/Vehicle Profile", fileName = "VehicleProfile")]
    public sealed class VehicleProfile : ScriptableObject
    {
        [Header("Identity")]
        public string id = "vehicle";
        public string displayName = "Vehicle";
        public VehicleType type;
        public Sprite portrait;
        public Color markerColour = Color.yellow;

        [Header("Handling")]
        public DriveTuning tuning = DriveTuning.Default;
        [Tooltip("Rigidbody mass, kg. Heavier vehicles shove lighter ones less.")] public float mass = 2000f;
        [Tooltip("How far the body leans into turns and slopes (visual only), degrees.")] public float maxBodyLean = 4f;
        [Tooltip("Wheel/drum radius for spin animation, m.")] public float wheelRadius = 0.5f;
        [Tooltip("Scroll speed multiplier for track textures.")] public float trackScroll = 0.35f;

        [Header("Unlock")]
        [Tooltip("Builder Stars needed before this vehicle can be driven. 0 = from the start.")] public int starsRequired;

        [Header("Audio")]
        public AudioClip engineLoop;
        [Range(0.5f, 2f)] public float engineIdlePitch = 0.8f;
        [Range(0.5f, 2.5f)] public float engineTopPitch = 1.3f;
        [Range(0.5f, 2f)] public float hornPitch = 1f;
    }
}
