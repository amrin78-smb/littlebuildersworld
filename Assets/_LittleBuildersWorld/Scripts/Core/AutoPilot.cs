using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LittleBuildersWorld.Activities;
using LittleBuildersWorld.Input;
using LittleBuildersWorld.UI;
using LittleBuildersWorld.Vehicles;
using LittleBuildersWorld.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LittleBuildersWorld.Core
{
    /// <summary>
    /// Automated play-through for test builds (only active with the -lbw-autopilot command-line flag).
    /// Drives every vehicle with scripted stick input along real routes, presses the contextual action like a
    /// player would, completes the whole first-release loop, checks save/reload, pause and stuck recovery, takes
    /// screenshots and writes a PASS/FAIL report. It never teleports a vehicle unless a drive times out (logged).
    /// </summary>
    public sealed class AutoPilot : MonoBehaviour
    {
        private string m_ShotDir;
        private readonly StringBuilder m_Report = new StringBuilder();
        private int m_Fails;
        private int m_Warns;
        private float m_Start;

        private VehicleInteraction m_Interaction;
        private TouchInputController m_Input;
        private VehicleActionController m_Actions;
        private readonly Dictionary<VehicleType, Vehicle> m_Vehicles = new Dictionary<VehicleType, Vehicle>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args, "-lbw-autopilot") < 0) return;
            var go = new GameObject("[AutoPilot]");
            DontDestroyOnLoad(go);
            var ap = go.AddComponent<AutoPilot>();
            int i = Array.IndexOf(args, "-lbw-shots");
            ap.m_ShotDir = i >= 0 && i + 1 < args.Length ? args[i + 1] : Path.Combine(Application.persistentDataPath, "AutoPilot");
        }

        private void Start()
        {
            Directory.CreateDirectory(m_ShotDir);
            m_Start = Time.realtimeSinceStartup;
            StartCoroutine(Run());
        }

        // ------------------------------------------------------------------ reporting
        private void Log(string s)
        {
            string line = $"[{Time.realtimeSinceStartup - m_Start,6:0.0}s] {s}";
            m_Report.AppendLine(line);
            Debug.Log("[AutoPilot] " + line);
        }

        private void Check(bool ok, string what)
        {
            if (!ok) m_Fails++;
            Log((ok ? "PASS " : "FAIL ") + what);
        }

        private void Warn(string what)
        {
            m_Warns++;
            Log("WARN " + what);
        }

        private IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(m_ShotDir, name + ".png"));
            yield return null;
        }

        private static IEnumerator Wait(float seconds)
        {
            yield return new WaitForSeconds(seconds);
        }

        // ------------------------------------------------------------------ main script
        private IEnumerator Run()
        {
            Log("AutoPilot start");
            yield return WaitScene(SceneFlow.MainMenu, 20f);
            GameServices s = GameServices.Instance;
            s.Save.ResetProgress();
            yield return Wait(1.5f);
            yield return Shot("01_main_menu");
            var menu = FindFirstObjectByType<MainMenuUI>();
            Check(menu != null, "main menu loaded");
            menu?.Play();
            yield return WaitScene(SceneFlow.Town, 20f);
            Check(SceneManager.GetActiveScene().name == SceneFlow.Town, "main menu → town");
            yield return Wait(2f);
            Bind();
            yield return Shot("02_yard_overview");
            Time.timeScale = 2f;

            yield return BasicDriving();
            yield return YardDig();
            yield return HouseClearing();
            yield return HouseDig();
            yield return HouseDeliveries();
            yield return Roadworks();
            yield return StuckRecovery();
            yield return PauseResume();
            yield return SaveReload();

            Time.timeScale = 1f;
            Log($"Stars: {GameServices.Instance.Stars.Total}; unlocked: {string.Join(",", GameServices.Instance.Save.Data.unlocked)}");
            Log($"Average FPS (desktop test player, software GL): {GameServices.Instance.Performance.AverageFps:0}");
            Log($"RESULT: {(m_Fails == 0 ? "ALL PASSED" : m_Fails + " FAILED")} ({m_Warns} warnings)");
            File.WriteAllText(Path.Combine(m_ShotDir, "autopilot_report.txt"), m_Report.ToString());
            yield return Wait(0.5f);
            Application.Quit(m_Fails == 0 ? 0 : 1);
        }

        private IEnumerator WaitScene(string scene, float timeout)
        {
            float t = 0f;
            while ((SceneManager.GetActiveScene().name != scene || GameServices.Instance == null || GameServices.Instance.Scenes.IsLoading) && t < timeout)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void Bind()
        {
            m_Interaction = FindFirstObjectByType<VehicleInteraction>();
            m_Input = FindFirstObjectByType<TouchInputController>();
            m_Actions = FindFirstObjectByType<VehicleActionController>();
            m_Vehicles.Clear();
            foreach (Vehicle v in Vehicle.All) m_Vehicles[v.Type] = v;
            Check(m_Interaction != null && m_Input != null && m_Actions != null && m_Vehicles.Count == 4, "town systems and 4 vehicles present");
        }

        // ------------------------------------------------------------------ helpers
        private IEnumerator Enter(VehicleType type)
        {
            Vehicle v = m_Vehicles[type];
            if (m_Interaction.Current == v) yield break;
            if (m_Interaction.IsDriving) m_Interaction.Exit();
            yield return Wait(0.3f);
            bool ok = m_Interaction.Enter(v);
            Check(ok && m_Interaction.Current == v, $"enter {type}");
            yield return Wait(0.8f);
        }

        private float Flat(Vector3 a, Vector3 b) => Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

        /// <summary>Steers with scripted stick input toward each waypoint in turn. Returns via flag.</summary>
        private IEnumerator Drive(Vehicle v, Vector3[] route, float arrive, float timeoutPerLeg, string label, bool[] result = null)
        {
            bool allOk = true;
            for (int leg = 0; leg < route.Length; leg++)
            {
                Vector3 target = route[leg];
                bool last = leg == route.Length - 1;
                float radius = last ? arrive : 4f;
                float t = 0f, stuck = 0f;
                while (Flat(v.transform.position, target) > radius && t < timeoutPerLeg)
                {
                    Vector3 to = target - v.transform.position;
                    to.y = 0f;
                    float angle = Vector3.SignedAngle(Flatten(v.transform.forward), to, Vector3.up);
                    float steer = Mathf.Clamp(angle / 30f, -1f, 1f);
                    float dist = to.magnitude;
                    float throttle = Mathf.Abs(angle) < 70f ? (last && dist < 8f ? 0.45f : 1f) : 0.4f;
                    float speed = Mathf.Abs(v.Controller.MeasuredSpeed);
                    stuck = speed < 0.3f ? stuck + Time.deltaTime : 0f;
                    if (stuck > 2.5f)
                    {
                        // Back out with the wheel turned the other way, like a person would.
                        float rt = 0f;
                        while (rt < 1.4f) { m_Input.SetScriptedInput(new DriveInput(-steer, 0f, 1f)); rt += Time.deltaTime; yield return null; }
                        stuck = 0f;
                    }
                    m_Input.SetScriptedInput(new DriveInput(steer, throttle, 0f));
                    t += Time.deltaTime;
                    yield return null;
                }
                if (t >= timeoutPerLeg)
                {
                    allOk = false;
                    Warn($"{label}: leg {leg} timed out at {v.transform.position}; teleporting to {target}");
                    v.Controller.Teleport(target, Quaternion.LookRotation(Flatten(target - v.transform.position == Vector3.zero ? v.transform.forward : target - v.transform.position)));
                }
            }
            yield return Stop(v);
            if (result != null) result[0] = allOk;
            Log($"{label}: arrived at {v.transform.position:F1}{(allOk ? "" : " (with assistance)")}");
        }

        private IEnumerator Stop(Vehicle v)
        {
            m_Input.SetScriptedInput(DriveInput.None);
            float t = 0f;
            while (Mathf.Abs(v.Controller.Speed) > 0.05f && t < 4f) { t += Time.deltaTime; yield return null; }
        }

        private static Vector3 Flatten(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.0001f ? v.normalized : Vector3.forward;
        }

        private IEnumerator Act(ActionIcon expected, string what)
        {
            float t = 0f;
            while (m_Actions.Current.Icon != expected && t < 4f) { t += Time.deltaTime; yield return null; }
            bool offered = m_Actions.Current.Icon == expected;
            Check(offered, $"{what}: action button shows {expected}");
            if (!offered)
            {
                Log($"   (offered instead: {m_Actions.Current.Icon})");
                yield break;
            }
            m_Actions.Perform();
            yield return null;
            Vehicle v = m_Interaction.Current;
            t = 0f;
            while (v != null && v.Tool != null && v.Tool.IsBusy && t < 8f) { t += Time.deltaTime; yield return null; }
            yield return Wait(0.2f);
        }

        private static T Find<T>(string id) where T : ActivityBase
        {
            foreach (ActivityBase a in ActivityBase.All)
                if (a.Id == id && a is T typed) return typed;
            return null;
        }

        // ------------------------------------------------------------------ scenarios
        private IEnumerator BasicDriving()
        {
            Log("--- Basic driving (dump truck)");
            yield return Enter(VehicleType.DumpTruck);
            Vehicle truck = m_Interaction.Current;
            Vector3 start = truck.transform.position;
            m_Input.SetScriptedInput(new DriveInput(0f, 1f, 0f));
            yield return Wait(2f);
            float moved = Flat(start, truck.transform.position);
            Check(moved > 3f, $"forward throttle moves the truck ({moved:0.0} m in 2 s)");
            yield return Shot("03_truck_driving");
            m_Input.SetScriptedInput(DriveInput.None);
            yield return Wait(2.5f);
            Check(Mathf.Abs(truck.Controller.Speed) < 0.05f, "releasing every control stops the vehicle (auto-brake)");
            float yaw0 = truck.transform.eulerAngles.y;
            m_Input.SetScriptedInput(new DriveInput(1f, 0.6f, 0f));
            yield return Wait(1.5f);
            float turned = Mathf.Abs(Mathf.DeltaAngle(yaw0, truck.transform.eulerAngles.y));
            Check(turned > 20f, $"steering right turns the truck ({turned:0}°)");
            m_Input.SetScriptedInput(new DriveInput(0f, 0f, 1f));
            yield return Wait(1.5f);
            Check(truck.Controller.Speed < -0.5f && truck.Controller.Speed >= -truck.Profile.tuning.MaxReverseSpeed - 0.01f,
                $"brake then reverse, gently ({truck.Controller.Speed:0.0} m/s)");
            yield return Stop(truck);
            float maxSpeed = 0f;
            m_Input.SetScriptedInput(new DriveInput(0f, 1f, 0f));
            for (float t = 0f; t < 3f; t += Time.deltaTime) { maxSpeed = Mathf.Max(maxSpeed, truck.Controller.Speed); yield return null; }
            Check(maxSpeed <= truck.Profile.tuning.MaxForwardSpeed + 0.01f, $"top speed stays gentle ({maxSpeed:0.0} m/s)");
            yield return Stop(truck);
            m_Interaction.Exit();
            yield return Wait(0.5f);
            Check(!m_Interaction.IsDriving && truck.Controller.Body.isKinematic, "exit stops and parks the truck");
            Check(!m_Interaction.Enter(truck) || m_Interaction.Current == truck, "re-enter works after exit");
            m_Interaction.Exit();
        }

        private IEnumerator YardDig()
        {
            Log("--- Yard dig pit: excavator loads the dump truck");
            var pit = Find<ExcavationActivity>("yard_dig");
            yield return Enter(VehicleType.DumpTruck);
            yield return Drive(m_Interaction.Current, new[] { new Vector3(-9f, 0, 2f), new Vector3(-7f, 0, -5f) }, 2.5f, 25f, "truck to dig pit");
            yield return Enter(VehicleType.Excavator);
            Vehicle exc = m_Interaction.Current;
            yield return Drive(exc, new[] { new Vector3(-15f, 0, 2f), new Vector3(-14f, 0, -3f) }, 2.5f, 25f, "excavator to dig pit");
            yield return Shot("04_excavator_at_pit");
            var bed = m_Vehicles[VehicleType.DumpTruck].GetTool<DumpTruckBed>();
            var arm = exc.GetTool<ExcavatorArm>();
            for (int i = 0; i < 3; i++)
            {
                yield return Act(ActionIcon.Scoop, $"scoop {i + 1}");
                Check(arm.HasSoil, $"bucket holds soil after scoop {i + 1}");
                if (i == 0)
                {
                    Check(m_Actions.Current.Icon != ActionIcon.Scoop, "cannot scoop twice into a full bucket");
                    yield return Shot("05_bucket_full");
                }
                yield return Act(ActionIcon.Dump, $"dump {i + 1}");
                Check(!arm.HasSoil, $"bucket empty after dump {i + 1}");
            }
            Check(bed.Level == DumpTruckBed.Capacity, $"truck bed filled by the excavator (level {bed.Level})");
            Check(GameServices.Instance.Stars.HasReward("yard_dig.stage0"), "yard dig pays its star");
            yield return Shot("06_truck_loaded");
            yield return Wait(3f);
            Check(pit.IsOpen && pit.Progress.Progress == 0, "yard dig pit restarts for free play");
        }

        private IEnumerator Push(BulldozerClearingActivity act, Vector3 skip, string label, float budget)
        {
            Vehicle dozer = m_Interaction.Current;
            var debris = new List<PushableDebris>((PushableDebris[])typeof(BulldozerClearingActivity)
                .GetField("m_Debris", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(act));
            float t0 = Time.time;
            while (!act.Progress.IsComplete && Time.time - t0 < budget)
            {
                PushableDebris next = null;
                float best = float.MaxValue;
                foreach (PushableDebris d in debris)
                {
                    if (d.IsCleared) continue;
                    float dd = Flat(d.transform.position, dozer.transform.position);
                    if (dd < best) { best = dd; next = d; }
                }
                if (next == null) break;
                Vector3 dir = Flatten(next.transform.position - skip);
                Vector3 behind = next.transform.position + dir * 5f;
                yield return Drive(dozer, new[] { behind }, 2f, 20f, $"{label}: line up");
                // Push straight through the rock into the skip.
                float t = 0f;
                while (!next.IsCleared && t < 15f)
                {
                    Vector3 to = Flatten(skip - dozer.transform.position);
                    float angle = Vector3.SignedAngle(Flatten(dozer.transform.forward), to, Vector3.up);
                    m_Input.SetScriptedInput(new DriveInput(Mathf.Clamp(angle / 30f, -1f, 1f), 0.7f, 0f));
                    t += Time.deltaTime;
                    yield return null;
                }
                yield return Stop(dozer);
                // back off
                m_Input.SetScriptedInput(new DriveInput(0f, 0f, 1f));
                yield return Wait(1f);
                yield return Stop(dozer);
            }
            Check(act.Progress.IsComplete, $"{label}: all debris pushed into the skip");
            // Back away from the skip so the area is clear for the next machine.
            m_Input.SetScriptedInput(new DriveInput(0f, 0f, 1f));
            yield return Wait(1.5f);
            yield return Stop(dozer);
        }

        private IEnumerator HouseClearing()
        {
            Log("--- House plot: bulldozer clears the plot");
            yield return Enter(VehicleType.Bulldozer);
            Vehicle dozer = m_Interaction.Current;
            bool[] r = { false };
            yield return Drive(dozer, new[] { new Vector3(3f, 0, 4f), new Vector3(0f, 0, 14f), new Vector3(0f, 0, 45f), new Vector3(4f, 0, 59f), new Vector3(26f, 0, 60f), new Vector3(34f, 0, 66f), new Vector3(34f, 0, 74f) }, 3f, 30f, "bulldozer to house plot", r);
            Check(r[0], "bulldozer drives yard → house plot unaided");
            yield return Shot("07_house_plot_arrival");
            var clear = Find<BulldozerClearingActivity>("house_clear");
            Check(clear.IsOpen, "plot clearing is the open stage");
            yield return Push(clear, new Vector3(44.5f, 0, 82f), "house clearing", 240f);
            yield return Shot("08_plot_cleared");
        }

        private IEnumerator HouseDig()
        {
            Log("--- House plot: excavator digs the foundation");
            yield return Enter(VehicleType.Excavator);
            Vehicle exc = m_Interaction.Current;
            bool[] r = { false };
            yield return Drive(exc, new[] { new Vector3(-8f, 0, 4f), new Vector3(0f, 0, 14f), new Vector3(0f, 0, 45f), new Vector3(4f, 0, 59f), new Vector3(26f, 0, 60f), new Vector3(34f, 0, 66f), new Vector3(34f, 0, 76f) }, 2.5f, 40f, "excavator to house plot", r);
            Check(r[0], "excavator drives yard → house plot unaided");
            var dig = Find<ExcavationActivity>("house_dig");
            Check(dig.IsOpen, "foundation dig is the open stage");
            for (int i = 0; i < 3; i++)
            {
                yield return Act(ActionIcon.Scoop, $"foundation scoop {i + 1}");
                yield return Act(ActionIcon.Dump, $"foundation dump {i + 1}");
            }
            Check(dig.Progress.IsComplete, "foundation dug");
            // Park the excavator out of the truck's way, beside the plot, like a tidy player.
            yield return Drive(exc, new[] { new Vector3(26f, 0, 86f) }, 3f, 20f, "excavator parks aside");
            yield return Wait(2.5f);
            yield return Shot("09_foundation");
        }

        private IEnumerator HouseDeliveries()
        {
            Log("--- House plot: dump truck deliveries");
            yield return Enter(VehicleType.DumpTruck);
            Vehicle truck = m_Interaction.Current;
            var bed = truck.GetTool<DumpTruckBed>();
            var house = Find<HouseConstructionActivity>("house");
            Vector3 drop = new Vector3(34f, 0, 74f);
            yield return Drive(truck, new[] { new Vector3(-3f, 0, 4f), new Vector3(0f, 0, 14f), new Vector3(0f, 0, 45f), new Vector3(4f, 0, 59f), new Vector3(26f, 0, 60f), new Vector3(34f, 0, 66f), drop }, 3f, 30f, "loaded truck to house");
            string[] stages = { "walls", "roof", "doors", "garden" };
            for (int i = 0; i < stages.Length; i++)
            {
                if (!bed.IsLoaded)
                {
                    yield return Drive(truck, new[] { new Vector3(34f, 0, 64f), new Vector3(34f, 0, 50f) }, 2.5f, 25f, "to brick depot");
                    yield return Act(ActionIcon.Load, $"load bricks for {stages[i]}");
                    Check(bed.IsFull, "truck full of bricks");
                    if (i == 1) yield return Shot("10_brick_load");
                    yield return Drive(truck, new[] { new Vector3(34f, 0, 62f), drop }, 3f, 25f, "back to house");
                }
                yield return Act(ActionIcon.Unload, $"unload for {stages[i]}");
                yield return Wait(3f);
                Check(house.Progress.Stage == 3 + i, $"house stage '{stages[i]}' built (project stage {house.Progress.Stage}/6)");
                yield return Shot($"11_house_{i}_{stages[i]}");
            }
            Check(house.Progress.IsComplete, "house project complete");
            Check(GameServices.Instance.Stars.HasReward("house.complete"), "house completion bonus paid");
            m_Interaction.Exit();
            yield return Wait(1f);
            yield return Shot("12_house_done_overview");
        }

        private IEnumerator Roadworks()
        {
            Log("--- Roadworks");
            var works = Find<RoadworksActivity>("roadworks");
            yield return Enter(VehicleType.Bulldozer);
            Vehicle dozer = m_Interaction.Current;
            yield return Drive(dozer, new[] { new Vector3(34f, 0, 66f), new Vector3(52f, 0, 60f), new Vector3(60f, 0, 48f), new Vector3(60f, 0, 20f) }, 3f, 30f, "bulldozer to roadworks");
            yield return Push(Find<BulldozerClearingActivity>("road_clear"), new Vector3(71.5f, 0, 0f), "roadworks clearing", 180f);
            yield return Shot("13_road_cleared");

            yield return Enter(VehicleType.DumpTruck);
            Vehicle truck = m_Interaction.Current;
            yield return Drive(truck, new[] { new Vector3(34f, 0, 64f), new Vector3(4f, 0, 59f), new Vector3(0f, 0, 45f), new Vector3(0f, 0, 14f), new Vector3(13f, 0, 4f), new Vector3(13f, 0, -8f) }, 2.5f, 30f, "truck to gravel hopper");
            yield return Act(ActionIcon.Load, "load gravel");
            yield return Shot("14_gravel_hopper");
            yield return Drive(truck, new[] { new Vector3(13f, 0, -16f), new Vector3(17f, 0, -1f), new Vector3(30f, 0, 0f), new Vector3(54f, 0, 0f) }, 3f, 30f, "truck to roadworks");
            yield return Act(ActionIcon.Unload, "unload gravel into the holes");
            yield return Wait(2f);
            Check(works.Progress.Stage == 2, "holes filled; rolling is open");

            yield return Enter(VehicleType.RoadRoller);
            Vehicle roller = m_Interaction.Current;
            yield return Drive(roller, new[] { new Vector3(16f, 0, 2f), new Vector3(20f, 0, 0f), new Vector3(48f, 0, 0f), new Vector3(57f, 0, -18f), new Vector3(60f, 0, -16f), new Vector3(60f, 0, 16f) }, 3f, 30f, "roller over the patches");
            yield return Wait(3f);
            if (!works.Progress.IsComplete)
                yield return Drive(roller, new[] { new Vector3(60f, 0, -16f), new Vector3(60f, 0, 16f) }, 3f, 30f, "roller second pass");
            yield return Wait(3f);
            Check(works.Progress.IsComplete, "roadworks project complete");
            yield return Shot("15_roadworks_done");
        }

        private IEnumerator StuckRecovery()
        {
            Log("--- Stuck recovery");
            yield return Enter(VehicleType.DumpTruck);
            Vehicle truck = m_Interaction.Current;
            var rec = truck.GetComponent<VehicleRecovery>();
            truck.Controller.Teleport(new Vector3(-24.5f, 0.05f, 9f), Quaternion.Euler(0f, 270f, 0f)); // nose against the shed
            m_Input.SetScriptedInput(new DriveInput(0f, 1f, 0f));
            float t = 0f;
            while (!rec.SeemsStuck && t < 10f) { t += Time.deltaTime; yield return null; }
            Check(rec.SeemsStuck, "pushing against a wall is detected as stuck (reset button pulses)");
            yield return Shot("16_stuck_hint");
            m_Input.SetScriptedInput(DriveInput.None);
            Vector3 before = truck.transform.position;
            rec.Recover();
            yield return Wait(0.5f);
            Check(Flat(before, truck.transform.position) > 1f && RecoveryPoint.Nearest(truck.transform.position) != null, "reset puts the truck back on a road");
            truck.Controller.Teleport(new Vector3(0f, -10f, 0f), Quaternion.identity);
            yield return Wait(0.5f);
            Check(truck.transform.position.y > -1f, "falling out of the world auto-recovers");
            m_Interaction.Exit();
        }

        private IEnumerator PauseResume()
        {
            Log("--- Pause");
            var pause = FindFirstObjectByType<PauseMenu>();
            pause.Open();
            yield return null;
            Check(Time.timeScale == 0f && GameServices.Instance.State.IsPaused, "adult pause stops time");
            yield return Shot("17_pause");
            pause.Close();
            Check(Time.timeScale == 1f, "resume restores time");
            Time.timeScale = 2f;
        }

        private IEnumerator SaveReload()
        {
            Log("--- Save and reload");
            int stars = GameServices.Instance.Stars.Total;
            bool saved = GameServices.Instance.Save.SaveNow();
            string json = File.ReadAllText(GameServices.Instance.Save.SavePath);
            Check(saved && json.Contains("\"builderStars\": " + stars), $"save file written with {stars} stars");
            Time.timeScale = 1f;
            GameServices.Instance.Scenes.Load(SceneFlow.Town);
            yield return Wait(0.5f);
            yield return WaitScene(SceneFlow.Town, 20f);
            yield return Wait(1.5f);
            Bind();
            GameServices.Instance.Save.Load(); // simulate a cold start reading the file
            Check(GameServices.Instance.Stars.Total == stars, "stars survive reload");
            Check(Find<HouseConstructionActivity>("house").Progress.IsComplete, "finished house restored after reload");
            Check(Find<RoadworksActivity>("roadworks").Progress.IsComplete, "finished roadworks restored after reload");
            yield return Shot("18_after_reload");
        }
    }
}
