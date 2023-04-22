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
	public class ModuleIRLEE : PartModule, IModuleInfo
	{
		public KerbalFSM fsm;
		public string currentState;

		public KFSMState st_preattached;

		public KFSMState st_searching;
		public KFSMEvent ev_tosearching;

		public KFSMState st_found;
		public KFSMEvent ev_tofound;

		public KFSMState st_catched;
		public KFSMEvent ev_tocatched;
		public KFSMState st_latching;
		public KFSMEvent ev_tolatching;
		public KFSMState st_latched;
		public KFSMEvent ev_tolatched;

		public KFSMState st_docked;
		public KFSMEvent ev_todocked;
		public KFSMState st_docked_sameVessel;
		public KFSMEvent ev_todocked_sameVessel;

		public KFSMState st_released;
		public KFSMEvent ev_toreleased;
		public KFSMState st_disabled;
		public KFSMEvent ev_todisabled;


		public ModuleIRGF grappleFixture;

		// attachement
		public ConfigurableJoint attachJoint = null;
		public float attachedBreakForce;

		public Guid attachedVesselId;
		public uint attachedPartId;

// FEHLER, temp, mal sehen ob's so bleibt
PartJoint sameVesselDockJoint;

		public DockedVesselInfo vesselInfo = null;
		public DockedVesselInfo attachedVesselInfo = null;

		PartResourceDefinition electricResource = null;

float maxCatchDistance = 0.05f;
float maxCatchSpeed = 0.5f;
float minReleaseDistance = 0.4f;

float maxFindDistance = 0.4f;

		[KSPField(isPersistant = false)] public string dockingNodeTransformName = "dockingNode";
		public Transform nodeTransform;

		[KSPField(isPersistant = false)] public string dockingNodeName = "top";

		[KSPField(isPersistant = false)] public bool gendered = true;
		[KSPField(isPersistant = false)] public bool genderFemale = true;

		// Sounds
		[KSPField(isPersistant = false)] public string preAttachSoundFilePath = "";
		[KSPField(isPersistant = false)] public string latchSoundFilePath = "";
		[KSPField(isPersistant = false)] public string detachSoundFilePath = "";
		
		[KSPField(isPersistant = false)] public string activatingSoundFilePath = "";
		[KSPField(isPersistant = false)] public string activatedSoundFilePath = "";
		[KSPField(isPersistant = false)] public string deactivatingSoundFilePath = "";

		protected SoundSource soundSound = null;

		// Info
		[KSPField(guiActive = true, guiName = "State", guiFormat = "S")]
		public string state;

		[KSPField(guiActive = false, guiName = "Distance", guiFormat = "F2")]
		public float statedistance;

		[KSPField(guiActive = false, guiName = "Angle", guiFormat = "F1")]
		public float stateangle;

		[KSPField(guiActive = false, guiName = "Rotation", guiFormat = "F1")]
		public float staterotation;


		public ModuleIRLEE()
		{
		}

		////////////////////////////////////////
		// Callbacks

		public override void OnAwake()
		{
			DebugInit();

			GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
			GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);

			GameEvents.onJointBreak.Add(OnJointBroken);

	// FEHLER, nur wenn wir wirklich einer sind... zwar... na ja... evtl. wär's 'ne Idee... mal sehen dann -> dual-re-undock und so ginge nicht zwar bei anderen, aber... na ja... ist ja egal, oder?
			// reicht doch, wenn ich das kann... oder nicht??
//			part.dockingPorts.AddUnique(this);

			nodeTransform = part.FindModelTransform(dockingNodeTransformName);
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			if(state == StartState.Editor) // FEHLER, müsste ich None abfangen?? wieso sollte das je aufgerufen werden???
				return;

			nodeTransform = part.FindModelTransform(dockingNodeTransformName);

			electricResource = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

			if(activatedSoundFilePath != "")
			{
				if(soundSound == null)
					soundSound = new SoundSource(part, "connector");
				soundSound.Setup(activatedSoundFilePath, true);
			}

AttachNode atn = part.FindAttachNode("top"); // FEHLER, später konfigurierbar machen? weil... pre-attach-Erkennung?
if(atn.attachedPart != null)
	grappleFixture = atn.attachedPart.GetComponent<ModuleIRGF>();
// FEHLER, blöd, fixen hier

			if(atn.attachedPart != null)
				currentState = "PreAttached";

/*
			switch(attachType)
			{
			case AttachType.Part:
				StartCoroutine(WaitAndInitPartAttach());
				break;

			case AttachType.Docked:
				{
// FEHLER, dieser case hier -> total überarbeiten -> ist totaler Schrott... aber docking sowieso
/*
					dockedPart = GetPartByID(vessel, attachedPartId).GetComponent<ModuleIRAttachment>();

					if(dockedPart && (dockedPart.part == part.parent || dockedPart.part.parent == part)) // FEHLER, was'n des für ein Furz???
					{
						ModuleIRAttachment dockedPartTmp = dockedPart.GetComponent<ModuleIRAttachment>();
						if(dockedPartTmp == null)
						{
							Logger.Log("OnLoad(Core) Unable to get docked module!", Logger.Level.Fatal);
							attachType = AttachType.None;
						}
						else if(dockedPartTmp.attachType == AttachType.Docked
		                   && dockedPartTmp.attachedPartId == part.flightID
				           && dockedPartTmp.vesselInfo != null)
						{
							Logger.Log(string.Format("OnLoad(Core) Part already docked to {0}", dockedPartTmp.part.partInfo.title), Logger.Level.Fatal);
							dockedPart = dockedPartTmp;
							dockedPartTmp.dockedPart = this;
						}
						else
						{
							Logger.Log(string.Format("OnLoad(Core) Re-set docking on {0}", dockedPartTmp.part.partInfo.title), Logger.Level.Fatal);
							attachedPart = dockedPartTmp.part;
							AttachDocked(partBreakForce);
						}
					}
					else
*//*					{
						Logger.Log("OnLoad(Core) Unable to get saved docked part!", Logger.Level.Fatal);
						attachType = AttachType.None;
					}
				}
				break;
			}

		//	AttachContextMenu();
*/
		//	UpdateUI(); -> do this only after fsm started

			StartCoroutine(delayFSMStart(state));
		}

// FEHLER, wieso verzögert? einfach zum Spass? na gut, kommt ja nicht drauf an
		public IEnumerator delayFSMStart(StartState state)
		{
			yield return null; // FEHLER, wieso nicht was schlaues?

			SetupFSM();

AttachNode atn = part.FindAttachNode("top"); // FEHLER, später konfigurierbar machen? weil... pre-attach-Erkennung?
if(atn.attachedPart != null)
	grappleFixture = atn.attachedPart.GetComponent<ModuleIRGF>();

			fsm.StartFSM(currentState /*atn.attachedPart == null ? st_searching : st_preattached*/); // FEHLER, neueste Idee... mal sehen ob's gut ist

			currentState = fsm.currentStateName;

			UpdateUI();
		}

// FEHLER, ist wohl eine Furzidee mit FSM, weil sinnlos, aber ich zieh's mal durch... mal gucken halt
		public void SetupFSM()
		{
			fsm = new KerbalFSM();

// FEHLER, bei allen kann man sich fragen, ob man FixedUpdate oder Update oder LateUpdate nutzen will

			st_preattached = new KFSMState("PreAttached");
			fsm.AddState(st_preattached);

			st_searching = new KFSMState("Searching");
			st_searching.OnEnter = delegate
				{
					UpdateUI();
				};
			st_searching.OnFixedUpdate = delegate
				{
					grappleFixture = FindGrappleFixture();

					if(grappleFixture)
						fsm.RunEvent(ev_tofound);
				};
			fsm.AddState(st_searching);

			st_found = new KFSMState("Found");
			st_found.OnEnter = delegate
				{
					UpdateUI();
				};
			st_found.OnFixedUpdate = delegate
				{
					UpdateUIPositionData();

					if(CheckCatchCondition(maxCatchDistance))
					{
						if(grappleFixture.vessel != vessel)
						{
							if((part.Rigidbody.velocity - grappleFixture.part.Rigidbody.velocity).sqrMagnitude > maxCatchSpeed * maxCatchSpeed)
								return;
						}

						BuildCatchedJoint();

						fsm.RunEvent(ev_tocatched);

						return;
					}

					if(CheckReleaseCondition())
						fsm.RunEvent(ev_tosearching);
				};
			fsm.AddState(st_found);

			st_catched = new KFSMState("Catched");
			st_catched.OnEnter = delegate
				{
					UpdateUI();
				};
			st_catched.OnFixedUpdate = delegate
				{
					UpdateUIPositionData();

					if(!CheckCatchCondition(maxCatchDistance * 2))
					{
						if(attachJoint)
							Destroy(attachJoint);
						attachJoint = null;

						fsm.RunEvent(ev_tosearching);
					}
				};
			fsm.AddState(st_catched);

			st_latching = new KFSMState("Latching");
			st_latching.OnEnter = delegate
				{
					UpdateUI();
				};
			st_latching.OnFixedUpdate = delegate
				{
					UpdateUIPositionData();
				};
			fsm.AddState(st_latching);

			st_latched = new KFSMState("Latched");
			st_latched.OnEnter = delegate
				{
					UpdateUI();
				};
			fsm.AddState(st_latched);

			st_docked = new KFSMState("Docked");
			st_docked.OnEnter = delegate
				{
					UpdateUI();
				};
			fsm.AddState(st_docked);

			st_docked_sameVessel = new KFSMState("Docked (same vessel)");
			st_docked_sameVessel.OnEnter = delegate
				{
					UpdateUI();
				};
			fsm.AddState(st_docked_sameVessel);

			st_released = new KFSMState("Released");
			st_released.OnEnter = delegate
				{
					UpdateUI();
				};
			st_released.OnFixedUpdate = delegate
				{
					if(CheckReleaseCondition())
						fsm.RunEvent(ev_tosearching);
				};
			st_released.OnLeave = delegate
				{
					grappleFixture = null;
				};
			fsm.AddState(st_released);

			st_disabled = new KFSMState("Disabled");
			st_disabled.OnEnter = delegate
				{
					if(soundSound != null)
						soundSound.Stop();

					if(deactivatingSoundFilePath != "")
						AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(deactivatingSoundFilePath), part.transform.position);

					UpdateUI();
				};
			st_disabled.OnLeave = delegate
				{
					if(activatingSoundFilePath != "")
						AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(activatingSoundFilePath), part.transform.position);

					if(soundSound != null)
						soundSound.Play();
				};
			fsm.AddState(st_disabled);


			ev_tosearching = new KFSMEvent("to Searching");
			ev_tosearching.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			ev_tosearching.OnEvent = delegate
				{
				};
			ev_tosearching.GoToStateOnEvent = st_searching;
			fsm.AddEvent(ev_tosearching, st_found, st_catched, st_released, st_disabled);

			ev_tofound = new KFSMEvent("to Found");
			ev_tofound.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			ev_tofound.OnEvent = delegate
				{
				};
			ev_tofound.GoToStateOnEvent = st_found;
			fsm.AddEvent(ev_tofound, st_searching);

			ev_tocatched = new KFSMEvent("to Catched");
			ev_tocatched.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			ev_tocatched.OnEvent = delegate
				{
				};
			ev_tocatched.GoToStateOnEvent = st_catched;
			fsm.AddEvent(ev_tocatched, st_found);

			ev_tolatching = new KFSMEvent("to Latching");
			ev_tolatching.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			ev_tolatching.OnEvent = delegate
				{
				};
			ev_tolatching.GoToStateOnEvent = st_latching;
			fsm.AddEvent(ev_tolatching, st_catched);

			ev_tolatched = new KFSMEvent("to Latched");
			ev_tolatched.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			ev_tolatched.OnEvent = delegate
				{
				};
			ev_tolatched.GoToStateOnEvent = st_latched;
			fsm.AddEvent(ev_tolatched, st_latching, st_docked, st_docked_sameVessel);

			ev_todocked = new KFSMEvent("to Docked");
			ev_todocked.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			ev_todocked.OnEvent = delegate
				{
				};
			ev_todocked.GoToStateOnEvent = st_docked;
			fsm.AddEvent(ev_todocked, st_latched, st_docked_sameVessel);

			ev_todocked_sameVessel = new KFSMEvent("to Docked (same vessel)");
			ev_todocked_sameVessel.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			ev_todocked_sameVessel.OnEvent = delegate
				{
				};
			ev_todocked_sameVessel.GoToStateOnEvent = st_docked_sameVessel;
			fsm.AddEvent(ev_todocked_sameVessel, st_latched);

			ev_toreleased = new KFSMEvent("to Released");
			ev_toreleased.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			ev_toreleased.OnEvent = delegate
				{
				};
			ev_toreleased.GoToStateOnEvent = st_released;
			fsm.AddEvent(ev_toreleased, st_catched, st_latched, st_docked, st_docked_sameVessel, st_preattached);

			ev_todisabled = new KFSMEvent("to Disabled");
			ev_todisabled.updateMode = KFSMUpdateMode.MANUAL_TRIGGER;
			ev_todisabled.OnEvent = delegate
				{
				};
			ev_todisabled.GoToStateOnEvent = st_disabled;
			fsm.AddEvent(ev_todisabled, st_searching, st_found, st_released);

/*
OnEnter bauen bei den Teils
		st_docked_docker.OnEnter = delegate
		{
			base.Events["Undock"].active = true;
			base.part.fuelLookupTargets.Add(otherNode.part);
			otherNode.part.fuelLookupTargets.Add(base.part);
			GameEvents.onPartFuelLookupStateChange.Fire(new GameEvents.HostedFromToAction<bool, Part>(host: true, otherNode.part, base.part));
		};
		st_docked_docker.OnUpdate = delegate
		{
			BaseEvent baseEvent2 = base.Events["Undock"];
			int active2;
			if(!IsAdjusterBlockingUndock())
				active2 = ((!otherNode.IsAdjusterBlockingUndock()) ? 1 : 0);
			else
				active2 = 0;
			baseEvent2.active = ((byte)active2 != 0);
		};
		st_docked_docker.OnLeave = delegate
		{
			base.Events["Undock"].active = false;
			base.part.fuelLookupTargets.Remove(otherNode.part);
			otherNode.part.fuelLookupTargets.Remove(base.part);
			GameEvents.onPartFuelLookupStateChange.Fire(new GameEvents.HostedFromToAction<bool, Part>(host: true, base.part, otherNode.part));
		};
 * 
		st_docker_sameVessel.OnEnter = delegate
		{
			base.Events["UndockSameVessel"].active = true;
			if(!base.vessel.packed)
			{
				DockToSameVessel(otherNode);
				return;
			}
		};
*/

		}

		public IEnumerator WaitAndInitPartAttach()
		{
			yield return new WaitForEndOfFrame();
/*
			if(attachType != AttachType.Part)
				goto end; // FEHLER, wieso sollte das je passieren???

			Part attachToPart = GetPartByID(attachedVesselId, attachedPartId);
			if(attachToPart)
			{
				Logger.Log(string.Format("OnLoad(Core) Re-set fixed joint on {0}", attachToPart.partInfo.title));
        
				AttachPart(attachToPart, attachedBreakForce);
			}
			else
			{
				Logger.Log("OnLoad(Core) Unable to get saved connected part of the fixed joint !");

				attachType = AttachType.None;
			}

end:;*/
		}

		public void OnDestroy()
		{
		//	DetachContextMenu();

			GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
			GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);

			GameEvents.onJointBreak.Remove(OnJointBroken);
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			if(fsm.Started)
			{
				node.AddValue("currentState", currentState);

				if(fsm.CurrentState == st_docked)
				{
					node.AddValue("attachedBreakForce", attachedBreakForce);
					node.AddValue("attachedPartId", attachedPartId);

					vesselInfo.Save(node.AddNode("vesselInfo"));
					attachedVesselInfo.Save(node.AddNode("attachedVesselInfo"));
				}
				else if(fsm.CurrentState == st_docked_sameVessel)
				{
					node.AddValue("attachedBreakForce", attachedBreakForce);
					node.AddValue("attachedPartId", attachedPartId);
				}
			}
/*

			if(fsm.CurrentState


st_preattached;
st_searching;
st_found;
st_catched;
st_latching;
st_latched;
st_docked;
st_docked_sameVessel;
st_released;
st_disabled;

			ähm ja... genau

sich merken wo dran wir gedockt sind - evtl. auch das andere attach

ah und -> dock/undock so bauen, dass es nicht gleich losolässt oder Kamera verschiebt

	und editor attach verhindern? hmm


		public ConfigurableJoint attachJoint = null;
		public float attachedBreakForce;

		public Guid attachedVesselId;
		public uint attachedPartId;

// FEHLER, temp, mal sehen ob's so bleibt
PartJoint sameVesselDockJoint;

		public DockedVesselInfo vesselInfo = null;
		public DockedVesselInfo attachedVesselInfo = null;


/*
			switch(attachType)
			{
			case AttachType.Part:
				{
					ConfigNode subNode = node.AddNode("PARTATTACH"); // FEHLER, Add/GetNode ist doch defekt, kann man gar nicht nutzen, oder nicht?
					subNode.AddValue("attachedPartId", attachedPartId.ToString());
				//	subNode.AddValue("attachedVesselId", attachedVesselId.ToString());
					subNode.AddValue("breakForce", attachedBreakForce);
				}
				break;

			case AttachType.Docked:
				{
					ConfigNode subNode = node.AddNode("DOCKEDVESSEL");
					subNode.AddValue("attachedPartId", attachedPartId.ToString());
					subNode.AddValue("attachedVesselId", attachedVesselId.ToString());
					subNode.AddValue("breakForce", attachedBreakForce);
					vesselInfo.Save(subNode);
				}
				break;
			}*/
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

			if(!node.TryGetValue("currentState", ref currentState))
				currentState = "Searching";

			if(HighLogic.LoadedSceneIsFlight)
			{
				if(currentState == "Docked")
				{
					node.TryGetValue("attachedBreakForce", ref attachedBreakForce);
					node.TryGetValue("attachedPartId", ref attachedPartId);

					vesselInfo = new DockedVesselInfo();
					vesselInfo.Load(node.GetNode("vesselInfo"));

					attachedVesselInfo = new DockedVesselInfo();
					attachedVesselInfo.Load(node.GetNode("attachedVesselInfo"));
				}
				else if(currentState == "Docked (same vessel)")
				{
					node.TryGetValue("attachedBreakForce", ref attachedBreakForce);
					node.TryGetValue("attachedPartId", ref attachedPartId);
				}
			}

//das docking Zeug neu erstellen

/*
			switch(attachType)
			{
			case AttachType.Part:
				{
					ConfigNode subNode = node.GetNode("PARTATTACH");
					attachedPartId = uint.Parse(subNode.GetValue("attachedPartId"));
				//	attachedVesselId = new Guid(subNode.GetValue("attachedVesselId"));
					attachedBreakForce = float.Parse(subNode.GetValue("breakForce"));
				}
				break;

			case AttachType.Docked:
				{
					ConfigNode subNode = node.GetNode("DOCKEDVESSEL");
					attachedPartId = uint.Parse(subNode.GetValue("attachedPartId"));
					attachedVesselId = new Guid(subNode.GetValue("attachedVesselId"));
					attachedBreakForce = float.Parse(subNode.GetValue("breakForce"));
					vesselInfo = new DockedVesselInfo();
					vesselInfo.Load(subNode);
				}
				break;
			}*/
		}

		public void OnVesselGoOnRails(Vessel v)
		{
			if(part.vessel != v)
				return;
		}

		public void OnVesselGoOffRails(Vessel v)
		{
			if(part.vessel != v)
				return;
		}

		private void OnJointBroken(EventReport report)
		{
			if((report.origin == part) || (report.origin == grappleFixture.part))
				StartCoroutine(WaitAndCheckJoint());
		}

		private IEnumerator WaitAndCheckJoint()
		{
			yield return new WaitForFixedUpdate();

			if((fsm.CurrentState == st_catched)
			|| (fsm.CurrentState == st_latching)
			|| (fsm.CurrentState == st_latched))
			{
				if(attachJoint == null)
					Detach();
			}
			else if(fsm.CurrentState == st_docked)
			{
				// etwas erkennen und was tun FEHLER, fehlt
			}
			else if(fsm.CurrentState == st_docked_sameVessel)
			{
				// etwas erkennen und was tun FEHLER, fehlt
			}
		}


			// FEHLER, nötig? oder lassen wir das lieber ganz weg? -> gleiche Frage wie beim IRAttachment
		public virtual void OnPartDie()
		{
			Detach();
		}

//		public;

		////////////////////////////////////////
		// Functions

		private ModuleIRGF FindGrappleFixture()
		{
			if(part.packed)
				return null;

			for(int i = 0; i < FlightGlobals.VesselsLoaded.Count; i++)
			{
				Vessel vessel = FlightGlobals.VesselsLoaded[i];

				if(vessel.packed)
					continue;

				// FEHLER, blöd, eine Liste von GFs wär super... sonst muss ich immer alle Teile durchsuchen, das ist super ineffizient

				foreach(Part p in vessel.parts)
				{
					if(p == part)
						continue;

					ModuleIRGF gf = p.GetComponent<ModuleIRGF>();

					if(gf == null)
						continue;

					if(gf.part.State == PartStates.DEAD)
						continue;

					if(!gf.isReady)
						continue;

					// nodeTypes... ja, könnte man noch einbauen -> damit nur der passende Port auf das passende Teil docken kann... also kein APAS auf ein GF
/*
				bool flag = true;
				HashSet<string>.Enumerator enumerator = nodeTypes.GetEnumerator();
				while(enumerator.MoveNext())
				{
					flag &= !moduleDockingNode.nodeTypes.Contains(enumerator.Current);
				}
				if(flag)
				{
					continue;
				}*/

					if(gf.gendered != gendered)
						continue;

					if(gendered && (gf.genderFemale == genderFemale))
						continue;

		//			if(gf.snapRotation != snapRotation)
		//				continue;

		//			if(snapRotation && (gf.snapOffset != snapOffset))
		//				continue;

// FEHLER, diese Zeilen hier zu einer Funktion machen -> haben sie auch getan... gute Idee
					if((nodeTransform.position - gf.nodeTransform.position).sqrMagnitude > maxFindDistance * maxFindDistance)
						continue;

					if(Vector3.Angle(nodeTransform.forward, -gf.nodeTransform.forward) > 90f) // FEHLER, 90 nicht fix
						continue;

		//			Vector3 otherDown = Vector3.ProjectOnPlane(-gf.nodeTransform.up, nodeTransform.forward);

		//			if(Vector3.Angle(nodeTransform.up, otherDown) > 30f) // FEHLER, nicht 30 und 3 Möglichkeiten erlaubt
		//				continue;
			// das muss dann zwar passen, aber für's Find ist mir das furzegal

					return gf;
				}
			}

			return null;
		}

		private bool CheckCatchCondition(float maxDistance)
		{
			if((grappleFixture == null) || (grappleFixture.nodeTransform == null))
			{
				// FEHLER, log
				return false;
			}

			if((nodeTransform.position - grappleFixture.nodeTransform.position).sqrMagnitude > maxDistance * maxDistance)
				return false;

			if(Vector3.Angle(nodeTransform.forward, -grappleFixture.nodeTransform.forward) > 15f) // FEHLER, 15 nicht fix
				return false;

			Vector3 otherDown = Vector3.ProjectOnPlane(-grappleFixture.nodeTransform.up, nodeTransform.forward);

			if(Vector3.Angle(nodeTransform.up, otherDown) > 30f) // FEHLER, nicht 30 und 3 Möglichkeiten erlaubt
				return false;

			return true;
		}

		private bool CheckReleaseCondition()
		{
			if((grappleFixture == null) || (grappleFixture.nodeTransform == null))
			{
				// FEHLER, log
				return true;
			}

			return (nodeTransform.position - grappleFixture.nodeTransform.position).sqrMagnitude > minReleaseDistance * minReleaseDistance;
		}

		private void BuildCatchedJoint()
		{
			if(fsm.CurrentState != st_found)
				return;

			attachedVesselId = grappleFixture.vessel.id;
			attachedPartId = grappleFixture.part.flightID;

			attachedBreakForce = catchedBreakForce;


			Logger.Log("AttachPart create joint on " + part.partInfo.title + " with " + grappleFixture.part.partInfo.title);

			attachJoint = part.gameObject.AddComponent<ConfigurableJoint>();
				// FEHLER, umdrehen, wenn -> Masse gross, klein bla bla... kennen wir ja

			attachJoint.connectedBody = grappleFixture.part.Rigidbody;
			attachJoint.breakForce = catchedBreakForce;
			attachJoint.breakTorque = catchedBreakForce;

			attachJoint.autoConfigureConnectedAnchor = false;
			attachJoint.anchor = attachJoint.transform.InverseTransformPoint(nodeTransform.position);
			attachJoint.connectedAnchor = attachJoint.connectedBody.transform.InverseTransformPoint(grappleFixture.nodeTransform.position);

			attachJoint.targetPosition = attachJoint.transform.InverseTransformVector(grappleFixture.nodeTransform.position - nodeTransform.position);

			attachJoint.xMotion = attachJoint.yMotion = attachJoint.zMotion = ConfigurableJointMotion.Free;
			attachJoint.angularXMotion = attachJoint.angularYMotion = attachJoint.angularZMotion = ConfigurableJointMotion.Free;

			attachJoint.xDrive = attachJoint.yDrive = attachJoint.zDrive = new JointDrive { maximumForce = catchedBreakForce, positionSpring = catchedBreakForce, positionDamper = 0f };
			attachJoint.angularXDrive = attachJoint.angularYZDrive = new JointDrive { maximumForce = catchedBreakForce, positionSpring = catchedBreakForce, positionDamper = 0f };

			// FEHLER, die haben jeweils noch geprüft, ob wir packed sind... also ein Part... muss ich das? kann ich überhaupt in einer Situation, in der ich packed bin, diese Funktion hier auslösen???
		}

		private void ExecuteLatching()
		{
			fsm.RunEvent(ev_tolatching);

// FEHLER; anpassen an aktuelle Position denke ich mal... damit's nicht zurückspringt
attachJoint.targetPosition = attachJoint.transform.InverseTransformVector(grappleFixture.nodeTransform.position - nodeTransform.position);

			attachedBreakForce = latchedBreakForce;

			attachJoint.xDrive = attachJoint.yDrive = attachJoint.zDrive = new JointDrive { maximumForce = latchedBreakForce, positionSpring = latchedBreakForce, positionDamper = 0f };
			attachJoint.angularXDrive = attachJoint.angularYZDrive = new JointDrive { maximumForce = latchedBreakForce, positionSpring = latchedBreakForce, positionDamper = 0f };

			// Sound
		//	AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(attachPartSoundFilePath), part.transform.position);
			// FEHLER; das dauernd abspielen, bis fertig reingezogen -> in der coroutine aber
	
			StartCoroutine(SlerpLatching());
//			DoItAtOnce();
		}

void DoItAtOnce()
{
			Vector3 forward = -grappleFixture.nodeTransform.forward;
			forward = Quaternion.FromToRotation(forward, nodeTransform.forward) * nodeTransform.forward;

			Vector3 right = Vector3.ProjectOnPlane(nodeTransform.right, forward);
			Vector3 up = Vector3.ProjectOnPlane(nodeTransform.up, forward);

			Quaternion targetRotation = Quaternion.Inverse(attachJoint.transform.rotation) * Quaternion.LookRotation(-up, forward);

			Vector3 targetPosition = Vector3.zero;

// slerp

			attachJoint.targetRotation = targetRotation;
			attachJoint.targetPosition = targetPosition;

			attachJoint.xMotion = attachJoint.yMotion = attachJoint.zMotion = ConfigurableJointMotion.Limited;
		//	attachJoint.angularXMotion = attachJoint.angularYMotion = attachJoint.angularZMotion = ConfigurableJointMotion.Limited; -> dazu müssten wir die Limiten umsetzen

			fsm.RunEvent(ev_tolatched);


// FEHLER, kann ich das am Ende wieder zurückstellen oder nicht??? -> so direkt denke ich nicht... aber gut, ist ja auch egal, oder? wir wollen es ja jetzt docken... denke ich mal
//	attachJoint.xMotion = attachJoint.yMotion = attachJoint.zMotion = ConfigurableJointMotion.Free;
//	attachJoint.angularXMotion = attachJoint.angularYMotion = attachJoint.angularZMotion = ConfigurableJointMotion.Free;

}

public static Vector3 haha;
public static Vector3 haha2;

		private IEnumerator SlerpLatching()
		{
// alle joints suchen von mir bis zum root oder von mir bis zum nächsten LEE... sag ich mal... die stellen wir auf relax... oder... nur 4? na mal sehen...

			List<ModuleIRServo_v3> servos = new List<ModuleIRServo_v3>();

			Part p = part.parent;
			while((p != null) && (servos.Count < 4))
			{
				ModuleIRServo_v3 servo = p.GetComponent<ModuleIRServo_v3>();

				if(servo != null)
					servos.Add(servo);

				p = p.parent;
			}

			float relaxFactor = 1f;

			foreach(ModuleIRServo_v3 s in servos)
				s.SetRelaxMode(relaxFactor);
// FEHLER, wenn das latching stockt (stuck, bzw. bewegt sich nicht genug nahe ran), dann dynamisch stärker freigeben der joints hier... -> das wär noch 'ne bessere idee du... echt jetzt



			Vector3 forward = -grappleFixture.nodeTransform.forward;
			forward = Quaternion.FromToRotation(forward, nodeTransform.forward) * nodeTransform.forward;

			Vector3 right = Vector3.ProjectOnPlane(nodeTransform.right, forward);
			Vector3 up = Vector3.ProjectOnPlane(nodeTransform.up, forward);

			Quaternion targetRotation = Quaternion.Inverse(attachJoint.transform.rotation) * Quaternion.LookRotation(-up, forward);

			Vector3 oldTargetPosition = attachJoint.targetPosition;

			bool bCheck; float oldDistanceSquare;

			for(int i = 0; i < 60; i++) // FEHLER, 120 ... fraglich
			{
				attachJoint.targetRotation = Quaternion.Slerp(Quaternion.identity, targetRotation, (float)i / 60f);
				attachJoint.targetPosition = Vector3.Slerp(oldTargetPosition, Vector3.zero, (float)i / 60f);

				haha = (attachJoint.transform.TransformPoint(attachJoint.targetPosition) - attachJoint.connectedBody.transform.TransformPoint(attachJoint.connectedAnchor));
				oldDistanceSquare = haha.sqrMagnitude;

				yield return new WaitForFixedUpdate();

				bCheck = false;

				foreach(ModuleIRServo_v3 s in servos)
				{ if(s.RelaxStep()) bCheck = true; }

				if(!bCheck && relaxFactor > 0.15f)
				{
					relaxFactor -= 0.1f;

					foreach(ModuleIRServo_v3 s in servos)
						s.SetRelaxMode(relaxFactor);
				}

				yield return new WaitForFixedUpdate();

				haha2 = (attachJoint.transform.TransformPoint(attachJoint.targetPosition) - attachJoint.connectedBody.transform.TransformPoint(attachJoint.connectedAnchor));
				bCheck = (oldDistanceSquare * 0.8 < haha2.sqrMagnitude);

				if(!bCheck && relaxFactor > 0.15f)
				{
					relaxFactor -= 0.1f;

					foreach(ModuleIRServo_v3 s in servos)
						s.SetRelaxMode(relaxFactor);
				}
			}

			attachJoint.targetRotation = targetRotation;
			attachJoint.targetPosition = Vector3.zero;

			attachJoint.xMotion = attachJoint.yMotion = attachJoint.zMotion = ConfigurableJointMotion.Limited;
		//	attachJoint.angularXMotion = attachJoint.angularYMotion = attachJoint.angularZMotion = ConfigurableJointMotion.Limited; -> dazu müssten wir die Limiten umsetzen


			foreach(ModuleIRServo_v3 s in servos)
				s.ResetRelaxMode();


			fsm.RunEvent(ev_tolatched);
		}

IEnumerator ahi(Vector3 position, Vector3 position2, Vector3 position3)
{
//	for(int i = 0; i < 10; i++)
//		yield return new WaitForEndOfFrame();
//	yield return new WaitForSeconds(4f);

	FlightCamera.fetch.GetPivot().position = position;
	FlightCamera.fetch.SetCamCoordsFromPosition(position2);
	FlightCamera.fetch.GetCameraTransform().position = position3;

	yield return new WaitForEndOfFrame(); // FEHLER, brauch ich evtl. nicht mal... kann das evtl. gleich sofort feuern
}

		private void ExecuteDocking()
		{
			if(attachJoint)
				Destroy(attachJoint);

			// das Teil jetzt noch dahinbewegen, wo es hingehört (einfach mit einem Ruck)

Vector3 forward = -grappleFixture.nodeTransform.forward;
forward = Quaternion.FromToRotation(forward, nodeTransform.forward) * nodeTransform.forward;

Vector3 right = Vector3.ProjectOnPlane(nodeTransform.right, forward);
Vector3 up = Vector3.ProjectOnPlane(nodeTransform.up, forward);

Quaternion targetRotation = Quaternion.LookRotation(-up, forward); // die, die ich haben sollte... glaub ich :-)



Vector3 targetPosition = grappleFixture.nodeTransform.position + targetRotation * transform.InverseTransformVector(transform.position - nodeTransform.position);


part.transform.SetPositionAndRotation(targetPosition, targetRotation); // FEHLER, das ist super geil und superexperimentell, das fliegt fast zu 100% in die Luft :-)



//StartCoroutine(ahi(FlightCamera.fetch.GetPivot().position, FlightCamera.fetch.GetCameraTransform().position, FlightCamera.fetch.GetCameraTransform().position));

	
			if(part.vessel != grappleFixture.part.vessel)
			{
				Debug.Log("Docking to vessel " + grappleFixture.part.vessel.vesselName, gameObject);

				if(Vessel.GetDominantVessel(vessel, grappleFixture.part.vessel) == vessel)
					BuildDockedJoint(grappleFixture.part, grappleFixture.nodeTransform, part, nodeTransform);
				else
					BuildDockedJoint(part, nodeTransform, grappleFixture.part, grappleFixture.nodeTransform);

				fsm.RunEvent(ev_todocked);
			}
			else
			{
					// FEHLER, blöde nodes hier definieren oder sowas...
				sameVesselDockJoint = PartJoint.Create(part, grappleFixture.part, null, null, grappleFixture.part.attachMode);
		//		GameEvents.onSameVesselDock.Fire(new GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode>(this, node)); -> na ja, wir sind keine ModuleDockingNode... also, hätte es einen Sinn das zu posten? oder lassen wir's lieber? oder posten wir was anderes? *hmm* mal überlegen dann

				fsm.RunEvent(ev_todocked_sameVessel);
			}
		}

		private void BuildDockedJoint(Part dockedPart, Transform dockedPartNode, Part hostPart, Transform hostPartNode)
		{
			vesselInfo = new DockedVesselInfo();
			vesselInfo.name = dockedPart.vessel.vesselName;
			vesselInfo.vesselType = dockedPart.vessel.vesselType;
			vesselInfo.rootPartUId = dockedPart.vessel.rootPart.flightID;

			attachedVesselInfo = new DockedVesselInfo();
			attachedVesselInfo.name = hostPart.vessel.vesselName;
			attachedVesselInfo.vesselType = hostPart.vessel.vesselType;
			attachedVesselInfo.rootPartUId = hostPart.vessel.rootPart.flightID;

			Vessel oldDockedVessel = dockedPart.vessel;

			uint persistentId = dockedPart.vessel.persistentId;
			uint persistentId2 = hostPart.vessel.persistentId;
			GameEvents.onVesselDocking.Fire(persistentId, persistentId2);
			GameEvents.onActiveJointNeedUpdate.Fire(hostPart.vessel);
			GameEvents.onActiveJointNeedUpdate.Fire(dockedPart.vessel);

//				node.vessel.SetRotation(node.vessel.transform.rotation);
//				base.vessel.SetRotation(Quaternion.FromToRotation(nodeTransform.forward, -node.nodeTransform.forward) * base.vessel.transform.rotation);
//				base.vessel.SetPosition(base.vessel.transform.position - (nodeTransform.position - node.nodeTransform.position), usePristineCoords: true);

Part dp = dockedPart;
dp = dockedPart.RigidBodyPart;

				// hostPart zurückdrehen auf das was es sein muss -> anhand von orgPos/orgRot
			hostPart.vessel.SetRotation(hostPart.vessel.transform.rotation);
			hostPart.vessel.SetPosition(hostPart.vessel.transform.position, true);

//dockedPart.vessel.SetRotation(dockedPart.vessel.transform.rotation);
//dockedPart.vessel.SetPosition(dockedPart.vessel.transform.position, true);
			dockedPart.vessel.SetRotation(Quaternion.Inverse(dockedPart.orgRot) * Quaternion.FromToRotation(hostPartNode.forward, -dockedPartNode.forward) * hostPart.vessel.transform.rotation * hostPart.orgRot);
			dockedPart.vessel.SetPosition(hostPartNode.position
+ (dockedPart.transform.position - dockedPartNode.position) // -> zum dockedPart
- dockedPart.vessel.transform.rotation * dockedPart.orgPos // zurück zum Root... oder?
				, true);

			dp.vessel.IgnoreGForces(10);
			dp.Couple(hostPart);

			GameEvents.onVesselPersistentIdChanged.Fire(persistentId, persistentId2);
			if(oldDockedVessel == FlightGlobals.ActiveVessel)
			{
				FlightGlobals.ForceSetActiveVessel(dockedPart.vessel);
				FlightInputHandler.SetNeutralControls();
			}
			else if(dockedPart.vessel == FlightGlobals.ActiveVessel)
			{
				dockedPart.vessel.MakeActive();
				FlightInputHandler.SetNeutralControls();
			}

			for(int i = 0; i < oldDockedVessel.parts.Count; i++)
			{
				FlightGlobals.PersistentLoadedPartIds.Add(oldDockedVessel.parts[i].persistentId, oldDockedVessel.parts[i]);
				if(oldDockedVessel.parts[i].protoPartSnapshot != null)
					FlightGlobals.PersistentUnloadedPartIds.Add(oldDockedVessel.parts[i].protoPartSnapshot.persistentId, oldDockedVessel.parts[i].protoPartSnapshot);
			}
			GameEvents.onVesselWasModified.Fire(dockedPart.vessel);
			GameEvents.onDockingComplete.Fire(new GameEvents.FromToAction<Part, Part>(dockedPart, hostPart));

StartCoroutine(ahi(FlightCamera.fetch.GetPivot().position, FlightCamera.fetch.GetCameraTransform().position, FlightCamera.fetch.GetCameraTransform().position));
		}

		public void Undock()
		{
			ReDock();

			if(grappleFixture.part.parent.parent == part) // komisch, weil wir immer zum Parent vom GF docken (weil das Teil nicht physikalisch ist)
				grappleFixture.part.parent.Undock(vesselInfo);
			else
				part.Undock(attachedVesselInfo);

			attachedVesselId = grappleFixture.vessel.id;
			attachedPartId = grappleFixture.part.flightID;

	attachedBreakForce = catchedBreakForce; // FEHLER, wenn latched, das doch viel viel höher setzen? oder? also echt jetzt -> fehlt ja total

			attachJoint = part.gameObject.AddComponent<ConfigurableJoint>();
				// FEHLER, umdrehen, wenn -> Masse gross, klein bla bla... kennen wir ja

			attachJoint.connectedBody = grappleFixture.part.Rigidbody;
			attachJoint.breakForce = catchedBreakForce;
			attachJoint.breakTorque = catchedBreakForce;

			attachJoint.anchor = attachJoint.transform.InverseTransformPoint(nodeTransform.position);

			attachJoint.xMotion = attachJoint.yMotion = attachJoint.zMotion = ConfigurableJointMotion.Limited;
			attachJoint.angularXMotion = attachJoint.angularYMotion = attachJoint.angularZMotion = ConfigurableJointMotion.Free;
					// = ConfigurableJointMotion.Limited; -> dazu müssten wir die Limiten umsetzen

			attachJoint.xDrive = attachJoint.yDrive = attachJoint.zDrive = new JointDrive { maximumForce = catchedBreakForce, positionSpring = catchedBreakForce, positionDamper = 0f };
			attachJoint.angularXDrive = attachJoint.angularYZDrive = new JointDrive { maximumForce = catchedBreakForce, positionSpring = catchedBreakForce, positionDamper = 0f };

			fsm.RunEvent(ev_tolatched);

			UpdateUI();
		}

		public void Detach()
		{
			if((fsm.CurrentState == st_catched)
			|| (fsm.CurrentState == st_latching)
			|| (fsm.CurrentState == st_latched))
			{
				if(fsm.CurrentState == st_latching)
					StopCoroutine(SlerpLatching());

				if(attachJoint)
					Destroy(attachJoint);
				attachJoint = null;

				fsm.RunEvent(ev_toreleased);

				// Sound
				if(detachSoundFilePath != "")
					AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(detachSoundFilePath), part.transform.position);
			}
			else if(fsm.CurrentState == st_docked)
			{
				ReDock();

				if(grappleFixture.part.parent.parent == part) // komisch, weil wir immer zum Parent vom GF docken (weil das Teil nicht physikalisch ist)
					grappleFixture.part.parent.Undock(vesselInfo);
				else
					part.Undock(attachedVesselInfo);

				fsm.RunEvent(ev_toreleased);

				// Sound
				if(detachSoundFilePath != "")
					AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(detachSoundFilePath), part.transform.position);
			}
			else if(fsm.CurrentState == st_docked_sameVessel)
			{
				sameVesselDockJoint.DestroyJoint();
				sameVesselDockJoint = null;

				fsm.RunEvent(ev_toreleased);

				// Sound
				if(detachSoundFilePath != "")
					AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(detachSoundFilePath), part.transform.position);
			}
			else if(fsm.CurrentState == st_preattached)
			{
				ReDock();

				if(grappleFixture.part.parent == part)
					grappleFixture.part.decouple();
				else
					part.decouple();

				fsm.RunEvent(ev_toreleased);

				// Sound
				if(detachSoundFilePath != "")
					AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(detachSoundFilePath), part.transform.position);
			}

			UpdateUI();
		}

		private void FindFind(Part p, ref List<ModuleIRLEE> l)
		{
			ModuleIRLEE m = p.GetComponent<ModuleIRLEE>();
			if(m != null)
				l.Add(m);

			for(int i = 0; i < p.children.Count; i++)
				FindFind(p.children[i], ref l);
		}

		private void ReDock()
		{
			List<ModuleIRLEE> l = new List<ModuleIRLEE>();
			FindFind(part, ref l);
			ModuleIRLEE[] lees = l.ToArray();

			int i = 0;
			while((i < lees.Length) && (lees[i].fsm.CurrentState != lees[i].st_docked_sameVessel)) ++i;

			if(i < lees.Length)
			{
				// sollten schon eine Verbindung haben, also reicht das

				StartCoroutine(WaitAndRedockLee(lees[i]));

				return;
			}

	// FEHLER, anders suchen... ja, egal jetzt mal
/*			ModuleDockingNode[] dns = part.GetComponentsInChildren<ModuleDockingNode>();

			int j = 0;
			while((j < dns.Length) && (dns[j].fsm.CurrentState != dns[j].st_docker_sameVessel)) ++j;

			if(j < dns.Length)
			{
				// verbinden

				// coroutine starten -> ist mir zu blöd im Moment

				return;
			}*/
		}

		private static IEnumerator WaitAndRedockLee(ModuleIRLEE lee)
		{
			yield return new WaitForFixedUpdate();
			yield return new WaitForFixedUpdate();

			if(lee.sameVesselDockJoint != null) // FEHLER, wer räumt das ab? das undock?? *hmm* ist doch komisch, oder? ... oder ist das der lee, den's nicht mehr gibt?
				lee.sameVesselDockJoint.DestroyJoint();
			lee.sameVesselDockJoint = null;

			lee.ExecuteDocking();
		}

	//	private IEnumerable WaitAndRedockDockingPort(ModuleDockingNode dockingPort)
	//	{
	//	}

		public static Part GetPartByID(Guid vesselID, uint partID)
		{
			Vessel searchVessel = FlightGlobals.FindVessel(vesselID);

			if(!searchVessel)
			{
				Logger.Log("GetPartByID - Searched vessel not found !");
				return null;
			}
		
			if(!searchVessel.loaded)
			{
				Logger.Log("GetPartByID - Searched vessel are not loaded, loading it...", Logger.Level.Warning);
				searchVessel.Load();
			}

			return GetPartByID(searchVessel, partID);
		}

		public static Part GetPartByID(Vessel vessel, uint partID)
		{
			for(int i = 0; i < vessel.parts.Count; i++)
			{
				if(vessel.parts[i].flightID == partID)
					return vessel.parts[i];
			}

			Logger.Log("GetPartByID - Searched part not found !");
			return null;
		}

		private bool UpdateAndConsumeElectricCharge()
		{
			return true; // FEHLER, ausgebaut im Moment... echt jetzt
/*			float amountToConsume;

			if(attachType == AttachType.None)
			{
				if(electricChargeRequiredIdle == 0f)
					return true;

				amountToConsume = electricChargeRequiredIdle * TimeWarp.fixedDeltaTime;
			}
			else
			{
				if(electricChargeRequiredConnected == 0f)
					return true;

				amountToConsume = electricChargeRequiredConnected * TimeWarp.fixedDeltaTime;
			}

			float amountConsumed = part.RequestResource(electricResource.id, amountToConsume);

			LastPowerDrawRate = amountConsumed / TimeWarp.fixedDeltaTime;

			return amountConsumed == amountToConsume;*/
		}

		////////////////////////////////////////
		// Update-Functions

		public void FixedUpdate()
		{
	//		DrawRelative(1, part.transform.position, part.transform.TransformDirection(rayDir));

/*			if(activated)
			{
				if(UpdateAndConsumeElectricCharge())
				{
					if(soundSound != null)
						soundSound.Play();
				}
				else
				{
					if(soundSound != null)
						soundSound.Stop();

					IsActive = false;

					LastPowerDrawRate = 0f;
				}
			}*/

			fsm.FixedUpdateFSM();
		}

		////////////////////////////////////////
		// Properties

		////////////////////////////////////////
		// Status

// FEHLER, den Status irgendwo anzeigen? -> ja, das tun... echt jetzt

		////////////////////////////////////////
		// Settings

		////////////////////////////////////////
		// Characteristics - values 'by design'

		[KSPField(isPersistant = false)] public float catchedBreakForce = 10f;
		[KSPField(isPersistant = false)] public float latchedBreakForce = 1000f;

		[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Electric charge for latching", guiUnits = "u")]
		public float electricChargeRequiredLatching = 0.3f;
			// FEHLER, für's latching? evtl.?

		public float ElectricChargeRequiredLatching
		{
			get { return electricChargeRequiredLatching; }
		}

		////////////////////////////////////////
		// Scaling

		[KSPField(isPersistant = false)] public float scaleMass = 1.0f;
		[KSPField(isPersistant = false)] public float scaleElectricChargeRequired = 2.0f;

		////////////////////////////////////////
		// Input

		////////////////////////////////////////
		// Editor

		////////////////////////////////////////
		// Context Menu

		private void UpdateUI()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				state = fsm.currentStateName;

				if((fsm.CurrentState == st_found)
				|| (fsm.CurrentState == st_catched)
				|| (fsm.CurrentState == st_latching))
				{
					Fields["statedistance"].guiActive = true;
					Fields["stateangle"].guiActive = true;
					Fields["staterotation"].guiActive = true;
				}
				else
				{
					Fields["statedistance"].guiActive = false;
					Fields["stateangle"].guiActive = false;
					Fields["staterotation"].guiActive = false;
				}

				Events["ContextMenuEnable"].guiActive = (fsm.CurrentState == st_disabled);
				Events["ContextMenuDisable"].guiActive = (fsm.CurrentState.StateEvents.Contains(ev_todisabled));

				Events["ContextMenuLatch"].guiActive = (fsm.CurrentState.StateEvents.Contains(ev_tolatching));
				Events["ContextMenuDock"].guiActive = (fsm.CurrentState.StateEvents.Contains(ev_todocked) || fsm.CurrentState.StateEvents.Contains(ev_todocked_sameVessel)) && (fsm.CurrentState != st_docked_sameVessel);
				Events["ContextMenuUndock"].guiActive = (fsm.CurrentState == st_docked) || (fsm.CurrentState == st_docked_sameVessel);
				Events["ContextMenuDetach"].guiActive = (fsm.CurrentState.StateEvents.Contains(ev_toreleased));

					// unfocused und so Zeugs noch? ... na mal sehen...
			}
			else
			{
				Fields["electricChargeRequiredLatching"].guiActiveEditor = (electricChargeRequiredLatching != 0f);
			}
		}

		private void UpdateUIPositionData()
		{
			statedistance = (nodeTransform.position - grappleFixture.nodeTransform.position).magnitude;
			stateangle = Vector3.Angle(nodeTransform.forward, -grappleFixture.nodeTransform.forward);
			
			Vector3 otherDown = Vector3.ProjectOnPlane(-grappleFixture.nodeTransform.up, nodeTransform.forward);
			staterotation = Vector3.Angle(nodeTransform.up, otherDown);
		}

		[KSPEvent(active = true, guiActive = false, guiActiveUnfocused = false, guiName = "Enable")]
		public void ContextMenuEnable()
		{
			fsm.RunEvent(ev_tosearching);
		}

		[KSPEvent(active = true, guiActive = false, guiActiveUnfocused = false, guiName = "Disable")]
		public void ContextMenuDisable()
		{
			fsm.RunEvent(ev_todisabled);
		}

		[KSPEvent(active = true, guiActive = false, guiActiveUnfocused = false, guiName = "Latch")]
		public void ContextMenuLatch()
		{
			ExecuteLatching();
		}

		[KSPEvent(active = true, guiActive = false, guiActiveUnfocused = false, guiName = "Dock")]
		public void ContextMenuDock()
		{
			if(grappleFixture != null)
				ExecuteDocking();
		}

		[KSPEvent(active = true, guiActive = false, guiActiveUnfocused = false, guiName = "Undock")]
		public void ContextMenuUndock()
		{
			Undock();
		}

		[KSPEvent(active = true, guiActive = false, guiActiveUnfocused = false, guiName = "Detach")]
		public void ContextMenuDetach()
		{
			Detach();
		}

		////////////////////////////////////////
		// Actions

		[KSPAction("Enable")]
		public void ActionEnable()
		{
			fsm.RunEvent(ev_tosearching);
		}

		[KSPAction("Disable")]
		public void ActionDisable()
		{
			fsm.RunEvent(ev_todisabled);
		}

		[KSPAction("Latch")]
		public void ActionLatch()
		{
			ExecuteLatching();
		}

		[KSPAction("Dock")]
		public void ActionDock()
		{
			ExecuteDocking();
		}

		[KSPAction("Detach")]
		public void ActionDetach()
		{
			Detach();
		}

		////////////////////////////////////////
		// IRescalable

		// Tweakscale support
		[KSPEvent(guiActive = false, active = true)]
		void OnPartScaleChanged(BaseEventDetails data)
		{
			OnRescale(new ScalingFactor(data.Get<float>("factorAbsolute")));
		}

		public void OnRescale(ScalingFactor factor)
		{
			ModuleIRLEE prefab = part.partInfo.partPrefab.GetComponent<ModuleIRLEE>();

			part.mass = prefab.part.mass * Mathf.Pow(factor.absolute.linear, scaleMass);
/*
			forceNeeded = prefab.forceNeeded * factor.absolute.linear;
 			partBreakForce = partBreakForce * factor.relative.linear;
 			groundBreakForce = groundBreakForce * factor.relative.linear;

			electricChargeRequiredIdle = prefab.electricChargeRequiredIdle * Mathf.Pow(factor.absolute.linear, scaleElectricChargeRequired);
			electricChargeRequiredConnected = prefab.electricChargeRequiredConnected * Mathf.Pow(factor.absolute.linear, scaleElectricChargeRequired);
*/
			UpdateUI();
		}

		////////////////////////////////////////
		// IModuleInfo

		string IModuleInfo.GetModuleTitle()
		{
			return "Latching End Effector";
		}

		string IModuleInfo.GetInfo()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("Attach strength (catched): {0:F0}\n", catchedBreakForce);
			sb.AppendFormat("Attach strength (latched): {0:F0}\n", latchedBreakForce);

			if(electricChargeRequiredLatching != 0f)
			{
				sb.Append("\n\n");
				sb.Append("<b><color=orange>Requires:</color></b>\n");
				
				if(electricChargeRequiredLatching != 0f)
					sb.AppendFormat("- <b>Electric Charge:</b> {0:F0}\n  (for latching)", electricChargeRequiredLatching);
			}

			return sb.ToString();
		}

		Callback<Rect> IModuleInfo.GetDrawModulePanelCallback()
		{
			return null;
		}

		string IModuleInfo.GetPrimaryField()
		{
			return null;
		}

		////////////////////////////////////////
		// Debug

/* FEHLER, vielleicht später...
  public void OnKISAction(Dictionary<string, object> eventData) {
    var action = eventData["action"].ToString();
    var tgtPart = eventData["targetPart"] as Part;

    if(action == "Store" || action == "AttachStart" || action == "DropEnd") {
      DetachGrapple();
    }
    if(action == "AttachEnd") {
      DetachGrapple();
      if(tgtPart == null) {
        AttachStaticGrapple();
      }
    }
  }
*/

		////////////////////////////////////////
		// Debug

		private MultiLineDrawer ld;

		private void DebugInit()
		{
			ld = new MultiLineDrawer();
			ld.Create(null);
		}

		public void DrawRelative(int idx, Vector3 p_from, Vector3 p_vector)
		{
			ld.Draw(idx, p_from, p_from + p_vector);
		}
	}

//// Zeugs zum umarbeiten

/*
	[KSPField(isPersistant = true)]
	public bool crossfeed = true;

	public Transform nodeTransform;
	public Transform controlTransform;

	public bool IsDisabled
	{
		get
		{
			int result;
			if(fsm != null)
			{
				if(fsm.Started)
				{
					result = ((fsm.CurrentState == st_disabled) ? 1 : 0);
					goto IL_004c;
				}
			}
			result = 0;
			goto IL_004c;
			IL_004c:
			return (byte)result != 0;
		}
	}

		nodeTransform = base.part.FindModelTransform(nodeTransformName);



	public override void OnActive()
	{
		if(!staged)
		{
			return;
		}
		if(!stagingEnabled)
		{
			return;
		}
		if(base.Events["Decouple"].active)
		{
			Decouple();
			return;
		}
		return;
	}
/*
	[KSPAction("#autoLOC_6001447")]
	public void MakeReferenceToggle(KSPActionParam act)
	{
		MakeReferenceTransform();
	}

	[KSPEvent(guiActive = true, guiName = "#autoLOC_6001447")]
	public void MakeReferenceTransform()
	{
		base.part.SetReferenceTransform(controlTransform);
		base.vessel.SetReferenceTransform(base.part);
	}
*/


}
