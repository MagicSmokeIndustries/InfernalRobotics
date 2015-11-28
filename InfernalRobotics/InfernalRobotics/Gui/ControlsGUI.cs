using InfernalRobotics.Command;
using InfernalRobotics.Control;
using InfernalRobotics.Utility;
using KSP.IO;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using File = System.IO.File;

namespace InfernalRobotics.Gui
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class IRGUIFlight : ControlsGUI
    {
        public override string AddonName { get { return this.name; } }
    }

    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class IRGUIEditor : ControlsGUI
    {
        public override string AddonName { get { return this.name; } }
    }

    public class ControlsGUI : MonoBehaviour
    {
        public virtual String AddonName { get; set; }

        private static Rect controlWindowPos;
        private static Rect editorWindowPos;
        private static Rect presetWindowPos;
        private static readonly int controlWindowID;
        private static readonly int editorWindowID;
        private static readonly int presetWindowID;

        private static GUIStyle tooltipStyle;
        private static GUIStyle buttonStyle;
        private static GUIStyle nameStyle;
        private static GUIStyle invPosStyle;
        private static GUIStyle dotStyle;
        private static GUIStyle alternateBGStyle;

        private static readonly RectOffset padding1px = new RectOffset(1, 1, 1, 1);
        private static readonly RectOffset padding2px = new RectOffset(2, 2, 2, 2);
        private static readonly RectOffset padding3px = new RectOffset(3, 3, 3, 3);

        private static bool resetWindow;
        private static Vector2 editorScroll;
        private static bool useElectricCharge;
        private static bool allowServoFlip;

        private static ControlsGUI guiController;
        private static bool guiSetupDone;
        private IButton irMinimizeButton;
        private ApplicationLauncherButton button;
        private static Texture2D buttonTexture;
        private bool guiGroupEditorEnabled;
        private bool guiPresetsEnabled;
        private IServo associatedServo;
        private bool guiPresetMode;
        private bool guiHidden;
        private string tooltipText = "";
        private string lastTooltipText = "";
        private float tooltipTime;
        private const float TOOLTIP_MAX_TIME = 8f;
        private const float TOOLTIP_DELAY = 1.5f;
        private string lastFocusedControlName = "";
        private string lastFocusedTextFieldValue = "";
        private static int editorWindowWidth = 400;
        private static int controlWindowWidth = 360;

        public bool GUIEnabled { get; set; }

        public static ControlsGUI IRGUI
        {
            get { return guiController; }
        }

        static ControlsGUI()
        {
            resetWindow = false;
            useElectricCharge = true;
            guiSetupDone = false;
            buttonTexture = new Texture2D(36, 36, TextureFormat.RGBA32, false);
            TextureLoader.LoadImageFromFile (buttonTexture, "icon_button.png");

            string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            //basic constructor to initialize windowIDs
            controlWindowID = UnityEngine.Random.Range(1000, 2000000) + assemblyName.GetHashCode();
            editorWindowID = controlWindowID + 1;
            presetWindowID = controlWindowID + 2;
        }

        /// <summary>
        ///     Load the textures from files to memory
        /// </summary>
        private static void InitGUI()
        {
            if (guiSetupDone) return;

            TextureLoader.InitTextures();

            tooltipStyle = new GUIStyle
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = new Color32(207, 207, 207, 255),
                    background = GUIDragAndDrop.ImgBackground
                },
                stretchHeight = true,
                border = padding3px,
                padding = new RectOffset(4, 4, 6, 4),
                alignment = TextAnchor.MiddleLeft
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                padding = padding2px,
                alignment = TextAnchor.MiddleCenter
            };

            dotStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                alignment = TextAnchor.MiddleCenter
            };

            nameStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                clipping = TextClipping.Clip
            };

            invPosStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Italic,
                alignment = TextAnchor.MiddleCenter,
                normal =
                {
                    textColor = new Color(1, 1, 0)
                }
            };

            alternateBGStyle = new GUIStyle("label")
            {
                normal =
                {
                    background = TextureLoader.EditorBackgroundText
                }
            };

            guiSetupDone = true;
        }

        private void Awake()
        {
            
            LoadConfigXml();

            Logger.Log("[GUI] awake, Mode: " + AddonName);

            GUIEnabled = false;

            guiGroupEditorEnabled = false;

            GameScenes scene = HighLogic.LoadedScene;

            if (scene == GameScenes.FLIGHT || scene == GameScenes.EDITOR)
            {
                guiController = this;
            }
            else
            {
                guiController = null;
                //actually we don't need to go further if it's not flight or editor
                return;
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
                GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequestedForAppLauncher);

                if (ApplicationLauncher.Ready && button == null)
                {
                    OnAppReady();
                }
                Logger.Log("[GUI] Added Toolbar GameEvents Handlers", Logger.Level.Debug);
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
            Logger.Log(string.Format("[GUI] OnAppReady Called, button=null: {0}", (button == null)), Logger.Level.Debug);

            if (button != null) return;

            if (!ApplicationLauncher.Ready)
                return;

            try
            {
                button = ApplicationLauncher.Instance.AddModApplication(delegate { GUIEnabled = true; },
                    delegate { GUIEnabled = false; }, null, null, null, null,
                    ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB |
                    ApplicationLauncher.AppScenes.SPH, buttonTexture);

                ApplicationLauncher.Instance.AddOnHideCallback(OnHideCallback);
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("[GUI OnAppReady Exception, {0}", ex.Message), Logger.Level.Fatal);
            }

            Logger.Log(string.Format("[GUI] OnAppReady finished, button=null: {0}", (button == null)), Logger.Level.Debug);
        }

        private void OnHideCallback()
        {
            GUIEnabled = false;
        }

        void OnGameSceneLoadRequestedForAppLauncher(GameScenes SceneToLoad)
        {
            DestroyAppLauncherButton();
        }

        private void DestroyAppLauncherButton()
        {
            try
            {
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

        private void OnDestroy()
        {
            Logger.Log("[GUI] destroy");

            if (ToolbarManager.ToolbarAvailable)
            {
                irMinimizeButton.Destroy();
            }
            else
            {
                GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequestedForAppLauncher);
                DestroyAppLauncherButton();
            }

            GameEvents.onShowUI.Remove(OnShowUI);
            GameEvents.onHideUI.Remove(OnHideUI);

            EditorLock(false);
            SaveConfigXml();
            Logger.Log("[GUI] OnDestroy finished successfully", Logger.Level.Debug);
        }

        //servo control window used in flight
        private void ControlWindow(int windowID)
        {
            GUILayoutOption width20 = GUILayout.Width(20);

            GUILayout.BeginVertical();

            const int BUTTON_HEIGHT = 22;

            //use of for instead of foreach in intentional
            for (int i = 0; i < ServoController.Instance.ServoGroups.Count; i++)
            {
                ServoController.ControlGroup g = ServoController.Instance.ServoGroups[i];

                if (g.Servos.Any())
                {
                    GUILayout.BeginHorizontal();

                    if (g.Expanded)
                    {
                        g.Expanded = !GUILayout.Button(TextureLoader.CollapseIcon, buttonStyle, width20, GUILayout.Height(BUTTON_HEIGHT));
                    }
                    else
                    {
                        g.Expanded = GUILayout.Button(TextureLoader.ExpandIcon, buttonStyle, width20, GUILayout.Height(BUTTON_HEIGHT));
                    }

                    nameStyle.fontStyle = FontStyle.Bold;

                    GUILayout.Label(g.Name, nameStyle, GUILayout.ExpandWidth(true), GUILayout.Height(BUTTON_HEIGHT));

                    nameStyle.fontStyle = FontStyle.Normal;

                    g.Speed = GUILayout.TextField(g.Speed, GUILayout.Width(30), GUILayout.Height(BUTTON_HEIGHT));

                    Rect last = GUILayoutUtility.GetLastRect();
                    Vector2 pos = Event.current.mousePosition;
                    if (last.Contains(pos) && Event.current.type == EventType.Repaint)
                        tooltipText = "Speed Multiplier";

                    bool toggleVal = GUILayout.Toggle(g.MovingNegative, new GUIContent(TextureLoader.LeftToggleIcon, "Toggle Move -"), buttonStyle,
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
                        g.MoveLeft();
                    }

                    if (guiPresetMode)
                    {
                        if (GUILayout.Button(new GUIContent(TextureLoader.PrevIcon, "Previous Preset"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            //reset any group toggles
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MovePrevPreset();
                        }
                        SetTooltipText();

                        if (GUILayout.Button(new GUIContent(TextureLoader.AutoRevertIcon, "Reset"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            //reset any group toggles
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MoveCenter();
                        }
                        SetTooltipText();

                        if (GUILayout.Button(new GUIContent(TextureLoader.NextIcon, "Next Preset"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
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
                        if (GUILayout.RepeatButton(new GUIContent(TextureLoader.LeftIcon, "Hold to Move -"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MoveLeft();

                            g.ButtonDown = true;
                        }

                        SetTooltipText();

                        if (GUILayout.RepeatButton(new GUIContent(TextureLoader.RevertIcon, "Hold to Center"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MoveCenter();

                            g.ButtonDown = true;
                        }
                        SetTooltipText();

                        if (GUILayout.RepeatButton(new GUIContent(TextureLoader.RightIcon, "Hold to Move +"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                        {
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MoveRight();

                            g.ButtonDown = true;
                        }
                        SetTooltipText();
                    }

                    toggleVal = GUILayout.Toggle(g.MovingPositive, new GUIContent(TextureLoader.RightToggleIcon, "Toggle Move +"), buttonStyle,
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
                        g.MoveRight();
                    }

                    GUILayout.EndHorizontal();

                    if (g.Expanded)
                    {
                        GUILayout.BeginHorizontal(GUILayout.Height(5));
                        GUILayout.EndHorizontal();

                        foreach (var servo in g.Servos)
                        {
                            GUILayout.BeginHorizontal();

                            string servoStatus = servo.Mechanism.IsMoving ? "<color=lime>■</color>" : "<color=yellow>■</color>";

                            if (servo.Mechanism.IsLocked)
                                servoStatus = "<color=red>■</color>";

                            GUILayout.Label(servoStatus, dotStyle, GUILayout.Width(20), GUILayout.Height(BUTTON_HEIGHT));

                            GUILayout.Label(servo.Name, nameStyle, GUILayout.ExpandWidth(true), GUILayout.Height(BUTTON_HEIGHT));

                            nameStyle.fontStyle = FontStyle.Italic;
                            nameStyle.alignment = TextAnchor.MiddleCenter;

                            GUILayout.Label(string.Format("{0:#0.##}", servo.Mechanism.Position), servo.Mechanism.IsAxisInverted ? invPosStyle : nameStyle, GUILayout.Width(45), GUILayout.Height(BUTTON_HEIGHT));

                            nameStyle.fontStyle = FontStyle.Normal;
                            nameStyle.alignment = TextAnchor.MiddleLeft;

                            bool servoLocked = servo.Mechanism.IsLocked;
                            servoLocked = GUILayout.Toggle(servoLocked,
                                            servoLocked ? new GUIContent(TextureLoader.LockedIcon, "Unlock Servo") : new GUIContent(TextureLoader.UnlockedIcon, "Lock Servo"),
                                            buttonStyle, GUILayout.Width(28), GUILayout.Height(BUTTON_HEIGHT));
                            servo.Mechanism.IsLocked = servoLocked;

                            SetTooltipText();

                            if (guiPresetMode)
                            {
                                if (GUILayout.Button(new GUIContent(TextureLoader.PrevIcon, "Previous Preset"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;

                                    servo.Preset.MovePrev();
                                }
                                SetTooltipText();

                                var rowHeight = GUILayout.Height(BUTTON_HEIGHT);
                                DrawEditPresetButton(servo, buttonStyle, rowHeight);
                                SetTooltipText();

                                if (GUILayout.Button(new GUIContent(TextureLoader.NextIcon, "Next Preset"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;

                                    servo.Preset.MoveNext();
                                }
                                SetTooltipText();
                            }
                            else
                            {
                                if (GUILayout.RepeatButton(new GUIContent(TextureLoader.LeftIcon, "Hold to Move -"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;
                                    g.ButtonDown = true;

                                    servo.Mechanism.MoveLeft();
                                }
                                SetTooltipText();

                                if (GUILayout.RepeatButton(new GUIContent(TextureLoader.RevertIcon, "Hold to Center"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;
                                    g.ButtonDown = true;

                                    servo.Mechanism.MoveCenter();
                                }
                                SetTooltipText();

                                if (GUILayout.RepeatButton(new GUIContent(TextureLoader.RightIcon, "Hold to Move +"), buttonStyle, GUILayout.Width(22), GUILayout.Height(BUTTON_HEIGHT)))
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;
                                    g.ButtonDown = true;

                                    servo.Mechanism.MoveRight();
                                }
                                SetTooltipText();
                            }
                            bool servoInverted = servo.Mechanism.IsAxisInverted;

                            servoInverted = GUILayout.Toggle(servoInverted,
                                servoInverted ? new GUIContent(TextureLoader.InvertedIcon, "Un-invert Axis") : new GUIContent(TextureLoader.NoninvertedIcon, "Invert Axis"),
                                buttonStyle, GUILayout.Width(28), GUILayout.Height(BUTTON_HEIGHT));

                            SetTooltipText();

                            servo.Mechanism.IsAxisInverted = servoInverted;

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

            guiPresetMode = GUILayout.Toggle(guiPresetMode, new GUIContent(TextureLoader.PresetModeIcon, "Preset Mode"), buttonStyle,
                GUILayout.Width(32), GUILayout.Height(32));
            SetTooltipText();

            buttonStyle.padding = new RectOffset(3, 3, 3, 3);

            if (GUILayout.Button(new GUIContent(TextureLoader.StopButtonIcon, "Emergency Stop"), buttonStyle, GUILayout.Width(32), GUILayout.Height(32)))
            {
                foreach (var g in ServoController.Instance.ServoGroups)
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

        private void DrawPresetSelector(IServo servo, GUILayoutOption rowHeight)
        {
            
            int floor, ceiling;

            servo.Preset.GetNearestPresets(out floor, out ceiling);

            if (GUILayout.Button(new GUIContent(TextureLoader.PrevIcon, "Previous Preset" + ((floor >= 0) ? ": " + servo.Preset[floor] : "")),
                                 buttonStyle, GUILayout.Width(18), rowHeight))
            {
                servo.Preset.MovePrev();
            }
            SetTooltipText();

            DrawServoPosition(servo, rowHeight);

            if (GUILayout.Button(new GUIContent(TextureLoader.NextIcon, "Next Preset" + ((ceiling >= 0) ? ": " + servo.Preset[ceiling] : "")),
                                  buttonStyle, GUILayout.Width(18), rowHeight))
            {
                servo.Preset.MoveNext();
            }
            SetTooltipText();
        }
        /// <summary>
        /// Draws the text field and returns its value
        /// </summary>
        /// <returns>Entered value</returns>
        /// <param name="controlName">Control name.</param>
        /// <param name="value">Value.</param>
        /// <param name="format">Format.</param>
        /// <param name="style">Style.</param>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        private string DrawTextField(string controlName, float value, string format, GUIStyle style, GUILayoutOption width, GUILayoutOption height)
        {
            string focusedControlName = GUI.GetNameOfFocusedControl ();

            if (controlName == focusedControlName 
                && lastFocusedTextFieldValue == "")
            {
                lastFocusedTextFieldValue = string.Format (format, value);
            }

            string tmp = (controlName == focusedControlName) 
                ? lastFocusedTextFieldValue 
                : string.Format (format, value);

            GUI.SetNextControlName(controlName);
            tmp = GUILayout.TextField(tmp, style, width, height);

            if (controlName == focusedControlName 
                && focusedControlName == lastFocusedControlName)
                lastFocusedTextFieldValue = tmp;

            return tmp;
        }

        private void DrawServoPosition(IServo servo, GUILayoutOption rowHeight)
        {
            var customStyle = new GUIStyle(GUI.skin.textField)
            {
                normal =
                {
                    textColor = servo.Mechanism.IsAxisInverted ? Color.yellow : Color.white
                },
                alignment = TextAnchor.MiddleCenter,
                fontStyle = servo.Mechanism.IsAxisInverted ? FontStyle.Italic : FontStyle.Normal
            };
            var posFormat = Math.Abs(servo.Mechanism.MaxPosition - servo.Mechanism.MinPosition) > 10 ? "{0:#0.0#}" : "{0:#0.0##}";

            string focusedControlName = GUI.GetNameOfFocusedControl ();
            string thisControlName = "Position " + servo.UID;

            string tmp = DrawTextField (thisControlName, servo.Mechanism.Position, posFormat, customStyle, GUILayout.Width (40), rowHeight);

            var valueChanged = (thisControlName == focusedControlName && 
                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter));

            float tmpValue;

            if (float.TryParse (tmp, out tmpValue) && valueChanged) 
            {
                //focus changers are handled elsewhere
                tmpValue = Mathf.Clamp(tmpValue, servo.Mechanism.MinPositionLimit, servo.Mechanism.MaxPositionLimit);

                if (Math.Abs(servo.Mechanism.Position - tmpValue) > 0.005)
                    servo.Mechanism.MoveTo(tmpValue);
                
                lastFocusedTextFieldValue = "";
            }
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

            bool isEditor = (HighLogic.LoadedScene == GameScenes.EDITOR);

            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;

            editorScroll = GUILayout.BeginScrollView(editorScroll, false, true, maxHeight);

            //Kick off the window code
            GUIDragAndDrop.WindowBegin(editorWindowPos, editorScroll);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            //if we are showing the group handles then Pad the text so it still aligns with the text box
            if (GUIDragAndDrop.ShowGroupHandles)
                GUIDragAndDrop.PadText();

            GUILayout.Label("Group Name", expand, rowHeight);
            GUILayout.Label("Keys", GUILayout.Width(45), rowHeight);

            if (useElectricCharge)
            {
                GUILayout.Label("EC/s", GUILayout.Width(40), rowHeight);
            }

            if (isEditor)
                GUILayout.Label("Movement", GUILayout.Width(70), rowHeight);

            //make room for remove button
            if (ServoController.Instance.ServoGroups.Count > 1)
            {
                GUILayout.Space(45);
            }

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
                    buttonStyle.padding = padding3px;
                    grp.Expanded = GUILayout.Toggle(grp.Expanded, new GUIContent(TextureLoader.CogButtonIcon, "Adv. settings"), buttonStyle, GUILayout.Width(22), rowHeight);
                    SetTooltipText();
                    buttonStyle.padding = padding2px;
                }
                //<-keys->
                tmp = GUILayout.TextField(grp.ReverseKey, GUILayout.Width(20), rowHeight);
                grp.ReverseKey = tmp;

                tmp = GUILayout.TextField(grp.ForwardKey, GUILayout.Width(20), rowHeight);
                grp.ForwardKey = tmp;

                if (useElectricCharge)
                {
                    GUILayout.Label(grp.TotalElectricChargeRequirement.ToString(), dotStyle, GUILayout.Width(40), rowHeight);
                }

                if (isEditor)
                {
                    if (GUILayout.RepeatButton(new GUIContent(TextureLoader.LeftIcon, "Hold to Move -"), buttonStyle, GUILayout.Width(22), rowHeight))
                    {
                        grp.MoveLeft();
                    }
                    SetTooltipText();

                    if (GUILayout.Button(new GUIContent(TextureLoader.AutoRevertIcon, "Reset"), buttonStyle, GUILayout.Width(22), rowHeight))
                    {
                        grp.MoveCenter();
                    }
                    SetTooltipText();

                    if (GUILayout.RepeatButton(new GUIContent(TextureLoader.RightIcon, "Hold to Move +"), buttonStyle, GUILayout.Width(22), rowHeight))
                    {
                        grp.MoveRight();
                    }
                    SetTooltipText();
                }

                if (i > 0)
                {
                    GUILayout.Space(5);
                    if (GUILayout.Button(new GUIContent(TextureLoader.TrashIcon, "Delete Group"), buttonStyle, GUILayout.Width(30), rowHeight))
                    {
                        while (grp.Servos.Any())
                        {
                            var s = grp.Servos.First();
                            ServoController.MoveServo(grp, ServoController.Instance.ServoGroups[i - 1], s);
                        }

                        ServoController.Instance.ServoGroups.RemoveAt(i);
                        resetWindow = true;
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

                GUILayout.BeginHorizontal(alternateBGStyle);

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();

                // handle name   pos   range  move speed  group
                // <-20-><-flex-><-40-><-40-><-30-><-40-><-30->

                //Pad the text so it still aligns with the text box
                GUIDragAndDrop.PadText();

                GUILayout.Label("Servo Name", expand, rowHeight);

                if (isEditor)
                    GUILayout.Label("Position", GUILayout.Width(90), rowHeight);
                
                if (isEditor)
                    GUILayout.Label("Movement", GUILayout.Width(70), rowHeight);

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

                        if (editorWindowPos.Contains(mousePos))
                        {
                            Rect last = GUILayoutUtility.GetLastRect();
                            Vector2 pos = Event.current.mousePosition;
                            bool highlight = last.Contains(pos);
                            servo.Highlight = highlight;
                        }

                        //draw position presets and Edit Presets button in Editor only
                        if (isEditor)
                        {
                            DrawPresetSelector(servo, rowHeight);
                            DrawEditPresetButton(servo, buttonStyle, rowHeight);
                        }
                            
                        //else
                        //    GUILayout.Label(string.Format("{0:#0.##}", servo.Mechanism.Position), servo.Mechanism.IsAxisInverted ? invPosStyle : nameStyle, GUILayout.Width(70), rowHeight);

                        //individual servo movement when in editor
                        if (isEditor)
                        {
                            if (GUILayout.RepeatButton(new GUIContent(TextureLoader.LeftIcon, "Hold to Move-"), buttonStyle, GUILayout.Width(22), rowHeight))
                            {
                                servo.Mechanism.MoveLeft();
                            }
                            SetTooltipText();

                            if (GUILayout.Button(new GUIContent(TextureLoader.AutoRevertIcon, "Reset"), buttonStyle, GUILayout.Width(22), rowHeight))
                            {
                                servo.Mechanism.MoveCenter();
                            }
                            SetTooltipText();

                            if (GUILayout.RepeatButton(new GUIContent(TextureLoader.RightIcon, "Hold to Move+"), buttonStyle, GUILayout.Width(22), rowHeight))
                            {
                                servo.Mechanism.MoveRight();
                            }
                            SetTooltipText();

                            if (allowServoFlip)
                            {
                                //set a smaller height to align with text boxes
                                if (GUILayout.Button("Ͼ", GUILayout.Width(20), rowHeight))
                                {
                                    if (servo.RawServo.rotation == 0f && servo.RawServo.translation == 0f)
                                        servo.RawServo.transform.Rotate(0, 45f, 0, Space.Self);
                                    else
                                        ScreenMessages.PostScreenMessage("<color=#FF0000>Can't change part angle after adjusting part</color>");
                                }
                                //set a smaller height to align with text boxes
                                if (GUILayout.Button("Ͽ", GUILayout.Width(20), rowHeight))
                                {
                                    if (servo.RawServo.rotation == 0f && servo.RawServo.translation == 0f)
                                        servo.RawServo.transform.Rotate(0, -45f, 0, Space.Self);
                                    else
                                        ScreenMessages.PostScreenMessage("<color=#FF0000>Can't change part angle after adjusting part</color>");
                                }
                            }
                        }

                        if (grp.Expanded && isEditor)
                        {
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();

                            GUILayout.Label("Range: ", GUILayout.Width(40), rowHeight);
                            //GUI.SetNextControlName ("MinPositionLimit " + servo.UID);

                            string focusedControlName = GUI.GetNameOfFocusedControl ();
                            string thisControlName = "MinPositionLimit " + servo.UID;

                            tmp = DrawTextField (thisControlName, servo.Mechanism.MinPositionLimit, "{0:#0.0#}", 
                                                 GUI.skin.textField, GUILayout.Width (40), rowHeight);

                            var valueChanged = (thisControlName == focusedControlName && 
                                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter));

                            float tmpValue;

                            if (float.TryParse (tmp, out tmpValue) && valueChanged) 
                            {
                                //focus changers are handled elsewhere
                                servo.Mechanism.MinPositionLimit = tmpValue;
                                lastFocusedTextFieldValue = "";
                            }

                            thisControlName = "MaxPositionLimit " + servo.UID;

                            tmp = DrawTextField (thisControlName, servo.Mechanism.MaxPositionLimit, "{0:#0.0#}", 
                                                 GUI.skin.textField, GUILayout.Width (40), rowHeight);

                            valueChanged = (thisControlName == focusedControlName && 
                                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter));

                            if (float.TryParse (tmp, out tmpValue) && valueChanged) 
                            {
                                //focus changers are handled elsewhere
                                servo.Mechanism.MaxPositionLimit = tmpValue;
                                lastFocusedTextFieldValue = "";
                            }

                            GUILayout.Label("Spd: ", GUILayout.Width(30), rowHeight);

                            thisControlName = "Speed " + servo.UID;

                            tmp = DrawTextField (thisControlName, servo.Mechanism.SpeedLimit, "{0:#0.0#}", 
                                GUI.skin.textField, GUILayout.Width (30), rowHeight);

                            valueChanged = (thisControlName == focusedControlName && 
                                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter));

                            if (float.TryParse (tmp, out tmpValue) && valueChanged) 
                            {
                                //focus changers are handled elsewhere
                                servo.Mechanism.SpeedLimit = tmpValue;
                                lastFocusedTextFieldValue = "";
                            }

                            GUILayout.Label("Acc: ", GUILayout.Width(30), rowHeight);

                            thisControlName = "Acceleration " + servo.UID;

                            tmp = DrawTextField (thisControlName, servo.Mechanism.AccelerationLimit, "{0:#0.0#}", 
                                GUI.skin.textField, GUILayout.Width (30), rowHeight);

                            valueChanged = (thisControlName == focusedControlName && 
                                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter));

                            if (float.TryParse (tmp, out tmpValue) && valueChanged) 
                            {
                                //focus changers are handled elsewhere
                                servo.Mechanism.AccelerationLimit = tmpValue;
                                lastFocusedTextFieldValue = "";
                            }

                            bool servoInverted = servo.Mechanism.IsAxisInverted;

                            servoInverted = GUILayout.Toggle(servoInverted,
                                servoInverted ? new GUIContent(TextureLoader.InvertedIcon, "Un-invert Axis") : new GUIContent(TextureLoader.NoninvertedIcon, "Invert Axis"),
                                buttonStyle, GUILayout.Width(28), rowHeight);

                            SetTooltipText();
                            servo.Mechanism.IsAxisInverted = servoInverted;

                            if (GUILayout.Button(new GUIContent(TextureLoader.CloneIcon, "Apply Symmetry"), buttonStyle, GUILayout.Width(28), rowHeight))
                            {
                                servo.Mechanism.ApplyLimitsToSymmetry ();
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
                                if (GUILayout.Button(new GUIContent(TextureLoader.UpIcon, "To previous Group"), buttonStyle, GUILayout.Width(20), rowHeight))
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
                                if (GUILayout.Button(new GUIContent(TextureLoader.DownIcon, "To next Group"), buttonStyle, GUILayout.Width(20), rowHeight))
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

        private void DrawEditPresetButton(IServo servo, GUIStyle presetButtonStyle, GUILayoutOption rowHeight)
        {
            bool servoPresetsOpen = guiPresetsEnabled && (Equals(servo, associatedServo));
            bool toggleVal = GUILayout.Toggle(servoPresetsOpen, new GUIContent(TextureLoader.PresetsIcon, "Edit Presets"),
                presetButtonStyle, GUILayout.Width(22), rowHeight);
            if (servoPresetsOpen != toggleVal)
            {
                if (guiPresetsEnabled && Equals(associatedServo, servo))
                    guiPresetsEnabled = !guiPresetsEnabled;
                else
                {
                    associatedServo = servo;
                    if (!guiPresetsEnabled)
                        guiPresetsEnabled = true;
                }
            }
            SetTooltipText();
        }

        //Used by DragAndDrop to scroll the scrollview when dragging at top or bottom of window
        internal static void SetEditorScrollYPosition(Single newY)
        {
            editorScroll.y = newY;
        }

        private void ProcessFocusChange()
        {
            var temp = lastFocusedControlName.Split (' ');
            Logger.Log ("[GUI] Focus change, lastName = " + lastFocusedControlName 
                + ", lastValue = " + lastFocusedTextFieldValue 
                + ", temp.Length = " + temp.Length, Logger.Level.Debug);

            var servoFields = new string[6] {"Preset", "Position", "MinPositionLimit", "MaxPositionLimit", "Speed", "Acceleration"};

            var pos = Array.IndexOf (servoFields, temp [0]);

            if (pos == 0  && temp.Length == 2 && associatedServo != null)
            {
                int tmpVal = -1;
                if(int.TryParse(temp[1], out tmpVal))
                {
                    if (tmpVal >= 0 && tmpVal < associatedServo.Preset.Count)
                    {
                        float tmpValue;

                        if (float.TryParse (lastFocusedTextFieldValue, out tmpValue)) 
                        {
                            if (tmpValue != associatedServo.Preset [tmpVal] && associatedServo.Preset [tmpVal] == associatedServo.Mechanism.DefaultPosition) 
                            {
                                associatedServo.Mechanism.DefaultPosition = tmpValue;
                            }
                            associatedServo.Preset [tmpVal] = tmpValue;
                        }
                    }
                }
            }
            else if (temp.Length == 2 && pos > 0 && pos < 6)
            {
                uint servoUID = 0;
                if(uint.TryParse(temp[1], out servoUID))
                {
                    //find servo with UID and update its position
                    var allServos = new List<IServo>();
                    foreach (var g in ServoController.Instance.ServoGroups)
                    {
                        allServos.AddRange (g.Servos);
                    }
                    var s = allServos.Find (p => p.UID == servoUID);

                    if (s != null)
                    {
                        float tmpValue;

                        if (float.TryParse (lastFocusedTextFieldValue, out tmpValue)) 
                        {
                            if (pos == 1)
                            {
                                tmpValue = Mathf.Clamp(tmpValue, s.Mechanism.MinPositionLimit, s.Mechanism.MaxPositionLimit);

                                if (Math.Abs(s.Mechanism.Position - tmpValue) > 0.005)
                                    s.Mechanism.MoveTo(tmpValue);
                            }
                            else if (pos == 2)
                            {
                                s.Mechanism.MinPositionLimit = tmpValue;
                            }
                            else if (pos == 3)
                            {
                                s.Mechanism.MaxPositionLimit = tmpValue;
                            }
                            else if (pos == 4)
                            {
                                s.Mechanism.SpeedLimit = tmpValue;
                            }
                            else if (pos == 5)
                            {
                                s.Mechanism.AccelerationLimit = tmpValue;
                            }
                        }
                    }
                }
            }

            if (associatedServo != null)
            {
                associatedServo.Preset.Sort();
            }

            lastFocusedControlName = GUI.GetNameOfFocusedControl();
            lastFocusedTextFieldValue = "";
        }

        private void PresetsEditWindow(int windowID)
        {
            GUILayoutOption rowHeight = GUILayout.Height(22);
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Preset position" + (associatedServo.Mechanism.IsAxisInverted ? " (Inv axis)" : ""), GUILayout.ExpandWidth(true), rowHeight);
            if (GUILayout.Button("Add", buttonStyle, GUILayout.Width(30), rowHeight))
            {
                associatedServo.Preset.Add();
                associatedServo.Preset.Sort();
            }
            GUILayout.EndHorizontal();

            buttonStyle.padding = padding2px;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            for (int i = 0; i < associatedServo.Preset.Count; i++)
            {
                GUILayout.BeginHorizontal();

                string focusedControlName = GUI.GetNameOfFocusedControl ();
                string thisControlName = "Preset " + i;

                if (thisControlName == focusedControlName 
                    && lastFocusedTextFieldValue == "")
                {
                    lastFocusedTextFieldValue = string.Format ("{0:#0.0#}", associatedServo.Preset [i]);
                }

                string tmp = (thisControlName == focusedControlName) 
                                ? lastFocusedTextFieldValue 
                                : string.Format("{0:#0.0#}", associatedServo.Preset[i]);
                
                GUI.SetNextControlName(thisControlName);
                tmp = GUILayout.TextField(tmp, GUILayout.ExpandWidth(true), rowHeight);

                if (thisControlName == focusedControlName 
                    && focusedControlName == lastFocusedControlName)
                    lastFocusedTextFieldValue = tmp;
                
                var valueChanged = (thisControlName == focusedControlName && 
                                   (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter));

                float tmpValue;

                if (float.TryParse (tmp, out tmpValue) && valueChanged) 
                {
                    //focus changes are handled elsewhere
                    if (tmpValue != associatedServo.Preset [i] && associatedServo.Preset [i] == associatedServo.Mechanism.DefaultPosition) 
                    {
                        associatedServo.Mechanism.DefaultPosition = tmpValue;
                    }
                    associatedServo.Preset [i] = tmpValue;
                    associatedServo.Preset.Sort ();
                    //unfocus control as the list is sorted
                    GUIUtility.keyboardControl = 0;
                    lastFocusedTextFieldValue = "";
                }

                bool isDefault = (associatedServo.Preset[i] == associatedServo.Mechanism.DefaultPosition);
                isDefault = GUILayout.Toggle(isDefault, new GUIContent(TextureLoader.RevertIcon, "Set Default"),
                                buttonStyle, GUILayout.Width(28), rowHeight);

                SetTooltipText();
                if (isDefault)
                    associatedServo.Mechanism.DefaultPosition = associatedServo.Preset[i];

                if (GUILayout.Button(new GUIContent(TextureLoader.NextIcon, "Move Here"), buttonStyle, GUILayout.Width(30), rowHeight))
                {
                    associatedServo.Preset.MoveTo(i);
                }
                SetTooltipText();

                if (GUILayout.Button(new GUIContent(TextureLoader.TrashIcon, "Delete preset"), buttonStyle, GUILayout.Width(30), rowHeight))
                {
                    if (associatedServo.Preset[i] == associatedServo.Mechanism.DefaultPosition)
                        associatedServo.Mechanism.DefaultPosition = 0;
                    associatedServo.Preset.RemoveAt(i);
                }
                SetTooltipText();
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply Symmetry", buttonStyle))
            {
                associatedServo.Preset.Save(true);
            }

            if (GUILayout.Button("Close", buttonStyle, GUILayout.Width(70)))
            {
                associatedServo.Preset.Save();
                guiPresetsEnabled = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void RefreshKeysFromGUI()
        {
            foreach (var g in ServoController.Instance.ServoGroups)
            {
                g.RefreshKeys();
            }
        }

        private void OnGUI()
        {
            if (!ServoController.APIReady)
            {
                if (ToolbarManager.ToolbarAvailable)
                    irMinimizeButton.Visible = false;
                if (button != null)
                {
                    button.VisibleInScenes = ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB;
                }
                return;
            }
            //at this point we have ServoController active with at least one group in it.
            if (ToolbarManager.ToolbarAvailable)
                irMinimizeButton.Visible = true;

            if (button != null)
            {
                button.VisibleInScenes = ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB;
            }

            //what is that for?
            //if (InputLockManager.IsLocked(ControlTypes.LINEAR)) return;

            if (controlWindowPos.x == 0 && controlWindowPos.y == 0)
            {
                controlWindowPos = new Rect(Screen.width - 510, 70, 10, 10);
            }
            if (editorWindowPos.x == 0 && editorWindowPos.y == 0)
            {
                editorWindowPos = new Rect(Screen.width - 260, 50, 10, 10);
            }

            if (presetWindowPos.x == 0 && presetWindowPos.y == 0)
            {
                presetWindowPos = new Rect(Screen.width - 410, 220, 145, 130);
            }

            if (resetWindow)
            {
                controlWindowPos = new Rect(controlWindowPos.x, controlWindowPos.y, 10, 10);
                editorWindowPos = new Rect(editorWindowPos.x, editorWindowPos.y, 10, 10);
                presetWindowPos = new Rect(presetWindowPos.x, presetWindowPos.y, 10, 10);
                resetWindow = false;
            }
            GUI.skin = DefaultSkinProvider.DefaultSkin;
            GameScenes scene = HighLogic.LoadedScene;

            if (!guiSetupDone)
                InitGUI();

            //Call the DragAndDrop GUI Setup stuff
            //GUIDragAndDrop.Enabled = false;
            GUIDragAndDrop.ShowGroupHandles = true;
            GUIDragAndDrop.OnGUIOnceOnly();

            float maxServoNameUISize = 0f;

            foreach (ServoController.ControlGroup grp in ServoController.Instance.ServoGroups)
            {
                Vector2 size = nameStyle.CalcSize(new GUIContent(grp.Name));
                if (size.x > maxServoNameUISize) maxServoNameUISize = size.x;

                foreach (var s in grp.Servos)
                {
                    size = nameStyle.CalcSize(new GUIContent(s.Name));
                    if (size.x > maxServoNameUISize) maxServoNameUISize = size.x;
                }
            }

            controlWindowWidth = (int)Math.Round(maxServoNameUISize + 240);
            if (controlWindowWidth > Screen.width * 0.7)
                controlWindowWidth = (int)Math.Round(Screen.width * 0.7f);

            editorWindowWidth = (int)Math.Round(maxServoNameUISize + 340);
            if (editorWindowWidth > Screen.width * 0.7)
                editorWindowWidth = (int)Math.Round(Screen.width * 0.7f);

            if (GUIEnabled && !guiHidden) 
            {
                if (lastFocusedControlName != GUI.GetNameOfFocusedControl ()) 
                {
                    ProcessFocusChange ();
                }

                //this code defocuses the TexFields if you click mouse elsewhere
                if (GUIUtility.hotControl > 0 && GUIUtility.hotControl != GUIUtility.keyboardControl) 
                {
                    GUIUtility.keyboardControl = 0;
                }
            }

            if (scene == GameScenes.FLIGHT)
            {
                GUILayoutOption height = GUILayout.Height(Screen.height / 2f);
                if (GUIEnabled && !guiHidden)
                {
                    controlWindowPos = GUILayout.Window(controlWindowID, controlWindowPos,
                        ControlWindow,
                        "Servo Control",
                        GUILayout.Width(controlWindowWidth),
                        GUILayout.Height(80));
                    if (guiGroupEditorEnabled)
                        editorWindowPos = GUILayout.Window(editorWindowID, editorWindowPos,
                            EditorWindow,
                            "Servo Group Editor",
                            GUILayout.Width(editorWindowWidth - 100), //Using a variable here
                            height);
                    if (guiPresetsEnabled)
                        presetWindowPos = GUILayout.Window(presetWindowID, presetWindowPos,
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
                    editorWindowPos = GUILayout.Window(editorWindowID, editorWindowPos,
                        EditorWindow,
                        "Servo Configuration",
                        GUILayout.Width(editorWindowWidth), //Using a variable here
                        height);
                    if (guiPresetsEnabled)
                        presetWindowPos = GUILayout.Window(presetWindowID, presetWindowPos,
                            PresetsEditWindow,
                            associatedServo.Name,
                            GUILayout.Width(200),
                            GUILayout.Height(80));
                }

                var mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                bool lockEditor = GUIEnabled && (editorWindowPos.Contains(mousePos) || (guiPresetsEnabled && presetWindowPos.Contains(mousePos)));

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
            editorWindowPos = config.GetValue<Rect>("editorWinPos");
            presetWindowPos = config.GetValue<Rect>("presetWinPos");
            controlWindowPos = config.GetValue<Rect>("controlWinPos");
            useElectricCharge = config.GetValue<bool>("useEC");
            allowServoFlip = config.GetValue<bool>("allowFlipHack");
        }

        public void SaveConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<ControlsGUI>();
            config.SetValue("editorWinPos", editorWindowPos);
            config.SetValue("presetWinPos", presetWindowPos);
            config.SetValue("controlWinPos", controlWindowPos);
            config.SetValue("useEC", useElectricCharge);
            config.SetValue("allowFlipHack", allowServoFlip);
            config.save();
        }
    }
}