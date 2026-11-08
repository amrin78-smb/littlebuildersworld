using LittleBuildersWorld.Vehicles;
using NUnit.Framework;

namespace LittleBuildersWorld.Tests
{
    public sealed class DriveModelTests
    {
        private static VehicleDriveModel Model() => new VehicleDriveModel(DriveTuning.Default);

        private static void Run(VehicleDriveModel m, DriveInput input, float seconds)
        {
            for (float t = 0f; t < seconds; t += 0.02f) m.Step(input, 0.02f);
        }

        [Test]
        public void Accelerates_Smoothly_And_Caps_At_Top_Speed()
        {
            var m = Model();
            m.Step(new DriveInput(0, 1, 0), 0.02f);
            Assert.Less(m.Speed, 0.2f, "no instant jump to speed");
            Run(m, new DriveInput(0, 1, 0), 10f);
            Assert.AreEqual(DriveTuning.Default.MaxForwardSpeed, m.Speed, 0.001f);
        }

        [Test]
        public void Releasing_All_Input_Stops_The_Vehicle()
        {
            var m = Model();
            Run(m, new DriveInput(0, 1, 0), 5f);
            Run(m, DriveInput.None, 3f);
            Assert.AreEqual(0f, m.Speed);
            Assert.AreEqual(0f, m.YawRate);
        }

        [Test]
        public void Brake_Stops_Then_Reverses_Gently()
        {
            var m = Model();
            Run(m, new DriveInput(0, 1, 0), 5f);
            Run(m, new DriveInput(0, 0, 1), 0.5f);
            Assert.Greater(m.Speed, -0.01f, "first it brakes");
            Run(m, new DriveInput(0, 0, 1), 5f);
            Assert.AreEqual(-DriveTuning.Default.MaxReverseSpeed, m.Speed, 0.001f);
        }

        [Test]
        public void Wheeled_Vehicle_Does_Not_Turn_When_Stopped_But_Tracked_Can()
        {
            var wheeled = Model();
            Run(wheeled, new DriveInput(1, 0, 0), 1f);
            Assert.AreEqual(0f, wheeled.YawRate, 0.001f);

            DriveTuning tracked = DriveTuning.Default;
            tracked.TurnInPlace = 0.7f;
            var dozer = new VehicleDriveModel(tracked);
            Run(dozer, new DriveInput(1, 0, 0), 1f);
            Assert.Greater(dozer.YawRate, 1f);
        }

        [Test]
        public void Steering_Is_Smoothed_Not_Instant()
        {
            var m = Model();
            m.Step(new DriveInput(1, 1, 0), 0.02f);
            Assert.Less(m.Steer, 0.5f);
            Run(m, new DriveInput(1, 1, 0), 1f);
            Assert.AreEqual(1f, m.Steer, 0.001f);
        }

        [Test]
        public void Reversing_Turns_The_Nose_The_Other_Way()
        {
            var m = Model();
            Run(m, new DriveInput(0, 0, 1), 3f);
            Run(m, new DriveInput(1, 0, 1), 1f);
            Assert.Less(m.YawRate, 0f);
        }

        [Test]
        public void Blocked_Vehicle_Does_Not_Build_Hidden_Speed()
        {
            var m = Model();
            Run(m, new DriveInput(0, 1, 0), 5f);
            for (int i = 0; i < 10; i++) m.ReconcileMeasuredSpeed(0f);
            Assert.Less(m.Speed, 0.5f);
        }

        [Test]
        public void DriveInput_Clamps_Values()
        {
            var i = new DriveInput(5f, 2f, -1f);
            Assert.AreEqual(1f, i.Steer);
            Assert.AreEqual(1f, i.Throttle);
            Assert.AreEqual(0f, i.Brake);
        }
    }
}
