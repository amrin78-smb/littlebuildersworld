using System.Collections.Generic;

namespace LittleBuildersWorld.Guide
{
    /// <summary>
    /// Pure timing rules for the helper (no Unity objects, unit tested): which line to show and whether to speak it.
    /// <list type="bullet">
    /// <item>A hint must stay the same for a moment before it is shown, so driving along a zone edge never flickers.</item>
    /// <item>Events (a stage finished, an unlock) beat hints; the highest priority wins and at most two wait.</item>
    /// <item>The same line is spoken at most once per cooldown; the bubble still shows (the game works muted).</item>
    /// <item>An unchanged hint is gently repeated after a while; tapping the helper repeats it at once.</item>
    /// </list>
    /// </summary>
    public sealed class GuideScheduler
    {
        public struct Say
        {
            public string Key;
            public bool Speak;
            public bool IsEvent;
        }

        public float StableSeconds = 0.8f;
        public float QuickStableSeconds = 0.3f;
        public float RepeatSeconds = 30f;
        public float VoiceCooldown = 20f;
        public float PraiseCooldown = 20f;
        public int MaxQueuedEvents = 2;

        private string m_Candidate;
        private float m_CandidateSince;
        private string m_Current;
        private float m_CurrentShownAt;
        private float m_HoldUntil;
        private bool m_ReplayRequested;
        private string m_Praise;
        private float m_PraiseAt = -1000f;
        private float m_LastPraise = -1000f;
        private readonly List<(string key, int priority)> m_Events = new List<(string, int)>();
        private readonly Dictionary<string, float> m_LastSpoken = new Dictionary<string, float>();

        public string CurrentHint => m_Current;
        public int QueuedEvents => m_Events.Count;

        public static bool IsQuick(string key) => key != null && (key.StartsWith("do_") || key == "stuck");

        /// <summary>The hint that fits the game right now (call regularly). null = no opinion (e.g. a machine is busy).</summary>
        public void SetHint(string key, float now)
        {
            if (key == null) return;
            if (key == m_Candidate) return;
            m_Candidate = key;
            m_CandidateSince = now;
        }

        public void PushEvent(string key, int priority)
        {
            if (string.IsNullOrEmpty(key)) return;
            m_Events.Add((key, priority));
            m_Events.Sort((a, b) => b.priority.CompareTo(a.priority));
            while (m_Events.Count > MaxQueuedEvents) m_Events.RemoveAt(m_Events.Count - 1);
        }

        /// <summary>A little "well done" after a unit of work. Dropped if one was said recently or it goes stale.</summary>
        public void PushPraise(string key, float now)
        {
            if (string.IsNullOrEmpty(key) || now - m_LastPraise < PraiseCooldown) return;
            m_Praise = key;
            m_PraiseAt = now;
        }

        /// <summary>The child tapped the helper: say the current hint again, now.</summary>
        public void RequestReplay() => m_ReplayRequested = true;

        /// <summary>Nothing new is shown before this time (an event line is still being said).</summary>
        public void Hold(float until)
        {
            if (until > m_HoldUntil) m_HoldUntil = until;
        }

        /// <summary>The line was not actually heard (voice busy or off), so it does not count against the cooldown.</summary>
        public void ForgetSpoken(string key)
        {
            if (key != null) m_LastSpoken.Remove(key);
        }

        /// <summary>Forget the shown hint so it is shown again on the next chance (e.g. after the language changed).</summary>
        public void Refresh() => m_Current = null;

        public bool TryNext(float now, out Say say)
        {
            say = default;
            if (now < m_HoldUntil) return false;

            if (m_Events.Count > 0)
            {
                string key = m_Events[0].key;
                m_Events.RemoveAt(0);
                m_Current = null; // after the event, show the hint again
                say = new Say { Key = key, Speak = MarkSpoken(key, now, true), IsEvent = true };
                return true;
            }

            if (m_Praise != null)
            {
                string key = m_Praise;
                m_Praise = null;
                if (now - m_PraiseAt < 2f)
                {
                    m_LastPraise = now;
                    say = new Say { Key = key, Speak = MarkSpoken(key, now, true), IsEvent = true };
                    return true;
                }
            }

            if (m_Candidate != null && m_Candidate != m_Current)
            {
                float need = IsQuick(m_Candidate) ? QuickStableSeconds : StableSeconds;
                if (now - m_CandidateSince >= need)
                {
                    m_Current = m_Candidate;
                    m_CurrentShownAt = now;
                    m_ReplayRequested = false;
                    say = new Say { Key = m_Current, Speak = MarkSpoken(m_Current, now, false) };
                    return true;
                }
            }

            if (m_Current != null && m_ReplayRequested)
            {
                m_ReplayRequested = false;
                m_CurrentShownAt = now;
                say = new Say { Key = m_Current, Speak = MarkSpoken(m_Current, now, true) };
                return true;
            }
            m_ReplayRequested = false;

            if (m_Current != null && m_Current == m_Candidate && now - m_CurrentShownAt >= RepeatSeconds)
            {
                m_CurrentShownAt = now;
                say = new Say { Key = m_Current, Speak = MarkSpoken(m_Current, now, false) };
                return true;
            }
            return false;
        }

        private bool MarkSpoken(string key, float now, bool force)
        {
            if (!force && m_LastSpoken.TryGetValue(key, out float last) && now - last < VoiceCooldown) return false;
            m_LastSpoken[key] = now;
            return true;
        }
    }
}
