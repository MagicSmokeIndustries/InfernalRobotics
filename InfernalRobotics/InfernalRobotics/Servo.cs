using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using MuMech;
using Toolbar;

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

    //mrBlaq's addition
    private float rotateRate = Mathf.PI / 4;

	public string servoName = "New Servo";
	public int group = -1;

	protected static List<MuMechServo> allServos = new List<MuMechServo>();
	protected static MuMechServo GUIController = null;
	protected static List<MuMech.ServoGroup> groups = new List<MuMech.ServoGroup>();

	protected static Rect winPos;
	protected static Rect editorWinPos;
    protected static Rect hideEditorGUI;
	protected static Vector2 editorScroll = Vector2.zero;
	protected static bool resetWin = false;
    //private bool minimizeGUI = false;
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

	#region Lock robotic part action group
	[KSPAction("Engage Lock")]
	public void LockToggle(KSPActionParam param)
	{
		toggleLock();
	}

	public void toggleLock()
	{
		if (!isRotationLock)
		{
			Activate();
		}
		else
		{
			Deactivate();
		}
	}
	#endregion


	#region Lock robotic part event group
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
	#endregion

	#region hide gui
    //[KSPEvent(guiActive = true, guiName = "Hide GUI")]
    //public void HideGUI()
    //{
    //    isGuiShown = false;
    //    Events["HideGUI"].active = false;
    //    Events["ShowGUI"].active = true;
    //    foreach (MuMechServo servo in allServos)
    //    {
    //        servo.isGuiShown = isGuiShown;
    //    }
    //}

    //[KSPEvent(guiActive = true, guiName = "Show GUI", active = false)]
    //public void ShowGUI()
    //{
    //    isGuiShown = true;
    //    Events["HideGUI"].active = true;
    //    Events["ShowGUI"].active = false;

    //    foreach (MuMechServo servo in allServos)
    //    {
    //        servo.isGuiShown = isGuiShown;
    //    }
    //}

    //[KSPAction("Toggle GUI")]
    //public void GUIToggle(KSPActionParam param)
    //{
    //    toggleGUI();
    //}

    //public void toggleGUI()
    //{
    //    if (isGuiShown)
    //    {
    //        HideGUI();
    //    }
    //    else
    //    {
    //        ShowGUI();
    //    }
    //}
	#endregion

	public override void onBackup()
	{
		if (configsLoaded)
		{
			Dictionary<string, object> settings = new Dictionary<string, object>();
			settings["name"] = servoName;
			settings["rot"] = rotation;
			settings["trans"] = translation;

            // mrblaq - save new values
            settings["invertAxis"] = invertAxis;
            settings["minRange"] = minRange;
            settings["maxRange"] = maxRange;
            // mrblaq - save new values

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
            IRMinimizeButton = ToolbarManager.Instance.add("sirkut", "IREditorButton");
            IRMinimizeButton.TexturePath = "MagicSmokeIndustries/Textures/icon_button";
            IRMinimizeButton.ToolTip = "Infernal Robotics";
            IRMinimizeButton.OnClick += (e) => minimizeGUI = !minimizeGUI;
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

                // mrblaq - gracefully check for existing values. Otherwise, continues to use class var defined values.
                if (settings.ContainsKey("invertAxis")) { invertAxis = (bool)settings["invertAxis"]; }
                if (settings.ContainsKey("minRange")) { minRange = (string)settings["minRange"]; }
                if (settings.ContainsKey("maxRange")) { maxRange = (string)settings["maxRange"]; }
                // convert limit strings to float.
                parseMinMax();
                // mrblaq

			}
		}
		configsLoaded = true;
		base.onPartStart();
	}

	protected override void onFlightStart()
	{
        IRMinimizeButton = ToolbarManager.Instance.add("sirkut", "IREditorButton");
        IRMinimizeButton.TexturePath = "MagicSmokeIndustries/Textures/icon_button";
        IRMinimizeButton.ToolTip = "Infernal Robotics";
        IRMinimizeButton.OnClick += (e) => minimizeGUI = !minimizeGUI;
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
            IRMinimizeButton.Destroy(); //toolbar
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
				this.fxSndMotor.audio.Stop();
				this.isPlaying = false;
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
				this.fxSndMotor.audio.Stop();
				this.isPlaying = false;
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
				this.fxSndMotor.audio.Stop();
				this.isPlaying = false;
				break;
		}
	}

	protected override void onPartFixedUpdate()
	{
		foreach (Part p in vessel.parts)
		{
			if (p.attachJoint != null)
			{
				// ozraven p.attachJoint.breakForce = breakingForce;
				// ozraven p.attachJoint.breakTorque = breakingTorque;
                p.attachJoint.SetBreakingForces(breakingForce, breakingTorque); // ozraven
			}
		}
		if ((vessel != null) && (GUIController == null))
		{
			RenderingManager.AddToPostDrawQueue(0, new Callback(drawGUI));
			GUIController = this;
		}

		//sound support
		if (HighLogic.LoadedSceneIsFlight)
		{
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
					foreach (MuMechServo servo in groupServos)
					{
						if ((servo.revRotateKey != "" ? Input.GetKeyUp(servo.revRotateKey) : false) ||
							(servo.rotateKey != "" ? Input.GetKeyUp(servo.rotateKey) : false) ||
							(servo.translateKey != "" ? Input.GetKeyUp(servo.translateKey) : false) ||
							(servo.revTranslateKey != "" ? Input.GetKeyUp(servo.revTranslateKey) : false))
						{
							servo.fxSndMotor.audio.Stop();
							servo.isPlaying = false;
						}
					}
				}
			}
		}


		base.onPartFixedUpdate();
	}
    private IButton IRMinimizeButton;


    protected static Rect windowPos;
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

                // mrblaq - some new things
                GUILayout.Label("Inv", GUILayout.Width(20));
                GUILayout.Label("Min", GUILayout.Width(35));
                GUILayout.Label("Max", GUILayout.Width(35));
                //mrblaq

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
                            //servo.transform.RotateAround(servo.transform.up, Mathf.PI / 4);
							//servo.transform.Rotate(servo.transform.up, Mathf.PI / 4);

                            //mrblaq
                            servo.transform.Rotate(servo.transform.up, rotateRate);
                        }
                        if (GUILayout.Button(">", GUILayout.Width(20)))
                        {
                            //servo.transform.RotateAround(servo.transform.up, -Mathf.PI / 4);
							//servo.transform.Rotate(servo.transform.up, -Mathf.PI / 4);

                            //mrblaq
                            servo.transform.Rotate(servo.transform.up, -rotateRate);
                        }

                        // mrblaq - checkbox to invert direction
                        servo.invertAxis = GUILayout.Toggle(servo.invertAxis, "", GUILayout.Width(20));

                        // mrblaq: I dont' have limits for translation yet. So, either show limits input for rotation objects or don't.
                        if (servo.rotateJoint)
                        {
                            servo.minRange = GUILayout.TextField(servo.minRange, 4, GUILayout.Width(35));
                            servo.maxRange = GUILayout.TextField(servo.maxRange, 4, GUILayout.Width(35));
                        }
                        else
                        {
                            // mrblaq: I thought this would be 70 but looks like an input adds 2 px to each side from an inner width.
                            GUILayout.Space(78);
                        }
                        //mrblaq end



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
                MuMech.ServoGroup servo = new MuMech.ServoGroup();
                servo.name = "New Group" + (groups.Count + 1).ToString(); //add 1 to count name for grouping increment
                groups.Add(servo);
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
			    int forceFlags = (GUILayout.RepeatButton("<", GUILayout.Width(20)) ? 1 : 0) + (GUILayout.RepeatButton("O", GUILayout.Width(20)) ? 4 : 0) + (GUILayout.RepeatButton(">", GUILayout.Width(20)) ? 2 : 0);
	
	
			    //custom speed
			    int groupID = -1;
			    string tmpSpeed="1";
			    foreach (MuMechServo servo in groupServos)
			    {
	                    
				    if (groupID != servo.group)
				    {
					    tmpSpeed = GUILayout.TextField(servo.customSpeed, GUILayout.Width(40));
					    groupID = servo.group;
				    }
	                servo.customSpeed = tmpSpeed;
				    servo.moveFlags |= forceFlags;
				    if (Input.GetMouseButtonUp(0)) /*|| 
					    (servo.revRotateKey!=""?Input.GetKeyUp(servo.revRotateKey):false) || 
					    (servo.rotateKey!=""?Input.GetKeyUp(servo.rotateKey):false) || 
					    (servo.translateKey!=""?Input.GetKeyUp(servo.translateKey):false) || 
					    (servo.revTranslateKey!=""?Input.GetKeyUp(servo.revTranslateKey):false))*/
				    {
					    servo.fxSndMotor.audio.Stop();
					    servo.isPlaying = false;
				    }
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
        bool displayGUI = true;
		foreach (MuMechServo servo in allServos)
		{
			if (servo.vessel == FlightGlobals.ActiveVessel)
			{
				servosPresent = true;
                //servo.isGuiShown = servo.isGuiShown ? (displayGUI = true) : (displayGUI = false);
                //isGuiShown = servo.isGuiShown ? (displayGUI = true) : (displayGUI = false);
				break;
			}
		}


        //if (servosPresent && InputLockManager.IsUnlocked(ControlTypes.LINEAR) && displayGUI)
        if (servosPresent && displayGUI)
		{
			if (winPos.x == 0 && winPos.y == 0)
			{
				winPos = new Rect(Screen.width / 2, Screen.height / 2, 10, 10);
			}
			
			GUI.skin = MuUtils.DefaultSkin;
            if (!minimizeGUI)
            {
                winPos = GUILayout.Window(956, winPos, WindowGUI, "Servo Control", GUILayout.Width(210));
            }

			
		}
	}


	private void editorDrawGUI()
	{
		if (editorWinPos.x == 0 && editorWinPos.y == 0)
		{
			//editorWinPos = new Rect(Screen.width - 260, 50, 10, 10);

            //mrblaq - add more pixels for more width!
            editorWinPos = new Rect(Screen.width - 390, 50, 10, 10);
            hideEditorGUI = new Rect(55, 55, 10, 10);
		}
		if (resetWin)
		{
			winPos = new Rect(55, 55, 10, 10);
            hideEditorGUI = new Rect(winPos.x, winPos.y, 10, 10);
			resetWin = false;
		}

		GUI.skin = MuUtils.DefaultSkin;
        if (!minimizeGUI)
        {
            //editorWinPos = GUILayout.Window(957, editorWinPos, editorWindowGUI, "Servo Configuration", GUILayout.Width(250), GUILayout.Height(Screen.height / 2));

            // mrblaq - add more pixels for more width!
            editorWinPos = GUILayout.Window(957, editorWinPos, editorWindowGUI, "Servo Configuration", GUILayout.Width(380), GUILayout.Height(Screen.height / 2));
        }

	}
}