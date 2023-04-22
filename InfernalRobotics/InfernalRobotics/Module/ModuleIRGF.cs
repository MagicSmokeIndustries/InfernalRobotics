using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

using KSP.IO;
using UnityEngine;

using InfernalRobotics_v3.Effects;
using InfernalRobotics_v3.Utility;

namespace InfernalRobotics_v3.Module
{
	// soll eher ein passiver Docking-Port darstellen neuerdings...
	// FEHLER, noch nicht ganz, aber, kommt langsam

	public class ModuleIRGF : PartModule, ITargetable, IModuleInfo
	{
		[KSPField(isPersistant = false)]
		public string nodeTransformName = "dockingNode";

		public Transform nodeTransform;

		[KSPField(isPersistant = false)]
		public string nodeName = "";				// FEHLER, mal sehen wozu wir den dann nutzen könnten


		[KSPField(isPersistant = false)]
		public bool gendered = true;

		[KSPField(isPersistant = false)]
		public bool genderFemale = false;


		[KSPField(isPersistant = false)]
		public string nodeType = "GF";


	//	[KSPField(isPersistant = false)]
	//	public float captureRange = 0.06f;				// FEHLER, Vorgaben machen zum "wie weit der LEE weg sein darf"... -> oder nur im LEE? mal sehen

	//	[KSPField(isPersistant = false)]
	//	public float captureMinFwdDot = 0.998f;

	//	[KSPField(isPersistant = false)]
	//	public float captureMaxRvel = 0.3f;


		[KSPField(isPersistant = false)]
		public bool snapRotation = true;

		[KSPField(isPersistant = false)]
		public float snapOffset = 120f;


		[KSPField(isPersistant = true)]
		public bool crossfeed = false;


		private bool physicsLessMode;


		public BaseEvent evtSetAsTarget;

		public BaseEvent evtUnsetTarget;


		public string state = "Ready";

		public DockedVesselInfo vesselInfo;
		public uint dockedPartUId;


		public ModuleIRLEE otherNode;
		public PartJoint sameVesselDockJoint;	// FEHLER, unklar ob ich das brauche


		public KerbalFSM fsm;

		public KFSMState st_ready;

		public KFSMState st_acquire_dockee;
		public KFSMState st_docked_dockee;
		public KFSMState st_docked_dockee_sameVessel;

		public KFSMState st_disabled;

		public bool DebugFSMState;

		public KFSMEvent on_nodeApproach;
		public KFSMEvent on_nodeDistance;

		public KFSMEvent on_capture_dockee;
		public KFSMEvent on_capture_dockee_sameVessel;

		public KFSMEvent on_undock;
		public KFSMEvent on_sameVessel_disconnect;

		public KFSMEvent on_disable;
		public KFSMEvent on_enable;

		public KFSMEvent on_construction_Attach;
		public KFSMEvent on_construction_Detach;

// Status noch führen und sowas wie ein enable-disable und undock vom port her erlauben oder so... ja gut, undock evtl. nicht...

// FEHLER, das hier ist nicht ganz richtig
public bool isReady
{
	get { return true; }
}


		public ModuleIRGF()
		{
		}

		////////////////////////////////////////
		// Callbacks

		public override void OnAwake()
		{
		//	part.dockingPorts.AddUnique(this); -> wir sind nicht wirklich ein DockingPort
			nodeTransform = part.FindModelTransform(nodeTransformName);

			evtSetAsTarget = Events["SetAsTarget"];
			evtUnsetTarget = Events["UnsetTarget"];
		}
/* FEHLER, unbedingt einbauen... oder? oder tut das immer die aktive Seite?
		public override void OnLoad(ConfigNode node)
		{
			if(node.HasValue("state"))
				state = node.GetValue("state");
			if(node.HasValue("dockUId"))
				dockedPartUId = uint.Parse(node.GetValue("dockUId"));
			if(node.HasValue("dockNodeIdx"))
				dockingNodeModuleIndex = int.Parse(node.GetValue("dockNodeIdx"));
			if(node.HasNode("DOCKEDVESSEL"))
			{
				vesselInfo = new DockedVesselInfo();
				vesselInfo.Load(node.GetNode("DOCKEDVESSEL"));
			}
			if(referenceAttachNode != string.Empty)
				referenceNode = base.part.FindAttachNode(referenceAttachNode);
			part.fuelCrossFeed = crossfeed;
			Events["EnableXFeed"].active = !crossfeed;
			Events["DisableXFeed"].active = crossfeed;
			string[] array = nodeType.Split(new char[2] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			int i = 0;
			for(int num = array.Length; i < num; i++)
				nodeTypes.Add(array[i]);
		}

		public override void OnSave(ConfigNode node)
		{
			node.AddValue("state", ((fsm != null) && (fsm.Started)) ? fsm.currentStateName : "Ready");
			node.AddValue("dockUId", dockedPartUId);
			node.AddValue("dockNodeIdx", dockingNodeModuleIndex);
			if(vesselInfo != null)
				vesselInfo.Save(node.AddNode("DOCKEDVESSEL"));
		}
*/
		protected virtual void OnDestroy()
		{
		}

		public override void OnStart(StartState st)
		{
			base.OnStart(st);

		//	if(state == StartState.Editor) // FEHLER, müsste ich None abfangen?? wieso sollte das je aufgerufen werden???
		//		return;

			Events["Undock"].active = false;
			Events["UndockSameVessel"].active = false;

			nodeTransform = part.FindModelTransform(nodeTransformName);
			if(!nodeTransform)
			{
				Debug.LogWarning("[Docking Node Module]: WARNING - No node transform found with name " + nodeTransformName, part.gameObject);
				return;
			}

			if(part.physicalSignificance != 0)
			{
				Debug.LogWarning("[Docking Node Module]: WARNING - The part for a docking node module cannot be physicsless!", part.gameObject);
				part.physicalSignificance = Part.PhysicalSignificance.FULL;
			}

			part.fuelCrossFeed = crossfeed;
			Events["EnableXFeed"].active = !crossfeed;
			Events["DisableXFeed"].active = crossfeed;

			StartCoroutine(lateFSMStart(st));
		}

		public void Update()
		{
			if((this == null) || physicsLessMode)
				return;

			if((fsm != null) && (fsm.Started))
			{
				fsm.UpdateFSM();
				state = fsm.currentStateName;
			}

			if(HighLogic.LoadedSceneIsFlight)
			{
				if(FlightGlobals.fetch.VesselTarget == (ITargetable)this)
				{
					evtSetAsTarget.active = false;
					evtUnsetTarget.active = true;
					if(FlightGlobals.ActiveVessel == vessel)
						FlightGlobals.fetch.SetVesselTarget(null);
					else if((FlightGlobals.ActiveVessel.transform.position - nodeTransform.position).sqrMagnitude > 40000f)
						FlightGlobals.fetch.SetVesselTarget(vessel);
				}
				else
				{
					evtSetAsTarget.active = true;
					evtUnsetTarget.active = false;
				}
			}
		}

		public void FixedUpdate()
		{
			if((this == null) || physicsLessMode)
				return;

			if((fsm != null) && (fsm.Started))
				fsm.FixedUpdateFSM();
		}

		public void LateUpdate()
		{
			if((this == null) || physicsLessMode)
				return;

			if((fsm != null) && (fsm.Started))
				fsm.LateUpdateFSM();
		}
/*
		public override void OnInventoryModeEnable()
		{
			if(referenceAttachNode != string.Empty)
				referenceNode = base.part.FindAttachNode(referenceAttachNode);
			nodeTransform = base.part.FindModelTransform(nodeTransformName);
			if(referenceNode == null)
				return;
			if(!(referenceNode.attachedPart != null))
				return;
			int i = 0;
			for(int count = referenceNode.attachedPart.Modules.Count; i < count; i++)
			{
				ModuleDockingNode moduleDockingNode = referenceNode.attachedPart.Modules[i] as ModuleDockingNode;
				if(moduleDockingNode == null)
					continue;
				if(moduleDockingNode.referenceNode == null)
					continue;
				if(!(moduleDockingNode.referenceNode.attachedPart == base.part))
					continue;
				otherNode = moduleDockingNode;
				if(fsm.Started)
					fsm.RunEvent(on_construction_Attach);
				otherNode.otherNode = this;
				otherNode.OnConstructionAttach();
				return;
			}
		}

		public override void OnInventoryModeDisable()
		{
			if(referenceAttachNode != string.Empty)
				referenceNode = base.part.FindAttachNode(referenceAttachNode);
			nodeTransform = base.part.FindModelTransform(nodeTransformName);
			if(fsm.Started)
				fsm.RunEvent(on_construction_Detach);
		}
*/

		////////////////////////////////////////
		// Functions

		public bool IsDisabled
		{
			get
			{
				if((fsm != null) && fsm.Started)
					return fsm.CurrentState == st_disabled;
				return false;
			}
		}

		public override void DemoteToPhysicslessPart()
		{
			physicsLessMode = true;
		}

		public override void PromoteToPhysicalPart()
		{
			physicsLessMode = false;
		}

		public ModuleIRLEE FindOtherNode()
		{
			Part part = FlightGlobals.FindPartByID(dockedPartUId);
			if(part != null)
				return part.FindModuleImplementing<ModuleIRLEE>();
			return null;
		}

		public IEnumerator lateFSMStart(StartState st)
		{
			yield return null;

			SetupFSM();

			if((st & StartState.Editor) != 0)
				yield break;

			if(state.Contains("Docked"))
			{
				otherNode = FindOtherNode();
				if(otherNode == null)
					state = "Ready";
				else if(state.Contains("(dockee)"))
				{
					if(otherNode.state.Contains("Acquire"))
						state = st_acquire_dockee.name;
					else
					{
						if(!otherNode.state.Contains("Docked"))
							state = "Ready";
						else if(otherNode.part.parent != part)
						{
							if(part.parent != otherNode.part)
								state = "Ready";
						}
					}
				}
			}

			if(otherNode == null)
			{
				if(state.Contains("Acquire") || state.Contains("Disengage"))
				{
					otherNode = FindOtherNode();
					if(otherNode == null)
						state = "Ready";
					}
			}

			fsm.StartFSM(state);
		}

		public void SetupFSM()
		{
			fsm = new KerbalFSM();
			fsm.OnStateChange = (Callback<KFSMState, KFSMState, KFSMEvent>)Delegate.Combine(fsm.OnStateChange, new Callback<KFSMState, KFSMState, KFSMEvent>(OnFSMStateChange));
			fsm.OnEventCalled = (Callback<KFSMEvent>)Delegate.Combine(fsm.OnEventCalled, new Callback<KFSMEvent>(OnFSMEventCalled));

			st_ready = new KFSMState("Ready");
			fsm.AddState(st_ready);

			st_acquire_dockee = new KFSMState("Acquire (dockee)");
			fsm.AddState(st_acquire_dockee);

			st_docked_dockee = new KFSMState("Docked (dockee)");
			st_docked_dockee.OnEnter = delegate
			{
				Events["Undock"].active = true;
			};
			st_docked_dockee.OnLeave = delegate
			{
				Events["Undock"].active = false;
				otherNode = null;
				vesselInfo = null;
			};
			fsm.AddState(st_docked_dockee);

			st_docked_dockee_sameVessel = new KFSMState("Docked (same vessel)");
			st_docked_dockee_sameVessel.OnEnter = delegate
			{
				Events["Undock"].active = true;
			};
			st_docked_dockee_sameVessel.OnLeave = delegate
			{
				Events["Undock"].active = false;
				otherNode = null;
				vesselInfo = null;
			};
			fsm.AddState(st_docked_dockee_sameVessel);

			st_disabled = new KFSMState("Disabled");
			fsm.AddState(st_disabled);


		public KFSMEvent on_nodeApproach;
		on_nodeApproach = new KFSMEvent("Node Approach");
		on_nodeApproach.updateMode = KFSMUpdateMode.UPDATE;
		on_nodeApproach.GoToStateOnEvent = st_acquire;
		on_nodeApproach.OnCheckCondition = (KFSMState st) => otherNode != null;
		on_nodeApproach.OnEvent = delegate
		{
			dockedPartUId = otherNode.part.flightID;
			dockingNodeModuleIndex = otherNode.part.Modules.IndexOf(otherNode);
			if(otherNode.vessel != base.vessel)
			{
				if(Vessel.GetDominantVessel(base.vessel, otherNode.vessel) == base.vessel)
					on_nodeApproach.GoToStateOnEvent = st_acquire_dockee;
				else
					on_nodeApproach.GoToStateOnEvent = st_acquire;
			}
			else if(GetDominantNode(this, otherNode) == this)
				on_nodeApproach.GoToStateOnEvent = st_docked_dockee;
			else
				on_nodeApproach.GoToStateOnEvent = st_acquire;
		};
		fsm.AddEvent(on_nodeApproach, st_ready);

public KFSMEvent on_nodeDistance;
		on_nodeDistance = new KFSMEvent("Node Distanced");
		on_nodeDistance.updateMode = KFSMUpdateMode.FIXEDUPDATE;
		on_nodeDistance.GoToStateOnEvent = st_ready;
		on_nodeDistance.OnCheckCondition = (KFSMState st) => NodeIsTooFar();
		on_nodeDistance.OnEvent = delegate
		{
			otherNode = null;
			vesselInfo = null;
		};
		fsm.AddEvent(on_nodeDistance, st_acquire, st_acquire_dockee, st_disengage, st_docked_dockee);

		on_capture = new KFSMEvent("Capture");
		on_capture.updateMode = KFSMUpdateMode.UPDATE;
		on_capture.OnCheckCondition = delegate
		{
			if(otherNode.vessel != base.vessel)
			{
				if(CheckDockContact(this, otherNode, captureRange, captureMinFwdDot, captureMinRollDot))
					return (base.part.rb.velocity - otherNode.part.rb.velocity).sqrMagnitude <= captureMaxRvel * captureMaxRvel;
				return false;
			}
			return CheckDockContact(this, otherNode, captureRange, captureMinFwdDot, captureMinRollDot);
		};
		on_capture.OnEvent = delegate
		{
			if(otherNode.vessel != base.vessel)
			{
				if(Vessel.GetDominantVessel(base.vessel, otherNode.vessel) == base.vessel)
				{
					on_capture.GoToStateOnEvent = st_docked_dockee;
					otherNode.DockToVessel(this);
					otherNode.fsm.RunEvent(otherNode.on_capture_docker);
					return;
				}
				on_capture.GoToStateOnEvent = st_docked_docker;
				DockToVessel(otherNode);
				otherNode.fsm.RunEvent(otherNode.on_capture_dockee);
			}
			else
			{
				if(GetDominantNode(this, otherNode) == this)
				{
					on_capture.GoToStateOnEvent = st_docked_dockee;
					otherNode.fsm.RunEvent(otherNode.on_capture_docker_sameVessel);
					return;
				}
				on_capture.GoToStateOnEvent = st_docker_sameVessel;
				otherNode.fsm.RunEvent(otherNode.on_capture_dockee);
			}
			if(animCaptureOff)
			{
				if((bool)deployAnimator)
					deployAnimator.Events["Toggle"].active = false;
			}
		};
		fsm.AddEvent(on_capture, st_acquire);
		on_capture_dockee = new KFSMEvent("Capture (dockee)");
		on_capture_dockee.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
		on_capture_dockee.GoToStateOnEvent = st_docked_dockee;
		fsm.AddEvent(on_capture_dockee, st_acquire_dockee);
		fsm.AddEvent(on_capture_dockee, st_docker_sameVessel);
		fsm.AddEvent(on_capture_dockee, st_disengage);
		on_capture_docker = new KFSMEvent("Capture (docker)");
		on_capture_docker.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
		on_capture_docker.OnEvent = delegate
		{
			if(animCaptureOff && (bool)deployAnimator)
				deployAnimator.Events["Toggle"].active = false;
		};
		on_capture_docker.GoToStateOnEvent = st_docked_docker;
		fsm.AddEvent(on_capture_docker, st_acquire);
		fsm.AddEvent(on_capture_docker, st_docker_sameVessel);
		fsm.AddEvent(on_capture_docker, st_ready);
		on_capture_docker_sameVessel = new KFSMEvent("Capture (docker same vessel)");
		on_capture_docker_sameVessel.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
		on_capture_docker_sameVessel.OnEvent = delegate
		{
			if(animCaptureOff && (bool)deployAnimator)
				deployAnimator.Events["Toggle"].active = false;
		};
		on_capture_docker_sameVessel.GoToStateOnEvent = st_docker_sameVessel;
		fsm.AddEvent(on_capture_docker_sameVessel, st_acquire);
		fsm.AddEvent(on_capture_docker_sameVessel, st_acquire_dockee);

		public KFSMEvent on_capture_dockee;
		public KFSMEvent on_capture_dockee_sameVessel;

		public KFSMEvent on_undock;
		public KFSMEvent on_sameVessel_disconnect;

		public KFSMEvent on_disable;
		public KFSMEvent on_enable;

		public KFSMEvent on_construction_Attach;
		public KFSMEvent on_construction_Detach;


			on_undock = new KFSMEvent("Undock");
			on_undock.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_undock.GoToStateOnEvent = st_disengage;
			on_undock.OnEvent = delegate
			{
				KFSMEvent kFSMEvent3 = on_undock;
				KFSMState goToStateOnEvent;
				if(!otherNode)
					goToStateOnEvent = st_ready;
				else
					goToStateOnEvent = st_disengage;
				kFSMEvent3.GoToStateOnEvent = goToStateOnEvent;
				base.Events["Undock"].active = false;
			};
			fsm.AddEvent(on_undock, st_docked_docker, st_docked_dockee, st_preattached, st_docker_sameVessel);

			on_sameVessel_disconnect = new KFSMEvent("Same Vessel Disconnect");
			on_sameVessel_disconnect.updateMode = KFSMUpdateMode.FIXEDUPDATE;
			on_sameVessel_disconnect.GoToStateOnEvent = st_ready;
			on_sameVessel_disconnect.OnCheckCondition = delegate
			{
				if(otherNode.vessel != base.vessel)
				{
					if(sameVesselUndockNode == null)
					{
						if(sameVesselUndockOtherNode == null)
							goto IL_00a2;
					}
					if(sameVesselUndockNode != this)
					{
						if(sameVesselUndockOtherNode != this)
							goto IL_00a2;
					}
				}
				sameVesselUndockNode = null;
				sameVesselUndockOtherNode = null;
				on_sameVessel_disconnect.GoToStateOnEvent = st_ready;
				if(!(otherNode == null))
					return otherNode.vessel != base.vessel;
				return true;
				IL_00a2:
				sameVesselUndockRedock = true;
				otherNode.sameVesselUndockRedock = true;
				if(Vessel.GetDominantVessel(base.vessel, otherNode.vessel) == base.vessel)
					on_sameVessel_disconnect.GoToStateOnEvent = st_docked_dockee;
				else
					on_sameVessel_disconnect.GoToStateOnEvent = st_docked_docker;
				return true;
			};
			on_sameVessel_disconnect.OnEvent = delegate
			{
				if((bool)otherNode && !sameVesselUndockRedock)
					otherNode.OnOtherNodeSameVesselDisconnect();
			};
			fsm.AddEvent(on_sameVessel_disconnect, st_docker_sameVessel);

			on_disable = new KFSMEvent("Disable");
			on_disable.updateMode = KFSMUpdateMode.UPDATE;
			on_disable.OnCheckCondition = delegate
			{ return false; };
			on_disable.GoToStateOnEvent = st_disabled;
			fsm.AddEvent(on_disable, st_ready, st_disengage);

			on_enable = new KFSMEvent("Enable");
			on_enable.updateMode = KFSMUpdateMode.UPDATE;
			on_enable.OnCheckCondition = delegate
			{ return false; };
			on_enable.GoToStateOnEvent = st_ready;
			fsm.AddEvent(on_enable, st_disabled);

			on_swapPrimary = new KFSMEvent("SwapPrimary");
			on_swapPrimary.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_swapPrimary.GoToStateOnEvent = st_docked_docker;
			fsm.AddEvent(on_swapPrimary, st_docked_dockee);
			on_swapSecondary = new KFSMEvent("SwapSecondary");
			on_swapSecondary.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_swapSecondary.GoToStateOnEvent = st_docked_dockee;
			fsm.AddEvent(on_swapSecondary, st_docked_docker);
			on_construction_Attach = new KFSMEvent("OnConstructionAttach");
			on_construction_Attach.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_construction_Attach.GoToStateOnEvent = st_preattached;
			KFSMEvent kFSMEvent = on_construction_Attach;
	/* FEHLER, weiss nicht was das ist, ich tippe aber auf Tutorial-Feedback -> somit wär's mir egal
			KFSMCallback kFSMCallback = _003C_003Ec._003C_003E9__142_31;
			if(kFSMCallback == null)
			{
				kFSMCallback = (_003C_003Ec._003C_003E9__142_31 = delegate
				{
				});
			}
			kFSMEvent.OnEvent = kFSMCallback;*/
			fsm.AddEvent(on_construction_Attach, st_ready, st_acquire, st_acquire_dockee, st_docked_dockee, st_docked_docker, st_docker_sameVessel, st_disengage, st_disabled, st_preattached);
			on_construction_Detach = new KFSMEvent("OnConstructionDetach");
			on_construction_Detach.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			on_construction_Detach.GoToStateOnEvent = st_ready;
			KFSMEvent kFSMEvent2 = on_construction_Detach;
	/* FEHLER, weiss nicht was das ist, ich tippe aber auf Tutorial-Feedback -> somit wär's mir egal
			KFSMCallback kFSMCallback2 = _003C_003Ec._003C_003E9__142_32;
			if(kFSMCallback2 == null)
			{
				kFSMCallback2 = (_003C_003Ec._003C_003E9__142_32 = delegate
				{
				});
			}
			kFSMEvent2.OnEvent = kFSMCallback2;*/
			fsm.AddEvent(on_construction_Detach, st_ready, st_acquire, st_acquire_dockee, st_docked_dockee, st_docked_docker, st_docker_sameVessel, st_disengage, st_disabled, st_preattached);
		}

		private void OnFSMStateChange(KFSMState oldStatea, KFSMState newState, KFSMEvent fsmEvent)
		{
			if(DebugFSMState)
				Debug.LogFormat("[ModuleDockingNode]: Part:{0}-{1} FSM State Changed, Old State:{2} New State:{3} Event:{4}", part.partInfo.title, part.persistentId, oldStatea.name, newState.name, fsmEvent.name);
		}

		private void OnFSMEventCalled(KFSMEvent fsmEvent)
		{
			if(DebugFSMState)
				Debug.LogFormat("[ModuleDockingNode]: Part:{0}-{1} FSM Event Called, Event:{2}", part.partInfo.title, part.persistentId, fsmEvent.name);
		}

		[KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = true, unfocusedRange = 2f, guiName = "#autoLOC_6001445")]
		public void Undock()
		{
			otherNode.Undock();
		}

		[KSPAction("#autoLOC_6001444", activeEditor = false)]
		public void UndockAction(KSPActionParam param)
		{
			if(Events["Undock"].active)
				Undock();
		}

		public void OnConstructionAttach()
		{
			if(fsm.Started)
				fsm.RunEvent(on_construction_Attach);
		}

// FEHLER, wo ist das Detach? das läuft irgendwie anders -> trotzdem mal noch suchen

		[KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = false, unfocusedRange = 200f, guiName = "Set As Target")]
		public void SetAsTarget()
		{
			FlightGlobals.fetch.SetVesselTarget(this);
		}

		[KSPEvent(guiActiveUnfocused = true, externalToEVAOnly = false, guiActive = false, unfocusedRange = 200f, guiName = "Unset As Target")]
		public void UnsetTarget()
		{
			FlightGlobals.fetch.SetVesselTarget(null);
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "#autoLOC_236028")]
		public void EnableXFeed()
		{
			Events["EnableXFeed"].active = false;
			Events["DisableXFeed"].active = true;
	
			crossfeed = true;

			if(!part.fuelCrossFeed)
			{
				part.fuelCrossFeed = true;
				GameEvents.onPartCrossfeedStateChange.Fire(part);
			}
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "#autoLOC_236030")]
		public void DisableXFeed()
		{
			base.Events["EnableXFeed"].active = true;
			base.Events["DisableXFeed"].active = false;

			crossfeed = false;

			if(part.fuelCrossFeed)
			{
				part.fuelCrossFeed = false;
				GameEvents.onPartCrossfeedStateChange.Fire(base.part);
			}
		}

		[KSPAction("#autoLOC_236028")]
		public void EnableXFeedAction(KSPActionParam param)
		{
			EnableXFeed();
		}

		[KSPAction("#autoLOC_236030")]
		public void DisableXFeedAction(KSPActionParam param)
		{
			DisableXFeed();
		}

		[KSPAction("#autoLOC_236032")]
		public void ToggleXFeedAction(KSPActionParam param)
		{
			if(crossfeed)
				DisableXFeed();
			else
				EnableXFeed();
		}

		////////////////////////////////////////
		// ITargetable

		public Transform GetTransform()
		{
			return nodeTransform;
		}

		public Vector3 GetObtVelocity()
		{
			return vessel.obt_velocity;
		}

		public Vector3 GetSrfVelocity()
		{
			return vessel.srf_velocity;
		}

		public Vector3 GetFwdVector()
		{
			return nodeTransform.forward;
		}

		public Vessel GetVessel()
		{
			return vessel;
		}

		public string GetName()
		{
			return "GF on " + vessel.vesselName;
				// FEHLER, schlaueren Namen wählen, sonst kann man die Teils ja nie unterscheiden -> bzw. unterstützen, dass man sie benennen kann
		}

		public string GetDisplayName()
		{
			return GetName();
		}

		public Orbit GetOrbit()
		{
			return vessel.orbit;
		}

		public OrbitDriver GetOrbitDriver()
		{
			return vessel.orbitDriver;
		}

		public VesselTargetModes GetTargetingMode()
		{
			return VesselTargetModes.DirectionVelocityAndOrientation;
		}

		public bool GetActiveTargetable()
		{
			return false;
		}

		////////////////////////////////////////
		// IRescalable

//das gleich anpassen oder rausschmeissen

//		// Tweakscale support
//		[KSPEvent(guiActive = false, active = true)]
//		void OnPartScaleChanged(BaseEventDetails data)
//		{
//			OnRescale(new ScalingFactor(data.Get<float>("factorAbsolute")));
//		}

//		public void OnRescale(ScalingFactor factor)
//		{
//			ModuleIRGF prefab = part.partInfo.partPrefab.GetComponent<ModuleIRGF>();

///*			part.mass = prefab.part.mass * Mathf.Pow(factor.absolute.linear, scaleMass);

//			forceNeeded = prefab.forceNeeded * factor.absolute.linear;
// 			partBreakForce = partBreakForce * factor.relative.linear;
// 			groundBreakForce = groundBreakForce * factor.relative.linear;

//			electricChargeRequiredIdle = prefab.electricChargeRequiredIdle * Mathf.Pow(factor.absolute.linear, scaleElectricChargeRequired);
//			electricChargeRequiredConnected = prefab.electricChargeRequiredConnected * Mathf.Pow(factor.absolute.linear, scaleElectricChargeRequired);

//			UpdateUI();*/
//		}

// FEHLER IResourceConsumer evtl. noch? -> nein, eher nicht, weil das ist ja kein Motor

// FEHLER -> AdjusterDockingNodeBase für .. weiss ned "defekte Teile" um zu verhindern, dass es undocken kann?

// und animationen wurden rausgenommen, die könnte man auch anders lösen oben drüber

		////////////////////////////////////////
		// IModuleInfo

		string IModuleInfo.GetModuleTitle()
		{
			return "Grapple Fixture";
		}

		string IModuleInfo.GetInfo()
		{
			return "";
		}

		Callback<Rect> IModuleInfo.GetDrawModulePanelCallback()
		{
			return null;
		}

		string IModuleInfo.GetPrimaryField()
		{
			return null;
		}
	}
}


