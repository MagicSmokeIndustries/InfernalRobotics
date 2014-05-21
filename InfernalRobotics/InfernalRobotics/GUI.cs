using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MuMech;


namespace MuMech
{
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
        protected static bool resetWin = false;
        protected static Vector2 editorScroll;
        List<Group> servo_groups;
        protected static MuMechGUI gui_controller;


        public static MuMechGUI gui
        {
            get { return gui_controller; }
        }

        static void move_servo(Group from, Group to, MuMechToggle servo)
        {
            to.servos.Add(servo);
            from.servos.Remove(servo);
            servo.groupName = to.name;
            servo.forwardKey = to.forwardKey;
            servo.reverseKey = to.reverseKey;
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
                    gui.servo_groups.Add(new Group(servo));
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

            //foreach (Part p in v.parts)
            //foreach (PartModule m in p.Modules)
            //if (m.name.Equals("ModuleDockingNode"))

            group.servos.Add(servo);
            servo.groupName = group.name;
            servo.forwardKey = group.forwardKey;
            servo.reverseKey = group.reverseKey;
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
                }
                num += group.servos.Count;
            }
            gui.enabled = num > 0;
        }

        void onVesselChange(Vessel v)
        {
            Debug.Log(String.Format("[IR GUI] vessel {0}", v.name));

            servo_groups = null;
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
            }

            foreach (Part p in v.Parts)
            {
                foreach (var servo in p.Modules.OfType<MuMechToggle>())
                {
                    servo.setupJoints();
                }
            }

        }

        void onPartAttach(GameEvents.HostTargetAction<Part, Part> host_target)
        {
            //Part p = host_target.host;
            //foreach (var servo in p.Modules.OfType<MuMechToggle>()) {
            //    add_servo(servo);
            //}
            Part part = host_target.host;
            Part parent = host_target.target;
            foreach (var p in part.GetComponentsInChildren<MuMechToggle>())
            {
                add_servo(p);
            }
        }

        void onPartRemove(GameEvents.HostTargetAction<Part, Part> host_target)
        {
            //Part p = host_target.target;
            //foreach (var servo in p.Modules.OfType<MuMechToggle>()) {
            //    remove_servo(servo);
            //}
            Part part = host_target.target;
            foreach (var p in part.GetComponentsInChildren<MuMechToggle>())
            {
                remove_servo(p);
            }
        }

        //void onHideUI()
        //{
        //    enabled = false;
        //}

        //void onShowUI()
        //{
        //    enabled = servo_groups != null;
        //}

        //bool enabled = true;
        IButton IRMinimizeButton;
        IButton IRMinimizeGroupButton;
        bool groupEditorEnabled = false;
        void Awake()
        {
            Debug.Log("[IR GUI] awake");
            //enabled = false;
            guiEnabled = false;
            groupEditorEnabled = false;
            //GameEvents.onHideUI.Add(onHideUI);
            //GameEvents.onShowUI.Add(onShowUI);
            var scene = HighLogic.LoadedScene;
            if (scene == GameScenes.FLIGHT)
            {
                GameEvents.onVesselChange.Add(onVesselChange);
                GameEvents.onVesselWasModified.Add(this.onVesselWasModified);
                gui_controller = this;
            }
            else if (scene == GameScenes.EDITOR || scene == GameScenes.SPH)
            {
                GameEvents.onPartAttach.Add(onPartAttach);
                GameEvents.onPartRemove.Add(onPartRemove);
                gui_controller = this;
            }
            else
            {
                gui_controller = null;
            }
            if (ToolbarManager.ToolbarAvailable)
            {
                IRMinimizeButton = ToolbarManager.Instance.add("sirkut", "IREditorButton");
                IRMinimizeButton.TexturePath = "MagicSmokeIndustries/Textures/icon_button";
                IRMinimizeButton.ToolTip = "Infernal Robotics";
                IRMinimizeButton.Visibility = new GameScenesVisibility(GameScenes.EDITOR, GameScenes.SPH, GameScenes.FLIGHT);
                //IRMinimizeButton.OnClick += (e) => enabled = !enabled;
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
                guiEnabled = true;
                groupEditorEnabled = true;
            }
        }
        bool guiEnabled = false;

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
            //GameEvents.onHideUI.Remove(onHideUI);
            //GameEvents.onShowUI.Remove(onShowUI);
            GameEvents.onVesselChange.Remove(onVesselChange);
            GameEvents.onPartAttach.Remove(onPartAttach);
            GameEvents.onPartRemove.Remove(onPartRemove);
            GameEvents.onVesselWasModified.Remove(this.onVesselWasModified);
            if (ToolbarManager.ToolbarAvailable)
            {
                IRMinimizeButton.Destroy();
                IRMinimizeGroupButton.Destroy();
            }
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
                    
                    int forceFlags = 0;
                    var width20 = GUILayout.Width(20);
                    var width40 = GUILayout.Width(40);
                    forceFlags |= GUILayout.RepeatButton("<", width20) ? 1 : 0;
                    forceFlags |= GUILayout.RepeatButton("O", width20) ? 4 : 0;
                    forceFlags |= GUILayout.RepeatButton(">", width20) ? 2 : 0;
                    
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

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
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
                    if (GUILayout.Button("Remove", width60))
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

                GUILayout.BeginHorizontal();

                GUILayout.Space(20);

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Servo Name", expand);

                GUILayout.Label("Rotate", width40);

                if (servo_groups.Count > 1)
                {
                    GUILayout.Label("Group", width40);
                }
                GUILayout.EndHorizontal();

                foreach (var servo in grp.servos)
                {
                    GUILayout.BeginHorizontal();
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

                    if (GUILayout.Button("<", width20))
                    {
                        servo.transform.Rotate(0, 45f, 0, Space.Self);

                    }
                    if (GUILayout.Button(">", width20))
                    {
                        servo.transform.Rotate(0, -45f, 0, Space.Self);
                    }

                    if (servo_groups.Count > 1)
                    {
                        if (i > 0)
                        {
                            if (GUILayout.Button("/\\", width20))
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
                            if (GUILayout.Button("\\/", width20))
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

            GUI.DragWindow();
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

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
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
                    if (GUILayout.Button("Remove", width60))
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

                GUILayout.BeginHorizontal();

                GUILayout.Space(20);

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                GUILayout.Label("Servo Name", expand);

                if (servo_groups.Count > 1)
                {
                    GUILayout.Label("Group", width40);
                }
                GUILayout.EndHorizontal();

                foreach (var servo in grp.servos)
                {
                    GUILayout.BeginHorizontal();
                    servo.servoName = GUILayout.TextField(servo.servoName,
                                                          expand);
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
                            if (GUILayout.Button("/\\", width20))
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
                            if (GUILayout.Button("\\/", width20))
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

        void OnGUI()
        {
            // This particular test isn't needed due to the GUI being enabled
            // and disabled as appropriate, but it saves potential NREs.
            if (servo_groups == null)
                return;
            if (InputLockManager.IsLocked(ControlTypes.LINEAR))
                return;
            if (controlWinPos.x == 0 && controlWinPos.y == 0)
            {
                controlWinPos = new Rect(Screen.width / 2, Screen.height / 2,
                                         10, 10);
            }
            if (editorWinPos.x == 0 && editorWinPos.y == 0)
            {
                editorWinPos = new Rect(Screen.width - 260, 50, 10, 10);
            }

            if (groupEditorWinPos.x == 0 && groupEditorWinPos.y == 0)
            {
                groupEditorWinPos = new Rect(Screen.width - 260, 50, 10, 10);
            }

            if (resetWin)
            {
                controlWinPos = new Rect(controlWinPos.x, controlWinPos.y,
                                         10, 10);
                editorWinPos = new Rect(editorWinPos.x, editorWinPos.y,
                                        10, 10);
                groupEditorWinPos = new Rect(groupEditorWinPos.x, groupEditorWinPos.y,
                                        10, 10);
                resetWin = false;
            }
            GUI.skin = MuUtils.DefaultSkin;
            var scene = HighLogic.LoadedScene;
            if (scene == GameScenes.FLIGHT)
            {
                var height = GUILayout.Height(Screen.height / 2);
                if (guiEnabled)
                    controlWinPos = GUILayout.Window(956, controlWinPos,
                                                     ControlWindow,
                                                     "Servo Control",
                                                     GUILayout.MinWidth(210));
                if (groupEditorEnabled)
                    groupEditorWinPos = GUILayout.Window(958, groupEditorWinPos,
                                                    GroupEditorWindow,
                                                    "Servo Group Editor",
                                                    GUILayout.Width(250),
                                                    height);
                refreshKeysFromGUI();
            }
            else if (scene == GameScenes.EDITOR || scene == GameScenes.SPH)
            {
                var height = GUILayout.Height(Screen.height / 2);
                if (guiEnabled)
                    editorWinPos = GUILayout.Window(957, editorWinPos,
                                                    EditorWindow,
                                                    "Servo Configuration",
                                                    GUILayout.Width(250),
                                                    height);
            }
        }
    }
}
