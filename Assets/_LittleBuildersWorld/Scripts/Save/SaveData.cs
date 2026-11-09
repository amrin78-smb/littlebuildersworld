using System;
using System.Collections.Generic;
using UnityEngine;

namespace LittleBuildersWorld.Save
{
    /// <summary>
    /// Everything the game persists. Only progression and settings: no names, no device IDs, no personal data.
    /// Bump <see cref="CurrentVersion"/> and add a step in <see cref="SaveSerializer.Migrate"/> when the shape changes.
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public int builderStars;
        /// <summary>One entry per reward ever granted, so a stage can never pay out twice.</summary>
        public List<string> grantedRewards = new List<string>();
        public List<ActivityRecord> activities = new List<ActivityRecord>();
        public List<string> unlocked = new List<string>();
        public List<PaintRecord> paints = new List<PaintRecord>();
        public string selectedVehicle = "";
        public AudioSettingsData audio = new AudioSettingsData();
        public ControlSettingsData controls = new ControlSettingsData();
        /// <summary>Helper language: "en", "th", or "" = follow the device language.</summary>
        public string language = "";

        public ActivityRecord GetActivity(string id, bool create)
        {
            for (int i = 0; i < activities.Count; i++)
            {
                if (activities[i].id == id) return activities[i];
            }
            if (!create) return null;
            var record = new ActivityRecord { id = id };
            activities.Add(record);
            return record;
        }

        public int GetPaint(string vehicleId)
        {
            foreach (PaintRecord p in paints)
            {
                if (p.vehicleId == vehicleId) return p.paintIndex;
            }
            return 0;
        }

        public void SetPaint(string vehicleId, int index)
        {
            foreach (PaintRecord p in paints)
            {
                if (p.vehicleId == vehicleId) { p.paintIndex = index; return; }
            }
            paints.Add(new PaintRecord { vehicleId = vehicleId, paintIndex = index });
        }

        /// <summary>Repairs values a hand-edited or partially written file could contain.</summary>
        public void Sanitise()
        {
            grantedRewards ??= new List<string>();
            activities ??= new List<ActivityRecord>();
            unlocked ??= new List<string>();
            paints ??= new List<PaintRecord>();
            selectedVehicle ??= "";
            audio ??= new AudioSettingsData();
            controls ??= new ControlSettingsData();
            language ??= "";
            if (language != "" && language != "en" && language != "th") language = "";
            builderStars = Mathf.Max(0, builderStars);
            activities.RemoveAll(a => a == null || string.IsNullOrEmpty(a.id));
            foreach (ActivityRecord a in activities)
            {
                a.stage = Mathf.Max(0, a.stage);
                a.progress = Mathf.Max(0, a.progress);
            }
            paints.RemoveAll(p => p == null || string.IsNullOrEmpty(p.vehicleId));
            audio.Sanitise();
            controls.Sanitise();
        }
    }

    [Serializable]
    public sealed class ActivityRecord
    {
        public string id;
        /// <summary>Index of the stage currently being worked on (== stage count when finished).</summary>
        public int stage;
        /// <summary>Units of work done inside the current stage (scoops, deliveries, cleared rocks...).</summary>
        public int progress;
        public int timesCompleted;
    }

    [Serializable]
    public sealed class PaintRecord
    {
        public string vehicleId;
        public int paintIndex;
    }

    [Serializable]
    public sealed class AudioSettingsData
    {
        public float music = 0.6f;
        public float sfx = 0.9f;
        public float voice = 1f;
        public bool voicePrompts = true;

        public void Sanitise()
        {
            music = Mathf.Clamp01(music);
            sfx = Mathf.Clamp01(sfx);
            voice = Mathf.Clamp01(voice);
        }
    }

    public enum SteeringMode
    {
        /// <summary>Two large left/right buttons (default: easiest for small hands).</summary>
        Buttons = 0,
        /// <summary>Forgiving virtual joystick.</summary>
        Joystick = 1
    }

    [Serializable]
    public sealed class ControlSettingsData
    {
        public SteeringMode steering = SteeringMode.Buttons;
        /// <summary>Holding a steering button alone also drives slowly forward, so one finger is enough.</summary>
        public bool easyDrive = true;
        /// <summary>Multiplier on steering response, 0.5..1.5.</summary>
        public float steerSensitivity = 1f;

        public void Sanitise()
        {
            if (!Enum.IsDefined(typeof(SteeringMode), steering)) steering = SteeringMode.Buttons;
            steerSensitivity = Mathf.Clamp(steerSensitivity <= 0f ? 1f : steerSensitivity, 0.5f, 1.5f);
        }
    }
}
