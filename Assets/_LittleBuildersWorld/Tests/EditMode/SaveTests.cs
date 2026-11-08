using LittleBuildersWorld.Save;
using NUnit.Framework;

namespace LittleBuildersWorld.Tests
{
    public sealed class SaveTests
    {
        [Test]
        public void RoundTrip_Keeps_Progress_And_Settings()
        {
            var d = new SaveData { builderStars = 7, selectedVehicle = "excavator" };
            d.grantedRewards.Add("house.stage0");
            d.GetActivity("house", true).stage = 3;
            d.SetPaint("dumptruck", 2);
            d.audio.music = 0.25f;
            d.controls.steering = SteeringMode.Joystick;
            SaveData back = SaveSerializer.Deserialize(SaveSerializer.Serialize(d), out LoadOutcome outcome);
            Assert.AreEqual(LoadOutcome.Loaded, outcome);
            Assert.AreEqual(7, back.builderStars);
            Assert.AreEqual(3, back.GetActivity("house", false).stage);
            Assert.AreEqual(2, back.GetPaint("dumptruck"));
            Assert.AreEqual(0.25f, back.audio.music, 0.0001f);
            Assert.AreEqual(SteeringMode.Joystick, back.controls.steering);
            Assert.AreEqual("excavator", back.selectedVehicle);
        }

        [TestCase("")]
        [TestCase("   ")]
        public void Missing_File_Gives_Defaults(string json)
        {
            SaveData d = SaveSerializer.Deserialize(json, out LoadOutcome outcome);
            Assert.AreEqual(LoadOutcome.Missing, outcome);
            Assert.AreEqual(0, d.builderStars);
        }

        [TestCase("not json at all")]
        [TestCase("{\"builderStars\": 5, \"activities\": [")]
        [TestCase("\u0000\u0001garbage")]
        public void Corrupt_File_Gives_Safe_Defaults(string json)
        {
            SaveData d = SaveSerializer.Deserialize(json, out LoadOutcome outcome);
            Assert.AreEqual(LoadOutcome.Corrupt, outcome);
            Assert.AreEqual(0, d.builderStars);
            Assert.IsNotNull(d.activities);
        }

        [Test]
        public void Unversioned_File_Is_Migrated()
        {
            SaveData d = SaveSerializer.Deserialize("{\"builderStars\": 4}", out LoadOutcome outcome);
            Assert.AreEqual(LoadOutcome.Migrated, outcome);
            Assert.AreEqual(4, d.builderStars);
            Assert.AreEqual(SaveData.CurrentVersion, d.version);
        }

        [Test]
        public void Newer_Version_Loads_Best_Effort()
        {
            SaveData d = SaveSerializer.Deserialize("{\"version\": 99, \"builderStars\": 3, \"futureField\": 1}", out LoadOutcome outcome);
            Assert.AreEqual(LoadOutcome.NewerVersion, outcome);
            Assert.AreEqual(3, d.builderStars);
        }

        [Test]
        public void Sanitise_Repairs_Bad_Values()
        {
            SaveData d = SaveSerializer.Deserialize("{\"version\":1,\"builderStars\":-5,\"audio\":{\"music\":7},\"controls\":{\"steering\":9,\"steerSensitivity\":0},\"activities\":[{\"id\":\"\",\"stage\":1},{\"id\":\"a\",\"stage\":-2}]}", out _);
            Assert.AreEqual(0, d.builderStars);
            Assert.AreEqual(1f, d.audio.music);
            Assert.AreEqual(SteeringMode.Buttons, d.controls.steering);
            Assert.AreEqual(1f, d.controls.steerSensitivity);
            Assert.AreEqual(1, d.activities.Count);
            Assert.AreEqual(0, d.activities[0].stage);
        }

        [Test]
        public void Save_Contains_No_Personal_Data_Fields()
        {
            string json = SaveSerializer.Serialize(new SaveData());
            foreach (string banned in new[] { "name", "email", "device", "location", "age" })
                StringAssert.DoesNotContain("\"" + banned, json.ToLowerInvariant());
        }
    }
}
