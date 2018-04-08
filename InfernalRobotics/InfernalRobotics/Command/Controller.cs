using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using InfernalRobotics_v3.Interfaces;
using InfernalRobotics_v3.Servo;
using InfernalRobotics_v3.Module;
using InfernalRobotics_v3.Utility;


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

		private int loadedVesselCounter = 0;

		public static Controller Instance { get { return ControllerInstance; } }

		public static bool APIReady { get { return ControllerInstance != null && ControllerInstance.ServoGroups != null && ControllerInstance.ServoGroups.Count > 0; } }

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
					Instance.ServoGroups.Add(new ControlGroup(servo));
				else
					controlGroup.AddControl(servo, -1);
			}

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

			for(int i = 0; i < Instance.ServoGroups.Count; i++)
			{
				if(Instance.ServoGroups[i].Name == servo.GroupName)
				{
					Instance.ServoGroups[i].RemoveControl(servo);
					
					if(Instance.ServoGroups[i].Servos.Count == 0)
						Instance.ServoGroups.RemoveAt(i--);
				}
			}

			if(Gui.WindowManager.Instance)
				Gui.WindowManager.Instance.Invalidate();

			if(Gui.IRBuildAid.IRBuildAidManager.Instance)
				Gui.IRBuildAid.IRBuildAidManager.Instance.HideServoRange(servo);

			Logger.Log("[ServoController] AddServo finished successfully", Logger.Level.Debug);
		}

		private void OnEditorPartAttach(GameEvents.HostTargetAction<Part, Part> hostTarget)
		{
			Part part = hostTarget.host;

			foreach(var p in part.GetChildServos())
				AddServo(p);

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
			RebuildServoGroupsEditor(s);

			if(Gui.WindowManager.Instance != null)
				Gui.WindowManager.Instance.Invalidate();

			if(Gui.IRBuildAid.IRBuildAidManager.Instance)
				Gui.IRBuildAid.IRBuildAidManager.Reset();
			
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
				GameEvents.onVesselLoaded.Add(OnVesselLoaded);
				GameEvents.onVesselDestroy.Add(OnVesselUnloaded);
				GameEvents.onVesselGoOnRails.Add(OnVesselUnloaded);
				ControllerInstance = this;								// FEHLER, oder auch behalten? könnte man ja optimieren... beim Editor auch? oder wird das jeweils beim Szenenwechsel überschrieben? -> müsste an dann hier oder für den Editor ein RebuildGroup aufrufen????
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
	}
}
