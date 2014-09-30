using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MuMech;
using System.IO;
using InfernalRobotics;
using KSPAPIExtensions;
using KSP.IO;


namespace MuMech
{
    //18.3
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class MuMechGUI : MonoBehaviour
    {
        public class Group
        {
            public string name;
            public List<MuMechToggle> servos;
            public string forwardKey;
            public string reverseKey;
            public string speed;
            public bool showGUI;
            public float groupTotalECRequirement;

            public Group(MuMechToggle servo)
            {
                this.name = servo.groupName;
                forwardKey = servo.forwardKey;
                reverseKey = servo.reverseKey;
                speed = servo.customSpeed.ToString("g");
                servos = new List<MuMechToggle>();
                showGUI = servo.showGUI;
                servos.Add(servo);
            }

            public Group()
            {
                this.name = "New Group";
                forwardKey = "";
                reverseKey = "";
                speed = "1";
                showGUI = true;
                servos = new List<MuMechToggle>();
            }
        }
        
        protected static Rect controlWinPos;
        protected static Rect editorWinPos;
        protected static Rect groupEditorWinPos;
        protected static Rect tweakWinPos;
        protected static bool resetWin = false;
        protected static Vector2 editorScroll;
        internal List<Group> servo_groups;  //Changed Scope so draganddrop can use it
        protected static MuMechGUI gui_controller;
        bool guiEnabled = false;
        private static bool initialGroupECUpdate;
        protected static bool useEC = true;
        ApplicationLauncherButton button;

        #region UITweaks
        //New sizes for a couple of things
        internal static Int32 EditorWidth = 332;
        internal static Int32 EditorButtonHeights = 25;
        #endregion

        public static MuMechGUI gui
        {
            get { return gui_controller; }
        }

        static void updateGroupECRequirement(Group servoGroup)
        {
            //var ecSum = servoGroup.servos.Select(s => s.ElectricChargeRequired).Sum();
            var ecSum = servoGroup.servos.Where(s => s.freeMoving == false).Select(s => s.ElectricChargeRequired).Sum();
            foreach (var servo in servoGroup.servos)
            {
                servo.GroupElectricChargeRequired = ecSum;
            }
            servoGroup.groupTotalECRequirement = ecSum;
        }

        static void move_servo(Group from, Group to, MuMechToggle servo)
        {
            to.servos.Add(servo);
            from.servos.Remove(servo);
            servo.groupName = to.name;
            servo.forwardKey = to.forwardKey;
            servo.reverseKey = to.reverseKey;

            if (useEC)
            {
                updateGroupECRequirement(from);
                updateGroupECRequirement(to);
            }
        }

        public static void add_servo(MuMechToggle servo)
        {
            if (!gui)
                return;
            gui.enabled = true;
            if (servo.part.customPartData != null
                && servo.part.customPartData != "")
            {
                servo.ParseCData();
            }
            if (gui.servo_groups == null)
                gui.servo_groups = new List<Group>();
            Group group = null;
            if (servo.groupName != null && servo.groupName != "")
            {
                for (int i = 0; i < gui.servo_groups.Count; i++)
                {
                    if (servo.groupName == gui.servo_groups[i].name)
                    {
                        group = gui.servo_groups[i];
                        break;
                    }
                }
                if (group == null)
                {
                    var newGroup = new Group(servo);
                    if (useEC)
                    {
                        updateGroupECRequirement(newGroup);
                    }
                    gui.servo_groups.Add(newGroup);
                    return;
                }
            }
            if (group == null)
            {
                if (gui.servo_groups.Count < 1)
                {
                    gui.servo_groups.Add(new Group());
                }
                group = gui.servo_groups[gui.servo_groups.Count - 1];
            }

            group.servos.Add(servo);
            servo.groupName = group.name;
            servo.forwardKey = group.forwardKey;
            servo.reverseKey = group.reverseKey;

            if (useEC)
            {
                updateGroupECRequirement(group);
            }
        }

        public static void remove_servo(MuMechToggle servo)
        {
            if (!gui)
                return;
            if (gui.servo_groups == null)
                return;
            int num = 0;
            foreach (var group in gui.servo_groups)
            {
                if (group.name == servo.groupName)
                {
                    group.servos.Remove(servo);

                    if (useEC)
                    {
                        updateGroupECRequirement(group);
                    }
                }
                num += group.servos.Count;
            }
            gui.enabled = num > 0;
        }


        void onVesselChange(Vessel v)
        {
            Debug.Log(String.Format("[IR GUI] vessel {0}", v.name));
            servo_groups = null;
            guiTweakEnabled = false;
            resetWin = true;

            var groups = new List<Group>();
            var group_map = new Dictionary<string, int>();

            foreach (Part p in v.Parts)
            {
                foreach (var servo in p.Modules.OfType<MuMechToggle>())
                {
                    if (servo.part.customPartData != null
                        && servo.part.customPartData != "")
                    {
                        servo.ParseCData();
                    }
                    if (!group_map.ContainsKey(servo.groupName))
                    {
                        groups.Add(new Group(servo));
                        group_map[servo.groupName] = groups.Count - 1;
                    }
                    else
                    {
                        Group g = groups[group_map[servo.groupName]];
                        g.servos.Add(servo);
                    }
                }
            }
            Debug.Log(String.Format("[IR GUI] {0} groups", groups.Count));

            if (groups.Count == 0)
            {
                if (ToolbarManager.ToolbarAvailable)
                {
                    IRMinimizeButton.Visible = false;
                    IRMinimizeGroupButton.Visible = false;
                }
            }
            if (groups.Count > 0)
            {
                servo_groups = groups;
                if (ToolbarManager.ToolbarAvailable)
                {
                    IRMinimizeButton.Visible = true;
                    IRMinimizeGroupButton.Visible = true;
                }

                if (useEC)
                {
                    foreach (var servoGroup in servo_groups)
                    {
                        updateGroupECRequirement(servoGroup);
                    }
                }
            }

            foreach (Part p in v.Parts)
            {
                foreach (var servo in p.Modules.OfType<MuMechToggle>())
                {
                    servo.setupJoints();
                }
            }
        }

        int partCounter = 0;
        void onPartAttach(GameEvents.HostTargetAction<Part, Part> host_target)
        {
            Vector3 tempAxis;
            Part part = host_target.host;
            try
            {
                if (part.Modules.OfType<MuMechToggle>().Any())
                {
                    var temp = part.GetComponentInChildren<MuMechToggle>();
                    temp.transform.rotation.ToAngleAxis(out temp.originalAngle, out tempAxis);
                    if (temp.rotateJoint)
                    {
                        temp.originalAngle = temp.transform.eulerAngles.x;
                        temp.fixedMeshOriginalLocation = temp.transform.Find("model/" + temp.fixedMesh).eulerAngles;
                    }
                    else if (temp.translateJoint)
                    {
                        temp.originalTranslation = temp.transform.localPosition.y;
                    }
                }
            }
            catch { }


            if ((EditorLogic.fetch.ship.parts.Count >= partCounter) && (EditorLogic.fetch.ship.parts.Count != partCounter)) 
            {
                if ((partCounter != 1) && (EditorLogic.fetch.ship.parts.Count != 1))
                {
                    foreach (var p in part.GetComponentsInChildren<MuMechToggle>())
                    {
                        add_servo(p);
                    }
                    partCounter = EditorLogic.fetch.ship.parts.Count;
                }
            }
            if ((EditorLogic.fetch.ship.parts.Count == 0) && (partCounter == 0))
            {
                if ((partCounter != 1) && (EditorLogic.fetch.ship.parts.Count != 1))
                {
                    foreach (var p in part.GetComponentsInChildren<MuMechToggle>())
                    {
                        add_servo(p);
                    }
                    partCounter = EditorLogic.fetch.ship.parts.Count;
                }
            }

        }

        void onPartRemove(GameEvents.HostTargetAction<Part, Part> host_target)
        {
            Part part = host_target.target;
            try
            {
                if (part.Modules.OfType<MuMechToggle>().Any())
                {
                    var temp = part.Modules.OfType<MuMechToggle>().First();
                    
                    if (temp.rotateJoint)
                    {
                        if (!temp.part.name.Contains("IR.Rotatron.OffAxis"))
                        {
                            //silly check to prevent base creeping when reaching the limits
                            if(temp.rotation == temp.rotateMax && temp.rotateLimits)
                                temp.part.transform.Find("model/" + temp.fixedMesh).Rotate(temp.rotateAxis, temp.rotation-1);
                            else if(temp.rotation ==temp.rotateMin && temp.rotateLimits)
                                temp.part.transform.Find("model/" + temp.fixedMesh).Rotate(temp.rotateAxis, temp.rotation+1);
                            else
                                temp.part.transform.Find("model/" + temp.fixedMesh).Rotate(temp.rotateAxis, temp.rotation);
                            temp.rotation = 0;
                            temp.rotationEuler = 0;
                        }
                    }
                    else if (temp.translateJoint)
                    {
                        temp.part.transform.Find("model/" + temp.fixedMesh).position = temp.part.transform.position;
                        temp.translation = 0;
                    }
                }

            }
            catch { }

            
            foreach (var p in part.GetComponentsInChildren<MuMechToggle>())
            {
                remove_servo(p);
            }
            if (EditorLogic.fetch.ship.parts.Count == 1)
                partCounter = 0;
            else
                partCounter = EditorLogic.fetch.ship.parts.Count;

            if (part.Modules.OfType<MuMechToggle>().Any())
            {
                var temp1 = part.Modules.OfType<MuMechToggle>().First();
                if (temp1.part.name.Contains("IR.Rotatron.OffAxis"))
                {
                    temp1.rotation = 0;
                    temp1.rotationEuler = 0;
                    temp1.transform.Find("model/" + temp1.fixedMesh).eulerAngles = temp1.transform.eulerAngles;
                }
            }
        }

        void onEditorShipModified(ShipConstruct ship)
        {
            List<Part> shipParts = ship.parts;
            servo_groups = null;
            

            var groups = new List<Group>();
            var group_map = new Dictionary<string, int>();

            foreach (Part p in ship.Parts)
            {
                foreach (var servo in p.Modules.OfType<MuMechToggle>())
                {
                    if (servo.part.customPartData != null
                        && servo.part.customPartData != "")
                    {
                        servo.ParseCData();
                    }
                    if (!group_map.ContainsKey(servo.groupName))
                    {
                        groups.Add(new Group(servo));
                        group_map[servo.groupName] = groups.Count - 1;
                    }
                    else
                    {
                        Group g = groups[group_map[servo.groupName]];
                        g.servos.Add(servo);
                    }
                }
            }

            if (groups.Count == 0)
            {
                if (ToolbarManager.ToolbarAvailable)
                {
                    IRMinimizeButton.Visible = false;
                    IRMinimizeGroupButton.Visible = false;
                }
            }
            if (groups.Count > 0)
            {
                servo_groups = groups;
                if (ToolbarManager.ToolbarAvailable)
                {
                    IRMinimizeButton.Visible = true;
                    IRMinimizeGroupButton.Visible = true;
                }

                if (useEC)
                {
                    foreach (var servoGroup in servo_groups)
                    {
                        updateGroupECRequirement(servoGroup);
                    }
                }
            }

            if (EditorLogic.fetch.ship.parts.Count == 1)
            {
                partCounter = 0;
            }
            else
                partCounter = EditorLogic.fetch.ship.parts.Count;
        }


        bool update14to15 = false;
        IButton IRMinimizeButton;
        IButton IRMinimizeGroupButton;
        bool groupEditorEnabled = false;
        void Awake()
        {
            loadConfigXML();
            Debug.Log("[IR GUI] awake");
            //enabled = false;
            guiEnabled = false;
            groupEditorEnabled = false;
            var scene = HighLogic.LoadedScene;
            if (scene == GameScenes.FLIGHT)
            {
                
                GameEvents.onVesselChange.Add(onVesselChange);
                GameEvents.onVesselWasModified.Add(this.onVesselWasModified);
                gui_controller = this;
            }
            else if (scene == GameScenes.EDITOR || scene == GameScenes.SPH)
            {
                //partCounter = EditorLogic.fetch.ship.parts.Count;    
                GameEvents.onPartAttach.Add(onPartAttach);
                GameEvents.onPartRemove.Add(onPartRemove);
                GameEvents.onEditorShipModified.Add(onEditorShipModified);
                gui_controller = this;
            }
            else
            {
                gui_controller = null;
            }

            if (System.IO.File.Exists(KSPUtil.ApplicationRootPath + @"GameData/MagicSmokeIndustries/Plugins/14to15.txt"))
            {
                Debug.Log("debug found!");
                update14to15 = true;
            }

            if (ToolbarManager.ToolbarAvailable)
            {
                IRMinimizeButton = ToolbarManager.Instance.add("sirkut", "IREditorButton");
                IRMinimizeButton.TexturePath = "MagicSmokeIndustries/Textures/icon_button";
                IRMinimizeButton.ToolTip = "Infernal Robotics";
                IRMinimizeButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.SPH, GameScenes.FLIGHT);
                IRMinimizeButton.OnClick += (e) => guiEnabled = !guiEnabled;

                IRMinimizeGroupButton = ToolbarManager.Instance.add("sirkut2", "IREditorGroupButton");
                IRMinimizeGroupButton.TexturePath = "MagicSmokeIndustries/Textures/icon_buttonGROUP";
                IRMinimizeGroupButton.ToolTip = "Infernal Robotics Group Editor";
                IRMinimizeGroupButton.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
                IRMinimizeGroupButton.OnClick += (e) => groupEditorEnabled = !groupEditorEnabled;
            }
            else
            {
                //enabled = true;
                //            	guiEnabled = true;
                //            	groupEditorEnabled = true;
                GameEvents.onGUIApplicationLauncherReady.Add(onAppReady);
            }

            initialGroupECUpdate = false;
        }


        void onAppReady() 
        {
        	if (button == null)
        	{
        		var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("InfernalRobotics.IRbutton.png");
        		var texButton = new Texture2D(38, 38);
        		texButton.LoadImage(new System.IO.BinaryReader(stream).ReadBytes((int)stream.Length)); // embedded resource loading is stupid
        		
        		button = ApplicationLauncher.Instance.AddModApplication(delegate() { guiEnabled = true; }, delegate() { guiEnabled = false; }, null, null, null, null,
        		                                                        ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, texButton);
        	}
        }


        void onVesselWasModified(Vessel v)
        {
            if (v == FlightGlobals.ActiveVessel)
            {
                servo_groups = null;
                
                onVesselChange(v);
            }
        }

        void OnDestroy()
        {
            Debug.Log("[IR GUI] destroy");
            GameEvents.onVesselChange.Remove(onVesselChange);
            GameEvents.onPartAttach.Remove(onPartAttach);
            GameEvents.onPartRemove.Remove(onPartRemove);
            GameEvents.onVesselWasModified.Remove(this.onVesselWasModified);
            GameEvents.onEditorShipModified.Remove(onEditorShipModified);
            if (ToolbarManager.ToolbarAvailable)
            {
                IRMinimizeButton.Destroy();
                IRMinimizeGroupButton.Destroy();
            }
            else
            {
				GameEvents.onGUIApplicationLauncherReady.Remove(onAppReady);
            	if (button != null)
            	{
	            	ApplicationLauncher.Instance.RemoveModApplication(button);
	            	button = null;
            	}
            }
            EditorLock(false);
            saveConfigXML();

        }

        private void ControlWindow(int windowID)
        {
            GUILayout.BeginVertical();
            foreach (Group g in servo_groups)
            {
                if (g.servos.Count() > 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(g.name, GUILayout.ExpandWidth(true));

                    if (useEC)
                    {
                        var totalConsumption = g.servos.Sum(servo => Mathf.Abs(servo.LastPowerDraw));
                        var displayText = string.Format("({0:#0.##} Ec/s)", totalConsumption);
                        GUILayout.Label(displayText, GUILayout.ExpandWidth(true));
                    }

                    int forceFlags = 0;
                    var width20 = GUILayout.Width(20);
                    var width40 = GUILayout.Width(40);
                    forceFlags |= GUILayout.RepeatButton("←", width20) ? 1 : 0;
                    forceFlags |= GUILayout.RepeatButton("○", width20) ? 4 : 0;
                    forceFlags |= GUILayout.RepeatButton("→", width20) ? 2 : 0;

                    g.speed = GUILayout.TextField(g.speed, width40);
                    float speed;
                    bool speed_ok = float.TryParse(g.speed, out speed);
                    foreach (MuMechToggle servo in g.servos)
                    {
                        servo.reverseKey = g.reverseKey;
                        servo.forwardKey = g.forwardKey;
                        if (speed_ok)
                        {
                            servo.customSpeed = speed;
                        }
                        servo.moveFlags &= ~7;
                        servo.moveFlags |= forceFlags;
                    }

                    GUILayout.EndHorizontal();
                }
            }
            if (ToolbarManager.ToolbarAvailable)
            {
                if (GUILayout.Button("Close"))
                {
                    saveConfigXML();
                    guiEnabled = false;
                }
            }
            else
            {
            	if (GUILayout.Button(groupEditorEnabled?"Close Edit":"Edit"))
            	{
            		groupEditorEnabled = !groupEditorEnabled;
            	}
            }

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        void EditorWindow(int windowID)
        {
            var expand = GUILayout.ExpandWidth(true);
            var width20 = GUILayout.Width(20);
            var width40 = GUILayout.Width(40);
            var width60 = GUILayout.Width(60);
            var maxHeight = GUILayout.MaxHeight(Screen.height / 2);

            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;

            editorScroll = GUILayout.BeginScrollView(editorScroll, false,
                                                     false, maxHeight);

            //Kick off the window code
            GUIDragAndDrop.WindowBegin(editorWinPos,editorScroll);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();

            //if we are showing the group handles then Pad the text so it still aligns with the text box
            if(GUIDragAndDrop.ShowGroupHandles)
                GUIDragAndDrop.PadText();
            GUILayout.Label("Group Name", expand);
            GUILayout.Label("Keys", width40);
            GUILayout.Label("Move", width40);

            if (servo_groups.Count > 1)
            {
                GUILayout.Space(60);
            }
            GUILayout.EndHorizontal();

            for (int i = 0; i < servo_groups.Count; i++)
            {
                Group grp = servo_groups[i];

                GUILayout.BeginHorizontal();

                //Call the Add Group Handle code
                GUIDragAndDrop.DrawGroupHandle(grp.name,i );

                string tmp = GUILayout.TextField(grp.name, expand);

                if (grp.name != tmp)
                {
                    grp.name = tmp;
                }

                tmp = GUILayout.TextField(grp.forwardKey, width20);
                if (grp.forwardKey != tmp)
                {
                    grp.forwardKey = tmp;
                }
                tmp = GUILayout.TextField(grp.reverseKey, width20);
                if (grp.reverseKey != tmp)
                {
                    grp.reverseKey = tmp;
                }

                if (GUILayout.RepeatButton("←", width20, GUILayout.Height(EditorButtonHeights)))
                {
                    foreach (var servo in grp.servos)
                    {
                        servo.moveLeft();
                    }
                }

                if (GUILayout.RepeatButton("→", width20, GUILayout.Height(EditorButtonHeights)))
                {
                    foreach (var servo in grp.servos)
                    {
                        servo.moveRight();
                    }
                }

                if (i > 0)
                {

                    //set a smaller height to align with text boxes
                    if (GUILayout.Button("Remove", width60, GUILayout.Height(EditorButtonHeights)))
                    {
                        foreach (var servo in grp.servos)
                        {
                            move_servo(grp, servo_groups[i - 1], servo);
                        }
                        servo_groups.RemoveAt(i);
                        resetWin = true;
                        return;
                    }
                }
                else
                {
                    if (servo_groups.Count > 1)
                    {
                        GUILayout.Space(60);
                    }
                }

                GUILayout.EndHorizontal();

                if (useEC)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    updateGroupECRequirement(grp);
                    GUILayout.Label(string.Format("Estimated Power Draw: {0:#0.##} Ec/s", grp.groupTotalECRequirement), expand);
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

                if (servo_groups.Count > 1)
                {
                    GUILayout.Label("Group", width40);
                }
                GUILayout.EndHorizontal();

                //Changed this to a for loop so it could use the index
                //foreach (var servo in grp.servos)
                for (int iS = 0; iS < grp.servos.Count; iS++)
                {
                    var servo = grp.servos[iS];
                    //if (!servo.freeMoving)
                    {
                        GUILayout.BeginHorizontal();

                        //Call the Add Servo Handle code
                        GUIDragAndDrop.DrawServoHandle(servo.servoName,i, iS);

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

                        servo.groupName = grp.name;
                        servo.reverseKey = grp.reverseKey;
                        servo.forwardKey = grp.forwardKey;
                        servo.refreshKeys();
                        if (editorWinPos.Contains(mousePos))
                        {
                            var last = GUILayoutUtility.GetLastRect();
                            var pos = Event.current.mousePosition;
                            bool highlight = last.Contains(pos);
                            servo.part.SetHighlight(highlight);
                        }

                        //set a smaller height to align with text boxes
                        if (GUILayout.Button("Ͼ", width20, GUILayout.Height(EditorButtonHeights)))
                        {
                            if (servo.rotation == 0f && servo.translation == 0f)
                                servo.transform.Rotate(0, 45f, 0, Space.Self);
                            else
                                ScreenMessages.PostScreenMessage("<color=#FF0000>Can't rotate position after adjusting part</color>");
                        }
                        //set a smaller height to align with text boxes
                        if (GUILayout.Button("Ͽ", width20, GUILayout.Height(EditorButtonHeights)))
                        {
                            if (servo.rotation == 0f && servo.translation == 0f)
                                servo.transform.Rotate(0, -45f, 0, Space.Self);
                            else
                                ScreenMessages.PostScreenMessage("<color=#FF0000>Can't rotate position after adjusting part</color>");
                        }

                        if (servo_groups.Count > 1)
                        {
                            if (i > 0)
                            {
                                //Changed these to actual arrows - and set a smaller height to align with text boxes
                                if (GUILayout.Button("↑", width20, GUILayout.Height(EditorButtonHeights)))
                                {
                                    move_servo(grp, servo_groups[i - 1], servo);
                                }
                            }
                            else
                            {
                                GUILayout.Space(20);
                            }
                            if (i < (servo_groups.Count - 1))
                            {
                                //Changed these to actual arrows - and set a smaller height to align with text boxes
                                if (GUILayout.Button("↓", width20, GUILayout.Height(EditorButtonHeights)))
                                {
                                    move_servo(grp, servo_groups[i + 1], servo);
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
                Group temp = new Group();
                temp.name = "New Group" + (servo_groups.Count + 1).ToString();
                servo_groups.Add(temp);
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
            if (!GUIDragAndDrop.draggingItem)
                GUI.DragWindow();
        }

        //Used by DragAndDrop to scroll the scrollview when dragging at top or bottom of window
        internal static void SetEditorScrollYPosition(Single NewY)
        {
            editorScroll.y = NewY;
        }

        void GroupEditorWindow(int windowID)
        {
            var expand = GUILayout.ExpandWidth(true);
            var width20 = GUILayout.Width(20);
            var width40 = GUILayout.Width(40);
            var width60 = GUILayout.Width(60);
            var maxHeight = GUILayout.MaxHeight(Screen.height / 2);

            Vector2 mousePos = Input.mousePosition;
            mousePos.y = Screen.height - mousePos.y;

            editorScroll = GUILayout.BeginScrollView(editorScroll, false,
                                                     false, maxHeight);

            //Kick off the window code
            GUIDragAndDrop.WindowBegin(groupEditorWinPos, editorScroll);

            GUILayout.BeginVertical();
            if (ToolbarManager.ToolbarAvailable)
            {
                if (GUILayout.Button("Close"))
                {
                    saveConfigXML();
                    groupEditorEnabled = false;
                }
            }
            GUILayout.BeginHorizontal();

            //if we are showing the group handles then Pad the text so it still aligns with the text box
            if (GUIDragAndDrop.ShowGroupHandles)
                GUIDragAndDrop.PadText();
            GUILayout.Label("Group Name", expand);
            GUILayout.Label("Keys", width40);

            if (servo_groups.Count > 1)
            {
                GUILayout.Space(60);
            }
            GUILayout.EndHorizontal();

            for (int i = 0; i < servo_groups.Count; i++)
            {
                Group grp = servo_groups[i];

                GUILayout.BeginHorizontal();

                //Call the Add Group Handle code
                GUIDragAndDrop.DrawGroupHandle(grp.name, i);

                string tmp = GUILayout.TextField(grp.name, expand);

                if (grp.name != tmp)
                {
                    grp.name = tmp;
                }

                tmp = GUILayout.TextField(grp.forwardKey, width20);
                if (grp.forwardKey != tmp)
                {
                    grp.forwardKey = tmp;
                }
                tmp = GUILayout.TextField(grp.reverseKey, width20);
                if (grp.reverseKey != tmp)
                {
                    grp.reverseKey = tmp;
                }

                if (i > 0)
                {
                    //set a smaller height to align with text boxes
                    if (GUILayout.Button("Remove", width60, GUILayout.Height(EditorButtonHeights)))
                    {
                        foreach (var servo in grp.servos)
                        {
                            move_servo(grp, servo_groups[i - 1], servo);
                        }
                        servo_groups.RemoveAt(i);
                        resetWin = true;
                        return;
                    }
                }
                else
                {
                    if (servo_groups.Count > 1)
                    {
                        GUILayout.Space(60);
                    }
                }
                GUILayout.EndHorizontal();

                if (useEC)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    GUILayout.Label(string.Format("Estimated Power Draw: {0:#0.##} Ec/s", grp.groupTotalECRequirement), expand);
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

                if (servo_groups.Count > 1)
                {
                    GUILayout.Label("Group", width40);
                }
                GUILayout.EndHorizontal();

                //foreach (var servo in grp.servos)
                for (int iS = 0; iS < grp.servos.Count; iS++)
                {
                    var servo = grp.servos[iS];
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
                        servo.groupName = grp.name;
                        servo.reverseKey = grp.reverseKey;
                        servo.forwardKey = grp.forwardKey;

                        if (groupEditorWinPos.Contains(mousePos))
                        {
                            var last = GUILayoutUtility.GetLastRect();
                            var pos = Event.current.mousePosition;
                            bool highlight = last.Contains(pos);
                            servo.part.SetHighlight(highlight);
                        }

                        if (servo_groups.Count > 1)
                        {
                            if (i > 0)
                            {
                                //Changed these to actual arrows - and set a smaller height to align with text boxes
                                if (GUILayout.Button("↑", width20, GUILayout.Height(EditorButtonHeights)))
                                {
                                    move_servo(grp, servo_groups[i - 1], servo);
                                }
                            }
                            else
                            {
                                GUILayout.Space(20);
                            }
                            if (i < (servo_groups.Count - 1))
                            {
                                //Changed these to actual arrows - and set a smaller height to align with text boxes
                                if (GUILayout.Button("↓", width20, GUILayout.Height(EditorButtonHeights)))
                                {
                                    move_servo(grp, servo_groups[i + 1], servo);
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
                Group temp = new Group();
                temp.name = "New Group" + (servo_groups.Count + 1).ToString();
                servo_groups.Add(temp);
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
            if (!GUIDragAndDrop.draggingItem)
                GUI.DragWindow();
        }

        string tmpMin = "";
        string tmpMax = "";
        MuMechToggle servoTweak;
        void tweakWindow(int windowID)
        {
            var expand = GUILayout.ExpandWidth(true);
            var width20 = GUILayout.Width(20);
            var width40 = GUILayout.Width(40);
            var width60 = GUILayout.Width(60);
            var maxHeight = GUILayout.MaxHeight(Screen.height / 2);

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
                        for (int i = 0; i < servoTweak.part.symmetryCounterparts.Count; i++)
                        {
                            float.TryParse(tmpMin, out ((MuMechToggle)servoTweak.part.symmetryCounterparts[i].Modules["MuMechToggle"]).minTweak);
                            float.TryParse(tmpMax, out ((MuMechToggle)servoTweak.part.symmetryCounterparts[i].Modules["MuMechToggle"]).maxTweak);
                        }
                    }
                }
                float.TryParse(tmpMin, out servoTweak.minTweak);
                float.TryParse(tmpMax, out servoTweak.maxTweak);
            }
            if (GUILayout.Button("Close", GUILayout.Width(50)))
            {
                saveConfigXML();
                guiTweakEnabled = false;
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }


        void refreshKeysFromGUI()
        {
            foreach (Group g in servo_groups)
            {
                if (g.servos.Count() > 0)
                {
                    foreach (MuMechToggle servo in g.servos)
                    {
                        servo.reverseKey = g.reverseKey;
                        servo.forwardKey = g.forwardKey;
                        servo.refreshKeys();
                    }
                }
            }

        }

        bool guiTweakEnabled = false;
        void OnGUI()
        {
            // This particular test isn't needed due to the GUI being enabled
            // and disabled as appropriate, but it saves potential NREs.
            if (servo_groups == null)
                return;
            if (InputLockManager.IsLocked(ControlTypes.LINEAR))
                return;

            if (useEC)
            {
                if (!initialGroupECUpdate)
                {
                    foreach (var servoGroup in servo_groups)
                    {
                        updateGroupECRequirement(servoGroup);
                    }
                    initialGroupECUpdate = true;
                }
            }

            if (controlWinPos.x == 0 && controlWinPos.y == 0)
            {
                controlWinPos = new Rect(Screen.width - 510, 70, 10, 10);
            }
            if (editorWinPos.x == 0 && editorWinPos.y == 0)
            {
                editorWinPos = new Rect(Screen.width - 260, 50, 10, 10);
            }

            if (groupEditorWinPos.x == 0 && groupEditorWinPos.y == 0)
            {
                groupEditorWinPos = new Rect(Screen.width - 260, 50, 10, 10);
            }

            if (tweakWinPos.x == 0 && tweakWinPos.y == 0)
            {
                tweakWinPos = new Rect(Screen.width - 410, 220, 145, 130);
            }

            if (resetWin)
            {
                controlWinPos = new Rect(controlWinPos.x, controlWinPos.y,
                                         10, 10);
                editorWinPos = new Rect(editorWinPos.x, editorWinPos.y,
                                        10, 10);
                groupEditorWinPos = new Rect(groupEditorWinPos.x, groupEditorWinPos.y,
                                        10, 10);

                tweakWinPos = new Rect(tweakWinPos.x, tweakWinPos.y,
                                        10, 10);
                resetWin = false;
            }
            GUI.skin = MuUtils.DefaultSkin;
            var scene = HighLogic.LoadedScene;

            //Call the DragAndDrop GUI Setup stuff
            GUIDragAndDrop.OnGUIOnceOnly();

            if (scene == GameScenes.FLIGHT)
            {
                var height = GUILayout.Height(Screen.height / 2);
                if (guiEnabled)
                //{
                    controlWinPos = GUILayout.Window(956, controlWinPos,
                                                     ControlWindow,
                                                     "Servo Control",
                                                     GUILayout.Width(300),
                                                     GUILayout.Height(80));
                    if (groupEditorEnabled)
                        groupEditorWinPos = GUILayout.Window(958, groupEditorWinPos,
                                                        GroupEditorWindow,
                                                        "Servo Group Editor",
                                                        GUILayout.Width(EditorWidth - 48), //Using a variable here
                                                        height);
                    if (guiTweakEnabled)
                        tweakWinPos = GUILayout.Window(959, tweakWinPos,
                                                         tweakWindow,
                                                         servoTweak.servoName,
                                                         GUILayout.Width(100),
                                                         GUILayout.Height(80));
                //}
                refreshKeysFromGUI();
            }
            else if (scene == GameScenes.EDITOR || scene == GameScenes.SPH)
            {
                var height = GUILayout.Height(Screen.height / 2);
                if (guiEnabled)
                    editorWinPos = GUILayout.Window(957, editorWinPos,
                                                    EditorWindow,
                                                    "Servo Configuration",
                                                    GUILayout.Width(EditorWidth ), //Using a variable here
                                                    height);
                if (guiTweakEnabled)
                {
                    tweakWinPos = GUILayout.Window(959, tweakWinPos,
                                                     tweakWindow,
                                                     servoTweak.servoName,
                                                     GUILayout.Width(100),
                                                     GUILayout.Height(80));
                }
                EditorLock(guiEnabled && editorWinPos.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)));
            }

            GUIDragAndDrop.OnGUIEvery();
        }

        /// <summary>
        /// Applies or removes the lock
        /// </summary>
        /// <param name="Apply">Which way are we going</param>
        internal void EditorLock(Boolean Apply)
        {
            //only do this lock in the editor - no point elsewhere
            if (HighLogic.LoadedSceneIsEditor && Apply)
            {
                //only add a new lock if there isnt already one there
                if (!(InputLockManager.GetControlLock("IRGUILockOfEditor") == ControlTypes.EDITOR_LOCK))
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

        public void loadConfigXML()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<MuMechGUI>();
            config.load();
            editorWinPos = config.GetValue<Rect>("editorWinPos");
            tweakWinPos = config.GetValue<Rect>("tweakWinPos");
            controlWinPos = config.GetValue<Rect>("controlWinPos");
            groupEditorWinPos = config.GetValue<Rect>("groupEditorWinPos");
            useEC = config.GetValue<bool>("useEC");

        }

        public void saveConfigXML()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<MuMechGUI>();
            config.SetValue("editorWinPos", editorWinPos);
            config.SetValue("tweakWinPos", tweakWinPos);
            config.SetValue("controlWinPos", controlWinPos);
            config.SetValue("groupEditorWinPos", groupEditorWinPos);
            config.SetValue("useEC", useEC);
            config.save();
        }
    }
}
