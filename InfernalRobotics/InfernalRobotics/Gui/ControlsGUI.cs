using InfernalRobotics.Command;
using InfernalRobotics.Control;
using InfernalRobotics.Extension;
using InfernalRobotics.Module;
using KSP.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using File = System.IO.File;
using InfernalRobotics.Command;
using InfernalRobotics.Utility;

namespace InfernalRobotics.Gui
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class ControlsGUI : MonoBehaviour
    {
        protected static Rect ControlWindowPos;
        protected static Rect EditorWindowPos;
        protected static Rect PresetWindowPos;
        protected static int ControlWindowID;
        protected static int EditorWindowID;
        protected static int PresetWindowID;

        protected static bool ResetWindow = false;
        protected static Vector2 EditorScroll;
        protected static bool UseElectricCharge = true;
        protected static ControlsGUI GUIController;
        private IButton irMinimizeButton;

        private ApplicationLauncherButton button;
        private bool guiGroupEditorEnabled;
        private bool guiPresetsEnabled;

        private IServo associatedServo;
        private string tmpMax = "";
        private string tmpMin = "";

        internal static bool GUISetupDone = false;

        public bool guiPresetMode = false;
        public bool guiHidden = false;

        private string tooltipText = "";
        private string lastTooltipText = "";
        private float tooltipTime;
        private const float TOOLTIP_MAX_TIME = 8f;
        private const float TOOLTIP_DELAY = 1.5f;

        private string lastFocusedControlName = "";

        //New sizes for a couple of things
        internal static Int32 EditorWindowWidth = 400;

        internal static Int32 ControlWindowWidth = 360;
        internal static Int32 EditorButtonHeights = 25;

        public bool GUIEnabled { get; set; }

        public static ControlsGUI IRGUI
        {
            get { return GUIController; }
        }

        static ControlsGUI()
        {
            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            //basic constructor to initilize windowIDs
            ControlWindowID = UnityEngine.Random.Range(1000, 2000000) + assemblyName.GetHashCode();
            EditorWindowID = ControlWindowID + 1;
            PresetWindowID = ControlWindowID + 2;
        }
        
        /// <summary>
        ///     Load the textures from files to memory
        /// </summary>
        private static void InitTextures()
        {
            if (!GUISetupDone)
            {
                TextureLoader.InitTextures();

                GUISetupDone = true;
            }
        }

        private void Awake()
        {
            LoadConfigXml();

            Logger.Log("[GUI] awake");

            GUIEnabled = false;

            guiGroupEditorEnabled = false;

            InitTextures();

            GameScenes scene = HighLogic.LoadedScene;

            if (scene == GameScenes.FLIGHT || scene == GameScenes.EDITOR)
            {
                GUIController = this;
            }
            else
            {
                GUIController = null;
            }

            if (ToolbarManager.ToolbarAvailable)
            {
                irMinimizeButton = ToolbarManager.Instance.add("sirkut", "IREditorButton");
                irMinimizeButton.TexturePath = "MagicSmokeIndustries/Textures/icon_button_small";
                irMinimizeButton.ToolTip = "Infernal Robotics";
                irMinimizeButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.FLIGHT);
                irMinimizeButton.OnClick += e => GUIEnabled = !GUIEnabled;
            }
            else
            {
                GameEvents.onGUIApplicationLauncherReady.Add(OnAppReady);
            }

            GameEvents.onShowUI.Add(OnShowUI);
            GameEvents.onHideUI.Add(OnHideUI);

            Logger.Log("[GUI] awake finished successfully", Logger.Level.Debug);
        }

        private void OnShowUI()
        {
            guiHidden = false;
        }

        private void OnHideUI()
        {
            guiHidden = true;
        }

        private void OnAppReady()
        {
            if (button == null)
            {
                try
                {
                    var texture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
                    texture.LoadImage(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../Textures/icon_button.png")));

                    button = ApplicationLauncher.Instance.AddModApplication(delegate { GUIEnabled = true; },
                        delegate { GUIEnabled = false; }, null, null, null, null,
                        ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB |
                        ApplicationLauncher.AppScenes.SPH, texture);

                    ApplicationLauncher.Instance.AddOnHideCallback(OnHideCallback);
                }
                catch (Exception ex)
                {
                    Logger.Log(string.Format("[GUI OnnAppReady Exception, {0}", ex.Message), Logger.Level.Fatal);
                }
            }
        }

        private void OnHideCallback()
        {
            GUIEnabled = false;
        }
        private void OnDestroy()
        {
            Logger.Log("[GUI] destroy");

            if (ToolbarManager.ToolbarAvailable)
            {
                irMinimizeButton.Destroy();
            }
            else
            {
                try
                {
                    GameEvents.onGUIApplicationLauncherReady.Remove(OnAppReady);

                    if (button != null && ApplicationLauncher.Instance != null)
                    {
                        ApplicationLauncher.Instance.RemoveModApplication(button);
                        button = null;
                    }

                    if (ApplicationLauncher.Instance != null)
                        ApplicationLauncher.Instance.RemoveOnHideCallback(OnHideCallback);
                }
                catch (Exception e)
                {
                    Logger.Log("[GUI] Failed unregistering AppLauncher handlers," + e.Message);
                }
            }

            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onHideUI.Remove(OnHideUI);

            EditorLock(false);
            SaveConfigXml();
            Logger.Log("[GUI] OnDestroy finished sucessfully", Logger.Level.Debug);
        }

        //servo control window used in flight
        private void ControlWindow(int windowID)
        {
            GUILayoutOption width20 = GUILayout.Width(20);

            GUILayout.BeginVertical();

            const int BUTTON_HEIGHT = 22;

            var buttonStyle = new GUIStyle(GUI.skin.button);

            var padding2px = new RectOffset(2, 2, 2, 2);

            buttonStyle.padding = padding2px;
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            //use of for instead of foreach in intentional
            for (int i = 0; i < ServoController.Instance.ServoGroups.Count; i++)
            {
                ServoController.ControlGroup g = ServoController.Instance.ServoGroups[i];

                if (g.Servos.Any())
                {
                    GUILayout.BeginHorizontal();

                    if (g.Expanded)
                    {
                        g.Expanded = !GUILayout.Button(TextureLoader.collapseIcon, buttonStyle, width20, GUILayout.Height(BUTTON_HEIGHT));
                    }
                    else
                    {
                        g.Expanded = GUILayout.Button(TextureLoader.expandIcon, buttonStyle, width20, GUILayout.Height(BUTTON_HEIGHT));
                    }

                    //overload default GUIStyle with bold font
                    var t = new GUIStyle(GUI.skin.label.name)
                    {
                        fontStyle = FontStyle.Bold
                    };

                    GUILayout.Label(g.Name, t, GUILayout.ExpandWidth(true), GUILayout.Height(BUTTON_HEIGHT));

                    g.Speed = GUILayout.TextField(g.Speed, GUILayout.Width(30), GUILayout.Height(BUTTON_HEIGHT));

                    Rect last = GUILayoutUtility.GetLastRect();
                    Vector2 pos = Event.current.mousePosition;
                    if (last.Contains(pos) && Event.current.type == EventType.Repaint)
                        tooltipText = "Speed Multiplier";

                    bool toggleVal = GUILayout.Toggle(g.MovingNegative, new GUIContent(TextureLoader.leftToggleIcon, "Toggle Move -"), buttonStyle,
                        GUILayout.Width(28), GUILayout.Height(BUTTON_HEIGHT));

                    SetTooltipText();

                    if (g.MovingNegative != toggleVal)
                    {
                        if (!toggleVal) g.Stop();
                        g.MovingNegative = toggleVal;
                    }

                    if (g.MovingNegative)
                    {
                        g.MovingPositive = false;
                        g.MoveNegative();
                    }

                    if (guiPresetMode)
                    {
                        if (GUILayout.Button(new GUIContent(TextureLoader.prevIcon, "Previous Preset"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            //reset any group toggles
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MovePrevPreset();
                        }
                        SetTooltipText();

                        if (GUILayout.Button(new GUIContent(TextureLoader.autoRevertIcon, "Reset"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            //reset any group toggles
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MoveCenter();
                        }
                        SetTooltipText();

                        if (GUILayout.Button(new GUIContent(TextureLoader.nextIcon, "Next Preset"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            //reset any group toggles
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MoveNextPreset();
                        }
                        SetTooltipText();
                    }
                    else
                    {
                        if (GUILayout.RepeatButton(new GUIContent(TextureLoader.leftIcon, "Hold to Move -"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MoveNegative();

                            g.ButtonDown = true;
                        }

                        SetTooltipText();

                        if (GUILayout.RepeatButton(new GUIContent(TextureLoader.revertIcon, "Hold to Center"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MoveCenter();

                            g.ButtonDown = true;
                        }
                        SetTooltipText();

                        if (GUILayout.RepeatButton(new GUIContent(TextureLoader.rightIcon, "Hold to Move +"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MovePositive();

                            g.ButtonDown = true;
                        }
                        SetTooltipText();
                    }

                    toggleVal = GUILayout.Toggle(g.MovingPositive, new GUIContent(TextureLoader.rightToggleIcon, "Toggle Move +"), buttonStyle,
                                                            GUILayout.Width(28), GUILayout.Height(BUTTON_HEIGHT));
                    SetTooltipText();

                    if (g.MovingPositive != toggleVal)
                    {
                        if (!toggleVal) g.Stop();
                        g.MovingPositive = toggleVal;
                    }

                    if (g.MovingPositive)
                    {
                        g.MovingNegative = false;
                        g.MovePositive();
                    }

                    GUILayout.EndHorizontal();

                    if (g.Expanded)
                    {
                        GUILayout.BeginHorizontal(GUILayout.Height(5));
                        GUILayout.EndHorizontal();

                        foreach (var servo in g.Servos)
                        {
                            GUILayout.BeginHorizontal();

                            var dotStyle = new GUIStyle(GUI.skin.label)
                            {
                                richText = true,
                                alignment = TextAnchor.MiddleCenter
                            };

                            string servoStatus = servo.Mechanism.IsFreeMoving ? "<color=lime>■</color>" : "<color=yellow>■</color>";

                            if (servo.Mechanism.IsLocked)
                                servoStatus = "<color=red>■</color>";

                            GUILayout.Label(servoStatus, dotStyle, GUILayout.Width(20), GUILayout.Height(BUTTON_HEIGHT));

                            var nameStyle = new GUIStyle(GUI.skin.label)
                            {
                                alignment = TextAnchor.MiddleLeft,
                                clipping = TextClipping.Clip
                            };

                            GUILayout.Label(servo.Name, nameStyle, GUILayout.ExpandWidth(true), GUILayout.Height(BUTTON_HEIGHT));

                            nameStyle.fontStyle = FontStyle.Italic;
                            nameStyle.alignment = TextAnchor.MiddleCenter;

                            var posStyle = new GUIStyle (nameStyle);
                            if(servo.RawServo.Translator.IsAxisInverted)
                            {
                                posStyle.fontStyle = FontStyle.Italic;
                                posStyle.normal.textColor = new Color (1, 1, 0);
                            }
                            GUILayout.Label(string.Format("{0:#0.##}", servo.RawServo.Translator.ToExternalPos(servo.Mechanism.Position)), posStyle, GUILayout.Width(45), GUILayout.Height(BUTTON_HEIGHT));

                            bool servoLocked = servo.Mechanism.IsLocked;
                            servoLocked = GUILayout.Toggle(servoLocked,
                                            servoLocked ? new GUIContent(TextureLoader.lockedIcon, "Unlock Servo") : new GUIContent(TextureLoader.unlockedIcon, "Lock Servo"),
                                            buttonStyle, GUILayout.Width(28), GUILayout.Height(BUTTON_HEIGHT));
                            servo.Mechanism.IsLocked = servoLocked;

                            SetTooltipText();

                            if (guiPresetMode)
                            {
                                if (GUILayout.Button(new GUIContent(TextureLoader.prevIcon, "Previous Preset"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;

                                    servo.Preset.MovePrev();
                                }
                                SetTooltipText();

                                bool servoPresetsOpen = guiPresetsEnabled && (servo == associatedServo);
                                toggleVal = GUILayout.Toggle(servoPresetsOpen, new GUIContent(TextureLoader.presetsIcon, "Edit Presets"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT));
                                if (servoPresetsOpen != toggleVal)
                                {
                                    if (guiPresetsEnabled && associatedServo == servo)
                                        guiPresetsEnabled = !guiPresetsEnabled;
                                    else
                                    {
                                        associatedServo = servo;
                                        if (!guiPresetsEnabled)
                                            guiPresetsEnabled = true;
                                    }
                                }
                                SetTooltipText();

                                if (GUILayout.Button(new GUIContent(TextureLoader.nextIcon, "Next Preset"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;

                                    servo.Preset.MovePrev();
                                }
                                SetTooltipText();
                            }
                            else
                            {
                                if (GUILayout.RepeatButton(new GUIContent(TextureLoader.leftIcon, "Hold to Move -"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;
                                    g.ButtonDown = true;

                                    //servo.RawServo.Translator.Move(float.NegativeInfinity, servo.RawServo.customSpeed * servo.RawServo.speedTweak);
                                    servo.Mechanism.MoveLeft();
                                }
                                SetTooltipText();

                                if (GUILayout.RepeatButton(new GUIContent(TextureLoader.revertIcon, "Hold to Center"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;
                                    g.ButtonDown = true;

                                    //servo.RawServo.Translator.Move(servo.RawServo.Translator.ToExternalPos(0f), servo.RawServo.customSpeed * servo.RawServo.speedTweak);
                                    servo.Mechanism.MoveCenter();
                                }
                                SetTooltipText();

                                if (GUILayout.RepeatButton(new GUIContent(TextureLoader.rightIcon, "Hold to Move +"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;
                                    g.ButtonDown = true;

                                    //servo.RawServo.Translator.Move(float.PositiveInfinity, servo.RawServo.customSpeed * servo.RawServo.speedTweak);
                                    servo.Mechanism.MoveRight();
                                }
                                SetTooltipText();
                            }
                            bool servoInverted = servo.RawServo.invertAxis;

                            servoInverted = GUILayout.Toggle(servoInverted,
                                servoInverted ? new GUIContent(TextureLoader.invertedIcon, "Un-invert Axis") : new GUIContent(TextureLoader.noninvertedIcon, "Invert Axis"),
                                buttonStyle, GUILayout.Width(28), GUILayout.Height(BUTTON_HEIGHT));

                            SetTooltipText();

                            if (servo.RawServo.invertAxis != servoInverted)
                                servo.RawServo.InvertAxisToggle();

                            GUILayout.EndHorizontal();
                        }

                        GUILayout.BeginHorizontal(GUILayout.Height(5));
                        GUILayout.EndHorizontal();
                    }

                    if (g.ButtonDown && Input.GetMouseButtonUp(0))
                    {
                        //one of the repeat buttons in the group was pressed, but now mouse button is up
                        g.ButtonDown = false;
                        g.Stop();
                    }
                }
            }

            GUILayout.BeginHorizontal(GUILayout.Height(32));

            if (GUILayout.Button(guiGroupEditorEnabled ? "Close Edit" : "Edit Groups", GUILayout.Height(32)))
            {
                guiGroupEditorEnabled = !guiGroupEditorEnabled;
            }

            guiPresetMode = GUILayout.Toggle(guiPresetMode, new GUIContent(TextureLoader.presetModeIcon, "Preset Mode"), buttonStyle,
                GUILayout.Width(32), GUILayout.Height(32));
            SetTooltipText();

            buttonStyle.padding = new RectOffset(3, 3, 3, 3);

            if (GUILayout.Button(new GUIContent(TextureLoader.stopButtonIcon, "Emergency Stop"), buttonStyle, GUILayout.Width(32), GUILayout.Height(32)))
            {
                foreach (ServoController.ControlGroup g in ServoController.Instance.ServoGroups)
                {
                    g.Stop();
                }
            }
            SetTooltipText();

            buttonStyle.padding = padding2px;

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        /// <summary>
        /// Has to be called after any GUI element with tooltips.
        /// </summary>
        private void SetTooltipText()
        {
            if (Event.current.type == EventType.Repaint)
            {
                tooltipText = GUI.tooltip;
            }
        }

        /// <summary>
        /// Called in the end of OnGUI(), draws tooltip saved in tooltipText
        /// </summary>
        private void DrawTooltip()
        {
            Vector2 pos = Event.current.mousePosition;
            if (tooltipText != "" && tooltipTime < TOOLTIP_MAX_TIME)
            {
                var tooltipStyle = new GUIStyle
                {
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal =
                    {
                        textColor = new Color32(207, 207, 207, 255),
                        background = GUIDragAndDrop.ImgBackground
                    },
                    stretchHeight = true,
                    border = new RectOffset(3, 3, 3, 3),
                    padding = new RectOffset(4, 4, 6, 4),
                    alignment = TextAnchor.MiddleLeft
                };

                var tooltip = new GUIContent(tooltipText);
                Vector2 size = tooltipStyle.CalcSize(tooltip);

                var tooltipPos = new Rect(pos.x - (size.x / 4), pos.y + 17, size.x, size.y);

                if (tooltipText != lastTooltipText)
                {
                    //reset timer
                    tooltipTime = 0f;
                }

                if (tooltipTime > TOOLTIP_DELAY)
                {
                    GUI.Label(tooltipPos, tooltip, tooltipStyle);
                    GUI.depth = 0;
                }

                tooltipTime += Time.deltaTime;
            }

            if (tooltipText != lastTooltipText) tooltipTime = 0f;
            lastTooltipText = tooltipText;
        }

        /// <summary>
        /// Implements Group Editor window. Used both in VAB/SPH and in Flight,
        /// uses HighLogic.LoadedScene to check whether to display certain fields.
        /// </summary>
        /// <param name="windowID">Window ID</param>
        private void EditorWindow(int windowID)
        {
            GUILayoutOption expand = GUILayout.ExpandWidth(true);
            GUILayoutOption rowHeight = GUILayout.Height(22);

            GUILayoutOption maxHeight = GUILayout.MaxHeight(Screen.height * 0.67f);

            var buttonStyle = new GUIStyle(GUI.skin.button);
            var padding1px = new RectOffset(1, 1, 1, 1);
            var padding2px = new RectOffset(2, 2, 2, 2);

            buttonStyle.padding = padding2px;
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            var cogButtonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = new RectOffset(3, 3, 3, 3)
            };

            bool isEditor = (HighLogic.LoadedScene == GameScenes.EDITOR);

            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;

            EditorScroll = GUILayout.BeginScrollView(EditorScroll, false, false, maxHeight);

            //Kick off the window code
            GUIDragAndDrop.WindowBegin(EditorWindowPos, EditorScroll);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            // pad   name     cog  keys   move  ec/s  remove
            // <-20-><-flex-><-20-><-45-><-45-><-33-><-45->

            //if we are showing the group handles then Pad the text so it still aligns with the text box
            if (GUIDragAndDrop.ShowGroupHandles)
                GUIDragAndDrop.PadText();

            GUILayout.Label("Group Name", expand, rowHeight);
            GUILayout.Label("Keys", GUILayout.Width(45), rowHeight);

            if (isEditor)
                GUILayout.Label("Move", GUILayout.Width(45), rowHeight);

            if (UseElectricCharge)
            {
                GUILayout.Label("EC/s", GUILayout.Width(33), rowHeight);
            }

            //make room for remove button
            if (ServoController.Instance.ServoGroups.Count > 1)
            {
                GUILayout.Space(45);
            }

            var alternateBG = new GUIStyle("label")
            {
                normal =
                {
                    background = TextureLoader.editorBGTex
                }
            };

            GUILayout.EndHorizontal();

            for (int i = 0; i < ServoController.Instance.ServoGroups.Count; i++)
            {
                ServoController.ControlGroup grp = ServoController.Instance.ServoGroups[i];

                GUILayout.BeginHorizontal();

                //Call the Add Group Handle code
                GUIDragAndDrop.DrawGroupHandle(grp.Name, i);

                string tmp = GUILayout.TextField(grp.Name, expand, rowHeight);

                grp.Name = tmp;

                if (isEditor)
                {
                    grp.Expanded = GUILayout.Toggle(grp.Expanded, new GUIContent(TextureLoader.cogButtonIcon, "Adv. settings"), cogButtonStyle, GUILayout.Width(22), rowHeight);
                    SetTooltipText();
                }
                //<-keys->
                tmp = GUILayout.TextField(grp.ReverseKey, GUILayout.Width(20), rowHeight);
                grp.ReverseKey = tmp;

                tmp = GUILayout.TextField(grp.ForwardKey, GUILayout.Width(20), rowHeight);
                grp.ForwardKey = tmp;

                if (isEditor)
                {
                    if (GUILayout.RepeatButton(new GUIContent(TextureLoader.leftIcon, "Hold to Move -"), buttonStyle, GUILayout.Width(22), rowHeight))
                    {
                        foreach (var servo in grp.Servos)
                        {
                            servo.Mechanism.MoveLeft();
                        }
                    }
                    SetTooltipText();

                    if (GUILayout.RepeatButton(new GUIContent(TextureLoader.rightIcon, "Hold to Move +"), buttonStyle, GUILayout.Width(22), rowHeight))
                    {
                        foreach (var servo in grp.Servos)
                        {
                            servo.Mechanism.MoveRight();
                        }
                    }
                    SetTooltipText();
                }

                if (UseElectricCharge)
                {
                    var t = new GUIStyle(GUI.skin.label.name) { alignment = TextAnchor.MiddleCenter };
                    GUILayout.Label(grp.TotalElectricChargeRequirement.ToString(), t, GUILayout.Width(33), rowHeight);
                }

                if (i > 0)
                {
                    GUILayout.Space(5);
                    if (GUILayout.Button(new GUIContent(TextureLoader.trashIcon, "Delete Group"), buttonStyle, GUILayout.Width(30), rowHeight))
                    {
                        while (grp.Servos.Any())
                        {
                            var s = grp.Servos.First();
                            ServoController.MoveServo(grp, ServoController.Instance.ServoGroups[i - 1], s);
                        }

                        ServoController.Instance.ServoGroups.RemoveAt(i);
                        ResetWindow = true;
                        return;
                    }
                    SetTooltipText();
                    GUILayout.Space(5);
                }
                else
                {
                    if (ServoController.Instance.ServoGroups.Count > 1)
                    {
                        GUILayout.Space(45);
                    }
                }

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal(alternateBG);

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();

                // handle name   pos   range  move speed  group
                // <-20-><-flex-><-40-><-40-><-30-><-40-><-30->

                //Pad the text so it still aligns with the text box
                GUIDragAndDrop.PadText();

                GUILayout.Label("Servo Name", expand, rowHeight);

                GUILayout.Space(25);

                GUILayout.Label("Pos.", GUILayout.Width(40), rowHeight);

                if (isEditor)
                    GUILayout.Label("Move", GUILayout.Width(45), rowHeight);

                GUILayout.Label("Group", GUILayout.Width(45), rowHeight);

                GUILayout.EndHorizontal();

                for (int iS = 0; iS < grp.Servos.Count; iS++)
                {
                    var servo = grp.Servos[iS];
                    if (!servo.Mechanism.IsFreeMoving)
                    {
                        GUILayout.BeginHorizontal();

                        //Call the Add Servo Handle code
                        GUIDragAndDrop.DrawServoHandle(servo.Name, i, iS);

                        if (isEditor)
                        {
                            GUILayout.BeginVertical();
                            GUILayout.BeginHorizontal();
                        }

                        servo.Name = GUILayout.TextField(servo.Name, expand, rowHeight);

                        servo.Group.Name = grp.Name;
                        servo.Input.Reverse = grp.ReverseKey;
                        servo.Input.Forward = grp.ForwardKey;

                        if (EditorWindowPos.Contains(mousePos))
                        {
                            Rect last = GUILayoutUtility.GetLastRect();
                            Vector2 pos = Event.current.mousePosition;
                            bool highlight = last.Contains(pos);
                            servo.RawServo.part.SetHighlight(highlight, false);
                        }

                        bool servoPresetsOpen = guiPresetsEnabled && (servo == associatedServo);
                        bool toggleVal = GUILayout.Toggle(servoPresetsOpen, new GUIContent(TextureLoader.presetsIcon, "Edit Presets"), buttonStyle, GUILayout.Width(22), rowHeight);
                        if (servoPresetsOpen != toggleVal)
                        {
                            if (guiPresetsEnabled && associatedServo == servo)
                                guiPresetsEnabled = !guiPresetsEnabled;
                            else
                            {
                                associatedServo = servo;
                                if (!guiPresetsEnabled)
                                    guiPresetsEnabled = true;
                            }
                        }
                        SetTooltipText();

                        var posStyle = new GUIStyle (GUI.skin.label);
                        if(servo.RawServo.Translator.IsAxisInverted)
                        {
                            posStyle.fontStyle = FontStyle.Italic;
                            posStyle.normal.textColor = new Color (1, 1, 0);
                        }
                        GUILayout.Label(string.Format("{0:#0.##}", servo.RawServo.Translator.ToExternalPos(servo.Mechanism.Position)), posStyle, GUILayout.Width(40), rowHeight);

                        //individual servo movement when in editor
                        if (isEditor)
                        {
                            if (GUILayout.RepeatButton(new GUIContent(TextureLoader.leftIcon, "Hold to Move-"), buttonStyle, GUILayout.Width(22), rowHeight))
                            {
                                servo.Mechanism.MoveLeft();
                            }
                            SetTooltipText();

                            if (GUILayout.RepeatButton(new GUIContent(TextureLoader.rightIcon, "Hold to Move+"), buttonStyle, GUILayout.Width(22), rowHeight))
                            {
                                servo.Mechanism.MoveRight();
                            }
                            SetTooltipText();
                        }

                        if (grp.Expanded && isEditor)
                        {
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();

                            GUILayout.Label("Range: ", GUILayout.Width(40), rowHeight);
                            tmpMin = GUILayout.TextField(string.Format("{0:#0.0#}", servo.Mechanism.MinPositionLimit), GUILayout.Width(40), rowHeight);
                            float tmpValue;

                            if (float.TryParse(tmpMin, out tmpValue))
                            {
                                servo.Mechanism.MinPositionLimit = tmpValue;
                            }

                            tmpMax = GUILayout.TextField(string.Format("{0:#0.0#}", servo.Mechanism.MaxPositionLimit), GUILayout.Width(40), rowHeight);
                            if (float.TryParse(tmpMax, out tmpValue))
                            {
                                servo.Mechanism.MaxPositionLimit = tmpValue;
                            }

                            GUILayout.Label("Spd: ", GUILayout.Width(30), rowHeight);
                            tmpMin = GUILayout.TextField(string.Format("{0:#0.0##}", servo.Mechanism.SpeedLimit), GUILayout.Width(40), rowHeight);
                            if (float.TryParse(tmpMin, out tmpValue))
                            {
                                servo.Mechanism.SpeedLimit = tmpValue;
                            }

                            GUILayout.Label("Acc: ", GUILayout.Width(30), rowHeight);
                            tmpMin = GUILayout.TextField(string.Format("{0:#0.0##}", servo.Mechanism.AccelerationLimit), GUILayout.Width(40), rowHeight);
                            if (float.TryParse(tmpMin, out tmpValue))
                            {
                                servo.Mechanism.AccelerationLimit = tmpValue;
                            }
                        }

                        if (isEditor)
                        {
                            GUILayout.EndHorizontal();
                            GUILayout.EndVertical();
                        }

                        if (ServoController.Instance.ServoGroups.Count > 1)
                        {
                            buttonStyle.padding = padding1px;

                            if (i > 0)
                            {
                                if (GUILayout.Button(new GUIContent(TextureLoader.upIcon, "To previous Group"), buttonStyle, GUILayout.Width(20), rowHeight))
                                {
                                    ServoController.MoveServo(grp, ServoController.Instance.ServoGroups[i - 1], servo);
                                }
                                SetTooltipText();
                            }
                            else
                            {
                                GUILayout.Space(22);
                            }
                            if (i < (ServoController.Instance.ServoGroups.Count - 1))
                            {
                                if (GUILayout.Button(new GUIContent(TextureLoader.downIcon, "To next Group"), buttonStyle, GUILayout.Width(20), rowHeight))
                                {
                                    ServoController.MoveServo(grp, ServoController.Instance.ServoGroups[i + 1], servo);
                                }
                                SetTooltipText();
                            }
                            else
                            {
                                GUILayout.Space(22);
                            }

                            buttonStyle.padding = padding2px;
                        }
                        else
                        {
                            GUILayout.Space(45);
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                //Updates the Groups Details with a height for all servos
                GUIDragAndDrop.EndDrawGroup(i);

                GUILayout.EndVertical();

                GUILayout.EndHorizontal();

                //empty line
                GUILayout.BeginHorizontal();
                GUILayout.Label(" ", expand, GUILayout.Height(7));
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Add new Group"))
            {
                var temp = new ServoController.ControlGroup { Name = string.Format("New Group {0}", (ServoController.Instance.ServoGroups.Count + 1)) };
                ServoController.Instance.ServoGroups.Add(temp);
            }

            GUILayout.EndVertical();

            GUILayout.EndScrollView();

            //Do the End of window Code for DragAnd Drop
            GUIDragAndDrop.WindowEnd();

            //If we are dragging an item disable the windowdrag
            if (!GUIDragAndDrop.DraggingItem)
                GUI.DragWindow();
        }

        //Used by DragAndDrop to scroll the scrollview when dragging at top or bottom of window
        internal static void SetEditorScrollYPosition(Single newY)
        {
            EditorScroll.y = newY;
        }

        private void PresetsEditWindow(int windowID)
        {
            var buttonStyle = new GUIStyle(GUI.skin.button);
            //var padding1px = new RectOffset(1, 1, 1, 1);
            var padding2px = new RectOffset(2, 2, 2, 2);

            GUILayoutOption rowHeight = GUILayout.Height(22);
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Preset position" + (associatedServo.RawServo.Translator.IsAxisInverted ? " (Inv axis)" :""), GUILayout.ExpandWidth(true), rowHeight);
            if (GUILayout.Button("Add", buttonStyle, GUILayout.Width(30), rowHeight))
            {
                associatedServo.RawServo.PresetPositions.Add(associatedServo.Mechanism.Position);
            }
            GUILayout.EndHorizontal();

            buttonStyle.padding = padding2px;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < associatedServo.RawServo.PresetPositions.Count; i++)
            {
                GUILayout.BeginHorizontal();
                GUI.SetNextControlName("Preset " + i);
                string tmp = GUILayout.TextField(string.Format("{0:#0.0#}", associatedServo.RawServo.Translator.ToExternalPos(associatedServo.RawServo.PresetPositions[i])), GUILayout.ExpandWidth(true), rowHeight);

                float tmpValue;
                if (float.TryParse(tmp, out tmpValue))
                {
                    tmpValue = associatedServo.RawServo.Translator.ToInternalPos (tmpValue);
                    tmpValue = Mathf.Clamp(tmpValue, associatedServo.Mechanism.MinPositionLimit, associatedServo.Mechanism.MaxPositionLimit);
                    associatedServo.RawServo.PresetPositions[i] = tmpValue;
                }

                if (GUILayout.Button(new GUIContent(TextureLoader.trashIcon, "Delete preset"), buttonStyle, GUILayout.Width(30), rowHeight))
                {
                    associatedServo.RawServo.PresetPositions.RemoveAt(i);
                }
                SetTooltipText();
                GUILayout.EndHorizontal();
            }

            if (lastFocusedControlName != GUI.GetNameOfFocusedControl())
            {
                associatedServo.RawServo.PresetPositions.Sort();
                lastFocusedControlName = GUI.GetNameOfFocusedControl();
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply Symmetry", buttonStyle))
            {
                associatedServo.RawServo.PresetPositions.Sort();
                associatedServo.RawServo.presetPositionsSerialized = associatedServo.RawServo.SerializePresets();

                if (associatedServo.RawServo.part.symmetryCounterparts.Count > 1)
                {
                    foreach (Part part in associatedServo.RawServo.part.symmetryCounterparts)
                    {
                        ((MuMechToggle)part.Modules["MuMechToggle"]).presetPositionsSerialized = associatedServo.RawServo.presetPositionsSerialized;
                        ((MuMechToggle)part.Modules["MuMechToggle"]).ParsePresetPositions();
                    }
                }
            }

            if (GUILayout.Button("Save&Exit", buttonStyle, GUILayout.Width(70)))
            {
                associatedServo.RawServo.PresetPositions.Sort();
                associatedServo.RawServo.presetPositionsSerialized = associatedServo.RawServo.SerializePresets();
                guiPresetsEnabled = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void RefreshKeysFromGUI()
        {
            foreach (ServoController.ControlGroup g in ServoController.Instance.ServoGroups)
            {
                if (g.Servos.Any())
                {
                    foreach (var servo in g.Servos)
                    {
                        servo.Input.Reverse = g.ReverseKey;
                        servo.Input.Forward = g.ForwardKey;
                    }
                }
            }
        }

        private void OnGUI()
        {
            // This particular test isn't needed due to the GUI being enabled
            // and disabled as appropriate, but it saves potential NREs.

            if (ServoController.Instance == null)
                return;
            if (ServoController.Instance.ServoGroups == null)
            {
                irMinimizeButton.Visible = false;
                return;
            }

            if (ToolbarManager.ToolbarAvailable)
                irMinimizeButton.Visible = true;
            
            //what is that for?
            //if (InputLockManager.IsLocked(ControlTypes.LINEAR)) return;

            if (ControlWindowPos.x == 0 && ControlWindowPos.y == 0)
            {
                ControlWindowPos = new Rect(Screen.width - 510, 70, 10, 10);
            }
            if (EditorWindowPos.x == 0 && EditorWindowPos.y == 0)
            {
                EditorWindowPos = new Rect(Screen.width - 260, 50, 10, 10);
            }

            if (PresetWindowPos.x == 0 && PresetWindowPos.y == 0)
            {
                PresetWindowPos = new Rect(Screen.width - 410, 220, 145, 130);
            }

            if (ResetWindow)
            {
                ControlWindowPos = new Rect(ControlWindowPos.x, ControlWindowPos.y, 10, 10);
                EditorWindowPos = new Rect(EditorWindowPos.x, EditorWindowPos.y, 10, 10);
                PresetWindowPos = new Rect(PresetWindowPos.x, PresetWindowPos.y, 10, 10);
                ResetWindow = false;
            }
            GUI.skin = DefaultSkinProvider.DefaultSkin;
            GameScenes scene = HighLogic.LoadedScene;

            //Call the DragAndDrop GUI Setup stuff
            GUIDragAndDrop.OnGUIOnceOnly();

            if (scene == GameScenes.FLIGHT)
            {
                float maxServoNameLabelSize = 0f;
                var nameStyle = new GUIStyle(GUI.skin.label)
                {
                    wordWrap = false
                };

                var boldStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    wordWrap = false
                };

                foreach (ServoController.ControlGroup grp in ServoController.Instance.ServoGroups)
                {
                    Vector2 size = boldStyle.CalcSize(new GUIContent(grp.Name));
                    if (size.x > maxServoNameLabelSize) maxServoNameLabelSize = size.x;

                    foreach (var s in grp.Servos)
                    {
                        size = nameStyle.CalcSize(new GUIContent(s.Name));
                        if (size.x > maxServoNameLabelSize) maxServoNameLabelSize = size.x;
                    }
                }

                ControlWindowWidth = (int)Math.Round(maxServoNameLabelSize + 240);

                if (ControlWindowWidth > Screen.width * 0.7) ControlWindowWidth = (int)Math.Round(Screen.width * 0.7f);

                GUILayoutOption height = GUILayout.Height(Screen.height / 2f);
                if (GUIEnabled && !guiHidden)
                {
                    ControlWindowPos = GUILayout.Window(ControlWindowID, ControlWindowPos,
                        ControlWindow,
                        "Servo Control",
                        GUILayout.Width(ControlWindowWidth),
                        GUILayout.Height(80));
                    if (guiGroupEditorEnabled)
                        EditorWindowPos = GUILayout.Window(EditorWindowID, EditorWindowPos,
                            EditorWindow,
                            "Servo Group Editor",
                            GUILayout.Width(EditorWindowWidth), //Using a variable here
                            height);
                    if (guiPresetsEnabled)
                        PresetWindowPos = GUILayout.Window(PresetWindowID, PresetWindowPos,
                            PresetsEditWindow,
                            associatedServo.Name,
                            GUILayout.Width(200),
                            GUILayout.Height(80));
                }
                RefreshKeysFromGUI();
            }
            else if (scene == GameScenes.EDITOR)
            {
                GUILayoutOption height = GUILayout.Height(Screen.height / 2f);

                if (GUIEnabled && !guiHidden)
                {
                    EditorWindowPos = GUILayout.Window(958, EditorWindowPos,
                        EditorWindow,
                        "Servo Configuration",
                        GUILayout.Width(EditorWindowWidth), //Using a variable here
                        height);
                    if (guiPresetsEnabled)
                        PresetWindowPos = GUILayout.Window(960, PresetWindowPos,
                            PresetsEditWindow,
                            associatedServo.Name,
                            GUILayout.Width(200),
                            GUILayout.Height(80));
                }

                var mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                bool lockEditor = GUIEnabled && (EditorWindowPos.Contains(mousePos) || PresetWindowPos.Contains(mousePos));

                EditorLock(lockEditor);
            }

            GUIDragAndDrop.OnGUIEvery();
            DrawTooltip();
        }

        /// <summary>
        ///     Applies or removes the lock
        /// </summary>
        /// <param name="apply">Which way are we going</param>
        internal void EditorLock(Boolean apply)
        {
            //only do this lock in the editor - no point elsewhere
            if (HighLogic.LoadedSceneIsEditor && apply)
            {
                //only add a new lock if there isnt already one there
                if (InputLockManager.GetControlLock("IRGUILockOfEditor") != ControlTypes.EDITOR_LOCK)
                {
                    Logger.Log(String.Format("[GUI] AddingLock-{0}", "IRGUILockOfEditor"), Logger.Level.Debug);

                    InputLockManager.SetControlLock(ControlTypes.EDITOR_LOCK, "IRGUILockOfEditor");
                }
            }
            //Otherwise make sure the lock is removed
            else
            {
                //Only try and remove it if there was one there in the first place
                if (InputLockManager.GetControlLock("IRGUILockOfEditor") == ControlTypes.EDITOR_LOCK)
                {
                    Logger.Log(String.Format("[GUI] Removing-{0}", "IRGUILockOfEditor"), Logger.Level.Debug);
                    InputLockManager.RemoveControlLock("IRGUILockOfEditor");
                }
            }
        }

        public void LoadConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<ControlsGUI>();
            config.load();
            EditorWindowPos = config.GetValue<Rect>("editorWinPos");
            PresetWindowPos = config.GetValue<Rect>("presetWinPos");
            ControlWindowPos = config.GetValue<Rect>("controlWinPos");
            UseElectricCharge = config.GetValue<bool>("useEC");
        }

        public void SaveConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<ControlsGUI>();
            config.SetValue("editorWinPos", EditorWindowPos);
            config.SetValue("presetWinPos", PresetWindowPos);
            config.SetValue("controlWinPos", ControlWindowPos);
            config.SetValue("useEC", UseElectricCharge);
            config.save();
        }
    }
}