using System;
using InfernalRobotics_v3.Control;
using InfernalRobotics_v3.Control.Servo;
using InfernalRobotics_v3.Module;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace InfernalRobotics_v3.Command
{
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class ControllerFlight : Controller
	{
		public override string AddonName { get { return this.name; } }
	}

	[KSPAddon(KSPAddon.Startup.EditorAny, false)]
	public class ControllerEditor : Controller
	{
		public override string AddonName { get { return this.name; } }
	}

	public class Controller : MonoBehaviour
	{
		public virtual String AddonName { get; set; }

		protected static Controller ControllerInstance;
		
		public List<ControlGroup> ServoGroups;

	//	private int partCounter;
		private int loadedVesselCounter = 0;

		public static Controller Instance { get { return ControllerInstance; } }

		public static bool APIReady { get { return ControllerInstance != null && ControllerInstance.ServoGroups != null && ControllerInstance.ServoGroups.Count > 0; } }

		public static bool bUserInput = false;
		public static bool bMove = false; // gibt an, ob das Zeug jetzt ausgeführt werden soll oder nicht


		// FEHLER, Zeichnungs-Zeug, was ich für's IK brauche (das gehört ja eigentlich hier rein)
		private static LineDrawer[] al = new LineDrawer[13];
		private static Color[] alColor = new Color[13];

		private static void DrawRelative(int idx, Vector3 p_from, Vector3 p_vector)
		{
			al[idx].DrawLineInGameView(p_from, p_from + p_vector, alColor[idx]);
		}


		static Controller()
		{
			for(int i = 0; i < 13; i++)
				al[i] = new LineDrawer();

			alColor[0] = Color.red;
			alColor[1] = Color.green;
			alColor[2] = Color.yellow;
			alColor[3] = Color.magenta;	// axis
			alColor[4] = Color.blue;		// secondaryAxis
			alColor[5] = Color.white;
			alColor[6] = new Color(33.0f / 255.0f, 154.0f / 255.0f, 193.0f / 255.0f);
			alColor[7] = new Color(154.0f / 255.0f, 193.0f / 255.0f, 33.0f / 255.0f);
			alColor[8] = new Color(193.0f / 255.0f, 33.0f / 255.0f, 154.0f / 255.0f);
			alColor[9] = new Color(193.0f / 255.0f, 33.0f / 255.0f, 255.0f / 255.0f);
			alColor[10] = new Color(244.0f / 255.0f, 238.0f / 255.0f, 66.0f / 255.0f);
	//		alColor[11] = new Color(209.0f / 255.0f, 247.0f / 255.0f, 74.0f / 255.0f);
			alColor[11] = new Color(244.0f / 255.0f, 170.0f / 255.0f, 66.0f / 255.0f); // orange
			alColor[12] = new Color(247.0f / 255.0f, 186.0f / 255.0f, 74.0f / 255.0f);
		}

		public static void MoveServo(ControlGroup from, ControlGroup to, int index, IServo servo)
		{
			from.RemoveControl(servo);
			to.AddControl(servo, index);
		}

		public static void AddServo(IServo servo)
		{
			if(!Instance)
				return;
			
			if(Instance.ServoGroups == null)
				Instance.ServoGroups = new List<ControlGroup>();

			ControlGroup controlGroup = null;

			if(!string.IsNullOrEmpty(servo.GroupName))
			{
				foreach(ControlGroup cg in Instance.ServoGroups)
				{
					if(servo.GroupName == cg.Name)
					{
						controlGroup = cg;
						break;
					}
				}
				if(controlGroup == null)
				{
					Instance.ServoGroups.Add(new ControlGroup(servo));
					Logger.Log("[ServoController] AddServo adding new ControlGroup", Logger.Level.Debug);
					return;
				}
			}

			if(controlGroup == null)
			{
				if(Instance.ServoGroups.Count < 1)
					Instance.ServoGroups.Add(new ControlGroup());
				controlGroup = Instance.ServoGroups[Instance.ServoGroups.Count - 1];
			}

			controlGroup.AddControl(servo, -1);

			Logger.Log("[ServoController] AddServo finished successfully", Logger.Level.Debug);

			if(Gui.WindowManager.Instance != null)
				Gui.WindowManager.Instance.Invalidate();
		}

		public static void RemoveServo(IServo servo)
		{
			if(!Instance)
				return;

			if(Instance.ServoGroups == null)
				return;

			int num = 0;
			for(int i = 0; i < Instance.ServoGroups.Count; i++)
			{
				if(Instance.ServoGroups[i].Name == servo.GroupName)
				{
					Instance.ServoGroups[i].RemoveControl(servo);
				}

				num += Instance.ServoGroups[i].Servos.Count;
			}

			if(Gui.WindowManager.Instance)
				Gui.WindowManager.Instance.Invalidate();

// FEHLER, schneller Bugfix -> geht immer, weil der das nur tut, wenn's was drin hat
if(Gui.IRBuildAid.IRBuildAidManager.Instance)
	Gui.IRBuildAid.IRBuildAidManager.Instance.HideServoRange(servo);

			Logger.Log("[ServoController] AddServo finished successfully", Logger.Level.Debug);
		}

		private void OnEditorPartAttach(GameEvents.HostTargetAction<Part, Part> hostTarget)
		{
			Part part = hostTarget.host;

	//		if(EditorLogic.fetch.ship.parts.Count > partCounter)
			{
	//			if((partCounter != 1) && (EditorLogic.fetch.ship.parts.Count != 1))
				{
					foreach(var p in part.GetChildServos())
						AddServo(p);
	//				partCounter = EditorLogic.fetch.ship.parts.Count;
				}
			}
/*			
			if((EditorLogic.fetch.ship.parts.Count == 0) && (partCounter == 0))
			{
				foreach(var p in part.GetChildServos())
					AddServo(p);
				partCounter = EditorLogic.fetch.ship.parts.Count;
			}
*/
			Logger.Log("[ServoController] OnPartAttach finished successfully", Logger.Level.Debug);
		}

		private void OnEditorPartRemove(GameEvents.HostTargetAction<Part, Part> hostTarget)
		{
			Part part = hostTarget.target;
			try
			{
				var servos = part.ToServos();
				foreach(var temp in servos)
					temp.EditorReset();
			}
			catch(Exception ex)
			{
				Logger.Log("[ServoController] OnPartRemove Error: " + ex, Logger.Level.Debug);
			}

			foreach(var p in part.GetChildServos())
				RemoveServo(p);

	//		partCounter = EditorLogic.fetch.ship.parts.Count == 1 ? 0 : EditorLogic.fetch.ship.parts.Count;

			Logger.Log("[ServoController] OnPartRemove finished successfully", Logger.Level.Debug);
		}

		private void RebuildServoGroupsEditor(ShipConstruct ship = null)
		{
//return; // FEHLER, ist das nötig? jeder servo meldet sich ja selber...
	// FEHLER, aber ja gut... kann man von mir aus -> mal testen, ob ein "Load" eines Schiffs das auch auslöste

			if(ship == null)
				ship = EditorLogic.fetch.ship;

			ServoGroups = null;

			var groups = new List<ControlGroup>();
			var groupMap = new Dictionary<string, int>();

			foreach(Part p in ship.Parts)
			{
				foreach(var servo in p.ToServos())
				{
					if(!groupMap.ContainsKey(servo.GroupName))
					{
						groups.Add(new ControlGroup(servo));
						groupMap[servo.GroupName] = groups.Count - 1;
					}
					else
					{
						ControlGroup g = groups[groupMap[servo.GroupName]];
						g.AddControl(servo, -1);
					}
				}
			}

			if(groups.Count > 0)
				ServoGroups = groups;
		}
	   
/*		private void OnEditorShipModified(ShipConstruct ship)
		{
	//		RebuildServoGroupsEditor(ship); -> called too often

			if(Gui.WindowManager.Instance != null)
				Gui.WindowManager.Instance.Invalidate();

			if(Gui.IRBuildAid.IRBuildAidManager.Instance)
				Gui.IRBuildAid.IRBuildAidManager.Reset();
			
			partCounter = EditorLogic.fetch.ship.parts.Count == 1 ? 0 : EditorLogic.fetch.ship.parts.Count;
			Logger.Log("[ServoController] OnEditorShipModified finished successfully", Logger.Level.Debug);
		}
*/
		private void OnEditorRestart()
		{
			ServoGroups = null;

			if(Gui.WindowManager.Instance != null)
				Gui.WindowManager.Instance.Invalidate();

			if(Gui.IRBuildAid.IRBuildAidManager.Instance)
				Gui.IRBuildAid.IRBuildAidManager.Reset();

			Logger.Log ("OnEditorRestart called", Logger.Level.Debug);
		}

		private void OnEditorLoad(ShipConstruct s, KSP.UI.Screens.CraftBrowserDialog.LoadType t)
		{
//			OnEditorShipModified (s); -> raus und neu alles hier drin... das baut sonst zu oft alles neu und schmeisst alles weg -> total unnötig

			RebuildServoGroupsEditor(s);

			if(Gui.WindowManager.Instance != null)
				Gui.WindowManager.Instance.Invalidate();

			if(Gui.IRBuildAid.IRBuildAidManager.Instance)
				Gui.IRBuildAid.IRBuildAidManager.Reset();
			
//			partCounter = EditorLogic.fetch.ship.parts.Count == 1 ? 0 : EditorLogic.fetch.ship.parts.Count;
	//		Logger.Log("[ServoController] OnEditorShipModified finished successfully", Logger.Level.Debug);

			Logger.Log ("OnEditorLoad called", Logger.Level.Debug);
		}

		private void RebuildServoGroupsFlight()
		{
List<ControlGroup> old = ServoGroups; // FEHLER, schneller Bugfix
if(old == null) old = new List<ControlGroup>();

			ServoGroups = new List<ControlGroup>();

			for(int i = 0; i < FlightGlobals.Vessels.Count; i++)
			{
				var vessel = FlightGlobals.Vessels[i];

				if(!vessel.loaded)
					continue;
				
				var groups = new List<ControlGroup>();
				var groupMap = new Dictionary<string, int>();

				foreach(var servo in vessel.ToServos())
				{
					if(!groupMap.ContainsKey(servo.GroupName))
					{
ControlGroup cg = new ControlGroup(servo, vessel);
						groups.Add(cg);
						groupMap[servo.GroupName] = groups.Count - 1;

// FEHLER, schneller Bugfix -> alles anpassen
for(int j = 0; j < old.Count; j++)
{
	if(old[j].Name == cg.Name)
	{
		cg.bIsAdvancedOn = old[j].bIsAdvancedOn;
		cg.Expanded = old[j].Expanded;
	}
}
					}
					else
					{
						ControlGroup g = groups[groupMap[servo.GroupName]];
						g.AddControl(servo, -1);
					}
				}

				ServoGroups.AddRange(groups);
			}

			if(ServoGroups.Count == 0)
				ServoGroups = null;

			if(Gui.WindowManager.Instance != null)
				Gui.WindowManager.Instance.Invalidate();
		}

		private void OnVesselChange(Vessel v)
		{
			Logger.Log(string.Format("[ServoController] vessel {0}", v.name));

			RebuildServoGroupsFlight();

// FEHLER, ist sowas echt nötig hier???
		//	foreach(var servo in v.ToServos())
		//		servo.SetupJoints();

			Logger.Log("[ServoController] OnVesselChange finished successfully", Logger.Level.Debug);
		}

		private void OnVesselPartCountModified(Vessel v)
		{
			RebuildServoGroupsFlight();
		}

		private void OnVesselLoaded(Vessel v)
		{
			Logger.Log("[ServoController] OnVesselLoaded, v=" + v.GetName(), Logger.Level.Debug);
			RebuildServoGroupsFlight();
		}

		private void OnVesselUnloaded(Vessel v)
		{
			Logger.Log("[ServoController] OnVesselUnloaded, v=" + v.GetName(), Logger.Level.Debug);
			RebuildServoGroupsFlight();
		}

		private void Awake()
		{
			Logger.Log("[ServoController] awake, AddonName = " + this.AddonName);

			GameScenes scene = HighLogic.LoadedScene;

			if(scene == GameScenes.FLIGHT)
			{
				GameEvents.onVesselChange.Add(OnVesselChange);
				GameEvents.onVesselPartCountChanged.Add(OnVesselPartCountModified);
				GameEvents.onVesselLoaded.Add (OnVesselLoaded);
				GameEvents.onVesselDestroy.Add (OnVesselUnloaded);
				GameEvents.onVesselGoOnRails.Add (OnVesselUnloaded);
				ControllerInstance = this;
			}
			else if(scene == GameScenes.EDITOR)
			{
				GameEvents.onPartAttach.Add(OnEditorPartAttach);
				GameEvents.onPartRemove.Add(OnEditorPartRemove);
	//			GameEvents.onEditorShipModified.Add(OnEditorShipModified);
				GameEvents.onEditorLoad.Add(OnEditorLoad);
				GameEvents.onEditorRestart.Add(OnEditorRestart);
				ControllerInstance = this;
			}
			else
			{
				ControllerInstance = null;
			}

			Logger.Log("[ServoController] awake finished successfully, AddonName = " + this.AddonName, Logger.Level.Debug);
		}

		/// <summary>
		/// Sets the wheel auto-struting for the Vessel v. 
		/// In flight mode we need to set to false before moving 
		/// the joint and to true aferwards
		/// </summary>
		public static void SetWheelAutoStruts(bool value, Vessel v)
		{
			if(!HighLogic.LoadedSceneIsFlight)
				return;

			/*foreach(var p in v.Parts)
			{
				if(!value)
				{
					p.autoStrutMode = Part.AutoStrutMode.Off;
					p.UpdateAutoStrut ();
				}
			}
*/
			/*var activeVesselWheels = v.FindPartModulesImplementing<ModuleWheelBase>();
			foreach(var mwb in activeVesselWheels)
			{
				if(value)
				{
					if(!mwb.autoStrut) //we only need to Cycle once
						mwb.CycleWheelStrut();
				}
				else
					mwb.ReleaseWheelStrut();

				mwb.autoStrut = value;

			}*/
		}

		private void FixedUpdate()
		{
			//because OnVesselDestroy and OnVesselGoOnRails seem to only work for active vessel I had to build this stupid workaround
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(FlightGlobals.Vessels.Count(v => v.loaded) != loadedVesselCounter)
				{
					RebuildServoGroupsFlight ();
					loadedVesselCounter = FlightGlobals.Vessels.Count(v => v.loaded);
				}

				if(ServoGroups == null)
					return;

				//check if all servos stopped running and enable the struts, otherwise disable wheel autostruts
				var anyActive = new Dictionary<Vessel, bool>();

				foreach(var g in ServoGroups)
				{
					if(!anyActive.ContainsKey(g.Vessel))
						anyActive.Add(g.Vessel, false);
					
					foreach(var s in g.Servos)
					{
						if(s.IsMoving)
						{
							anyActive[g.Vessel] = true;
							break;
						}
					}
				}
				foreach(var pair in anyActive)
				{
					SetWheelAutoStruts(!pair.Value, pair.Key);
				}
			}
		}

		private void OnDestroy()
		{
			Logger.Log("[ServoController] destroy", Logger.Level.Debug);

			GameEvents.onVesselChange.Remove(OnVesselChange);
			GameEvents.onPartAttach.Remove(OnEditorPartAttach);
			GameEvents.onPartRemove.Remove(OnEditorPartRemove);
			GameEvents.onVesselWasModified.Remove(OnVesselPartCountModified);
//			GameEvents.onEditorShipModified.Remove(OnEditorShipModified);
			GameEvents.onEditorLoad.Remove(OnEditorLoad);
			GameEvents.onEditorRestart.Remove(OnEditorRestart);

			GameEvents.onVesselLoaded.Remove (OnVesselLoaded);
			GameEvents.onVesselDestroy.Remove (OnVesselUnloaded);
			GameEvents.onVesselGoOnRails.Remove (OnVesselUnloaded);
			Logger.Log("[ServoController] OnDestroy finished successfully", Logger.Level.Debug);
		}

		private static Part GetPartUnderCursor()
		{
			Ray ray;
			if(HighLogic.LoadedSceneIsFlight)
				ray = FlightCamera.fetch.mainCamera.ScreenPointToRay(Input.mousePosition);
			else
				ray = Camera.main.ScreenPointToRay(Input.mousePosition);

			RaycastHit hit;
			if(Physics.Raycast(ray, out hit, 557059))
				return hit.transform.gameObject.GetComponent<Part>();
			else
				return null;
		}

		protected bool KeyPressed(string key)
		{
			return (/*key != "" && vessel == FlightGlobals.ActiveVessel
					&&*/ InputLockManager.IsUnlocked(ControlTypes.LINEAR)
					&& Input.GetKey(key));
		}

		protected bool KeyUnPressed(string key)
		{
			return (/*key != "" && vessel == FlightGlobals.ActiveVessel
					&&*/ InputLockManager.IsUnlocked(ControlTypes.LINEAR)
					&& Input.GetKeyUp(key));
		}


// FEHLER, temp um die Neuberechnung zu verzögern (damit man's im Debug Modus besser sieht)
static int iii = 0;
int loswert = 50;

		public void Update()		// FEHLER, viele von dem Scheiss direkt die ControlGroup machen lassen... die aktive... einfach der den Scheiss weiterleiten... also echt jetzt
		{
			if(ControlGroup.pSetEndEffectorGroup != null)
			{
				if(/*Input.GetKey(KeyCode.LeftControl) &&*/ Input.GetKeyDown(KeyCode.Mouse0))
				{
					Part p = GetPartUnderCursor();
					if(p && (p != ControlGroup.pSetEndEffectorGroup.pEndEffector))
					{
						if(ControlGroup.pSetEndEffectorGroup.pEndEffector)
							ControlGroup.pSetEndEffectorGroup.pEndEffector.Highlight(false);
						p.Highlight(Color.cyan);
						ControlGroup.pSetEndEffectorGroup.EndSetEndEffector(p);
					}
				}
				else if(ControlGroup.pSetEndEffectorGroup.pEndEffector)
					ControlGroup.pSetEndEffectorGroup.pEndEffector.Highlight(Color.cyan);
			}

		// FEHLER, Versuch -> ich probiere mal das aktuelle Teil anzuzeigen... seine Pfeile sozusagen
		// und die dann herumzuschieben... und das später als Ziel nutzen...
			if((ControlGroup.pActiveGroup != null) && (ControlGroup.pActiveGroup.pEndEffector != null))
			{
				DrawRelative(0, ControlGroup.pActiveGroup.pEndEffector.transform.position,
					ControlGroup.pActiveGroup.pEndEffector.transform.up);

				DrawRelative(1, ControlGroup.pActiveGroup.pEndEffector.transform.position,
					ControlGroup.pActiveGroup.pEndEffector.transform.right);

				float factor = 0.01f;

				//	w, s -> vor, zurück kippen (höhen) um rechts herum
				//	a, d -> links rechts drehen (seiten) um up herum, in unserem fall forward
				//	q, e -> links rechts drehen (quer) um forward herum in unserem fall up

				bool wasThisAKey = true;

				if(KeyPressed("l")) // rechts
					ControlGroup.pActiveGroup.pPos += ControlGroup.pActiveGroup.pRot2 * ControlGroup.pActiveGroup.pRight * factor;
				else if(KeyPressed("j")) // links
					ControlGroup.pActiveGroup.pPos -= ControlGroup.pActiveGroup.pRot2 * ControlGroup.pActiveGroup.pRight * factor;
				else if(KeyPressed("i")) // rauf		(vorwärts)
					ControlGroup.pActiveGroup.pPos += ControlGroup.pActiveGroup.pRot2 * Quaternion.AngleAxis(90f, ControlGroup.pActiveGroup.pUp) * ControlGroup.pActiveGroup.pRight * factor;
				else if(KeyPressed("k")) // runter		(rückwärts)
					ControlGroup.pActiveGroup.pPos -= ControlGroup.pActiveGroup.pRot2 * Quaternion.AngleAxis(90f, ControlGroup.pActiveGroup.pUp) * ControlGroup.pActiveGroup.pRight * factor;
				else if(KeyPressed("h")) // vorwärts	(rauf)
					ControlGroup.pActiveGroup.pPos += ControlGroup.pActiveGroup.pRot2 * ControlGroup.pActiveGroup.pUp * factor;
				else if(KeyPressed("n")) // rückwärts	(runter)
					ControlGroup.pActiveGroup.pPos -= ControlGroup.pActiveGroup.pRot2 * ControlGroup.pActiveGroup.pUp * factor;
				else if(KeyPressed("w"))
					ControlGroup.pActiveGroup.pRot2 *= Quaternion.AngleAxis(1f, ControlGroup.pActiveGroup.pRight);
				else if(KeyPressed("s"))
					ControlGroup.pActiveGroup.pRot2 *= Quaternion.AngleAxis(-1f, ControlGroup.pActiveGroup.pRight);
				else if(KeyPressed("a"))
					ControlGroup.pActiveGroup.pRot2 *= Quaternion.AngleAxis(1f, Quaternion.AngleAxis(90f, ControlGroup.pActiveGroup.pUp) * ControlGroup.pActiveGroup.pRight);
				else if(KeyPressed("d"))
					ControlGroup.pActiveGroup.pRot2 *= Quaternion.AngleAxis(-1f, Quaternion.AngleAxis(90f, ControlGroup.pActiveGroup.pUp) * ControlGroup.pActiveGroup.pRight);
				else if(KeyPressed("q"))
					ControlGroup.pActiveGroup.pRot2 *= Quaternion.AngleAxis(1f, ControlGroup.pActiveGroup.pUp);
				else if(KeyPressed("e"))
					ControlGroup.pActiveGroup.pRot2 *= Quaternion.AngleAxis(-1f, ControlGroup.pActiveGroup.pUp);
				else
					wasThisAKey = false;

				if(wasThisAKey)
					Controller.bUserInput = true; // dann gab's Input

				DrawRelative(2, //pActiveGroup.pEndEffector.transform.position
					ControlGroup.pActiveGroup.pUrsprungAberNichtDasBuch
					+ /*pRot2 **/ ControlGroup.pActiveGroup.pPos,
					ControlGroup.pActiveGroup.pRot2 * ControlGroup.pActiveGroup.pUp);
				DrawRelative(3, //pActiveGroup.pEndEffector.transform.position
					ControlGroup.pActiveGroup.pUrsprungAberNichtDasBuch
					+ /*pRot2 **/ ControlGroup.pActiveGroup.pPos,
					ControlGroup.pActiveGroup.pRot2 * ControlGroup.pActiveGroup.pRight);
			}

			// calculate IK for the active group
//			wenn aktiv -> berechnen

			if((ControlGroup.pActiveGroup != null) && (ControlGroup.pActiveGroup.pEndEffector != null))
			{
				if(++iii > loswert)
				{
					ControlGroup.pActiveGroup.berechneMal();

					iii = 0;
				}
			}
		}
	}
}
