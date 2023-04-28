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

		public static IIKModule _IKModule;
		public static IServoGroup _IKServoGroup;
		
		public List<IServoGroup> ServoGroups;

		private class IServoState { public bool bIsBuildAidOn = false; }
		private Dictionary<IServo, IServoState> servosState;

		private int loadedVesselCounter = 0;

		public static Controller Instance { get { return ControllerInstance; } }

		public static void RegisterIKModule(IIKModule IKModule)
		{
			_IKModule = IKModule;

			if(ControllerInstance != null)
				Gui.WindowManager.Instance.UpdateIKButtons();
		}

		public static bool APIReady { get { return ControllerInstance != null && ControllerInstance.servosState != null && ControllerInstance.servosState.Count > 0; } }

		public static void MoveServo(IServoGroup from, IServoGroup to, int index, IServo servo)
		{
			((ServoGroup)from.group).RemoveControl(servo);
			((ServoGroup)to.group).AddControl(servo, index);
		}

		private static void EditorAddServo(IServo servo)
		{
			if(!Instance)
				return;
			
			if(Instance.servosState == null)
				Instance.servosState = new Dictionary<IServo, IServoState>();

			Instance.servosState.Add(servo, new IServoState());

			if(Instance.ServoGroups == null)
				Instance.ServoGroups = new List<IServoGroup>();

			if(!string.IsNullOrEmpty(servo.GroupName))
			{
				List<string> groups = new List<string>(servo.GroupName.Split('|'));

				foreach(ServoGroup cg in Instance.ServoGroups)
				{
					if(groups.Contains(cg.Name))
					{
						cg.AddControl(servo, -1); // FEHLER, Index noch irgendwie behalten... ist ja lächerlich, dass wir das nicht können
						groups.Remove(cg.Name);
					}
				}

				while(groups.Count > 0)
				{
					Instance.ServoGroups.Add(new ServoGroup(servo, groups[0]));
					groups.RemoveAt(0);
				}
			}

			Logger.Log("[ServoController] AddServo finished successfully", Logger.Level.Debug);

			if(Gui.WindowManager.Instance != null)
				Gui.WindowManager.Instance.Invalidate();
		}

		private static void EditorRemoveServo(IServo servo)
		{
			if(!Instance)
				return;

			Instance.servosState.Remove(servo);

			if(Instance.ServoGroups == null)
				return;

			if(!string.IsNullOrEmpty(servo.GroupName))
			{
				List<string> groups = new List<string>(servo.GroupName.Split('|'));

				for(int i = 0; i < Instance.ServoGroups.Count; i++)
				{
					if(groups.Contains(Instance.ServoGroups[i].Name))
					{
						((ServoGroup)Instance.ServoGroups[i].group).RemoveControl(servo);
					}
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

			HashSet<ModuleIRServo_v3> servosNotToAdd = new HashSet<ModuleIRServo_v3>();
			HashSet<ModuleIRServo_v3> servosToRemove = new HashSet<ModuleIRServo_v3>();

			for(int i = 0; i < Instance.ServoGroups.Count; i++)
			{
				for(int j = 0; j < Instance.ServoGroups[i].Servos.Count; j++)
				{
					ModuleIRServo_v3 servo = (ModuleIRServo_v3)Instance.ServoGroups[i].Servos[j].servo;

					if(allServos.Contains(servo))
						servosNotToAdd.Add(servo);
					else
						servosToRemove.Add(servo);
				}
			}

			foreach(ModuleIRServo_v3 servo in servosToRemove)
				EditorRemoveServo(servo);

			foreach(ModuleIRServo_v3 servo in allServos)
			{
				if(!servosNotToAdd.Contains(servo))
					EditorAddServo(servo);
			}
		}

		// internal (not private) because we need to call it from "ModuleIRServo_v3.RemoveFromSymmetry2"
		internal void RebuildServoGroupsEditor(ShipConstruct ship = null)
		{
			if(ship == null)
				ship = EditorLogic.fetch.ship;

			Instance.servosState = new Dictionary<IServo, IServoState>();

			List<IServoGroup> oldServoGroups = (ServoGroups != null) ? ServoGroups : new List<IServoGroup>();

			ServoGroups = null;

			var servoGroups = new List<IServoGroup>();
			var groupMap = new Dictionary<string, int>();

			foreach(Part p in ship.Parts)
			{
				foreach(var servo in p.ToServos())
				{
					Instance.servosState.Add(servo, new IServoState());

					if(!string.IsNullOrEmpty(servo.GroupName))
					{
						List<string> groups = new List<string>(servo.GroupName.Split('|'));

						foreach(string group in groups)
						{
							if(!groupMap.ContainsKey(group))
							{
								ServoGroup g = new ServoGroup(servo, group);
								servoGroups.Add(g);
								groupMap[group] = servoGroups.Count - 1;

								// search old group and copy settings
								for(int j = 0; j < oldServoGroups.Count; j++)
								{
									if(oldServoGroups[j].Name == g.Name)
									{
										g.Expanded = oldServoGroups[j].Expanded;
										g.AdvancedMode = oldServoGroups[j].AdvancedMode;
										g.GroupSpeedFactor = oldServoGroups[j].GroupSpeedFactor;
										g.ForwardKey = oldServoGroups[j].ForwardKey;
										g.ReverseKey = oldServoGroups[j].ReverseKey;
										j = int.MaxValue - 1;
									}
								}
							}
							else
							{
								IServoGroup g = servoGroups[groupMap[group]];
								((ServoGroup)g.group).AddControl(servo, -1);
							}
						}
					}
				}
			}

			if(Instance.servosState.Count > 0)
				ServoGroups = servoGroups;

			if(Gui.WindowManager.Instance != null)
				Gui.WindowManager.Instance.Invalidate();
		}
	   
		private void OnEditorRestart()
		{
			ServoGroups = null;
			servosState = null;

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
			Instance.servosState = new Dictionary<IServo, IServoState>();

			List<IServoGroup> oldServoGroups = (ServoGroups != null) ? ServoGroups : new List<IServoGroup>();

			ServoGroups = new List<IServoGroup>();

			for(int i = 0; i < FlightGlobals.Vessels.Count; i++)
			{
				var vessel = FlightGlobals.Vessels[i];

				if(!vessel.loaded)
					continue;
				
				var servoGroups = new List<IServoGroup>();
				var groupMap = new Dictionary<string, int>();

				foreach(var servo in vessel.ToServos())
				{
					Instance.servosState.Add(servo, new IServoState());

					if(!string.IsNullOrEmpty(servo.GroupName))
					{
						List<string> groups = new List<string>(servo.GroupName.Split('|'));

						foreach(string group in groups)
						{
							if(!groupMap.ContainsKey(group))
							{
								ServoGroup g = new ServoGroup(servo, vessel, group);
								servoGroups.Add(g);
								groupMap[group] = servoGroups.Count - 1;

								// search old group and copy settings
								for(int j = 0; j < oldServoGroups.Count; j++)
								{
									if(oldServoGroups[j].Name == g.Name)
									{
										g.Expanded = oldServoGroups[j].Expanded;
										g.AdvancedMode = oldServoGroups[j].AdvancedMode;
										g.GroupSpeedFactor = oldServoGroups[j].GroupSpeedFactor;
										g.ForwardKey = oldServoGroups[j].ForwardKey;
										g.ReverseKey = oldServoGroups[j].ReverseKey;
										j = int.MaxValue - 1;
									}
								}
							}
							else
							{
								IServoGroup g = servoGroups[groupMap[group]];
								((ServoGroup)g.group).AddControl(servo, -1);
							}
						}
					}
				}

				ServoGroups.AddRange(servoGroups);
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
			if(HighLogic.LoadedSceneIsFlight)
			{
				// because OnVesselDestroy and OnVesselGoOnRails seem to only work for active vessel I had to build this stupid workaround
				if(FlightGlobals.Vessels.Count(v => v.loaded) != loadedVesselCounter)
				{
					RebuildServoGroupsFlight();
					loadedVesselCounter = FlightGlobals.Vessels.Count(v => v.loaded);
				}

				if(ServoGroups != null)
				{
					for(int i = 0; i < ServoGroups.Count; i++)
						((ServoGroup)ServoGroups[i]).CheckInputs();
				}
			}
		}

		private void OnDestroy()
		{
			Logger.Log("[ServoController] destroy", Logger.Level.Debug);

			GameEvents.onVesselChange.Remove(OnVesselChange);
			GameEvents.onVesselWasModified.Remove(OnVesselWasModified);
			GameEvents.onVesselLoaded.Remove(OnVesselLoaded);
			GameEvents.onVesselDestroy.Remove(OnVesselUnloaded);
			GameEvents.onVesselGoOnRails.Remove(OnVesselUnloaded);

			GameEvents.onPartAttach.Remove(OnEditorPartAttach);
			GameEvents.onPartRemove.Remove(OnEditorPartRemove);
			GameEvents.onEditorUndo.Remove(OnEditorUnOrRedo);
			GameEvents.onEditorRedo.Remove(OnEditorUnOrRedo);
			GameEvents.onEditorLoad.Remove(OnEditorLoad);
			GameEvents.onEditorRestart.Remove(OnEditorRestart);

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

		////////////////////////////////////////
		// BuildAid

		public bool ServoBuildAid(IServo s)
		{
			return servosState[s.servo].bIsBuildAidOn;
		}

		public void ServoBuildAid(IServo s, bool v)
		{
			servosState[s.servo].bIsBuildAidOn = v;
		}
	}

	public class ModuleIRController
	{
		public static void OnSave(ConfigNode config)
		{
			if((Controller.Instance == null) || (Controller.Instance.ServoGroups == null))
				return;

			config = config.AddNode("IRControllerData");

			config.AddValue("Groups", Controller.Instance.ServoGroups.Count);

			for(int i = 0; i < Controller.Instance.ServoGroups.Count; i++)
			{
				ConfigNode groupNode = config.AddNode("Group" + i);

				groupNode.AddValue("Name", Controller.Instance.ServoGroups[i].Name);
				if(Controller.Instance.ServoGroups[i].ForwardKey.Length > 0)
					groupNode.AddValue("ForwardKey", Controller.Instance.ServoGroups[i].ForwardKey);
				if(Controller.Instance.ServoGroups[i].ReverseKey.Length > 0)
					groupNode.AddValue("ReverseKey", Controller.Instance.ServoGroups[i].ReverseKey);
				groupNode.AddValue("GroupSpeedFactor", Controller.Instance.ServoGroups[i].GroupSpeedFactor);
			}
		}

		public static void OnLoad(ConfigNode config, Vessel v)
		{
			config = config.GetNode("IRControllerData");

			if(config == null)
				return;

			if(Controller.Instance.ServoGroups == null)
				Controller.Instance.ServoGroups = new List<IServoGroup>();

			Controller.Instance.ServoGroups.Clear();

			int Count = int.Parse(config.GetValue("Groups"));

			for(int i = 0; i < Count; i++)
			{
				ConfigNode groupNode = config.GetNode("Group" + i);

				ServoGroup g;

				if(v != null)
					g = new ServoGroup(v, groupNode.GetValue("Name"));
				else
					g = new ServoGroup(groupNode.GetValue("Name"));

				string forwardKey = groupNode.GetValue("ForwardKey");
				if(forwardKey != null)
					g.ForwardKey = forwardKey;
				string reverseKey = groupNode.GetValue("ReverseKey");
				if(reverseKey != null)
					g.ReverseKey = reverseKey;
				g.GroupSpeedFactor = float.Parse(groupNode.GetValue("GroupSpeedFactor"));

				Controller.Instance.ServoGroups.Add(g);
			}
		}
	}
}
