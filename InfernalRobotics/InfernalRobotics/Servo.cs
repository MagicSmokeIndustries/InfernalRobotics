using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using MuMech;

namespace MuMech
{
    [Serializable()]
    public class ServoGroup
    {
        public string name = "New Group";
        public string key = "";
        public string revKey = "";
    }
}

public class MuMechServo : MuMechToggle
{
    protected bool configsLoaded = false;
    protected bool loadFromSFS = false;


    public string servoName = "New Servo";
    public int group = -1;

    protected static List<MuMechServo> allServos = new List<MuMechServo>();
    protected static MuMechServo GUIController = null;
    protected static List<MuMech.ServoGroup> groups = new List<MuMech.ServoGroup>();

    protected static Rect winPos;
    protected static Rect editorWinPos;
    protected static Vector2 editorScroll = Vector2.zero;
    protected static bool resetWin = false;

    public override void onFlightStateLoad(Dictionary<string, KSPParseable> parsedData)
    {
        loadFromSFS = true;
        base.onFlightStateLoad(parsedData);
    }

    protected override void onPartAttach(Part parent)
    {
        if (groups.Count == 0)
        {
            groups.Add(new MuMech.ServoGroup());
        }
        if (group < 0)
        {
            group = groups.Count - 1;
        }

        base.onPartAttach(parent);
    }

    protected override void onPartDetach()
    {
        resetWin = true;
        base.onPartDetach();
    }

    //motion lock start
    [KSPEvent(guiActive = true, guiName = "Engage Lock")]
    public void Activate()
    {
        isRotationLock = true;
        Events["Activate"].active = false;
        Events["Deactivate"].active = true;
    }

    [KSPEvent(guiActive = true, guiName = "Disengage Lock", active = false)]
    public void Deactivate()
    {
        isRotationLock = false;
        Events["Activate"].active = true;
        Events["Deactivate"].active = false;
    }
    //motion lock stop

    public override void onBackup()
    {
        if (configsLoaded)
        {
            Dictionary<string, object> settings = new Dictionary<string, object>();
            settings["name"] = servoName;
            settings["rot"] = rotation;
            settings["trans"] = translation;
            if (group >= 0)
            {
                settings["group"] = groups[group].name;
                settings["key"] = groups[group].key;
                settings["revkey"] = groups[group].revKey;
            }
            else
            {
                settings["group"] = "";
                settings["key"] = "";
                settings["revkey"] = "";
            }
            customPartData = Convert.ToBase64String(KSP.IO.IOUtils.SerializeToBinary(settings)).Replace("=", "*").Replace("/", "|");
        }
        base.onBackup();
    }

    protected override void onEditorUpdate()
    {
        if (GUIController == null)
        {
            RenderingManager.AddToPostDrawQueue(0, new Callback(editorDrawGUI));
            GUIController = this;
        }
        if (group < 0)
        {
            group = groups.Count - 1;
        }
        base.onEditorUpdate();
    }

    protected override void onPartStart()
    {
        allServos.Add(this);
        if (customPartData != "")
        {
            Dictionary<string, object> settings = (Dictionary<string, object>)KSP.IO.IOUtils.DeserializeFromBinary(Convert.FromBase64String(customPartData.Replace("*", "=").Replace("|", "/")));
            servoName = (string)settings["name"];
            string groupName = (string)settings["group"];
            if (groupName != "")
            {
                bool found = false;
                for (int i = 0; i < groups.Count; i++)
                {
                    if (groups[i].name == groupName)
                    {
                        found = true;
                        group = i;
                        break;
                    }
                }
                if (!found)
                {
                    MuMech.ServoGroup newGroup = new MuMech.ServoGroup();
                    newGroup.name = groupName;
                    newGroup.key = (string)settings["key"];
                    newGroup.revKey = (string)settings["revkey"];
                    groups.Add(newGroup);
                    group = groups.Count - 1;
                }
            }
            if (group >= 0)
            {
                rotateKey = translateKey = groups[group].key;
                revRotateKey = revTranslateKey = groups[group].revKey;
            }
            if (!loadFromSFS)
            {
                rotation = (float)settings["rot"];
                translation = (float)settings["trans"];
            }
        }
        configsLoaded = true;
        base.onPartStart();
    }

    protected override void onFlightStart()
    {
        if (group >= 0)
        {
            rotateKey = translateKey = groups[group].key;
            revRotateKey = revTranslateKey = groups[group].revKey;
        }
        base.onFlightStart();
    }

    protected override void onPartDestroy()
    {
        allServos.Remove(this);
        if (GUIController == this)
        {
            RenderingManager.RemoveFromPostDrawQueue(0, new Callback(editorDrawGUI));
            RenderingManager.RemoveFromPostDrawQueue(0, new Callback(drawGUI));
            GUIController = null;
        }
        resetWin = true;
        base.onPartDestroy();
    }

    [KSPAction("Move +")]
    public void MovePlusAction(KSPActionParam param)
    {
        switch (param.type)
        {
            case KSPActionType.Activate:
                moveFlags |= 0x100;
                break;
            case KSPActionType.Deactivate:
                moveFlags &= ~0x100;
                break;
        }
    }

    [KSPAction("Move -")]
    public void MoveMinusAction(KSPActionParam param)
    {
        switch (param.type)
        {
            case KSPActionType.Activate:
                moveFlags |= 0x200;
                break;
            case KSPActionType.Deactivate:
                moveFlags &= ~0x200;
                break;
        }
    }

    [KSPAction("Move Center")]
    public void MoveCenterAction(KSPActionParam param)
    {
        switch (param.type)
        {
            case KSPActionType.Activate:
                moveFlags |= 0x400;
                break;
            case KSPActionType.Deactivate:
                moveFlags &= ~0x400;
                break;
        }
    }

    protected override void onPartFixedUpdate()
    {
        foreach (Part p in vessel.parts)
        {
            if (p.attachJoint != null)
            {
                p.attachJoint.breakForce = breakingForce;
                p.attachJoint.breakTorque = breakingTorque;
            }
        }
        if ((vessel != null) && (GUIController == null))
        {
            RenderingManager.AddToPostDrawQueue(0, new Callback(drawGUI));
            GUIController = this;
        }

        base.onPartFixedUpdate();
    }

    private void editorWindowGUI(int windowID)
    {
        Vector2 mousePos = Input.mousePosition;
        mousePos.y = Screen.height - mousePos.y;

        editorScroll = GUILayout.BeginScrollView(editorScroll, false, false, GUILayout.MaxHeight(Screen.height / 2));

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Group Name", GUILayout.ExpandWidth(true));
        GUILayout.Label("Keys", GUILayout.Width(40));
        if (groups.Count > 1)
        {
            GUILayout.Space(60);
        }
        GUILayout.EndHorizontal();

        for (int i = 0; i < groups.Count; i++)
        {
            MuMech.ServoGroup grp = groups[i];

            GUILayout.BeginHorizontal();
            string tmp = GUILayout.TextField(grp.name, GUILayout.ExpandWidth(true));
            if (grp.name != tmp)
            {
                grp.name = tmp;
                configsLoaded = true;
            }
            tmp = GUILayout.TextField(grp.key, GUILayout.Width(20));
            if (grp.key != tmp)
            {
                grp.key = tmp;
                configsLoaded = true;
            }
            tmp = GUILayout.TextField(grp.revKey, GUILayout.Width(20));
            if (grp.revKey != tmp)
            {
                grp.revKey = tmp;
                configsLoaded = true;
            }
            if (i > 0)
            {
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    foreach (MuMechServo servo in allServos)
                    {
                        if (servo.group >= i)
                        {
                            servo.group--;
                        }
                    }
                    groups.RemoveAt(i);
                    resetWin = true;
                    return;
                }
            }
            else
            {
                if (groups.Count > 1)
                {
                    GUILayout.Space(60);
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            GUILayout.Space(20);

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Servo Name", GUILayout.ExpandWidth(true));
            GUILayout.Label("Rotate", GUILayout.Width(40));
            if (groups.Count > 1)
            {
                GUILayout.Label("Group", GUILayout.Width(40));
            }
            GUILayout.EndHorizontal();

            foreach (MuMechServo servo in allServos)
            {
                if (servo.group == i)
                {
                    GUILayout.BeginHorizontal();
                    servo.servoName = GUILayout.TextField(servo.servoName, GUILayout.ExpandWidth(true));
                    if (editorWinPos.Contains(mousePos))
                    {
                        servo.SetHighlight(GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition));
                    }
                    if (GUILayout.Button("<", GUILayout.Width(20)))
                    {
                        servo.transform.RotateAround(servo.transform.up, Mathf.PI / 4);
                    }
                    if (GUILayout.Button(">", GUILayout.Width(20)))
                    {
                        servo.transform.RotateAround(servo.transform.up, -Mathf.PI / 4);
                    }
                    if (groups.Count > 1)
                    {
                        if (i > 0)
                        {
                            if (GUILayout.Button("/\\", GUILayout.Width(20)))
                            {
                                servo.group--;
                                configsLoaded = true;
                            }
                        }
                        else
                        {
                            GUILayout.Space(20);
                        }
                        if (i < (groups.Count - 1))
                        {
                            if (GUILayout.Button("\\/", GUILayout.Width(20)))
                            {
                                servo.group++;
                                configsLoaded = true;
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

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("Add new Group"))
        {
            groups.Add(new MuMech.ServoGroup());
        }

        GUILayout.EndVertical();

        GUILayout.EndScrollView();

        GUI.DragWindow();
    }

    private void WindowGUI(int windowID)
    {
        GUILayout.BeginVertical();

        foreach (MuMechServo servo in allServos)
        {
            servo.moveFlags &= ~0xFF;
        }

        for (int i = 0; i < groups.Count; i++)
        {
            List<MuMechServo> groupServos = new List<MuMechServo>();
            foreach (MuMechServo servo in allServos)
            {
                if ((servo.group == i) && (servo.vessel == FlightGlobals.ActiveVessel))
                {
                    groupServos.Add(servo);
                }
            }
            if (groupServos.Count > 0)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label(groups[i].name, GUILayout.ExpandWidth(true));
                int forceFlags = (GUILayout.RepeatButton("<", GUILayout.Width(20))?1:0) + (GUILayout.RepeatButton("O", GUILayout.Width(20))?4:0) + (GUILayout.RepeatButton(">", GUILayout.Width(20))?2:0);
                foreach (MuMechServo servo in groupServos)
                {
                    servo.moveFlags |= forceFlags;
                }

                GUILayout.EndHorizontal();
            }
        }

        GUILayout.EndVertical();

        GUI.DragWindow();
    }

    private void drawGUI()
    {
        bool servosPresent = false;
        foreach (MuMechServo servo in allServos)
        {
            if (servo.vessel == FlightGlobals.ActiveVessel)
            {
                servosPresent = true;
                break;
            }
        }
        if (servosPresent && InputLockManager.IsUnlocked(ControlTypes.LINEAR))
        {
            if (winPos.x == 0 && winPos.y == 0)
            {
                winPos = new Rect(Screen.width / 2, Screen.height / 2, 10, 10);
            }

            GUI.skin = MuUtils.DefaultSkin;

            winPos = GUILayout.Window(956, winPos, WindowGUI, "Servo Control", GUILayout.MinWidth(150));
        }
    }

    private void editorDrawGUI()
    {
        if (editorWinPos.x == 0 && editorWinPos.y == 0)
        {
            editorWinPos = new Rect(Screen.width - 260, 50, 10, 10);
        }
        if (resetWin)
        {
            winPos = new Rect(winPos.x, winPos.y, 10, 10);
            resetWin = false;
        }

        GUI.skin = MuUtils.DefaultSkin;

        editorWinPos = GUILayout.Window(957, editorWinPos, editorWindowGUI, "Servo Configuration", GUILayout.Width(250), GUILayout.Height(Screen.height / 2));
    }
}
