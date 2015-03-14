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
        protected static Rect GroupEditorWinPos;
        protected static Rect TweakWinPos;
        protected static bool ResetWin = false;
        protected static Vector2 EditorScroll;
        protected static bool UseElectricCharge = true;
        protected static ControlsGUI GUIController;
        private IButton irMinimizeButton;
        private IButton irMinimizeGroupButton;

        internal List<ControlGroup> ServoGroups; //Changed Scope so draganddrop can use it
        private ApplicationLauncherButton button;
        private bool groupEditorEnabled;
        private bool guiTweakEnabled;
        private int partCounter;
        private MuMechToggle servoTweak;
        private string tmpMax = "";
        private string tmpMin = "";
        private bool update14to15;

        #region UITweaks

        //New sizes for a couple of things
        internal static Int32 EditorWidth = 332;
        internal static Int32 EditorButtonHeights = 25;

        #endregion

        public bool GUIEnabled { get; set; }

        public static ControlsGUI GUI
        {
            get { return GUIController; }
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

        private static void move_servo(ControlGroup from, ControlGroup to, MuMechToggle servo)
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
            if (!GUI)
                return;
            GUI.enabled = true;
            if (!string.IsNullOrEmpty(servo.part.customPartData))
            {
                servo.ParseCData();
            }
            if (GUI.ServoGroups == null)
                GUI.ServoGroups = new List<ControlGroup>();
            ControlGroup controlGroup = null;
            if (!string.IsNullOrEmpty(servo.groupName))
            {
                foreach (ControlGroup cg in GUI.ServoGroups)
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
                    GUI.ServoGroups.Add(newGroup);
                    return;
                }
            }
            if (controlGroup == null)
            {
                if (GUI.ServoGroups.Count < 1)
                {
                    GUI.ServoGroups.Add(new ControlGroup());
                }
                controlGroup = GUI.ServoGroups[GUI.ServoGroups.Count - 1];
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

        public static void remove_servo(MuMechToggle servo)
        {
            if (!GUI)
                return;
            if (GUI.ServoGroups == null)
                return;
            int num = 0;
            foreach (ControlGroup group in GUI.ServoGroups)
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
            GUI.enabled = num > 0;
        }


        private void OnVesselChange(Vessel v)
        {
            Debug.Log(String.Format("[IR GUI] vessel {0}", v.name));
            ServoGroups = null;
            guiTweakEnabled = false;
            ResetWin = true;

            var groups = new List<ControlGroup>();
            var groupMap = new Dictionary<string, int>();

            foreach (Part p in v.Parts)
            {
                foreach (MuMechToggle servo in p.Modules.OfType<MuMechToggle>())
                {
                    if (!string.IsNullOrEmpty(servo.part.customPartData))
                    {
                        servo.ParseCData();
                    }
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
                                //temp.part.transform.Find("model/" + temp.fixedMesh).Rotate(temp.rotateAxis, temp.rotation - 1);
                                temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation - 1);
                            else if (temp.rotation == temp.rotateMin && temp.rotateLimits)
                                //temp.part.transform.Find("model/" + temp.fixedMesh).Rotate(temp.rotateAxis, temp.rotation + 1);
                                temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation + 1);
                            else if (temp.rotation == temp.minTweak && temp.rotateLimits)
                                //temp.part.transform.Find("model/" + temp.fixedMesh).Rotate(temp.rotateAxis, temp.rotation + 1);
                                temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation + 1);
                            else if (temp.rotation == temp.maxTweak && temp.rotateLimits)
                                //temp.part.transform.Find("model/" + temp.fixedMesh).Rotate(temp.rotateAxis, temp.rotation - 1);
                                temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation - 1);
                            else
                                //temp.part.transform.Find("model/" + temp.fixedMesh).Rotate(temp.rotateAxis, temp.rotation);
                                temp.FixedMeshTransform.Rotate(temp.rotateAxis, temp.rotation);
                            temp.rotation = 0;
                            temp.rotationEuler = 0;
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
                remove_servo(p);
            }
            partCounter = EditorLogic.fetch.ship.parts.Count == 1 ? 0 : EditorLogic.fetch.ship.parts.Count;

            if (part.Modules.OfType<MuMechToggle>().Any())
            {
                MuMechToggle temp1 = part.Modules.OfType<MuMechToggle>().First();
                if (temp1.part.name.Contains("IR.Rotatron.OffAxis"))
                {
                    temp1.rotation = 0;
                    temp1.rotationEuler = 0;
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
                    if (!string.IsNullOrEmpty(servo.part.customPartData))
                    {
                        servo.ParseCData();
                    }
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


        private void Awake()
        {
            LoadConfigXml();
            Debug.Log("[IR GUI] awake");
            //enabled = false;
            GUIEnabled = false;
            groupEditorEnabled = false;
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
                irMinimizeGroupButton.OnClick += e => groupEditorEnabled = !groupEditorEnabled;
            }
            else
            {
                //enabled = true;
                //            	guiEnabled = true;
                //            	groupEditorEnabled = true;
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

        private void ControlWindow(int windowID)
        {
            GUILayout.BeginVertical();
            foreach (ControlGroup g in ServoGroups)
            {
                if (g.Servos.Any())
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(g.Name, GUILayout.ExpandWidth(true));

                    if (UseElectricCharge)
                    {
                        float totalConsumption = g.Servos.Sum(servo => Mathf.Abs(servo.LastPowerDraw));
                        string displayText = string.Format("({0:#0.##} Ec/s)", totalConsumption);
                        GUILayout.Label(displayText, GUILayout.ExpandWidth(true));
                    }

                    int forceFlags = 0;
                    GUILayoutOption width20 = GUILayout.Width(20);
                    GUILayoutOption width40 = GUILayout.Width(40);
                    forceFlags |= GUILayout.RepeatButton("←", width20) ? 1 : 0;
                    forceFlags |= GUILayout.RepeatButton("○", width20) ? 4 : 0;
                    forceFlags |= GUILayout.RepeatButton("→", width20) ? 2 : 0;

                    g.Speed = GUILayout.TextField(g.Speed, width40);
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
                        servo.MoveFlags &= ~7;
                        servo.MoveFlags |= forceFlags;
                    }

                    GUILayout.EndHorizontal();
                }
            }
            if (ToolbarManager.ToolbarAvailable)
            {
                if (GUILayout.Button("Close"))
                {
                    SaveConfigXml();
                    GUIEnabled = false;
                }
            }
            else
            {
                if (GUILayout.Button(groupEditorEnabled ? "Close Edit" : "Edit"))
                {
                    groupEditorEnabled = !groupEditorEnabled;
                }
            }

            GUILayout.EndVertical();

            UnityEngine.GUI.DragWindow();
        }

        private void EditorWindow(int windowID)
        {
            GUILayoutOption expand = GUILayout.ExpandWidth(true);
            GUILayoutOption width20 = GUILayout.Width(20);
            GUILayoutOption width40 = GUILayout.Width(40);
            GUILayoutOption width60 = GUILayout.Width(60);
            GUILayoutOption maxHeight = GUILayout.MaxHeight(Screen.height/2f);

            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;

            EditorScroll = GUILayout.BeginScrollView(EditorScroll, false,
                false, maxHeight);

            //Kick off the window code
            GUIDragAndDrop.WindowBegin(EditorWinPos, EditorScroll);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            //if we are showing the group handles then Pad the text so it still aligns with the text box
            if (GUIDragAndDrop.ShowGroupHandles)
                GUIDragAndDrop.PadText();
            GUILayout.Label("Group Name", expand);
            GUILayout.Label("Keys", width40);
            GUILayout.Label("Move", width40);

            if (ServoGroups.Count > 1)
            {
                GUILayout.Space(60);
            }
            GUILayout.EndHorizontal();

            for (int i = 0; i < ServoGroups.Count; i++)
            {
                ControlGroup grp = ServoGroups[i];

                GUILayout.BeginHorizontal();

                //Call the Add Group Handle code
                GUIDragAndDrop.DrawGroupHandle(grp.Name, i);

                string tmp = GUILayout.TextField(grp.Name, expand);

                if (grp.Name != tmp)
                {
                    grp.Name = tmp;
                }

                tmp = GUILayout.TextField(grp.ForwardKey, width20);
                if (grp.ForwardKey != tmp)
                {
                    grp.ForwardKey = tmp;
                }
                tmp = GUILayout.TextField(grp.ReverseKey, width20);
                if (grp.ReverseKey != tmp)
                {
                    grp.ReverseKey = tmp;
                }

                if (GUILayout.RepeatButton("←", width20, GUILayout.Height(EditorButtonHeights)))
                {
                    foreach (MuMechToggle servo in grp.Servos)
                    {
                        servo.MoveLeft();
                    }
                }

                if (GUILayout.RepeatButton("→", width20, GUILayout.Height(EditorButtonHeights)))
                {
                    foreach (MuMechToggle servo in grp.Servos)
                    {
                        servo.MoveRight();
                    }
                }

                if (i > 0)
                {
                    //set a smaller height to align with text boxes
                    if (GUILayout.Button("Remove", width60, GUILayout.Height(EditorButtonHeights)))
                    {
                        foreach (MuMechToggle servo in grp.Servos)
                        {
                            move_servo(grp, ServoGroups[i - 1], servo);
                        }
                        ServoGroups.RemoveAt(i);
                        ResetWin = true;
                        return;
                    }
                }
                else
                {
                    if (ServoGroups.Count > 1)
                    {
                        GUILayout.Space(60);
                    }
                }

                GUILayout.EndHorizontal();

                if (UseElectricCharge)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    UpdateGroupEcRequirement(grp);
                    GUILayout.Label(
                        string.Format("Estimated Power Draw: {0:#0.##} Ec/s", grp.TotalElectricChargeRequirement),
                        expand);
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();

                GUILayout.Space(20);

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();

                //Pad the text so it still aligns with the text box
                GUIDragAndDrop.PadText();
                GUILayout.Label("Servo Name", expand);

                GUILayout.Label("Rotate", width40);

                if (ServoGroups.Count > 1)
                {
                    GUILayout.Label("Group", width40);
                }
                GUILayout.EndHorizontal();

                //Changed this to a for loop so it could use the index
                //foreach (var servo in grp.servos)
                for (int iS = 0; iS < grp.Servos.Count; iS++)
                {
                    MuMechToggle servo = grp.Servos[iS];
                    //if (!servo.freeMoving)
                    {
                        GUILayout.BeginHorizontal();

                        //Call the Add Servo Handle code
                        GUIDragAndDrop.DrawServoHandle(servo.servoName, i, iS);

                        //set a smaller height to align with text boxes
                        if (GUILayout.Button("[]", GUILayout.Width(30), GUILayout.Height(EditorButtonHeights)))
                        {
                            tmpMin = servo.minTweak.ToString();
                            tmpMax = servo.maxTweak.ToString();
                            servoTweak = servo;
                            guiTweakEnabled = true;
                        }

                        servo.servoName = GUILayout.TextField(servo.servoName,
                            expand);

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

                        //set a smaller height to align with text boxes
                        if (GUILayout.Button("Ͼ", width20, GUILayout.Height(EditorButtonHeights)))
                        {
                            if (servo.rotation == 0f && servo.translation == 0f)
                                servo.transform.Rotate(0, 45f, 0, Space.Self);
                            else
                                ScreenMessages.PostScreenMessage(
                                    "<color=#FF0000>Can't rotate position after adjusting part</color>");
                        }
                        //set a smaller height to align with text boxes
                        if (GUILayout.Button("Ͽ", width20, GUILayout.Height(EditorButtonHeights)))
                        {
                            if (servo.rotation == 0f && servo.translation == 0f)
                                servo.transform.Rotate(0, -45f, 0, Space.Self);
                            else
                                ScreenMessages.PostScreenMessage(
                                    "<color=#FF0000>Can't rotate position after adjusting part</color>");
                        }

                        if (ServoGroups.Count > 1)
                        {
                            if (i > 0)
                            {
                                //Changed these to actual arrows - and set a smaller height to align with text boxes
                                if (GUILayout.Button("↑", width20, GUILayout.Height(EditorButtonHeights)))
                                {
                                    move_servo(grp, ServoGroups[i - 1], servo);
                                }
                            }
                            else
                            {
                                GUILayout.Space(20);
                            }
                            if (i < (ServoGroups.Count - 1))
                            {
                                //Changed these to actual arrows - and set a smaller height to align with text boxes
                                if (GUILayout.Button("↓", width20, GUILayout.Height(EditorButtonHeights)))
                                {
                                    move_servo(grp, ServoGroups[i + 1], servo);
                                }
                            }
                            else
                            {
                                GUILayout.Space(20);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                //Updates the Groups Details with a height for all servos
                GUIDragAndDrop.EndDrawGroup(i);

                GUILayout.EndVertical();

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
                UnityEngine.GUI.DragWindow();
        }

        //Used by DragAndDrop to scroll the scrollview when dragging at top or bottom of window
        internal static void SetEditorScrollYPosition(Single newY)
        {
            EditorScroll.y = newY;
        }

        private void GroupEditorWindow(int windowID)
        {
            GUILayoutOption expand = GUILayout.ExpandWidth(true);
            GUILayoutOption width20 = GUILayout.Width(20);
            GUILayoutOption width40 = GUILayout.Width(40);
            GUILayoutOption width60 = GUILayout.Width(60);
            GUILayoutOption maxHeight = GUILayout.MaxHeight(Screen.height/2f);

            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;

            EditorScroll = GUILayout.BeginScrollView(EditorScroll, false,
                false, maxHeight);

            //Kick off the window code
            GUIDragAndDrop.WindowBegin(GroupEditorWinPos, EditorScroll);

            GUILayout.BeginVertical();
            if (ToolbarManager.ToolbarAvailable)
            {
                if (GUILayout.Button("Close"))
                {
                    SaveConfigXml();
                    groupEditorEnabled = false;
                }
            }
            GUILayout.BeginHorizontal();

            //if we are showing the group handles then Pad the text so it still aligns with the text box
            if (GUIDragAndDrop.ShowGroupHandles)
                GUIDragAndDrop.PadText();
            GUILayout.Label("Group Name", expand);
            GUILayout.Label("Keys", width40);

            if (ServoGroups.Count > 1)
            {
                GUILayout.Space(60);
            }
            GUILayout.EndHorizontal();

            for (int i = 0; i < ServoGroups.Count; i++)
            {
                ControlGroup grp = ServoGroups[i];

                GUILayout.BeginHorizontal();

                //Call the Add Group Handle code
                GUIDragAndDrop.DrawGroupHandle(grp.Name, i);

                string tmp = GUILayout.TextField(grp.Name, expand);

                if (grp.Name != tmp)
                {
                    grp.Name = tmp;
                }

                tmp = GUILayout.TextField(grp.ForwardKey, width20);
                if (grp.ForwardKey != tmp)
                {
                    grp.ForwardKey = tmp;
                }
                tmp = GUILayout.TextField(grp.ReverseKey, width20);
                if (grp.ReverseKey != tmp)
                {
                    grp.ReverseKey = tmp;
                }

                if (i > 0)
                {
                    //set a smaller height to align with text boxes
                    if (GUILayout.Button("Remove", width60, GUILayout.Height(EditorButtonHeights)))
                    {
                        foreach (MuMechToggle servo in grp.Servos)
                        {
                            move_servo(grp, ServoGroups[i - 1], servo);
                        }
                        ServoGroups.RemoveAt(i);
                        ResetWin = true;
                        return;
                    }
                }
                else
                {
                    if (ServoGroups.Count > 1)
                    {
                        GUILayout.Space(60);
                    }
                }
                GUILayout.EndHorizontal();

                if (UseElectricCharge)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(
                        string.Format("Estimated Power Draw: {0:#0.##} Ec/s", grp.TotalElectricChargeRequirement),
                        expand);
                    GUILayout.EndHorizontal();
                }

                GUILayout.BeginHorizontal();

                GUILayout.Space(20);

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();

                //Pad the text so it still aligns with the text box
                GUIDragAndDrop.PadText();
                GUILayout.Label("Servo Name", expand);
                if (update14to15)
                    GUILayout.Label("Rotation", expand);

                if (ServoGroups.Count > 1)
                {
                    GUILayout.Label("Group", width40);
                }
                GUILayout.EndHorizontal();

                //foreach (var servo in grp.servos)
                for (int iS = 0; iS < grp.Servos.Count; iS++)
                {
                    MuMechToggle servo = grp.Servos[iS];
                    if (!servo.freeMoving)
                    {
                        GUILayout.BeginHorizontal();

                        //Call the Add Servo Handle code
                        GUIDragAndDrop.DrawServoHandle(servo.servoName, i, iS);

                        //set a smaller height to align with text boxes
                        if (GUILayout.Button("[]", GUILayout.Width(30), GUILayout.Height(EditorButtonHeights)))
                        {
                            tmpMin = servo.minTweak.ToString();
                            tmpMax = servo.maxTweak.ToString();
                            servoTweak = servo;
                            guiTweakEnabled = true;
                        }

                        servo.servoName = GUILayout.TextField(servo.servoName,
                            expand);
                        //0.14 to 0.15 fix
                        if (update14to15)
                        {
                            string tempRot = GUILayout.TextField(servo.rotation.ToString(),
                                expand);
                            servo.rotation = float.Parse(tempRot);
                        }
                        //0.14 to 0.15 fix
                        servo.groupName = grp.Name;
                        servo.reverseKey = grp.ReverseKey;
                        servo.forwardKey = grp.ForwardKey;

                        if (GroupEditorWinPos.Contains(mousePos))
                        {
                            Rect last = GUILayoutUtility.GetLastRect();
                            Vector2 pos = Event.current.mousePosition;
                            bool highlight = last.Contains(pos);
                            servo.part.SetHighlight(highlight, false);
                        }

                        if (ServoGroups.Count > 1)
                        {
                            if (i > 0)
                            {
                                //Changed these to actual arrows - and set a smaller height to align with text boxes
                                if (GUILayout.Button("↑", width20, GUILayout.Height(EditorButtonHeights)))
                                {
                                    move_servo(grp, ServoGroups[i - 1], servo);
                                }
                            }
                            else
                            {
                                GUILayout.Space(20);
                            }
                            if (i < (ServoGroups.Count - 1))
                            {
                                //Changed these to actual arrows - and set a smaller height to align with text boxes
                                if (GUILayout.Button("↓", width20, GUILayout.Height(EditorButtonHeights)))
                                {
                                    move_servo(grp, ServoGroups[i + 1], servo);
                                }
                            }
                            else
                            {
                                GUILayout.Space(20);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUIDragAndDrop.EndDrawGroup(i);

                GUILayout.EndVertical();

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
                UnityEngine.GUI.DragWindow();
        }

        private void TweakWindow(int windowID)
        {
            GUILayoutOption width60 = GUILayout.Width(60);

            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;


            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Min");
            tmpMin = GUILayout.TextField(tmpMin, width60);
            if (servoTweak.rotateJoint)
                GUILayout.Label(servoTweak.rotateMin.ToString());
            else if (servoTweak.translateJoint)
                GUILayout.Label(servoTweak.translateMin.ToString());
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Max");
            tmpMax = GUILayout.TextField(tmpMax, width60);
            if (servoTweak.rotateJoint)
                GUILayout.Label(servoTweak.rotateMax.ToString());
            else if (servoTweak.translateJoint)
                GUILayout.Label(servoTweak.translateMax.ToString());
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save", GUILayout.Width(50)))
            {
                if (HighLogic.LoadedScene != GameScenes.FLIGHT)
                {
                    if (servoTweak.part.symmetryCounterparts.Count > 1)
                    {
                        foreach (Part part in servoTweak.part.symmetryCounterparts)
                        {
                            float.TryParse(tmpMin, out ((MuMechToggle) part.Modules["MuMechToggle"]).minTweak);
                            float.TryParse(tmpMax, out ((MuMechToggle) part.Modules["MuMechToggle"]).maxTweak);
                        }
                    }
                }
                float.TryParse(tmpMin, out servoTweak.minTweak);
                float.TryParse(tmpMax, out servoTweak.maxTweak);
            }
            if (GUILayout.Button("Close", GUILayout.Width(50)))
            {
                SaveConfigXml();
                guiTweakEnabled = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            UnityEngine.GUI.DragWindow();
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
            if (InputLockManager.IsLocked(ControlTypes.LINEAR))
                return;

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

            if (GroupEditorWinPos.x == 0 && GroupEditorWinPos.y == 0)
            {
                GroupEditorWinPos = new Rect(Screen.width - 260, 50, 10, 10);
            }

            if (TweakWinPos.x == 0 && TweakWinPos.y == 0)
            {
                TweakWinPos = new Rect(Screen.width - 410, 220, 145, 130);
            }

            if (ResetWin)
            {
                ControlWinPos = new Rect(ControlWinPos.x, ControlWinPos.y,
                    10, 10);
                EditorWinPos = new Rect(EditorWinPos.x, EditorWinPos.y,
                    10, 10);
                GroupEditorWinPos = new Rect(GroupEditorWinPos.x, GroupEditorWinPos.y,
                    10, 10);

                TweakWinPos = new Rect(TweakWinPos.x, TweakWinPos.y,
                    10, 10);
                ResetWin = false;
            }
            UnityEngine.GUI.skin = DefaultSkinProvider.DefaultSkin;
            GameScenes scene = HighLogic.LoadedScene;

            //Call the DragAndDrop GUI Setup stuff
            GUIDragAndDrop.OnGUIOnceOnly();

            if (scene == GameScenes.FLIGHT)
            {
                GUILayoutOption height = GUILayout.Height(Screen.height/2f);
                if (GUIEnabled)
                    //{
                    ControlWinPos = GUILayout.Window(956, ControlWinPos,
                        ControlWindow,
                        "Servo Control",
                        GUILayout.Width(300),
                        GUILayout.Height(80));
                if (groupEditorEnabled)
                    GroupEditorWinPos = GUILayout.Window(958, GroupEditorWinPos,
                        GroupEditorWindow,
                        "Servo Group Editor",
                        GUILayout.Width(EditorWidth - 48), //Using a variable here
                        height);
                if (guiTweakEnabled)
                    TweakWinPos = GUILayout.Window(959, TweakWinPos,
                        TweakWindow,
                        servoTweak.servoName,
                        GUILayout.Width(100),
                        GUILayout.Height(80));
                //}
                RefreshKeysFromGUI();
            }
            else if (scene == GameScenes.EDITOR)
            {
                GUILayoutOption height = GUILayout.Height(Screen.height/2f);
                if (GUIEnabled)
                    EditorWinPos = GUILayout.Window(957, EditorWinPos,
                        EditorWindow,
                        "Servo Configuration",
                        GUILayout.Width(EditorWidth), //Using a variable here
                        height);
                if (guiTweakEnabled)
                {
                    TweakWinPos = GUILayout.Window(959, TweakWinPos,
                        TweakWindow,
                        servoTweak.servoName,
                        GUILayout.Width(100),
                        GUILayout.Height(80));
                }
                EditorLock(GUIEnabled &&
                           EditorWinPos.Contains(new Vector2(Input.mousePosition.x,
                               Screen.height - Input.mousePosition.y)));
            }

            GUIDragAndDrop.OnGUIEvery();
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
            TweakWinPos = config.GetValue<Rect>("tweakWinPos");
            ControlWinPos = config.GetValue<Rect>("controlWinPos");
            GroupEditorWinPos = config.GetValue<Rect>("groupEditorWinPos");
            UseElectricCharge = config.GetValue<bool>("useEC");
        }

        public void SaveConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<ControlsGUI>();
            config.SetValue("editorWinPos", EditorWinPos);
            config.SetValue("tweakWinPos", TweakWinPos);
            config.SetValue("controlWinPos", ControlWinPos);
            config.SetValue("groupEditorWinPos", GroupEditorWinPos);
            config.SetValue("useEC", UseElectricCharge);
            config.save();
        }

        public class ControlGroup
        {
            public ControlGroup(MuMechToggle servo)
            {
                Name = servo.groupName;
                ForwardKey = servo.forwardKey;
                ReverseKey = servo.reverseKey;
                Speed = servo.customSpeed.ToString("g");
                Servos = new List<MuMechToggle>();
                ShowGUI = servo.showGUI;
                Servos.Add(servo);
            }

            public ControlGroup()
            {
                Name = "New Group";
                ForwardKey = string.Empty;
                ReverseKey = string.Empty;
                Speed = "1";
                ShowGUI = true;
                Servos = new List<MuMechToggle>();
            }

            public string Name { get; set; }
            public List<MuMechToggle> Servos { get; set; }
            public string ForwardKey { get; set; }
            public string ReverseKey { get; set; }
            public string Speed { get; set; }
            public bool ShowGUI { get; set; }
            public float TotalElectricChargeRequirement { get; set; }
        }
    }
}