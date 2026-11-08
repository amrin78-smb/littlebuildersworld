using LittleBuildersWorld.Audio;
using LittleBuildersWorld.Progression;
using LittleBuildersWorld.Save;
using UnityEngine;

namespace LittleBuildersWorld.Core
{
    /// <summary>
    /// The one persistent object. Owns the long-lived services (save, audio, stars, progression, scene flow) and is
    /// created before the first scene loads, so every scene — including one opened directly in the Editor — has them.
    /// Scene objects get services from here instead of searching the hierarchy.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameServices : MonoBehaviour
    {
        public static GameServices Instance { get; private set; }

        public GameConfig Config { get; private set; }
        public SaveSystem Save { get; private set; }
        public AudioManager Audio { get; private set; }
        public BuilderStarsManager Stars { get; private set; }
        public WorldProgressionManager Progression { get; private set; }
        public GameStateManager State { get; private set; }
        public SceneFlow Scenes { get; private set; }
        public PerformanceGovernor Performance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance == null) Create(null);
        }

        /// <summary>Creates the services. <paramref name="saveDirectory"/> null = persistentDataPath.</summary>
        public static GameServices Create(string saveDirectory)
        {
            var go = new GameObject("[GameServices]");
            DontDestroyOnLoad(go);
            var services = go.AddComponent<GameServices>();
            services.Initialise(saveDirectory);
            return services;
        }

        /// <summary>Tests: point the save at a throwaway folder and reload everything from it.</summary>
        public void ReinitialiseForTests(string saveDirectory)
        {
            Save.Initialise(saveDirectory);
            Save.Load();
            Stars.Refresh();
            Audio.ApplySettings();
        }

        private void Initialise(string saveDirectory)
        {
            Instance = this;
            Config = Resources.Load<GameConfig>(GameConfig.ResourcePath);
            if (Config == null)
            {
                Debug.LogError("[LBW] Resources/LBW_GameConfig is missing. Run 'Little Builders World ▸ Setup Project'.");
                Config = ScriptableObject.CreateInstance<GameConfig>();
            }

            Application.targetFrameRate = Mathf.Max(30, Config.targetFrameRate);
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            Save = gameObject.AddComponent<SaveSystem>();
            Save.Initialise(saveDirectory);
            Save.Load();

            State = gameObject.AddComponent<GameStateManager>();
            Audio = gameObject.AddComponent<AudioManager>();
            Audio.Initialise(Config.audio, Save);
            Stars = gameObject.AddComponent<BuilderStarsManager>();
            Stars.Initialise(Save, Config);
            Progression = gameObject.AddComponent<WorldProgressionManager>();
            Progression.Initialise(Save, Config);
            Scenes = gameObject.AddComponent<SceneFlow>();
            Performance = gameObject.AddComponent<PerformanceGovernor>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
