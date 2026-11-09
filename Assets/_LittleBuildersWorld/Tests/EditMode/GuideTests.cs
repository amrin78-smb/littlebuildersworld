using LittleBuildersWorld.Guide;
using NUnit.Framework;
using UnityEngine;

namespace LittleBuildersWorld.Tests
{
    public sealed class GuideTests
    {
        private static GuideScheduler.Say Next(GuideScheduler s, float now)
        {
            Assert.IsTrue(s.TryNext(now, out GuideScheduler.Say say), $"expected a line at t={now}");
            return say;
        }

        [Test]
        public void HintMustBeStableBeforeShowing()
        {
            var s = new GuideScheduler();
            s.SetHint("house_clear", 0f);
            Assert.IsFalse(s.TryNext(0.5f, out _), "flicker guard: too early");
            GuideScheduler.Say say = Next(s, 0.9f);
            Assert.AreEqual("house_clear", say.Key);
            Assert.IsTrue(say.Speak);
            Assert.IsFalse(s.TryNext(1f, out _), "no repeat right away");
        }

        [Test]
        public void ButtonHintsAreQuick()
        {
            var s = new GuideScheduler();
            s.SetHint("do_scoop", 0f);
            Assert.AreEqual("do_scoop", Next(s, 0.35f).Key);
        }

        [Test]
        public void FlickeringHintIsNeverShown()
        {
            var s = new GuideScheduler();
            for (int i = 0; i < 10; i++)
            {
                s.SetHint(i % 2 == 0 ? "a" : "b", i * 0.4f);
                Assert.IsFalse(s.TryNext(i * 0.4f + 0.1f, out _));
            }
        }

        [Test]
        public void SameLineIsNotSpokenAgainWithinCooldown()
        {
            var s = new GuideScheduler { VoiceCooldown = 20f };
            s.SetHint("a", 0f);
            Assert.IsTrue(Next(s, 1f).Speak);
            s.SetHint("b", 2f);
            Assert.IsTrue(Next(s, 3f).Speak);
            s.SetHint("a", 4f);
            GuideScheduler.Say again = Next(s, 5f);
            Assert.AreEqual("a", again.Key);
            Assert.IsFalse(again.Speak, "shown in the bubble, but not said twice in 20 s");
            s.SetHint("b", 30f);
            Assert.AreEqual("b", Next(s, 31f).Key);
            s.SetHint("a", 31.5f);
            GuideScheduler.Say later = Next(s, 32.5f);
            Assert.AreEqual("a", later.Key);
            Assert.IsTrue(later.Speak, "after the cooldown it is said again");
        }

        [Test]
        public void UnchangedHintIsRepeatedGently()
        {
            var s = new GuideScheduler { RepeatSeconds = 30f, VoiceCooldown = 20f };
            s.SetHint("a", 0f);
            Next(s, 1f);
            Assert.IsFalse(s.TryNext(20f, out _));
            GuideScheduler.Say r = Next(s, 31.5f);
            Assert.AreEqual("a", r.Key);
            Assert.IsTrue(r.Speak);
        }

        [Test]
        public void EventsBeatHintsAndHighestPriorityGoesFirst()
        {
            var s = new GuideScheduler { MaxQueuedEvents = 2 };
            s.SetHint("a", 0f);
            s.PushEvent("stage_done", 80);
            s.PushEvent("project_done", 100);
            s.PushEvent("praise", 10); // dropped: only two wait
            Assert.AreEqual("project_done", Next(s, 1f).Key);
            s.Hold(4f);
            Assert.IsFalse(s.TryNext(2f, out _), "held while the event line is said");
            GuideScheduler.Say second = Next(s, 4.1f);
            Assert.AreEqual("stage_done", second.Key);
            Assert.IsTrue(second.IsEvent && second.Speak);
            Assert.AreEqual("a", Next(s, 4.2f).Key, "the hint comes back after the events");
            Assert.AreEqual(0, s.QueuedEvents);
        }

        [Test]
        public void TapReplaysCurrentHintEvenWithinCooldown()
        {
            var s = new GuideScheduler();
            s.SetHint("a", 0f);
            Next(s, 1f);
            s.RequestReplay();
            GuideScheduler.Say r = Next(s, 2f);
            Assert.AreEqual("a", r.Key);
            Assert.IsTrue(r.Speak);
        }

        [Test]
        public void PraiseIsRateLimitedAndGoesStale()
        {
            var s = new GuideScheduler { PraiseCooldown = 20f };
            s.PushPraise("praise_dig", 0f);
            Assert.AreEqual("praise_dig", Next(s, 0.5f).Key);
            s.PushPraise("praise_dig", 5f);
            Assert.IsFalse(s.TryNext(5.1f, out _), "too soon for another well done");
            s.PushPraise("praise_dig", 30f);
            Assert.IsFalse(s.TryNext(33f, out _), "stale praise is dropped");
        }

        [Test]
        public void LanguageFollowsSettingThenDevice()
        {
            Assert.AreEqual(Language.Thai, Language.Resolve("th", SystemLanguage.English));
            Assert.AreEqual(Language.English, Language.Resolve("en", SystemLanguage.Thai));
            Assert.AreEqual(Language.Thai, Language.Resolve("", SystemLanguage.Thai));
            Assert.AreEqual(Language.English, Language.Resolve("", SystemLanguage.German));
            Assert.AreEqual(Language.English, Language.Resolve("xx", SystemLanguage.French));
        }

        [Test]
        public void ThaiToneMarksAreLiftedAndNudged()
        {
            // ที่ : tone mark after an upper vowel is lifted above it.
            Assert.AreEqual("ที<voffset=0.24em>่</voffset>", ThaiText.PlaceMarks("ที่"));
            // ปุ่ม : tall consonant, the tone mark moves left of the stem; the lower vowel does not count as upper.
            Assert.AreEqual("ปุ<space=-0.15em>่<space=0.15em>ม", ThaiText.PlaceMarks("ปุ่ม"));
            // น้ำ : tone mark before sara am is lifted over its circle.
            Assert.AreEqual("น<voffset=0.24em>้</voffset>ำ", ThaiText.PlaceMarks("น้ำ"));
            // Plain marks and Latin text are untouched.
            Assert.AreEqual("สวัสดี", ThaiText.PlaceMarks("สวัสดี"));
            Assert.AreEqual("แตะเลย!", ThaiText.PlaceMarks("แตะเลย!"));
            Assert.AreEqual("Hello!", ThaiText.PlaceMarks("Hello!"));
        }

        [Test]
        public void LibraryFallsBackToEnglishWhenThaiMissing()
        {
            var lib = ScriptableObject.CreateInstance<GuideLibrary>();
            lib.lines = new[]
            {
                new GuideLibrary.Line { key = "a", en = "Hello", th = "สวัสดี" },
                new GuideLibrary.Line { key = "b", en = "Only English" }
            };
            Assert.IsTrue(lib.TryGet("a", Language.Thai, out string t, out _));
            Assert.AreEqual("สวัสดี", t);
            Assert.IsTrue(lib.TryGet("b", Language.Thai, out t, out _));
            Assert.AreEqual("Only English", t);
            Assert.IsFalse(lib.TryGet("missing", Language.English, out _, out _));
            Object.DestroyImmediate(lib);
        }
    }
}
