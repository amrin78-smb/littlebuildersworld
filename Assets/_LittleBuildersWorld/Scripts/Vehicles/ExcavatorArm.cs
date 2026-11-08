using System;
using System.Collections;
using LittleBuildersWorld.Audio;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.World;
using UnityEngine;

namespace LittleBuildersWorld.Vehicles
{
    /// <summary>
    /// Excavator cab, boom, stick and bucket. Scoop and Dump are scripted, readable animations — no precise bucket
    /// control. The bucket holds at most one scoop, so loads cannot be duplicated. Dumping goes to the nearest truck
    /// with room, otherwise onto the dig site's stockpile, otherwise onto the ground in front.
    /// </summary>
    public sealed class ExcavatorArm : MonoBehaviour, IVehicleTool
    {
        /// <summary>Something soil can be dug from. Implemented by <see cref="Activities.DigZone"/>.</summary>
        public interface ISoilSource
        {
            Vector3 DigPoint { get; }
            Vector3 StockpilePoint { get; }
            bool HasStockpile { get; }
            void OnScooped();
            /// <summary>The scoop from here was put down (in a truck, the stockpile or on the ground).</summary>
            void OnSoilMoved(bool intoTruck);
        }

        private struct Pose
        {
            public float Cab, Boom, Stick, Bucket;

            public Pose(float cab, float boom, float stick, float bucket)
            {
                Cab = cab;
                Boom = boom;
                Stick = stick;
                Bucket = bucket;
            }

            public static Pose Lerp(Pose a, Pose b, float t) => new Pose(
                Mathf.LerpAngle(a.Cab, b.Cab, t), Mathf.Lerp(a.Boom, b.Boom, t), Mathf.Lerp(a.Stick, b.Stick, t), Mathf.Lerp(a.Bucket, b.Bucket, t));
        }

        // Angles in degrees about each pivot's local X (positive = down/curl) and the cab's local Y.
        private static readonly Pose Rest = new Pose(0f, -18f, 55f, 40f);
        private static readonly Pose Reach = new Pose(0f, 22f, 35f, -25f);
        private static readonly Pose Curl = new Pose(0f, 18f, 60f, 85f);
        private static readonly Pose Carry = new Pose(0f, -30f, 60f, 85f);

        [SerializeField] private Vehicle m_Vehicle;
        [SerializeField] private Transform m_Cab;
        [SerializeField] private Transform m_Boom;
        [SerializeField] private Transform m_Stick;
        [SerializeField] private Transform m_Bucket;
        [SerializeField] private GameObject m_BucketSoil;
        [SerializeField] private Transform m_BucketTip;
        [SerializeField] private float m_TruckReach = 8.5f;
        [SerializeField] private float m_StockpileReach = 14f;
        [SerializeField] private ParticleSystem m_Dust;

        private Pose m_Pose = Rest;
        private bool m_Busy;
        private ISoilSource m_Source;
        private Action m_PendingCommit;

        public bool IsBusy => m_Busy;
        public bool HasSoil { get; private set; }

        public event Action<ExcavatorArm> StateChanged;

        private void Awake()
        {
            if (m_Vehicle == null) m_Vehicle = GetComponent<Vehicle>();
            if (m_BucketSoil != null) m_BucketSoil.SetActive(false);
            Apply(m_Pose);
        }

        public bool CanScoop => !m_Busy && !HasSoil;

        public bool Scoop(ISoilSource source)
        {
            if (!CanScoop || source == null) return false;
            StartCoroutine(ScoopRoutine(source));
            return true;
        }

        private IEnumerator ScoopRoutine(ISoilSource source)
        {
            m_Busy = true;
            StateChanged?.Invoke(this);
            float cab = YawToward(source.DigPoint, 60f);
            GameServices.Instance?.Audio.Play(SfxId.Hydraulic);
            yield return Animate(WithCab(Reach, cab), 0.55f);
            GameServices.Instance?.Audio.Play(SfxId.Scoop);
            if (m_Dust != null) m_Dust.Play();
            m_PendingCommit = () =>
            {
                m_Source = source;
                HasSoil = true;
                if (m_BucketSoil != null) m_BucketSoil.SetActive(true);
                source.OnScooped();
            };
            yield return Animate(WithCab(Curl, cab), 0.35f);
            CommitPending();
            yield return Animate(WithCab(Carry, cab), 0.45f);
            yield return Animate(Carry, 0.35f);
            m_Busy = false;
            StateChanged?.Invoke(this);
        }

        private IEnumerator DumpRoutine()
        {
            m_Busy = true;
            StateChanged?.Invoke(this);
            ISoilSource source = m_Source;

            Vehicle truck = FindTruck();
            var bed = truck != null ? truck.GetTool<DumpTruckBed>() : null;
            Vector3 target;
            bool toTruck = bed != null;
            bool toStockpile = !toTruck && source != null && source.HasStockpile &&
                               Flat(source.StockpilePoint - transform.position).magnitude < m_StockpileReach;
            if (toTruck) target = bed.BedCentre;
            else if (toStockpile) target = source.StockpilePoint;
            else target = transform.position + transform.forward * 5.5f;

            float cab = YawToward(target, 180f);
            GameServices.Instance?.Audio.Play(SfxId.Hydraulic);
            yield return Animate(WithCab(Carry, cab), 0.65f);
            yield return Animate(WithCab(new Pose(0f, 0f, 45f, 85f), cab), 0.3f);

            m_PendingCommit = () =>
            {
                HasSoil = false;
                m_Source = null;
                if (m_BucketSoil != null) m_BucketSoil.SetActive(false);
                if (toTruck) bed.AddLoad(LoadKind.Soil, 1);
                else if (!toStockpile) SoilPilePool.Instance?.Spawn(target, (int)LoadKind.Soil);
                source?.OnSoilMoved(toTruck);
                GameServices.Instance?.Audio.Play(SfxId.SoilDump);
            };
            yield return Animate(WithCab(new Pose(0f, 5f, 40f, -45f), cab), 0.35f);
            CommitPending();
            yield return new WaitForSeconds(0.2f);
            yield return Animate(Rest, 0.7f);
            m_Busy = false;
            StateChanged?.Invoke(this);
        }

        private Vehicle FindTruck()
        {
            Vehicle best = null;
            float bestSq = m_TruckReach * m_TruckReach;
            foreach (Vehicle v in Vehicle.All)
            {
                if (v == m_Vehicle || v.Type != VehicleType.DumpTruck) continue;
                var bed = v.GetTool<DumpTruckBed>();
                if (bed == null || !bed.CanReceive) continue;
                float sq = Flat(v.transform.position - transform.position).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = v;
                }
            }
            return best;
        }

        private float YawToward(Vector3 world, float maxAngle)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            float yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
            return Mathf.Clamp(yaw, -maxAngle, maxAngle);
        }

        private static Pose WithCab(Pose p, float cab) => new Pose(cab, p.Boom, p.Stick, p.Bucket);

        private static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        private IEnumerator Animate(Pose to, float seconds)
        {
            Pose from = m_Pose;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                Apply(Pose.Lerp(from, to, Easing.Evaluate(Ease.InOutSine, t / seconds)));
                yield return null;
            }
            Apply(to);
        }

        private void Apply(Pose p)
        {
            m_Pose = p;
            if (m_Cab != null) m_Cab.localRotation = Quaternion.Euler(0f, p.Cab, 0f);
            if (m_Boom != null) m_Boom.localRotation = Quaternion.Euler(p.Boom, 0f, 0f);
            if (m_Stick != null) m_Stick.localRotation = Quaternion.Euler(p.Stick, 0f, 0f);
            if (m_Bucket != null) m_Bucket.localRotation = Quaternion.Euler(p.Bucket, 0f, 0f);
        }

        private void CommitPending()
        {
            Action a = m_PendingCommit;
            m_PendingCommit = null;
            a?.Invoke();
            StateChanged?.Invoke(this);
        }

        public void CancelToRest()
        {
            StopAllCoroutines();
            CommitPending(); // a scoop or dump that was under way still counts
            m_Busy = false;
            Apply(HasSoil ? Carry : Rest);
            StateChanged?.Invoke(this);
        }

        // ---- IActionProvider: Dump whenever the bucket is full (Scoop is offered by dig zones)
        public bool TryGetOffer(Vehicle vehicle, out ActionOffer offer)
        {
            offer = default;
            if (vehicle != m_Vehicle || m_Busy || !HasSoil) return false;
            offer = new ActionOffer { Icon = ActionIcon.Dump, Priority = 8, Provider = this };
            return true;
        }

        public void Execute(Vehicle vehicle, int code)
        {
            if (!m_Busy && HasSoil) StartCoroutine(DumpRoutine());
        }
    }
}
