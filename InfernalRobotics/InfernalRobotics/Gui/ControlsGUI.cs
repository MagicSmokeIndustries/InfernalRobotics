using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using InfernalRobotics.Module;
using KSP.IO;
using UnityEngine;
using BinaryReader = System.IO.BinaryReader;
using File = System.IO.File;

namespace InfernalRobotics.Gui
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class ControlsGUI : MonoBehaviour
    {
        private static bool initialGroupEcUpdate;

        protected static Rect ControlWinPos;
        protected static Rect EditorWinPos;
        protected static Rect PresetWinPos;
        protected static int ControlWinID;
        protected static int EditorWinID;
        protected static int PresetWinID;

        protected static bool ResetWin = false;
        protected static Vector2 EditorScroll;
        protected static bool UseElectricCharge = true;
        protected static ControlsGUI GUIController;
        private IButton irMinimizeButton;
        private IButton irMinimizeGroupButton;

        internal List<ControlGroup> ServoGroups; //Changed Scope so draganddrop can use it
        private ApplicationLauncherButton button;
        private bool guiGroupEditorEnabled;
        private bool guiPresetsEnabled;
        private int partCounter;
        private MuMechToggle servoTweak;
        private string tmpMax = "";
        private string tmpMin = "";

        private Texture2D editorBGTex;
        private Texture2D stopButtonIcon; 
        private Texture2D cogButtonIcon;

        private Texture2D expandIcon;
        private Texture2D collapseIcon;
        private Texture2D leftIcon;
        private Texture2D rightIcon;
        private Texture2D leftToggleIcon;
        private Texture2D rightToggleIcon;
        private Texture2D revertIcon;
        private Texture2D downIcon;
        private Texture2D upIcon;
        private Texture2D trashIcon;
        private Texture2D presetsIcon;
        private Texture2D lockedIcon;
        private Texture2D unlockedIcon;
        private Texture2D invertedIcon;
        private Texture2D noninvertedIcon;

        public bool guiPresetMode = false;

        private string tooltipText = "";
        private string lastTooltipText = "";
        private float tooltipTime = 0f;
        private float tooltipMaxTime = 5f;
        private float tooltipDelay = 1f;

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
            string AssemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            //basic constructor to initilize windowIDs
            ControlWinID = UnityEngine.Random.Range(1000, 2000000) + AssemblyName.GetHashCode();
            EditorWinID = ControlWinID + 1;
            PresetWinID = ControlWinID + 2;
        }

        private static void UpdateGroupEcRequirement(ControlGroup servoControlGroup)
        {
            //var ecSum = servoGroup.servos.Select(s => s.ElectricChargeRequired).Sum();
            float ecSum =
                servoControlGroup.Servos.Where(s => s.freeMoving == false).Select(s => s.electricChargeRequired).Sum();
            foreach (MuMechToggle servo in servoControlGroup.Servos)
            {
                servo.GroupElectricChargeRequired = ecSum;
            }
            servoControlGroup.TotalElectricChargeRequirement = ecSum;
        }

        private static void MoveServo(ControlGroup from, ControlGroup to, MuMechToggle servo)
        {
            to.Servos.Add(servo);
            from.Servos.Remove(servo);
            servo.groupName = to.Name;
            servo.forwardKey = to.ForwardKey;
            servo.reverseKey = to.ReverseKey;

            if (UseElectricCharge)
            {
                UpdateGroupEcRequirement(from);
                UpdateGroupEcRequirement(to);
            }
        }

        public static void AddServo(MuMechToggle servo)
        {
            if (!IRGUI)
                return;
            IRGUI.enabled = true;
            
            if (IRGUI.ServoGroups == null)
                IRGUI.ServoGroups = new List<ControlGroup>();

            ControlGroup controlGroup = null;
            
            if (!string.IsNullOrEmpty(servo.groupName))
            {
                foreach (ControlGroup cg in IRGUI.ServoGroups)
                {
                    if (servo.groupName == cg.Name)
                    {
                        controlGroup = cg;
                        break;
                    }
                }
                if (controlGroup == null)
                {
                    var newGroup = new ControlGroup(servo);
                    if (UseElectricCharge)
                    {
                        UpdateGroupEcRequirement(newGroup);
                    }
                    IRGUI.ServoGroups.Add(newGroup);
                    return;
                }
            }
            if (controlGroup == null)
            {
                if (IRGUI.ServoGroups.Count < 1)
                {
                    IRGUI.ServoGroups.Add(new ControlGroup());
                }
                controlGroup = IRGUI.ServoGroups[IRGUI.ServoGroups.Count - 1];
            }

            controlGroup.Servos.Add(servo);
            servo.groupName = controlGroup.Name;
            servo.forwardKey = controlGroup.ForwardKey;
            servo.reverseKey = controlGroup.ReverseKey;

            if (UseElectricCharge)
            {
                UpdateGroupEcRequirement(controlGroup);
            }
        }

        public static void RemoveServo(MuMechToggle servo)
        {
            if (!IRGUI)
                return;

            if (IRGUI.ServoGroups == null)
                return;

            int num = 0;
            foreach (ControlGroup group in IRGUI.ServoGroups)
            {
                if (group.Name == servo.groupName)
                {
                    group.Servos.Remove(servo);

                    if (UseElectricCharge)
                    {
                        UpdateGroupEcRequirement(group);
                    }
                }
                num += group.Servos.Count;
            }
            IRGUI.enabled = num > 0;
        }


        private void OnVesselChange(Vessel v)
        {
            Debug.Log(String.Format("[IR GUI] vessel {0}", v.name));
            ServoGroups = null;
            ResetWin = true;

            var groups = new List<ControlGroup>();
            var groupMap = new Dictionary<string, int>();

            foreach (Part p in v.Parts)
            {
                foreach (MuMechToggle servo in p.Modules.OfType<MuMechToggle>())
                {
                    if (!groupMap.ContainsKey(servo.groupName))
                    {
                        groups.Add(new ControlGroup(servo));
                        groupMap[servo.groupName] = groups.Count - 1;
                    }
                    else
                    {
                        ControlGroup g = groups[groupMap[servo.groupName]];
                        g.Servos.Add(servo);
                    }
                }
            }
            Debug.Log(String.Format("[IR GUI] {0} groups", groups.Count));

            if (groups.Count == 0)
            {
                if (ToolbarManager.ToolbarAvailable)
                {
                    irMinimizeButton.Visible = false;
                    irMinimizeGroupButton.Visible = false;
                }
            }
            if (groups.Count > 0)
            {
                ServoGroups = groups;
                if (ToolbarManager.ToolbarAvailable)
                {
                    irMinimizeButton.Visible = true;
                    irMinimizeGroupButton.Visible = true;
                }

                if (UseElectricCharge)
                {
                    foreach (ControlGroup servoGroup in ServoGroups)
                    {
                        UpdateGroupEcRequirement(servoGroup);
                    }
                }
            }

            foreach (Part p in v.Parts)
            {
                foreach (MuMechToggle servo in p.Modules.OfType<MuMechToggle>())
                {
                    servo.SetupJoints();
                }
            }
        }

        private void OnPartAttach(GameEvents.HostTargetAction<Part, Part> hostTarget)
        {
            Part part = hostTarget.host;
            try
            {
                if (part.Modules.OfType<MuMechToggle>().Any())
                {
                    var temp = part.GetComponentInChildren<MuMechToggle>();
                    Vector3 tempAxis;

                    float orginalAngle;
                    temp.transform.rotation.ToAngleAxis(out orginalAngle, out tempAxis);
                    temp.OriginalAngle = orginalAngle;

                    if (temp.rotateJoint)
                    {
                        temp.OriginalAngle = temp.transform.eulerAngles.x;
                        temp.fixedMeshOriginalLocation = temp.transform.Find("model/" + temp.fixedMesh).eulerAngles;
                    }
                    else if (temp.translateJoint)
                    {
                        temp.OriginalTranslation = temp.transform.localPosition.y;
                    }
                }
            }
            catch
            {
            }


            if ((EditorLogic.fetch.ship.parts.Count >= partCounter) &&
                (EditorLogic.fetch.ship.parts.Count != partCounter))
            {
                if ((partCounter != 1) && (EditorLogic.fetch.ship.parts.Count != 1))
                {
                    foreach (MuMechToggle p in part.GetComponentsInChildren<MuMechToggle>())
                    {
                        AddServo(p);
                    }
                    partCounter = EditorLogic.fetch.ship.parts.Count;
                }
            }
            if ((EditorLogic.fetch.ship.parts.Count == 0) && (partCounter == 0))
            {
                if ((partCounter != 1) && (EditorLogic.fetch.ship.parts.Count != 1))
                {
                    foreach (MuMechToggle p in part.GetComponentsInChildren<MuMechToggle>())
                    {
                        AddServo(p);
                    }
                    partCounter = EditorLogic.fetch.ship.parts.Count;
                }
            }
        }

        private void OnPartRemove(GameEvents.HostTargetAction<Part, Part> hostTarget)
        {
            Part part = hostTarget.target;
            try
            {
                if (part.Modules.OfType<MuMechToggle>().Any())
                {
                    MuMechToggle temp = part.Modules.OfType<MuMechToggle>().First();

                    if (temp.rotateJoint)
                    {
                        if (!temp.part.name.Contains("IR.Rotatron.OffAxis"))
                        {
                            //silly check to prevent base creeping when reaching the limits
                            if (temp.rotation == temp.rotateMax && temp.rotateLimits)
                                temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation - 1);
                            else if (temp.rotation == temp.rotateMin && temp.rotateLimits)
                                temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation + 1);
                            else if (temp.rotation == temp.minTweak && temp.rotateLimits)
                                temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation + 1);
                            else if (temp.rotation == temp.maxTweak && temp.rotateLimits)
                                temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation - 1);
                            else
                                temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation);

                            temp.rotation = 0;
                        }
                    }
                    else if (temp.translateJoint)
                    {
                        //temp.part.transform.Find("model/" + temp.fixedMesh).position = temp.part.transform.position;
                        temp.FixedMeshTransform.position = temp.part.transform.position;
                        temp.translation = 0;
                    }
                }
            }
            catch
            {
            }


            foreach (MuMechToggle p in part.GetComponentsInChildren<MuMechToggle>())
            {
                RemoveServo(p);
            }
            partCounter = EditorLogic.fetch.ship.parts.Count == 1 ? 0 : EditorLogic.fetch.ship.parts.Count;

            if (part.Modules.OfType<MuMechToggle>().Any())
            {
                MuMechToggle temp1 = part.Modules.OfType<MuMechToggle>().First();
                if (temp1.part.name.Contains("IR.Rotatron.OffAxis"))
                {
                    temp1.rotation = 0;
                    //temp1.transform.Find("model/" + temp1.fixedMesh).eulerAngles = temp1.transform.eulerAngles;
                    temp1.FixedMeshTransform.eulerAngles = temp1.transform.eulerAngles;
                }
            }
        }

        private void OnEditorShipModified(ShipConstruct ship)
        {
            ServoGroups = null;
            
            var groups = new List<ControlGroup>();
            var groupMap = new Dictionary<string, int>();

            foreach (Part p in ship.Parts)
            {
                foreach (MuMechToggle servo in p.Modules.OfType<MuMechToggle>())
                {
                    if (!groupMap.ContainsKey(servo.groupName))
                    {
                        groups.Add(new ControlGroup(servo));
                        groupMap[servo.groupName] = groups.Count - 1;
                    }
                    else
                    {
                        ControlGroup g = groups[groupMap[servo.groupName]];
                        g.Servos.Add(servo);
                    }
                }
            }

            if (groups.Count == 0)
            {
                if (ToolbarManager.ToolbarAvailable)
                {
                    irMinimizeButton.Visible = false;
                    irMinimizeGroupButton.Visible = false;
                }
            }
            if (groups.Count > 0)
            {
                ServoGroups = groups;
                if (ToolbarManager.ToolbarAvailable)
                {
                    irMinimizeButton.Visible = true;
                    irMinimizeGroupButton.Visible = true;
                }

                if (UseElectricCharge)
                {
                    foreach (ControlGroup servoGroup in ServoGroups)
                    {
                        UpdateGroupEcRequirement(servoGroup);
                    }
                }
            }

            partCounter = EditorLogic.fetch.ship.parts.Count == 1 ? 0 : EditorLogic.fetch.ship.parts.Count;
        }

        /// <summary>
        ///     Load the textures from files to memory
        /// </summary>
        private void InitTextures()
        {
            stopButtonIcon = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            GUIDragAndDrop.LoadImageFromFile (stopButtonIcon, "icon_stop.png");

            cogButtonIcon = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (cogButtonIcon, "icon_cog.png");

            expandIcon  = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (expandIcon, "expand.png");

            collapseIcon  = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (collapseIcon, "collapse.png");

            leftIcon  = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (leftIcon, "left.png");

            rightIcon  = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (rightIcon, "right.png");

            leftToggleIcon  = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (leftToggleIcon, "left_toggle.png");

            rightToggleIcon  = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (rightToggleIcon, "right_toggle.png");

            revertIcon  = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (revertIcon, "revert.png");

            downIcon  = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (downIcon, "down.png");

            upIcon  = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (upIcon, "up.png");

            trashIcon  = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (trashIcon, "trash.png");

            presetsIcon  = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (presetsIcon, "presets.png");

            lockedIcon  = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (lockedIcon, "locked.png");

            unlockedIcon  = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (unlockedIcon, "unlocked.png");

            invertedIcon  = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (invertedIcon, "inverted.png");

            noninvertedIcon  = new Texture2D(32, 32, TextureFormat.ARGB32, false);
            GUIDragAndDrop.LoadImageFromFile (noninvertedIcon, "noninverted.png");
        }

        private void Awake()
        {
            LoadConfigXml();

            Debug.Log("[IR GUI] awake, ControlWinID = " + ControlWinID);
            
            GUIEnabled = false;
            
            guiGroupEditorEnabled = false;

            editorBGTex = CreateTextureFromColor(1, 1, new Color32(81, 86, 94, 255));

            InitTextures ();

            GameScenes scene = HighLogic.LoadedScene;

            if (scene == GameScenes.FLIGHT)
            {
                GameEvents.onVesselChange.Add(OnVesselChange);
                GameEvents.onVesselWasModified.Add(OnVesselWasModified);
                GUIController = this;
            }
            else if (scene == GameScenes.EDITOR)
            {
                //partCounter = EditorLogic.fetch.ship.parts.Count;    
                GameEvents.onPartAttach.Add(OnPartAttach);
                GameEvents.onPartRemove.Add(OnPartRemove);
                GameEvents.onEditorShipModified.Add(OnEditorShipModified);
                GUIController = this;
            }
            else
            {
                GUIController = null;
            }

            if (ToolbarManager.ToolbarAvailable)
            {
                irMinimizeButton = ToolbarManager.Instance.add("sirkut", "IREditorButton");
                irMinimizeButton.TexturePath = "MagicSmokeIndustries/Textures/icon_button";
                irMinimizeButton.ToolTip = "Infernal Robotics";
                irMinimizeButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.FLIGHT);
                irMinimizeButton.OnClick += e => GUIEnabled = !GUIEnabled;

                irMinimizeGroupButton = ToolbarManager.Instance.add("sirkut2", "IREditorGroupButton");
                irMinimizeGroupButton.TexturePath = "MagicSmokeIndustries/Textures/icon_buttonGROUP";
                irMinimizeGroupButton.ToolTip = "Infernal Robotics Group Editor";
                irMinimizeGroupButton.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
                irMinimizeGroupButton.OnClick += e => guiGroupEditorEnabled = !guiGroupEditorEnabled;
            }
            else
            {
                GameEvents.onGUIApplicationLauncherReady.Add(OnAppReady);
            }

            initialGroupEcUpdate = false;
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
                }
                catch (Exception Ex)
                {
                    Debug.LogError(String.Format("[IR GUI OnnAppReady Exception, {0}", Ex.Message));
                }
            }
        }


        private void OnVesselWasModified(Vessel v)
        {
            if (v == FlightGlobals.ActiveVessel)
            {
                ServoGroups = null;

                OnVesselChange(v);
            }
        }

        private void OnDestroy()
        {
            Debug.Log("[IR GUI] destroy");
            GameEvents.onVesselChange.Remove(OnVesselChange);
            GameEvents.onPartAttach.Remove(OnPartAttach);
            GameEvents.onPartRemove.Remove(OnPartRemove);
            GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
            GameEvents.onEditorShipModified.Remove(OnEditorShipModified);
            if (ToolbarManager.ToolbarAvailable)
            {
                irMinimizeButton.Destroy();
                irMinimizeGroupButton.Destroy();
            }
            else
            {
                GameEvents.onGUIApplicationLauncherReady.Remove(OnAppReady);
                if (button != null)
                {
                    ApplicationLauncher.Instance.RemoveModApplication(button);
                    button = null;
                }
            }
            EditorLock(false);
            SaveConfigXml();
        }

        protected bool KeyPressed(string key)
        {
            return (key != "" && InputLockManager.IsUnlocked(ControlTypes.LINEAR) && Input.GetKey(key));
        }

        //servo control window used in flight
        private void ControlWindow(int windowID)
        {
            GUILayoutOption width20 = GUILayout.Width(20);

            GUILayout.BeginVertical();

            int buttonHeight = 22;

            var buttonStyle = new GUIStyle(GUI.skin.button);

            var padding2px = new RectOffset(2, 2, 2, 2);

            buttonStyle.padding = padding2px;
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            foreach (ControlGroup g in ServoGroups)
            {
                if (g.Servos.Any())
                {
                    GUILayout.BeginHorizontal();

                    if (g.Expanded)
                    {
                        g.Expanded = !GUILayout.Button(collapseIcon, buttonStyle, width20, GUILayout.Height(buttonHeight));
                    }
                    else
                    {
                        g.Expanded = GUILayout.Button(expandIcon, buttonStyle, width20, GUILayout.Height(buttonHeight));
                    }

                    //overload default GUIStyle with bold font
                    var t = new GUIStyle(GUI.skin.label.name);
                    t.fontStyle = FontStyle.Bold;

                    GUILayout.Label(g.Name, t, GUILayout.ExpandWidth(true), GUILayout.Height(buttonHeight));

                    g.Speed = GUILayout.TextField(g.Speed, GUILayout.Width(30), GUILayout.Height(buttonHeight));

                    float speed;
                    bool speedOk = float.TryParse(g.Speed, out speed);

                    foreach (MuMechToggle servo in g.Servos)
                    {
                        servo.reverseKey = g.ReverseKey;
                        servo.forwardKey = g.ForwardKey;
                        if (speedOk)
                        {
                            servo.customSpeed = speed;
                        }
                    }

                    var toggleVal = false;

                    toggleVal = GUILayout.Toggle(g.MovingNegative, new GUIContent(leftToggleIcon, "Toggle Move -"), buttonStyle, 
                                                            GUILayout.Width(28), GUILayout.Height(buttonHeight));
                    SetTooltipText();

                    if (g.MovingNegative != toggleVal)
                    {
                        if (!toggleVal) g.Stop();
                        g.MovingNegative = toggleVal;
                    }

                    if (g.MovingNegative)
                    {
                        g.MovingPositive = false;
                        g.MoveNegative ();
                    }

                    if (guiPresetMode)
                    {
                        if (GUILayout.Button (new GUIContent(leftIcon, "Previous Preset"), buttonStyle, GUILayout.Width (22), GUILayout.Height (buttonHeight))) 
                        {
                            //reset any group toggles
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MovePrevPreset ();

                        }
                        SetTooltipText();
                        
                        if (GUILayout.Button (new GUIContent(rightIcon, "Next Preset"), buttonStyle, GUILayout.Width (22), GUILayout.Height (buttonHeight))) 
                        {
                            //reset any group toggles
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MoveNextPreset ();

                        }
                        SetTooltipText();
                    }
                    else
                    {
                        if (GUILayout.RepeatButton (new GUIContent(leftIcon, "Hold to Move-"), buttonStyle, GUILayout.Width (22), GUILayout.Height (buttonHeight))) 
                        {
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MoveNegative ();

                            g.ButtonDown = true;
                        }
                        
                        SetTooltipText();


                        if (GUILayout.RepeatButton (new GUIContent(revertIcon, "Hold to Center"), buttonStyle, GUILayout.Width (22), GUILayout.Height (buttonHeight))) 
                        {
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MoveCenter ();

                            g.ButtonDown = true;
                        }
                        SetTooltipText();

                        if (GUILayout.RepeatButton (new GUIContent(rightIcon, "Hold to Move+"), buttonStyle, GUILayout.Width (22), GUILayout.Height (buttonHeight))) 
                        {
                            g.MovingNegative = false;
                            g.MovingPositive = false;

                            g.MovePositive ();

                            g.ButtonDown = true;
                        }
                        SetTooltipText();
                    }

                    toggleVal = GUILayout.Toggle(g.MovingPositive, new GUIContent(rightToggleIcon, "Toggle Move+"), buttonStyle, 
                                                            GUILayout.Width(28), GUILayout.Height(buttonHeight));
                    SetTooltipText();

                    if (g.MovingPositive != toggleVal)
                    {
                        if (!toggleVal) g.Stop();
                        g.MovingPositive = toggleVal;
                    }

                    if (g.MovingPositive)
                    {
                        g.MovingNegative = false;
                        g.MovePositive ();
                    }

                    GUILayout.EndHorizontal();

                    if (g.Expanded)
                    {
                        foreach (MuMechToggle servo in g.Servos)
                        {
                            GUILayout.BeginHorizontal();

                            t.richText = true;
                            t.alignment = TextAnchor.MiddleCenter;

                            string servoStatus = servo.Translator.IsMoving()? "<color=lime>•</color>" : "<color=yellow>•</color>";

                            if (servo.isMotionLock)
                                servoStatus = "<color=red>•</color>";
                            
                            GUILayout.Label(servoStatus,t, GUILayout.Width(18), GUILayout.Height(buttonHeight));

                            GUILayout.Label(servo.servoName, GUILayout.ExpandWidth(true), GUILayout.Height(buttonHeight));

                            t.fontStyle = FontStyle.Italic;
                            t.alignment = TextAnchor.MiddleCenter;

                            if (servo.rotateJoint)
                            {
                                GUILayout.Label(string.Format("{0:#0.##}", servo.rotation), t, GUILayout.Width(45), GUILayout.Height(buttonHeight));
                            }
                            else
                            {
                                GUILayout.Label(string.Format("{0:#0.##}", servo.translation), t, GUILayout.Width(45), GUILayout.Height(buttonHeight));
                            }

                            bool servoLocked = servo.isMotionLock;
                            servoLocked = GUILayout.Toggle(servoLocked, 
                                            servoLocked ? new GUIContent(lockedIcon, "Unlock Servo") : new GUIContent(unlockedIcon, "Lock Servo"), 
                                            buttonStyle, GUILayout.Width(28), GUILayout.Height(buttonHeight));
                            servo.SetLock (servoLocked);

                            SetTooltipText ();

                            if (guiPresetMode) 
                            {
                                if (GUILayout.Button (new GUIContent(leftIcon, "Previous Preset"), buttonStyle, GUILayout.Width (22), GUILayout.Height (buttonHeight))) 
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;
                                    
                                    servo.MovePrevPreset ();

                                }
                                SetTooltipText ();
                                
                                if (GUILayout.Button (new GUIContent(rightIcon, "Next Preset"), buttonStyle, GUILayout.Width (22), GUILayout.Height (buttonHeight))) 
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;

                                    servo.MoveNextPreset ();

                                }
                                SetTooltipText ();
                            }
                            else 
                            {
                                if (GUILayout.RepeatButton(new GUIContent(leftIcon, "Hold to Move-"), buttonStyle, GUILayout.Width(22), GUILayout.Height(buttonHeight))) 
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;
                                    g.ButtonDown = true;

                                    servo.Translator.Move (float.NegativeInfinity, servo.customSpeed * servo.speedTweak);
                                }
                                SetTooltipText();

                                if (GUILayout.RepeatButton(new GUIContent(revertIcon, "Hold to Center"), buttonStyle, GUILayout.Width(22), GUILayout.Height(buttonHeight))) 
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;
                                    g.ButtonDown = true;

                                    servo.Translator.Move (0f, servo.customSpeed * servo.speedTweak);
                                }
                                SetTooltipText();

                                if (GUILayout.RepeatButton(new GUIContent(rightIcon, "Hold to Move+"), buttonStyle, GUILayout.Width(22), GUILayout.Height(buttonHeight))) 
                                {
                                    //reset any group toggles
                                    g.MovingNegative = false;
                                    g.MovingPositive = false;
                                    g.ButtonDown = true;

                                    servo.Translator.Move (float.PositiveInfinity, servo.customSpeed * servo.speedTweak);
                                }
                                SetTooltipText();
                            }
                            bool servoInverted = servo.invertAxis;

                            servoInverted = GUILayout.Toggle(servoInverted, 
                                servoInverted ? new GUIContent(invertedIcon, "Revert Axis") : new GUIContent(noninvertedIcon, "Invert Axis"), 
                                buttonStyle, GUILayout.Width(28), GUILayout.Height(buttonHeight));
                            
                            SetTooltipText ();

                            if (servo.invertAxis != servoInverted)
                                servo.InvertAxisToggle ();

                            GUILayout.EndHorizontal();
                        }
                    }

                    if (g.ButtonDown && Input.GetMouseButtonUp(0))
                    {
                        //one of the repeat buttons in the group was pressed, but now mouse button is up
                        g.ButtonDown = false;
                        g.Stop();
                    }
                }
            }

            GUILayout.BeginHorizontal (GUILayout.Height(32));

            if (ToolbarManager.ToolbarAvailable)
            {
                if (GUILayout.Button("Close", GUILayout.Height(32)))
                {
                    SaveConfigXml();
                    GUIEnabled = false;
                }
            }
            else
            {
                if (GUILayout.Button(guiGroupEditorEnabled ? "Close Edit" : "Edit Groups", GUILayout.Height(32)))
                {
                    guiGroupEditorEnabled = !guiGroupEditorEnabled;
                }
            }

            guiPresetMode = GUILayout.Toggle(guiPresetMode, new GUIContent(presetsIcon, "Preset Mode"), buttonStyle, 
                GUILayout.Width(32), GUILayout.Height(32));
            SetTooltipText();

            if (GUILayout.Button (new GUIContent(stopButtonIcon, "Emergency Stop"), GUILayout.Width (32), GUILayout.Height (32))) 
            {
                foreach (ControlGroup g in ServoGroups) 
                {
                    g.Stop ();
                }
            }
            SetTooltipText();

            GUILayout.EndHorizontal ();

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
            if (tooltipText!="" && tooltipTime < tooltipMaxTime)
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
                
                var tooltipPos = new Rect(pos.x-(size.x/4), pos.y + 17, size.x, size.y);
                
                if (tooltipText != lastTooltipText)
                {
                    //reset timer
                    tooltipTime = 0f;
                }

                if (tooltipTime > tooltipDelay)
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
        /// Creates the solid texture of given size and Color.
        /// </summary>
        /// <returns>The texture from color.</returns>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        /// <param name="col">Color</param>
        private static Texture2D CreateTextureFromColor(int width, int height, Color col)
        {
            Color[] pix = new Color[width*height];

            for(int i = 0; i < pix.Length; i++)
                pix[i] = col;

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();

            return result;
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

            var cogButtonStyle = new GUIStyle(GUI.skin.button);

            cogButtonStyle.padding = new RectOffset(3, 3, 3, 3);

            bool isEditor = (HighLogic.LoadedScene == GameScenes.EDITOR);

            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;

            EditorScroll = GUILayout.BeginScrollView(EditorScroll, false, false, maxHeight);

            //Kick off the window code
            GUIDragAndDrop.WindowBegin(EditorWinPos, EditorScroll);

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
                GUILayout.Label ("EC/s", GUILayout.Width(33), rowHeight);
            }

            //make room for remove button
            if (ServoGroups.Count > 1)
            {
                GUILayout.Space(45);
            }

            var alternateBG = new GUIStyle ("label");

            alternateBG.normal.background = editorBGTex;

            GUILayout.EndHorizontal();

            for (int i = 0; i < ServoGroups.Count; i++)
            {
                ControlGroup grp = ServoGroups[i];

                GUILayout.BeginHorizontal();

                //Call the Add Group Handle code
                GUIDragAndDrop.DrawGroupHandle(grp.Name, i);

                string tmp = GUILayout.TextField(grp.Name, expand, rowHeight);

                if (grp.Name != tmp)
                {
                    grp.Name = tmp;
                }

                if (isEditor) 
                {
                    grp.Expanded = GUILayout.Toggle (grp.Expanded, new GUIContent(cogButtonIcon,"Adv. settings"), cogButtonStyle, GUILayout.Width (22), rowHeight);
                    SetTooltipText();
                }
                //<-keys->
                tmp = GUILayout.TextField(grp.ForwardKey, GUILayout.Width(20), rowHeight);
                if (grp.ForwardKey != tmp)
                {
                    grp.ForwardKey = tmp;
                }
                tmp = GUILayout.TextField(grp.ReverseKey, GUILayout.Width(20), rowHeight);
                if (grp.ReverseKey != tmp)
                {
                    grp.ReverseKey = tmp;
                }

                if (isEditor)
                {
                    if (GUILayout.RepeatButton(new GUIContent(leftIcon, "Hold to Move-"), buttonStyle, GUILayout.Width(22), rowHeight))
                    {
                        foreach (MuMechToggle servo in grp.Servos)
                        {
                            servo.MoveLeft();
                        }
                    }
                    SetTooltipText();

                    if (GUILayout.RepeatButton(new GUIContent(rightIcon, "Hold to Move+"), buttonStyle, GUILayout.Width(22), rowHeight))
                    {
                        foreach (MuMechToggle servo in grp.Servos)
                        {
                            servo.MoveRight();
                        }
                    }
                    SetTooltipText();
                }

                if (UseElectricCharge)
                {
                    UpdateGroupEcRequirement(grp);
                    var t = new GUIStyle(GUI.skin.label.name);
                    t.alignment = TextAnchor.MiddleCenter;
                    GUILayout.Label((string)grp.TotalElectricChargeRequirement.ToString(), t, GUILayout.Width(33), rowHeight);
                }

                if (i > 0)
                {
                    GUILayout.Space (5);
                    if (GUILayout.Button(new GUIContent(trashIcon, "Delete Group"), buttonStyle, GUILayout.Width(30), rowHeight))
                    {
                        foreach (MuMechToggle servo in grp.Servos)
                        {
                            MoveServo(grp, ServoGroups[i - 1], servo);
                        }
                        ServoGroups.RemoveAt(i);
                        ResetWin = true;
                        return;
                    }
                    SetTooltipText();
                    GUILayout.Space (5);
                }
                else
                {
                    if (ServoGroups.Count > 1)
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

                GUILayout.Space (25);

                GUILayout.Label("Pos.", GUILayout.Width(30), rowHeight);

                if (isEditor)
                    GUILayout.Label("Move", GUILayout.Width(45), rowHeight);

                GUILayout.Label("Group", GUILayout.Width(45), rowHeight);

                GUILayout.EndHorizontal();

                for (int iS = 0; iS < grp.Servos.Count; iS++)
                {
                    MuMechToggle servo = grp.Servos[iS];
                    //if (!servo.freeMoving)
                    {
                        GUILayout.BeginHorizontal ();

                        //Call the Add Servo Handle code
                        GUIDragAndDrop.DrawServoHandle(servo.servoName, i, iS);

                        if (isEditor)
                        {
                            GUILayout.BeginVertical();
                            GUILayout.BeginHorizontal();
                        }

                        servo.servoName = GUILayout.TextField(servo.servoName, expand, rowHeight);

                        servo.groupName = grp.Name;
                        servo.reverseKey = grp.ReverseKey;
                        servo.forwardKey = grp.ForwardKey;
                        servo.RefreshKeys();

                        if (EditorWinPos.Contains(mousePos))
                        {
                            Rect last = GUILayoutUtility.GetLastRect();
                            Vector2 pos = Event.current.mousePosition;
                            bool highlight = last.Contains(pos);
                            servo.part.SetHighlight(highlight, false);
                        }

                        if (GUILayout.Button(new GUIContent(presetsIcon, "Edit Presets"), cogButtonStyle, GUILayout.Width(22), rowHeight))
                        {
                            servoTweak = servo;
                            guiPresetsEnabled = !guiPresetsEnabled;
                        }
                        SetTooltipText();

                        GUILayout.Label(string.Format("{0:#0.##}", servo.Interpolator.Position), GUILayout.Width(30), rowHeight);

                        float tmpValue;

                        //individual servo movement when in editor
                        if (isEditor) 
                        {
                            if (GUILayout.RepeatButton (new GUIContent(leftIcon, "Hold to Move-"), buttonStyle, GUILayout.Width (22), rowHeight)) 
                            {
                                servo.MoveLeft ();
                            }
                            SetTooltipText();

                            if (GUILayout.RepeatButton (new GUIContent(rightIcon, "Hold to Move+"), buttonStyle, GUILayout.Width (22), rowHeight)) 
                            {
                                servo.MoveRight ();
                            }
                            SetTooltipText();
                        }

                        if (grp.Expanded && isEditor) 
                        {
                            GUILayout.EndHorizontal ();
                            GUILayout.BeginHorizontal ();

                            GUILayout.Label("Range: ", GUILayout.Width(40), rowHeight);
                            tmpMin = GUILayout.TextField(string.Format("{0:#0.0#}",servo.minTweak), GUILayout.Width(40), rowHeight);
                            if (float.TryParse(tmpMin, out tmpValue))
                            {
                                servo.minTweak = tmpValue;
                            }

                            tmpMax = GUILayout.TextField(string.Format("{0:#0.0#}",servo.maxTweak), GUILayout.Width(40), rowHeight);
                            if (float.TryParse(tmpMax, out tmpValue))
                            {
                                servo.maxTweak = tmpValue;
                            }

                            GUILayout.Label("Spd: ", GUILayout.Width(30), rowHeight);
                            tmpMin = GUILayout.TextField(string.Format("{0:#0.0##}",servo.speedTweak), GUILayout.Width(40), rowHeight);
                            if (float.TryParse(tmpMin, out tmpValue))
                            {
                                servo.speedTweak = tmpValue;
                            }

                            GUILayout.Label("Acc: ", GUILayout.Width(30), rowHeight);
                            tmpMin = GUILayout.TextField(string.Format("{0:#0.0##}",servo.accelTweak), GUILayout.Width(40), rowHeight);
                            if (float.TryParse(tmpMin, out tmpValue))
                            {
                                servo.accelTweak = tmpValue;
                            }
                        }

                        if(isEditor)
                        {
                            GUILayout.EndHorizontal();
                            GUILayout.EndVertical();
                        }

                        if (ServoGroups.Count > 1)
                        {
                            buttonStyle.padding = padding1px;

                            if (i > 0)
                            {
                                if (GUILayout.Button(new GUIContent(upIcon,"To previous Group"), buttonStyle, GUILayout.Width(20), rowHeight))
                                {
                                    MoveServo(grp, ServoGroups[i - 1], servo);
                                }
                                SetTooltipText();
                            }
                            else
                            {
                                GUILayout.Space(22);
                            }
                            if (i < (ServoGroups.Count - 1))
                            {
                                if (GUILayout.Button(new GUIContent(downIcon, "To next Group"), buttonStyle, GUILayout.Width(20), rowHeight))
                                {
                                    MoveServo(grp, ServoGroups[i + 1], servo);
                                }
                                SetTooltipText();
                            }
                            else
                            {
                                GUILayout.Space(22);
                            }

                            buttonStyle.padding = padding2px;

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
                var temp = new ControlGroup {Name = string.Format("New Group {0}", (ServoGroups.Count + 1))};
                ServoGroups.Add(temp);
            }

            GUILayout.EndVertical();

            GUILayout.EndScrollView();

            //Was gonna add a footer so you can drag resize the window and have the option to turn on dragging control
            //GUILayout.BeginHorizontal();
            //zTriggerTweaks.DragOn = GUILayout.Toggle(zTriggerTweaks.DragOn,new GUIContent(GameDatabase.Instance.GetTexture("MagicSmokeIndustries/Textures/icon_drag",false)));
            //GUILayout.EndHorizontal();

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
            string tmp;
            float tmpValue;

            var buttonStyle = new GUIStyle(GUI.skin.button);
            //var padding1px = new RectOffset(1, 1, 1, 1);
            var padding2px = new RectOffset(2, 2, 2, 2);
            
            GUILayoutOption rowHeight = GUILayout.Height(22);
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Preset position", GUILayout.ExpandWidth(true), rowHeight);
            if (GUILayout.Button("Add", buttonStyle, GUILayout.Width(30), rowHeight))
            {
                servoTweak.PresetPositions.Add(0f);
            }
            GUILayout.EndHorizontal();

            buttonStyle.padding = padding2px;
            buttonStyle.alignment = TextAnchor.MiddleCenter;

            for (int i = 0; i < servoTweak.PresetPositions.Count; i++)
            {
                GUILayout.BeginHorizontal();

                tmp = GUILayout.TextField(string.Format("{0:#0.0#}", servoTweak.PresetPositions[i]), GUILayout.ExpandWidth(true), rowHeight);

                if (float.TryParse(tmp, out tmpValue))
                {
                    servoTweak.PresetPositions[i] = Mathf.Clamp(tmpValue, servoTweak.minTweak, servoTweak.maxTweak);
                }

                if (GUILayout.Button(new GUIContent(trashIcon, "Delete preset"), buttonStyle, GUILayout.Width(30), rowHeight))
                {
                    servoTweak.PresetPositions.RemoveAt(i);
                }
                SetTooltipText();
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Apply Symmetry", buttonStyle))
            {
                servoTweak.PresetPositions.Sort();
                servoTweak.presetPositionsSerialized = servoTweak.SerializePresets();

                if (servoTweak.part.symmetryCounterparts.Count > 1)
                {
                    foreach (Part part in servoTweak.part.symmetryCounterparts)
                    {
                        ((MuMechToggle)part.Modules["MuMechToggle"]).presetPositionsSerialized = servoTweak.presetPositionsSerialized;
                        ((MuMechToggle)part.Modules["MuMechToggle"]).ParsePresetPositions();
                    }
                }
            }

            if (GUILayout.Button("Save&Exit", buttonStyle, GUILayout.Width(70)))
            {
                servoTweak.PresetPositions.Sort();
                servoTweak.presetPositionsSerialized = servoTweak.SerializePresets();
                guiPresetsEnabled = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        
        private void RefreshKeysFromGUI()
        {
            foreach (ControlGroup g in ServoGroups)
            {
                if (g.Servos.Any())
                {
                    foreach (MuMechToggle servo in g.Servos)
                    {
                        servo.reverseKey = g.ReverseKey;
                        servo.forwardKey = g.ForwardKey;
                        servo.RefreshKeys();
                    }
                }
            }
        }

        private void OnGUI()
        {
            // This particular test isn't needed due to the GUI being enabled
            // and disabled as appropriate, but it saves potential NREs.

            if (ServoGroups == null)
                return;
            
            //what is that for?
            //if (InputLockManager.IsLocked(ControlTypes.LINEAR)) return;

            if (UseElectricCharge)
            {
                if (!initialGroupEcUpdate)
                {
                    foreach (ControlGroup servoGroup in ServoGroups)
                    {
                        UpdateGroupEcRequirement(servoGroup);
                    }
                    initialGroupEcUpdate = true;
                }
            }

            if (ControlWinPos.x == 0 && ControlWinPos.y == 0)
            {
                ControlWinPos = new Rect(Screen.width - 510, 70, 10, 10);
            }
            if (EditorWinPos.x == 0 && EditorWinPos.y == 0)
            {
                EditorWinPos = new Rect(Screen.width - 260, 50, 10, 10);
            }

            if (PresetWinPos.x == 0 && PresetWinPos.y == 0)
            {
                PresetWinPos = new Rect(Screen.width - 410, 220, 145, 130);
            }

            if (ResetWin)
            {
                ControlWinPos = new Rect(ControlWinPos.x, ControlWinPos.y, 10, 10);
                EditorWinPos = new Rect(EditorWinPos.x, EditorWinPos.y, 10, 10);
                PresetWinPos = new Rect(PresetWinPos.x, PresetWinPos.y, 10, 10);
                ResetWin = false;
            }
            GUI.skin = DefaultSkinProvider.DefaultSkin;
            GameScenes scene = HighLogic.LoadedScene;

            //Call the DragAndDrop GUI Setup stuff
            GUIDragAndDrop.OnGUIOnceOnly();

            if (scene == GameScenes.FLIGHT)
            {
                GUILayoutOption height = GUILayout.Height(Screen.height/2f);
                if (GUIEnabled)
                    //{
                    ControlWinPos = GUILayout.Window(ControlWinID, ControlWinPos,
                        ControlWindow,
                        "Servo Control",
                        GUILayout.Width(ControlWindowWidth),
                        GUILayout.Height(80));
                if (guiGroupEditorEnabled)
                    EditorWinPos = GUILayout.Window(EditorWinID, EditorWinPos,
                        EditorWindow,
                        "Servo Group Editor",
                        GUILayout.Width(EditorWindowWidth), //Using a variable here
                        height);
                if (guiPresetsEnabled)
                    PresetWinPos = GUILayout.Window(PresetWinID, PresetWinPos,
                        PresetsEditWindow,
                        servoTweak.servoName,
                        GUILayout.Width(200),
                        GUILayout.Height(80));
                //}
                RefreshKeysFromGUI();
            }
            else if (scene == GameScenes.EDITOR)
            {
                GUILayoutOption height = GUILayout.Height(Screen.height/2f);

                if (GUIEnabled)
                    EditorWinPos = GUILayout.Window(958, EditorWinPos,
                        EditorWindow,
                        "Servo Configuration",
                        GUILayout.Width(EditorWindowWidth), //Using a variable here
                        height);
                if (guiPresetsEnabled)
                    PresetWinPos = GUILayout.Window(960, PresetWinPos,
                        PresetsEditWindow,
                        servoTweak.servoName,
                        GUILayout.Width(200),
                        GUILayout.Height(80));


                var mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                bool lockEditor = GUIEnabled && (EditorWinPos.Contains(mousePos) || PresetWinPos.Contains(mousePos));

                EditorLock(lockEditor);

            }

            GUIDragAndDrop.OnGUIEvery();
            DrawTooltip ();

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
#if DEBUG
                    Debug.Log(String.Format("[IR GUI] AddingLock-{0}", "IRGUILockOfEditor"));
#endif
                    InputLockManager.SetControlLock(ControlTypes.EDITOR_LOCK, "IRGUILockOfEditor");
                }
            }
                //Otherwise make sure the lock is removed
            else
            {
                //Only try and remove it if there was one there in the first place
                if (InputLockManager.GetControlLock("IRGUILockOfEditor") == ControlTypes.EDITOR_LOCK)
                {
#if DEBUG
                    Debug.Log(String.Format("[IR GUI] Removing-{0}", "IRGUILockOfEditor"));
#endif
                    InputLockManager.RemoveControlLock("IRGUILockOfEditor");
                }
            }
        }

        public void LoadConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<ControlsGUI>();
            config.load();
            EditorWinPos = config.GetValue<Rect>("editorWinPos");
            PresetWinPos = config.GetValue<Rect>("presetWinPos");
            ControlWinPos = config.GetValue<Rect>("controlWinPos");
            UseElectricCharge = config.GetValue<bool>("useEC");
        }

        public void SaveConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<ControlsGUI>();
            config.SetValue("editorWinPos", EditorWinPos);
            config.SetValue("presetWinPos", PresetWinPos);
            config.SetValue("controlWinPos", ControlWinPos);
            config.SetValue("useEC", UseElectricCharge);
            config.save();
        }

        public class ControlGroup
        {
            public ControlGroup(MuMechToggle servo)
            {
                Expanded = false;
                Name = servo.groupName;
                ForwardKey = servo.forwardKey;
                ReverseKey = servo.reverseKey;
                Speed = servo.customSpeed.ToString("g");
                Servos = new List<MuMechToggle>();
                ShowGUI = servo.showGUI;
                Servos.Add(servo);
                MovingNegative = false;
                MovingPositive = false;
                ButtonDown = false;
            }

            public ControlGroup()
            {
                Expanded = false;
                Name = "New Group";
                ForwardKey = string.Empty;
                ReverseKey = string.Empty;
                Speed = "1";
                ShowGUI = true;
                Servos = new List<MuMechToggle>();
                MovingNegative = false;
                MovingPositive = false;
                ButtonDown = false;
            }

            public bool Expanded { get; set; }
            public string Name { get; set; }
            public List<MuMechToggle> Servos { get; set; }
            public string ForwardKey { get; set; }
            public string ReverseKey { get; set; }
            public string Speed { get; set; }
            public bool ShowGUI { get; set; }
            public float TotalElectricChargeRequirement { get; set; }
            public bool MovingNegative { get; set;}
            public bool MovingPositive { get; set;}
            public bool ButtonDown { get; set; }

            public void MovePositive()
            {
                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.Translator.Move (float.PositiveInfinity, servo.customSpeed*servo.speedTweak);
                    }
                }
            }

            public void MoveNegative()
            {
                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.Translator.Move (float.NegativeInfinity, servo.customSpeed*servo.speedTweak);
                    }
                }
            }

            public void MoveCenter()
            {
                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.Translator.Move (0f, servo.customSpeed*servo.speedTweak); //TODO: to be precise this should be not Zero but a default rotation/translation as set in VAB/SPH
                    }
                }
            }

            public void MoveNextPreset()
            {
                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.MoveNextPreset();
                    }
                }
            }

            public void MovePrevPreset()
            {
                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.MovePrevPreset();
                    }
                }
            }


            public void Stop()
            {
                MovingNegative = false;
                MovingPositive = false;

                if (Servos.Any())
                {
                    foreach (MuMechToggle servo in Servos)
                    {
                        servo.Translator.Stop();
                    }
                }
            }
        }
    }
}