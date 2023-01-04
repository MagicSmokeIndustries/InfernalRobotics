using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using InfernalRobotics_v3.Interfaces;
using InfernalRobotics_v3.Interceptors;
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
		
		public List<IServoGroup> ServoGroups;

		private int loadedVesselCounter = 0;

		public static Controller Instance { get { return ControllerInstance; } }

		public static bool APIReady { get { return ControllerInstance != null && ControllerInstance.ServoGroups != null && ControllerInstance.ServoGroups.Count > 0; } }

		public static void MoveServo(IServoGroup from, IServoGroup to, int index, IServo servo)
		{
			((ServoGroup)from.group).RemoveControl(servo);
			((ServoGroup)to.group).AddControl(servo, index);
		}

		private static void EditorAddServo(IServo servo)
		{
			if(!Instance)
				return;
			
			if(Instance.ServoGroups == null)
				Instance.ServoGroups = new List<IServoGroup>();

			ServoGroup controlGroup = null;

			if(!string.IsNullOrEmpty(servo.GroupName))
			{
				foreach(ServoGroup cg in Instance.ServoGroups)
				{
					if(servo.GroupName == cg.Name)
					{
						controlGroup = cg;
						break;
					}
				}

				if(controlGroup == null)
					Instance.ServoGroups.Add(new ServoGroup(servo, servo.GroupName));
				else
					controlGroup.AddControl(servo, -1);
			}

			Logger.Log("[ServoController] AddServo finished successfully", Logger.Level.Debug);

			if(Gui.WindowManager.Instance != null)
				Gui.WindowManager.Instance.Invalidate();
		}

		private static void EditorRemoveServo(IServo servo)
		{
			if(!Instance)
				return;

			if(Instance.ServoGroups == null)
				return;

			for(int i = 0; i < Instance.ServoGroups.Count; i++)
			{
				if(Instance.ServoGroups[i].Name == servo.GroupName)
				{
					((ServoGroup)Instance.ServoGroups[i].group).RemoveControl(servo);
					
					if(Instance.ServoGroups[i].Servos.Count == 0)
						Instance.ServoGroups.RemoveAt(i--);
				}
			}

			Instance._ServoToServoInterceptor.Remove(servo);

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
				EditorAddServo(p);

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
				EditorRemoveServo(p);

			Logger.Log("[ServoController] OnPartRemove finished successfully", Logger.Level.Debug);
		}

		private void OnEditorUnOrRedo(ShipConstruct ship)
		{
			if(!Instance)
				return;

			if(Instance.ServoGroups == null)
				return;

			List<ModuleIRServo_v3> allServos = new List<ModuleIRServo_v3>();

			foreach(Part p in ship.parts)
			{
				ModuleIRServo_v3 servo = p.GetComponent<ModuleIRServo_v3>();

				if(servo != null)
					allServos.Add(servo);
			}

			List<ModuleIRServo_v3> servosToRemove = new List<ModuleIRServo_v3>();

			for(int i = 0; i < Instance.ServoGroups.Count; i++)
			{
				for(int j = 0; j < Instance.ServoGroups[i].Servos.Count; j++)
				{
					ModuleIRServo_v3 servo = (ModuleIRServo_v3)Instance.ServoGroups[i].Servos[j].servo;

					if(!allServos.Contains(servo))
						servosToRemove.Add(servo);
					else
						allServos.Remove(servo);
				}
			}

			foreach(ModuleIRServo_v3 servo in servosToRemove)
				EditorRemoveServo(servo);

			foreach(ModuleIRServo_v3 servo in allServos)
				EditorAddServo(servo);
		}

		// internal (not private) because we need to call it from "ModuleIRServo_v3.RemoveFromSymmetry2"
		internal void RebuildServoGroupsEditor(ShipConstruct ship = null)
		{
			if(ship == null)
				ship = EditorLogic.fetch.ship;

			ServoGroups = null;

			var groups = new List<IServoGroup>();
			var groupMap = new Dictionary<string, int>();

			foreach(Part p in ship.Parts)
			{
				foreach(var servo in p.ToServos())
				{
					if(!groupMap.ContainsKey(servo.GroupName))
					{
						groups.Add(new ServoGroup(servo, servo.GroupName));
						groupMap[servo.GroupName] = groups.Count - 1;
					}
					else
					{
						IServoGroup g = groups[groupMap[servo.GroupName]];
						((ServoGroup)g.group).AddControl(servo, -1);
					}
				}
			}

			if(groups.Count > 0)
				ServoGroups = groups;

			if(Gui.WindowManager.Instance != null)
				Gui.WindowManager.Instance.Invalidate();
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

		// internal (not private) because we need to call it from "ModuleIRServo_v3.RemoveFromSymmetry2"
		internal void RebuildServoGroupsFlight()
		{
			List<IServoGroup> oldServoGroups = (ServoGroups != null) ? ServoGroups : new List<IServoGroup>();

			ServoGroups = new List<IServoGroup>();

			for(int i = 0; i < FlightGlobals.Vessels.Count; i++)
			{
				var vessel = FlightGlobals.Vessels[i];

				if(!vessel.loaded)
					continue;
				
				var groups = new List<IServoGroup>();
				var groupMap = new Dictionary<string, int>();

				foreach(var servo in vessel.ToServos())
				{
					if(!groupMap.ContainsKey(servo.GroupName))
					{
						ServoGroup g = new ServoGroup(servo, vessel, servo.GroupName);
						groups.Add(g);
						groupMap[servo.GroupName] = groups.Count - 1;

						// search old group and copy settings
						for(int j = 0; j < oldServoGroups.Count; j++)
						{
							if(oldServoGroups[j].Name == g.Name)
							{
								g.Expanded = oldServoGroups[j].Expanded;
								g.AdvancedMode = oldServoGroups[j].AdvancedMode;
								j = int.MaxValue - 1;
							}
						}
					}
					else
					{
						IServoGroup g = groups[groupMap[servo.GroupName]];
						((ServoGroup)g.group).AddControl(servo, -1);
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

		private void OnVesselWasModified(Vessel v)
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

			if(HighLogic.LoadedSceneIsFlight)
			{
				GameEvents.onVesselChange.Add(OnVesselChange);
				GameEvents.onVesselWasModified.Add(OnVesselWasModified);
				GameEvents.onVesselLoaded.Add(OnVesselLoaded);
				GameEvents.onVesselDestroy.Add(OnVesselUnloaded);
				GameEvents.onVesselGoOnRails.Add(OnVesselUnloaded);
				ControllerInstance = this;
			}
			else if(HighLogic.LoadedSceneIsEditor)
			{
				GameEvents.onPartAttach.Add(OnEditorPartAttach);
				GameEvents.onPartRemove.Add(OnEditorPartRemove);
				GameEvents.onEditorUndo.Add(OnEditorUnOrRedo);
				GameEvents.onEditorRedo.Add(OnEditorUnOrRedo);
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
			// because OnVesselDestroy and OnVesselGoOnRails seem to only work for active vessel I had to build this stupid workaround
			if(HighLogic.LoadedSceneIsFlight)
			{
				if(FlightGlobals.Vessels.Count(v => v.loaded) != loadedVesselCounter)
				{
					RebuildServoGroupsFlight();
					loadedVesselCounter = FlightGlobals.Vessels.Count(v => v.loaded);
				}

				if(ServoGroups == null)
					return;

				// check if all servos stopped running and enable the struts, otherwise disable wheel autostruts
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

			if(HighLogic.LoadedSceneIsFlight)
			{
				GameEvents.onVesselChange.Remove(OnVesselChange);
				GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
				GameEvents.onVesselLoaded.Remove(OnVesselLoaded);
				GameEvents.onVesselDestroy.Remove(OnVesselUnloaded);
				GameEvents.onVesselGoOnRails.Remove(OnVesselUnloaded);
			}
			else if(HighLogic.LoadedSceneIsEditor)
			{
				GameEvents.onPartAttach.Remove(OnEditorPartAttach);
				GameEvents.onPartRemove.Remove(OnEditorPartRemove);
				GameEvents.onEditorUndo.Remove(OnEditorUnOrRedo);
				GameEvents.onEditorRedo.Remove(OnEditorUnOrRedo);
				GameEvents.onEditorLoad.Remove(OnEditorLoad);
				GameEvents.onEditorRestart.Remove(OnEditorRestart);
			}

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

		////////////////////////////////////////
		// Interceptors

		private Dictionary<IServo, IServo> _ServoToServoInterceptor = new Dictionary<IServo, IServo>();

		public IServo GetInterceptor(IServo servo)
		{
			// check if this is already an interceptor
			if(servo.servo != servo)
				return servo;

			foreach(var pair in _ServoToServoInterceptor)
			{
				if(pair.Key == servo)
					return pair.Value;
			}

			IServoInterceptor interceptor = new IServoInterceptor(servo);

			_ServoToServoInterceptor.Add(servo, interceptor);

			return interceptor;
		}

		private Dictionary<IServoGroup, IServoGroup> _ServoGroupToServoGroupInterceptor = new Dictionary<IServoGroup, IServoGroup>();

		public IServoGroup GetInterceptor(IServoGroup group)
		{
			// check if this is already an interceptor
			if(group.group != group)
				return group;

			foreach(var pair in _ServoGroupToServoGroupInterceptor)
			{
				if(pair.Key == group)
					return pair.Value;
			}

			IServoGroupInterceptor interceptor = new IServoGroupInterceptor(group);

			_ServoGroupToServoGroupInterceptor.Add(group, interceptor);

			return interceptor;
		}
	}
}
