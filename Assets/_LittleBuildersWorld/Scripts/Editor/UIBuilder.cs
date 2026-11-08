using System.Collections.Generic;
using LittleBuildersWorld.Input;
using LittleBuildersWorld.UI;
using LittleBuildersWorld.Vehicles;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using E = LittleBuildersWorld.EditorTools.EditorAssetUtil;

namespace LittleBuildersWorld.EditorTools
{
    /// <summary>
    /// Builds every canvas in code. Layout is authored at 1920×1080; the smallest child control is ~150 px
    /// (≈ 14 mm on a 6" phone), driving controls are 220–300 px, and controls sit inside the safe area.
    /// </summary>
    public static class UIBuilder
    {
        public const string FontPath = E.Root + "/Art/UI/Fonts/Fredoka-SemiBold.ttf";
        public const string FontAssetPath = E.Root + "/Art/UI/Fonts/Fredoka SDF.asset";

        private static TMP_FontAsset s_Font;

        public static readonly Color Ink = E.Hex("#3A2E2A");
        public static readonly Color Cream = E.Hex("#FFF7E8");

        // ------------------------------------------------------------------ font
        public static TMP_FontAsset Font()
        {
            if (s_Font != null) return s_Font;
            s_Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (s_Font != null) return s_Font;
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (font == null)
            {
                Debug.LogError("[LBW] Font missing at " + FontPath);
                return null;
            }
            s_Font = TMP_FontAsset.CreateFontAsset(font, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            s_Font.name = "Fredoka SDF";
            AssetDatabase.CreateAsset(s_Font, FontAssetPath);
            s_Font.material.name = "Fredoka SDF Material";
            AssetDatabase.AddObjectToAsset(s_Font.material, s_Font);
            foreach (Texture2D t in s_Font.atlasTextures)
            {
                t.name = "Fredoka SDF Atlas";
                AssetDatabase.AddObjectToAsset(t, s_Font);
            }
            s_Font.TryAddCharacters(" !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~");
            EditorUtility.SetDirty(s_Font);
            AssetDatabase.SaveAssets();
            return s_Font;
        }

        public const string OutlineMaterialPath = E.Root + "/Art/UI/Fonts/Fredoka SDF Outline.mat";

        /// <summary>Shared outlined variant of the font material (big titles and labels over the 3D scene).</summary>
        public static Material OutlineMaterial()
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            if (m != null) return m;
            m = new Material(Font().material) { name = "Fredoka SDF Outline" };
            m.EnableKeyword("OUTLINE_ON");
            m.SetFloat("_OutlineWidth", 0.22f);
            m.SetColor("_OutlineColor", Ink);
            m.SetFloat("_FaceDilate", 0.1f);
            AssetDatabase.CreateAsset(m, OutlineMaterialPath);
            return m;
        }

        // ------------------------------------------------------------------ primitives
        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            RectTransform rt = Rect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Img(string name, Transform parent, string sprite, Color colour, Vector2 anchor, Vector2 pos, Vector2 size, bool raycast = false)
        {
            RectTransform rt = Rect(name, parent, anchor, pos, size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite != null ? IconPainter.Sprite(sprite) : null;
            img.color = colour;
            img.raycastTarget = raycast;
            if (sprite == "ui_panel") img.type = Image.Type.Sliced;
            return img;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float size, Color colour, Vector2 anchor, Vector2 pos, Vector2 box,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool outline = false)
        {
            RectTransform rt = Rect(name, parent, anchor, pos, box);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.font = Font();
            t.text = text;
            t.fontSize = size;
            t.color = colour;
            t.alignment = align;
            t.raycastTarget = false;
            t.textWrappingMode = TextWrappingModes.Normal;
            if (outline) t.fontSharedMaterial = OutlineMaterial();
            return t;
        }

        /// <summary>Round button: soft shadow, coloured disc with dark rim, white icon.</summary>
        public static TapButton RoundButton(string name, Transform parent, Vector2 anchor, Vector2 pos, float size, Color colour, string icon, float iconScale = 0.62f)
        {
            RectTransform root = Rect(name, parent, anchor, pos, new Vector2(size, size));
            Img("Shadow", root, "ui_shadow", new Color(1, 1, 1, 0.6f), new Vector2(0.5f, 0.5f), new Vector2(0f, -size * 0.06f), new Vector2(size * 1.1f, size * 1.1f));
            Image bg = Img("Disc", root, "ui_circle_outlined", colour, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size), true);
            if (icon != null) Img("Icon", bg.transform, icon, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * iconScale, size * iconScale));
            var b = bg.gameObject.AddComponent<TapButton>();
            E.Set(b, "m_Background", bg);
            return b;
        }

        public static HoldButton HoldBtn(string name, Transform parent, Vector2 anchor, Vector2 pos, float size, Color colour, string icon)
        {
            RectTransform root = Rect(name, parent, anchor, pos, new Vector2(size, size));
            Img("Shadow", root, "ui_shadow", new Color(1, 1, 1, 0.6f), new Vector2(0.5f, 0.5f), new Vector2(0f, -size * 0.06f), new Vector2(size * 1.1f, size * 1.1f));
            // A generous invisible hit area around the visible disc: small fingers miss.
            Image hit = Img("HitArea", root, "ui_circle", new Color(1, 1, 1, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 1.2f, size * 1.2f), true);
            Image bg = Img("Disc", hit.transform, "ui_circle_outlined", colour, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            Img("Icon", bg.transform, icon, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.62f, size * 0.62f));
            var h = hit.gameObject.AddComponent<HoldButton>();
            E.Set(h, "m_Background", bg);
            E.Set(h, "m_Normal", colour);
            E.Set(h, "m_Pressed", colour * 0.78f + new Color(0, 0, 0, 0.22f));
            return h;
        }

        public static Canvas MakeCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.layer = 5;
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<AspectRatioScaler>();
            return canvas;
        }

        public static void EventSystem()
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.GetComponent<EventSystem>().sendNavigationEvents = false;
        }

        private static Slider MakeSlider(Transform parent, Vector2 pos, float min, float max)
        {
            RectTransform root = Rect("Slider", parent, new Vector2(0.5f, 0.5f), pos, new Vector2(560, 70));
            var slider = root.gameObject.AddComponent<Slider>();
            Image bg = Img("Background", root, "ui_panel", E.Hex("#E6DCCB"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 34));
            RectTransform fillArea = Stretch("FillArea", root);
            fillArea.offsetMin = new Vector2(10, 18);
            fillArea.offsetMax = new Vector2(-10, -18);
            Image fill = Img("Fill", fillArea, "ui_panel", E.Hex("#6BCB77"), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.sizeDelta = Vector2.zero;
            RectTransform handleArea = Stretch("HandleArea", root);
            handleArea.offsetMin = new Vector2(30, 0);
            handleArea.offsetMax = new Vector2(-30, 0);
            Image handle = Img("Handle", handleArea, "ui_circle_outlined", Color.white, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(70, 70), true);
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = min;
            slider.maxValue = max;
            bg.raycastTarget = true;
            return slider;
        }

        private static Toggle MakeToggle(Transform parent, Vector2 pos)
        {
            RectTransform root = Rect("Toggle", parent, new Vector2(0.5f, 0.5f), pos, new Vector2(90, 90));
            var toggle = root.gameObject.AddComponent<Toggle>();
            Image bg = Img("Box", root, "ui_circle_outlined", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90, 90), true);
            Image check = Img("Check", bg.transform, "ui_circle", E.Hex("#6BCB77"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56, 56));
            toggle.targetGraphic = bg;
            toggle.graphic = check;
            return toggle;
        }

        // ------------------------------------------------------------------ shared panels
        public static SettingsPanel BuildSettings(Transform canvas, TouchInputController input, out ParentGate gate)
        {
            RectTransform overlay = Stretch("Settings", canvas);
            Img("Dim", overlay, null, new Color(0.1f, 0.08f, 0.06f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 4000), true);
            Image panel = Img("Panel", overlay, "ui_panel", Cream, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 960), true);
            Transform p = panel.transform;
            Text("Title", p, "Grown-up Settings", 64, Ink, new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(1000, 90));
            string[] labels = { "Music", "Sounds", "Voice", "Voice prompts", "Joystick steering", "Easy Drive (one finger)", "Steering sensitivity" };
            float y = 270f;
            var sliders = new Slider[4];
            var toggles = new Toggle[3];
            int si = 0, ti = 0;
            for (int i = 0; i < labels.Length; i++)
            {
                Text("Label", p, labels[i], 40, Ink, new Vector2(0.5f, 0.5f), new Vector2(-330, y), new Vector2(560, 70), TextAlignmentOptions.Left);
                bool isToggle = i >= 3 && i <= 5;
                if (isToggle) toggles[ti++] = MakeToggle(p, new Vector2(140, y));
                else sliders[si++] = MakeSlider(p, new Vector2(330, y), i == 6 ? 0.5f : 0f, i == 6 ? 1.5f : 1f);
                y -= 88f;
            }
            TapButton reset = RoundButton("Reset", p, new Vector2(0.5f, 0f), new Vector2(-380, 90), 120, E.Hex("#FF595E"), "icon_reset", 0.6f);
            Text("ResetLabel", p, "Reset progress", 36, Ink, new Vector2(0.5f, 0f), new Vector2(-150, 90), new Vector2(320, 70), TextAlignmentOptions.Left);
            TapButton close = RoundButton("Close", p, new Vector2(1f, 1f), new Vector2(-30, -30), 130, E.Hex("#1982C4"), "icon_close", 0.5f);

            gate = BuildGate(overlay);
            var settings = overlay.gameObject.AddComponent<SettingsPanel>();
            E.Set(settings, "m_Music", sliders[0]);
            E.Set(settings, "m_Sfx", sliders[1]);
            E.Set(settings, "m_Voice", sliders[2]);
            E.Set(settings, "m_Sensitivity", sliders[3]);
            E.Set(settings, "m_VoicePrompts", toggles[0]);
            E.Set(settings, "m_Joystick", toggles[1]);
            E.Set(settings, "m_EasyDrive", toggles[2]);
            E.Set(settings, "m_Reset", reset);
            E.Set(settings, "m_Close", close);
            E.Set(settings, "m_Gate", gate);
            E.Set(settings, "m_Input", input);
            overlay.gameObject.SetActive(false);
            return settings;
        }

        private static ParentGate BuildGate(Transform parent)
        {
            RectTransform overlay = Stretch("ParentGate", parent);
            Img("Dim", overlay, null, new Color(0.1f, 0.08f, 0.06f, 0.6f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 4000), true);
            Image panel = Img("Panel", overlay, "ui_panel", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 620), true);
            TextMeshProUGUI q = Text("Question", panel.transform, "Grown-ups: what is 7 + 5?", 54, Ink, new Vector2(0.5f, 1f), new Vector2(0, -110), new Vector2(1000, 120));
            Text("Hint", panel.transform, "This will erase stars and building progress.", 32, E.Hex("#7A6A60"), new Vector2(0.5f, 1f), new Vector2(0, -200), new Vector2(1000, 60));
            var answers = new TapButton[3];
            var labels = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform r = Rect("Answer" + i, panel.transform, new Vector2(0.5f, 0f), new Vector2(-300 + i * 300, 170), new Vector2(240, 160));
                Image bg = Img("Bg", r, "ui_panel", E.Hex("#FFC93C"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(240, 160), true);
                labels[i] = Text("Value", bg.transform, "12", 72, Ink, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(220, 140));
                answers[i] = bg.gameObject.AddComponent<TapButton>();
                E.Set(answers[i], "m_Background", bg);
            }
            TapButton cancel = RoundButton("Cancel", panel.transform, new Vector2(1f, 1f), new Vector2(-20, -20), 110, E.Hex("#1982C4"), "icon_close", 0.5f);
            var gate = overlay.gameObject.AddComponent<ParentGate>();
            E.Set(gate, "m_Question", q);
            E.SetArray(gate, "m_Answers", answers);
            E.SetArray(gate, "m_AnswerLabels", labels);
            E.Set(gate, "m_Cancel", cancel);
            return gate;
        }

        // ------------------------------------------------------------------ town HUD
        public sealed class TownHud
        {
            public HUDController Hud;
            public StarCelebrationUI Stars;
            public DriveBubbles Bubbles;
            public PauseMenu Pause;
        }

        public static TownHud BuildTownHud(VehicleInteraction interaction, VehicleActionController actions, TouchInputController input, Camera cam, IList<Vehicle> vehicles)
        {
            Canvas canvas = MakeCanvas("HUD", 10);
            RectTransform safe = Stretch("SafeArea", canvas.transform);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // World tap layer and Drive bubbles (bottom of the stack so buttons win).
            Image tap = Img("WorldTap", safe, null, new Color(1, 1, 1, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, true);
            tap.rectTransform.anchorMin = new Vector2(-0.2f, -0.2f);
            tap.rectTransform.anchorMax = new Vector2(1.2f, 1.2f);
            RectTransform bubbleLayer = Stretch("Bubbles", safe);
            RectTransform bubbleRoot = Rect("BubbleTemplate", bubbleLayer, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190, 190));
            TapButton bubble = RoundButton("Bubble", bubbleRoot, new Vector2(0.5f, 0.5f), Vector2.zero, 190, Color.white, null);
            Img("Portrait", bubble.transform, null, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(168, 168));
            Image badge = Img("PlayBadge", bubble.transform, "ui_circle_outlined", E.Hex("#6BCB77"), new Vector2(0.85f, 0.15f), Vector2.zero, new Vector2(80, 80));
            Img("Play", badge.transform, "icon_play", Color.white, new Vector2(0.5f, 0.5f), new Vector2(3, 0), new Vector2(46, 46));
            bubbleRoot.gameObject.SetActive(false);
            var bubbles = canvas.gameObject.AddComponent<DriveBubbles>();
            E.Set(bubbles, "m_Interaction", interaction);
            E.Set(bubbles, "m_Camera", cam);
            E.Set(bubbles, "m_Container", bubbleLayer);
            E.Set(bubbles, "m_BubblePrefab", bubble);
            var catcher = tap.gameObject.AddComponent<WorldTapCatcher>();
            E.Set(catcher, "m_Bubbles", bubbles);

            // ---------------- On foot
            RectTransform foot = Stretch("FootPanel", safe);
            TapButton home = RoundButton("Home", foot, new Vector2(0f, 1f), new Vector2(120, -120), 170, E.Hex("#FF9F1C"), "icon_home");
            TapButton paint = RoundButton("Paint", foot, new Vector2(0f, 1f), new Vector2(320, -120), 150, E.Hex("#B388EB"), "icon_paint");
            RectTransform row = Rect("Selector", foot, new Vector2(0.5f, 0f), new Vector2(0, 150), new Vector2(1100, 240));
            var slots = new List<VehicleSelectorUI.Slot>();
            for (int i = 0; i < vehicles.Count; i++)
            {
                float x = (i - (vehicles.Count - 1) * 0.5f) * 260f;
                TapButton b = RoundButton("Vehicle" + i, row, new Vector2(0.5f, 0.5f), new Vector2(x, 0), 220, Color.white, null);
                Image portrait = Img("Portrait", b.transform, null, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(196, 196));
                Image ring = Img("Selected", b.transform, "ui_ring", E.Hex("#FFC93C"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(250, 250));
                Image lockBadge = Img("Lock", b.transform, "ui_circle_outlined", E.Hex("#FFFFFF"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 110));
                Img("LockIcon", lockBadge.transform, "icon_lock", Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(70, 70));
                TextMeshProUGUI need = Text("Stars", lockBadge.transform, "0", 34, Ink, new Vector2(0.5f, 0f), new Vector2(0, 14), new Vector2(100, 40));
                lockBadge.gameObject.SetActive(false);
                slots.Add(new VehicleSelectorUI.Slot { vehicle = vehicles[i], button = b, portrait = portrait, lockBadge = lockBadge.gameObject, starsNeeded = need, selectedRing = ring.gameObject });
            }
            var selector = row.gameObject.AddComponent<VehicleSelectorUI>();
            E.Set(selector, "m_Interaction", interaction);
            var so = new SerializedObject(selector);
            SerializedProperty arr = so.FindProperty("m_Slots");
            arr.arraySize = slots.Count;
            for (int i = 0; i < slots.Count; i++)
            {
                SerializedProperty e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("vehicle").objectReferenceValue = slots[i].vehicle;
                e.FindPropertyRelative("button").objectReferenceValue = slots[i].button;
                e.FindPropertyRelative("portrait").objectReferenceValue = slots[i].portrait;
                e.FindPropertyRelative("lockBadge").objectReferenceValue = slots[i].lockBadge;
                e.FindPropertyRelative("starsNeeded").objectReferenceValue = slots[i].starsNeeded;
                e.FindPropertyRelative("selectedRing").objectReferenceValue = slots[i].selectedRing;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            // ---------------- Driving
            RectTransform drive = Stretch("DrivePanel", safe);
            RectTransform buttonSteer = Stretch("ButtonSteering", drive);
            HoldButton left = HoldBtn("Left", buttonSteer, Vector2.zero, new Vector2(200, 200), 260, E.Hex("#1982C4"), "icon_left");
            HoldButton right = HoldBtn("Right", buttonSteer, Vector2.zero, new Vector2(530, 200), 260, E.Hex("#1982C4"), "icon_right");
            RectTransform joySteer = Rect("JoystickSteering", drive, Vector2.zero, new Vector2(340, 300), new Vector2(640, 560));
            Image joyArea = Img("Area", joySteer, null, new Color(1, 1, 1, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 560), true);
            Image joyBase = Img("Base", joySteer, "ui_circle_outlined", new Color(1f, 1f, 1f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 300));
            Image joyKnob = Img("Knob", joySteer, "ui_circle_outlined", E.Hex("#1982C4"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 150));
            var joystick = joyArea.gameObject.AddComponent<VirtualJoystick>();
            E.Set(joystick, "m_Base", joyBase.rectTransform);
            E.Set(joystick, "m_Knob", joyKnob.rectTransform);
            joySteer.gameObject.SetActive(false);

            HoldButton go = HoldBtn("Go", drive, new Vector2(1f, 0f), new Vector2(-210, 230), 300, E.Hex("#6BCB77"), "icon_go");
            HoldButton back = HoldBtn("Back", drive, new Vector2(1f, 0f), new Vector2(-530, 150), 220, E.Hex("#FF9F1C"), "icon_back");
            RectTransform actionRoot = Rect("ActionRoot", drive, new Vector2(1f, 0f), new Vector2(-250, 590), new Vector2(260, 260));
            actionRoot.gameObject.AddComponent<Pulse>();
            TapButton action = RoundButton("Action", actionRoot, new Vector2(0.5f, 0.5f), Vector2.zero, 260, E.Hex("#FFC93C"), null);
            Image actionIcon = Img("Icon", action.transform, "icon_scoop", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(190, 190));
            TapButton exit = RoundButton("Exit", drive, new Vector2(0f, 1f), new Vector2(120, -120), 170, E.Hex("#FF595E"), "icon_exit");
            RectTransform resetRoot = Rect("ResetRoot", drive, new Vector2(0f, 1f), new Vector2(310, -110), new Vector2(150, 150));
            Pulse resetPulse = resetRoot.gameObject.AddComponent<Pulse>();
            E.Set(resetPulse, "m_Amount", 0.15f);
            TapButton reset = RoundButton("Reset", resetRoot, new Vector2(0.5f, 0.5f), Vector2.zero, 150, E.Hex("#8E7DBE"), "icon_reset");
            TapButton horn = RoundButton("Horn", drive, new Vector2(0.5f, 0f), new Vector2(-60, 130), 170, E.Hex("#FFD447"), "icon_horn");

            // ---------------- Always: stars and the held pause gear
            RectTransform starBox = Rect("Stars", safe, new Vector2(1f, 1f), new Vector2(-380, -95), new Vector2(260, 130));
            Img("Back", starBox, "ui_panel", new Color(1f, 1f, 1f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(260, 120));
            Image starIcon = Img("StarIcon", starBox, "icon_star", Color.white, new Vector2(0f, 0.5f), new Vector2(70, 4), new Vector2(120, 120));
            TextMeshProUGUI count = Text("Count", starBox, "0", 76, Ink, new Vector2(0f, 0.5f), new Vector2(185, 0), new Vector2(140, 110));
            RectTransform gearRoot = Rect("Pause", safe, new Vector2(1f, 1f), new Vector2(-120, -105), new Vector2(150, 150));
            Image gearBg = Img("Disc", gearRoot, "ui_circle_outlined", E.Hex("#E6DCCB"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 140), true);
            Img("Icon", gearBg.transform, "icon_gear", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 96));
            Image fill = Img("HoldFill", gearRoot, "ui_ring", E.Hex("#1982C4"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160, 160));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillAmount = 0f;
            var gear = gearBg.gameObject.AddComponent<HoldToConfirmButton>();
            E.Set(gear, "m_Fill", fill);

            // ---------------- Celebration layer
            RectTransform fly = Stretch("FlyLayer", safe);
            Image flyStar = Img("FlyingStar", fly, "icon_star", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 110));
            flyStar.gameObject.SetActive(false);
            RectTransform card = Rect("UnlockCard", safe, new Vector2(0.5f, 1f), new Vector2(0, -230), new Vector2(460, 460));
            card.localScale = Vector3.one * 0.7f; // high and compact: never hides the vehicle
            Img("Burst", card, "fx_spark", E.Hex("#FFE066"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 700));
            Img("Bg", card, "ui_circle_outlined", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 420));
            Image swatch = Img("Swatch", card, "ui_circle", Color.white, new Vector2(0.5f, 0.5f), new Vector2(120, -120), new Vector2(130, 130));
            Image unlockIcon = Img("Icon", card, "icon_paint", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 280));
            var stars = canvas.gameObject.AddComponent<StarCelebrationUI>();
            E.Set(stars, "m_Count", count);
            E.Set(stars, "m_CounterIcon", starIcon.rectTransform);
            E.Set(stars, "m_FlyLayer", fly);
            E.Set(stars, "m_FlyingStarPrefab", flyStar);
            E.Set(stars, "m_WorldCamera", cam);
            E.Set(stars, "m_UnlockCard", card.gameObject);
            E.Set(stars, "m_UnlockIcon", unlockIcon);
            E.Set(stars, "m_UnlockSwatch", swatch);

            // ---------------- Pause + settings
            Canvas overlayCanvas = MakeCanvas("PauseCanvas", 20);
            RectTransform pauseRoot = Stretch("PauseMenu", overlayCanvas.transform);
            pauseRoot.gameObject.AddComponent<SafeAreaFitter>();
            RectTransform pausePanel = Stretch("Panel", pauseRoot);
            Img("Dim", pausePanel, null, new Color(0.1f, 0.08f, 0.06f, 0.55f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000, 4000), true);
            TapButton resume = RoundButton("Resume", pausePanel, new Vector2(0.5f, 0.5f), new Vector2(0, 40), 320, E.Hex("#6BCB77"), "icon_play");
            Text("ResumeLabel", pausePanel, "Keep playing", 44, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, -170), new Vector2(500, 70), TextAlignmentOptions.Center, true);
            TapButton settingsBtn = RoundButton("Settings", pausePanel, new Vector2(0.5f, 0.5f), new Vector2(-420, 40), 200, E.Hex("#E6DCCB"), "icon_gear");
            Text("SettingsLabel", pausePanel, "Settings", 40, Color.white, new Vector2(0.5f, 0.5f), new Vector2(-420, -110), new Vector2(400, 60), TextAlignmentOptions.Center, true);
            TapButton menuBtn = RoundButton("MainMenu", pausePanel, new Vector2(0.5f, 0.5f), new Vector2(420, 40), 200, E.Hex("#FF9F1C"), "icon_home");
            Text("MenuLabel", pausePanel, "Main menu", 40, Color.white, new Vector2(0.5f, 0.5f), new Vector2(420, -110), new Vector2(400, 60), TextAlignmentOptions.Center, true);
            SettingsPanel settings = BuildSettings(overlayCanvas.transform, input, out _);
            var pause = pauseRoot.gameObject.AddComponent<PauseMenu>();
            E.Set(pause, "m_Panel", pausePanel.gameObject);
            E.Set(pause, "m_Resume", resume);
            E.Set(pause, "m_Settings", settingsBtn);
            E.Set(pause, "m_MainMenu", menuBtn);
            E.Set(pause, "m_SettingsPanel", settings);

            // ---------------- HUD controller + input wiring
            var hud = canvas.gameObject.AddComponent<HUDController>();
            E.Set(hud, "m_Interaction", interaction);
            E.Set(hud, "m_Actions", actions);
            E.Set(hud, "m_Input", input);
            E.Set(hud, "m_Pause", pause);
            E.Set(hud, "m_DrivePanel", drive.gameObject);
            E.Set(hud, "m_FootPanel", foot.gameObject);
            E.Set(hud, "m_ActionRoot", actionRoot.gameObject);
            E.Set(hud, "m_Action", action);
            E.Set(hud, "m_ActionIcon", actionIcon);
            E.Set(hud, "m_Exit", exit);
            E.Set(hud, "m_Horn", horn);
            E.Set(hud, "m_Reset", reset);
            E.Set(hud, "m_ResetPulse", resetPulse);
            E.Set(hud, "m_Home", home);
            E.Set(hud, "m_Paint", paint);
            E.Set(hud, "m_PauseButton", gear);

            E.Set(input, "m_Left", left);
            E.Set(input, "m_Right", right);
            E.Set(input, "m_Go", go);
            E.Set(input, "m_Back", back);
            E.Set(input, "m_Joystick", joystick);
            E.Set(input, "m_ButtonSteering", buttonSteer.gameObject);
            E.Set(input, "m_JoystickSteering", joySteer.gameObject);

            return new TownHud { Hud = hud, Stars = stars, Bubbles = bubbles, Pause = pause };
        }

        // ------------------------------------------------------------------ main menu
        public static void BuildMainMenu()
        {
            Canvas canvas = MakeCanvas("MenuCanvas", 10);
            RectTransform safe = Stretch("SafeArea", canvas.transform);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            RectTransform titleRoot = Rect("Title", safe, new Vector2(0.5f, 1f), new Vector2(0, -190), new Vector2(1500, 260));
            Pulse bob = titleRoot.gameObject.AddComponent<Pulse>();
            E.Set(bob, "m_Amount", 0.025f);
            E.Set(bob, "m_Speed", 1.6f);
            Text("Line1", titleRoot, "Little Builders", 150, E.Hex("#FFC93C"), new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(1500, 170), TextAlignmentOptions.Center, true);
            Text("Line2", titleRoot, "World", 110, E.Hex("#FF595E"), new Vector2(0.5f, 0.5f), new Vector2(0, -85), new Vector2(1500, 130), TextAlignmentOptions.Center, true);
            RectTransform playRoot = Rect("PlayRoot", safe, new Vector2(0.5f, 0f), new Vector2(0, 250), new Vector2(380, 380));
            playRoot.gameObject.AddComponent<Pulse>();
            TapButton play = RoundButton("Play", playRoot, new Vector2(0.5f, 0.5f), Vector2.zero, 380, E.Hex("#6BCB77"), "icon_play", 0.55f);

            RectTransform gearRoot = Rect("Settings", safe, new Vector2(1f, 1f), new Vector2(-120, -110), new Vector2(150, 150));
            Image gearBg = Img("Disc", gearRoot, "ui_circle_outlined", E.Hex("#E6DCCB"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 140), true);
            Img("Icon", gearBg.transform, "icon_gear", Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 96));
            Image fill = Img("HoldFill", gearRoot, "ui_ring", E.Hex("#1982C4"), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160, 160));
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Radial360;
            fill.fillOrigin = (int)Image.Origin360.Top;
            fill.fillAmount = 0f;
            var gear = gearBg.gameObject.AddComponent<HoldToConfirmButton>();
            E.Set(gear, "m_Fill", fill);
            Text("Hold", gearRoot, "hold", 30, Ink, new Vector2(0.5f, 0f), new Vector2(0, -20), new Vector2(150, 40));

            Canvas overlay = MakeCanvas("SettingsCanvas", 20);
            SettingsPanel settings = BuildSettings(overlay.transform, null, out _);
            var menu = canvas.gameObject.AddComponent<MainMenuUI>();
            E.Set(menu, "m_Play", play);
            E.Set(menu, "m_SettingsGear", gear);
            E.Set(menu, "m_Settings", settings);
        }
    }
}
