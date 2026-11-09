using LittleBuildersWorld.Activities;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.Progression;
using LittleBuildersWorld.Vehicles;
using UnityEngine;

namespace LittleBuildersWorld.Guide
{
    /// <summary>
    /// The helper's brain. Looks at what the child is doing (on foot, driving, which button is offered, where the
    /// guidance arrow points) and picks one short line that says what to do next; celebrates finished stages,
    /// unlocks and good work. Words show in the speech bubble; the matching voice clip plays when voice is on.
    /// Timing and repetition rules live in <see cref="GuideScheduler"/>.
    /// </summary>
    public sealed class GuideDirector : MonoBehaviour
    {
        private static bool s_Welcomed;

        [SerializeField] private VehicleInteraction m_Interaction;
        [SerializeField] private VehicleActionController m_Actions;
        [SerializeField] private GuideBubbleUI m_Bubble;
        [SerializeField] private float m_RefreshSeconds = 0.25f;
        [SerializeField] private float m_WelcomeDelay = 1.2f;

        private readonly GuideScheduler m_Scheduler = new GuideScheduler();
        private GameConfig m_Config;
        private GuideLibrary m_Library;
        private float m_NextRefresh;
        private float m_StartTime;
        private string m_Language;
        private Vehicle m_LastVehicle;
        private bool m_PendingPaint;

        public GuideScheduler Scheduler => m_Scheduler;
        /// <summary>Key of the last line shown (tests and the play-through read it).</summary>
        public string LastShownKey { get; private set; }

        public static readonly Color DiscYellow = new Color(1f, 0.79f, 0.24f);
        public static readonly Color DiscRed = new Color(1f, 0.35f, 0.37f);
        public static readonly Color DiscPurple = new Color(0.56f, 0.49f, 0.75f);
        public static readonly Color DiscBlue = new Color(0.1f, 0.51f, 0.77f);
        public static readonly Color DiscOrange = new Color(1f, 0.62f, 0.11f);
        public static readonly Color DiscGreen = new Color(0.42f, 0.8f, 0.47f);
        public static readonly Color DiscGrey = new Color(0.45f, 0.48f, 0.55f);

        private void OnEnable()
        {
            ActivityEvents.StageFinished += OnStageFinished;
            ActivityEvents.Celebrate += OnCelebrate;
            if (m_Bubble != null) m_Bubble.MascotTapped += OnMascotTapped;
        }

        private void OnDisable()
        {
            ActivityEvents.StageFinished -= OnStageFinished;
            ActivityEvents.Celebrate -= OnCelebrate;
            if (m_Bubble != null) m_Bubble.MascotTapped -= OnMascotTapped;
            if (GameServices.Instance != null) GameServices.Instance.Stars.Unlocked -= OnUnlocked;
        }

        private void Start()
        {
            GameServices s = GameServices.Instance;
            m_Config = s != null ? s.Config : null;
            m_Library = m_Config != null ? m_Config.guide : null;
            if (m_Library == null) Debug.LogWarning("[LBW] Guide library missing: the helper stays quiet.");
            if (s != null) s.Stars.Unlocked += OnUnlocked;
            m_StartTime = Time.unscaledTime;
            m_Language = Language.Current;
        }

        private void Update()
        {
            GameServices s = GameServices.Instance;
            float now = Time.unscaledTime;
            bool paused = s != null && s.State.IsPaused;
            if (m_Bubble != null) m_Bubble.SetTalking(s != null && s.Audio.VoicePlaying);
            if (paused) return;

            if (!s_Welcomed && now - m_StartTime >= m_WelcomeDelay)
            {
                s_Welcomed = true;
                m_Scheduler.PushEvent("welcome", 50);
            }

            if (now >= m_NextRefresh)
            {
                m_NextRefresh = now + m_RefreshSeconds;
                string lang = Language.Current;
                if (lang != m_Language)
                {
                    m_Language = lang;
                    m_Scheduler.Refresh();
                }
                if (m_Interaction != null && m_Interaction.Current != m_LastVehicle)
                {
                    m_LastVehicle = m_Interaction.Current;
                    m_Scheduler.Refresh(); // just got in or out: say the fresh hint even if the words repeat
                    if (m_PendingPaint && m_LastVehicle == null)
                    {
                        m_PendingPaint = false; // the paint button is visible now (on foot)
                        m_Scheduler.PushEvent("unlock_paint", 90);
                    }
                }
                m_Scheduler.SetHint(ComputeHint(), now);
            }

            if (m_Scheduler.TryNext(now, out GuideScheduler.Say say)) Present(say, now);
        }

        // ------------------------------------------------------------------ what to say
        /// <summary>The single most useful instruction right now, or null to keep the current one.</summary>
        public string ComputeHint()
        {
            Vehicle v = m_Interaction != null ? m_Interaction.Current : null;
            if (v == null)
            {
                VehicleType? need = SuggestVehicle();
                return need.HasValue ? "pick_" + need.Value.ToString().ToLowerInvariant() : "pick_any";
            }
            if (v.Tool != null && v.Tool.IsBusy) return null;
            var recovery = v.GetComponent<VehicleRecovery>();
            if (recovery != null && recovery.SeemsStuck) return "stuck";
            // A low-priority "tip it anywhere" offer only becomes the hint when no job wants this machine.
            bool hasJob = GuidanceRegistry.TryFind(v, out _, out IGuidanceTarget job);
            if (m_Actions != null && m_Actions.HasAction && (m_Actions.Current.Priority >= 5 || !hasJob))
            {
                switch (m_Actions.Current.Icon)
                {
                    case ActionIcon.Scoop: return "do_scoop";
                    case ActionIcon.Dump: return "do_dump";
                    case ActionIcon.Load: return "do_load";
                    case ActionIcon.Unload: return "do_unload";
                }
            }
            if (hasJob)
            {
                string key = job.GuideHint(v);
                return m_Library == null || m_Library.Has(key) ? key : "follow_arrow";
            }
            return "no_job";
        }

        /// <summary>Which machine has work waiting: the open project stage nearest the yard.</summary>
        private VehicleType? SuggestVehicle()
        {
            Vector3 from = m_Interaction != null ? m_Interaction.FocusPoint : Vector3.zero;
            ActivityBase best = null;
            float bestSq = float.MaxValue;
            foreach (ActivityBase a in ActivityBase.All)
            {
                if (a == null || a.IsRepeatable || !a.IsOpen || a is ProjectActivity) continue;
                float sq = (a.FocusPosition - from).sqrMagnitude;
                if (sq < bestSq) { bestSq = sq; best = a; }
            }
            return best != null ? best.NeededVehicle : (VehicleType?)null;
        }

        // ------------------------------------------------------------------ events
        private void OnStageFinished(ActivityBase activity, int stage, bool projectDone)
        {
            if (activity == null) return;
            if (activity is ProjectActivity project)
            {
                if (projectDone) m_Scheduler.PushEvent(project.Id + "_done", 100);
                return; // the stage's own activity says its line
            }
            string key = activity.Id + "_done";
            if (m_Library == null || m_Library.Has(key)) m_Scheduler.PushEvent(key, 80);
        }

        private void OnUnlocked(UnlockDefinition u)
        {
            if (u == null) return;
            if (u.kind == UnlockKind.Paint && m_Interaction != null && m_Interaction.IsDriving)
            {
                m_PendingPaint = true; // "tap the paint button" only makes sense on foot, where the button is
                return;
            }
            string key = u.kind == UnlockKind.Paint ? "unlock_paint" : u.kind == UnlockKind.Area ? "unlock_area" : "unlock_decor";
            m_Scheduler.PushEvent(key, 90);
        }

        private void OnCelebrate(Vector3 at, CelebrationSize size)
        {
            if (size != CelebrationSize.Small) return;
            Vehicle v = m_Interaction != null ? m_Interaction.Current : null;
            if (v == null) return;
            string key;
            switch (v.Type)
            {
                case VehicleType.Excavator: key = "praise_dig"; break;
                case VehicleType.Bulldozer: key = "praise_push"; break;
                case VehicleType.RoadRoller: key = "praise_roll"; break;
                default: key = "praise_deliver"; break;
            }
            m_Scheduler.PushPraise(key, Time.unscaledTime);
        }

        private void OnMascotTapped()
        {
            GameServices.Instance?.Audio.Play(Audio.SfxId.Pop);
            m_Scheduler.RequestReplay();
        }

        // ------------------------------------------------------------------ showing
        private void Present(GuideScheduler.Say say, float now)
        {
            if (m_Library == null) return;
            if (!m_Library.TryGet(say.Key, m_Language, out string text, out AudioClip clip)) return;
            string previous = LastShownKey;
            LastShownKey = say.Key;
            float length = clip != null ? clip.length : 2.5f;
            GameServices s = GameServices.Instance;
            // A new instruction may cut off the previous one; the same line never interrupts itself.
            bool spoke = say.Speak && s != null && s.Audio.PlayVoice(clip, say.IsEvent || say.Key != previous);
            if (say.Speak && !spoke && !say.IsEvent) m_Scheduler.ForgetSpoken(say.Key); // not heard: may be said next time
            if (say.IsEvent) m_Scheduler.Hold(now + Mathf.Max(2.5f, (spoke ? length : 1.5f) + 0.6f));
            Sprite icon = IconFor(say.Key, out Color disc);
            if (m_Bubble != null) m_Bubble.Show(text, icon, disc, length + 3f);
        }

        private Sprite IconFor(string key, out Color disc)
        {
            disc = Color.white;
            if (m_Config == null || key == null) return null;
            if (key.StartsWith("pick_"))
            {
                foreach (VehicleProfile p in m_Config.vehicles)
                {
                    if (p != null && "pick_" + p.type.ToString().ToLowerInvariant() == key) return p.portrait;
                }
                return null;
            }
            switch (key)
            {
                case "do_scoop": disc = DiscYellow; return m_Config.iconScoop;
                case "do_dump": disc = DiscYellow; return m_Config.iconDump;
                case "do_load": disc = DiscYellow; return m_Config.iconLoad;
                case "do_unload": disc = DiscYellow; return m_Config.iconUnload;
                case "get_load": disc = DiscBlue; return m_Config.iconLoad;
                case "no_job": disc = DiscRed; return m_Config.iconExit;
                case "stuck": disc = DiscPurple; return m_Config.iconReset;
                case "follow_arrow": disc = DiscYellow; return m_Config.iconArrow;
            }
            if (key.StartsWith("unlock_") || key.StartsWith("praise_") || key.EndsWith("_done") || key == "welcome")
            {
                disc = Color.white;
                return m_Config.iconStar;
            }
            if (key.StartsWith("house_")) { disc = DiscOrange; return m_Config.iconHouse; }
            if (key.StartsWith("road_")) { disc = DiscGrey; return m_Config.iconRoad; }
            if (key.StartsWith("pg_")) { disc = DiscGreen; return m_Config.iconPlayground; }
            if (key.StartsWith("yard_")) { disc = DiscYellow; return m_Config.iconScoop; }
            return null;
        }
    }
}
