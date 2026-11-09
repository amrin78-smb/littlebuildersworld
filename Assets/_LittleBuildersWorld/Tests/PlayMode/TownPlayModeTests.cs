using System.Collections;
using System.IO;
using LittleBuildersWorld.Core;
using LittleBuildersWorld.Input;
using LittleBuildersWorld.UI;
using LittleBuildersWorld.Vehicles;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LittleBuildersWorld.Tests
{
    /// <summary>Loads the real town scene and exercises driving and interaction with a throwaway save.</summary>
    public sealed class TownPlayModeTests
    {
        private string m_SaveDir;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            m_SaveDir = Path.Combine(Application.temporaryCachePath, "lbw_tests_" + System.Guid.NewGuid().ToString("N"));
            if (GameServices.Instance == null) GameServices.Create(m_SaveDir);
            GameServices.Instance.ReinitialiseForTests(m_SaveDir);
            yield return SceneManager.LoadSceneAsync(SceneFlow.Town);
            yield return new WaitForSeconds(0.5f);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (Directory.Exists(m_SaveDir)) Directory.Delete(m_SaveDir, true);
        }

        private static Vehicle Get(VehicleType t)
        {
            foreach (Vehicle v in Vehicle.All) if (v.Type == t) return v;
            return null;
        }

        [UnityTest]
        public IEnumerator Enter_Drive_Release_Exit()
        {
            var interaction = Object.FindFirstObjectByType<VehicleInteraction>();
            var input = Object.FindFirstObjectByType<TouchInputController>();
            Vehicle truck = Get(VehicleType.DumpTruck);
            Assert.IsTrue(interaction.Enter(truck));
            Assert.IsFalse(interaction.CanEnter(truck), "a vehicle in use cannot be entered again");
            Vector3 start = truck.transform.position;
            input.SetScriptedInput(new DriveInput(0f, 1f, 0f));
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(Vector3.Distance(start, truck.transform.position), 1.5f);
            input.SetScriptedInput(null);
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(0f, truck.Controller.Speed, 0.05f, "no input means the vehicle stops");
            interaction.Exit();
            yield return null;
            Assert.IsTrue(truck.Controller.Body.isKinematic, "exited vehicle is parked");
            Assert.AreEqual(GameMode.OnFoot, GameServices.Instance.State.Mode);
        }

        [UnityTest]
        public IEnumerator Steering_Turns_And_Vehicle_Stays_On_Ground()
        {
            var interaction = Object.FindFirstObjectByType<VehicleInteraction>();
            var input = Object.FindFirstObjectByType<TouchInputController>();
            Vehicle truck = Get(VehicleType.DumpTruck);
            Assert.IsTrue(interaction.Enter(truck));
            input.SetScriptedInput(new DriveInput(0f, 1f, 0f));
            for (int i = 0; i < 100; i++) yield return new WaitForFixedUpdate();
            input.SetScriptedInput(new DriveInput(0f, 0f, 0f));
            for (int i = 0; i < 125; i++) yield return new WaitForFixedUpdate();
            float yaw0 = truck.transform.eulerAngles.y;
            input.SetScriptedInput(new DriveInput(1f, 0.6f, 0f));
            float maxY = 0f;
            for (int i = 0; i < 75; i++)
            {
                yield return new WaitForFixedUpdate();
                maxY = Mathf.Max(maxY, truck.transform.position.y);
            }
            float turned = Mathf.Abs(Mathf.DeltaAngle(yaw0, truck.transform.eulerAngles.y));
            Debug.Log($"[LBW Test] turned {turned:0}° speed {truck.Controller.Speed:0.0} at {truck.transform.position} maxY {maxY:0.00}");
            Assert.Greater(turned, 20f, "steering right turns the truck");
            Assert.Less(maxY, 0.45f, "the truck stays on the ground");
            Assert.Less(Vector3.Angle(truck.transform.up, Vector3.up), 1f, "the truck stays upright");
            input.SetScriptedInput(null);
            interaction.Exit();
        }

        [UnityTest]
        public IEnumerator Hold_Button_Releases_When_No_Pointer_Is_Down()
        {
            var interaction = Object.FindFirstObjectByType<VehicleInteraction>();
            Vehicle truck = Get(VehicleType.DumpTruck);
            interaction.Enter(truck);
            yield return null;
            HoldButton go = null;
            foreach (HoldButton h in Object.FindObjectsByType<HoldButton>(FindObjectsSortMode.None))
                if (h.transform.parent != null && h.transform.parent.name == "Go") go = h;
            Assert.IsNotNull(go, "Go button exists");
            go.OnPointerDown(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { pointerId = 3 });
            Assert.IsTrue(go.IsHeld);
            yield return new WaitForSecondsRealtime(0.3f); // no real finger is down: the watchdog must let go
            Assert.IsFalse(go.IsHeld, "a lost pointer-up never leaves the accelerator stuck");
        }

        [UnityTest]
        public IEnumerator Excavator_Scoops_In_The_Yard_Pit()
        {
            var interaction = Object.FindFirstObjectByType<VehicleInteraction>();
            var actions = Object.FindFirstObjectByType<VehicleActionController>();
            Vehicle exc = Get(VehicleType.Excavator);
            exc.Controller.Teleport(new Vector3(-14f, 0.05f, -3f), Quaternion.Euler(0f, 200f, 0f));
            interaction.Enter(exc);
            yield return null;
            yield return null;
            Assert.AreEqual(ActionIcon.Scoop, actions.Current.Icon);
            Assert.IsTrue(actions.Perform());
            var arm = exc.GetTool<ExcavatorArm>();
            float t = 0f;
            while (arm.IsBusy && t < 5f) { t += Time.deltaTime; yield return null; }
            Assert.IsTrue(arm.HasSoil);
            yield return null; // the button re-evaluates once per frame
            Assert.AreEqual(ActionIcon.Dump, actions.Current.Icon, "one action at a time: now Dump");
            interaction.Exit();
            Assert.IsTrue(arm.HasSoil, "leaving keeps the load; nothing is lost");
        }
    }
}
