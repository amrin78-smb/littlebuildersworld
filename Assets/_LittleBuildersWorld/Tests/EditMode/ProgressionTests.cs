using System.Collections.Generic;
using LittleBuildersWorld.Activities;
using LittleBuildersWorld.Progression;
using LittleBuildersWorld.Save;
using NUnit.Framework;

namespace LittleBuildersWorld.Tests
{
    public sealed class ProgressionTests
    {
        private static readonly List<UnlockDefinition> Unlocks = new List<UnlockDefinition>
        {
            new UnlockDefinition { id = "a", starsRequired = 2 },
            new UnlockDefinition { id = "b", starsRequired = 5 }
        };

        [Test]
        public void Each_Reward_Pays_Only_Once()
        {
            var d = new SaveData();
            Assert.IsTrue(StarLedger.TryGrant(d, "house.stage0", 1));
            Assert.IsFalse(StarLedger.TryGrant(d, "house.stage0", 1));
            Assert.AreEqual(1, d.builderStars);
        }

        [Test]
        public void Unlocks_Follow_The_Total_And_Are_Never_Removed()
        {
            var d = new SaveData();
            StarLedger.TryGrant(d, "x", 2);
            List<UnlockDefinition> fresh = StarLedger.EvaluateUnlocks(d, Unlocks);
            Assert.AreEqual(1, fresh.Count);
            Assert.AreEqual("a", fresh[0].id);
            Assert.AreEqual(0, StarLedger.EvaluateUnlocks(d, Unlocks).Count, "no repeat celebrations");
            d.builderStars = 0; // even if something odd happened to the total
            StarLedger.EvaluateUnlocks(d, Unlocks);
            Assert.Contains("a", d.unlocked);
        }

        [Test]
        public void Activity_Lifecycle_Goes_Available_Active_StageCompleted_Completed()
        {
            var p = new ActivityProgress(new[] { 2, 1 });
            Assert.AreEqual(ActivityState.Available, p.State);
            Assert.AreEqual(-1, p.AddProgress());
            Assert.AreEqual(ActivityState.Active, p.State);
            Assert.AreEqual(0, p.AddProgress());
            Assert.AreEqual(ActivityState.StageCompleted, p.State);
            Assert.AreEqual(1, p.AddProgress());
            Assert.AreEqual(ActivityState.Completed, p.State);
            Assert.AreEqual(-1, p.AddProgress(), "work after completion is ignored");
            Assert.AreEqual(2, p.Stage);
        }

        [Test]
        public void Restore_Clamps_Out_Of_Range_Saves()
        {
            var p = new ActivityProgress(new[] { 3 });
            p.Restore(0, 99);
            Assert.AreEqual(2, p.Progress, "never restores a finished stage as unfinished-but-full");
            p.Restore(7, 1);
            Assert.IsTrue(p.IsComplete);
            p.Restore(-3, -3);
            Assert.AreEqual(0, p.Stage);
            Assert.AreEqual(0, p.Progress);
        }

        [Test]
        public void Abandon_And_Resume_Keeps_Partial_Progress()
        {
            var p = new ActivityProgress(new[] { 3 });
            p.AddProgress();
            p.AddProgress();
            var resumed = new ActivityProgress(new[] { 3 });
            resumed.Restore(p.Stage, p.Progress);
            Assert.AreEqual(ActivityState.Active, resumed.State);
            Assert.AreEqual(0, resumed.AddProgress());
        }
    }
}
