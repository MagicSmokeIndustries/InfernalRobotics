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
	public class ModuleIRAttachment : PartModule, IModuleInfo/*, IRescalable -> TweakScale */
	{
		public enum AttachType { None = 0, Ground = 1, Part = 2, Docked = 3 };
		[KSPField(isPersistant = true)] public AttachType attachType = AttachType.None;

		// attachement
		public ConfigurableJoint attachJoint = null;
		public float attachedBreakForce;

		// attachement to the ground
		public GameObject attachedGround = null;

		// attachement to a part and docked
		public Part attachedPart = null;

		public Guid attachedVesselId;
		public uint attachedPartId;

// FEHLER, temp, mal sehen ob's so bleibt
PartJoint sameVesselDockJoint;
AttachNode referenceNode = null; // aktuell nur für second-dock genutzt... eigentlich blöd -> aber part-couple tut das anders... ist halt so...

		public DockedVesselInfo vesselInfo = null;
		public DockedVesselInfo attachedVesselInfo = null;

		// Electric Power
		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Current Draw", guiUnits = "u/s")]
		private double LastPowerDrawRate;

		PartResourceDefinition electricResource = null;

		// Sounds
		[KSPField(isPersistant = false)] public string attachGroundSoundFilePath = "";
		[KSPField(isPersistant = false)] public string attachPartSoundFilePath = "";
		[KSPField(isPersistant = false)] public string attachDockedSoundFilePath = "";
		[KSPField(isPersistant = false)] public string detachSoundFilePath = "";
		
		[KSPField(isPersistant = false)] public string activatingSoundFilePath = "";
		[KSPField(isPersistant = false)] public string activatedSoundFilePath = "";
		[KSPField(isPersistant = false)] public string deactivatingSoundFilePath = "";

		protected SoundSource soundSound = null;

		// Info
		[KSPField(guiActive = true, guiName = "State", guiFormat = "S")]
		public string state = "Idle";


		public ModuleIRAttachment()
		{
			DebugInit();
		}

		////////////////////////////////////////
		// Callbacks

		public override void OnAwake()
		{
			GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
			GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);

			GameEvents.onJointBreak.Add(OnJointBroken);

	// FEHLER, nur wenn wir wirklich einer sind... zwar... na ja... evtl. wär's 'ne Idee... mal sehen dann -> dual-re-undock und so ginge nicht zwar bei anderen, aber... na ja... ist ja egal, oder?
			// reicht doch, wenn ich das kann... oder nicht??
//			part.dockingPorts.AddUnique(this);
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			if(state == StartState.Editor) // FEHLER, müsste ich None abfangen?? wieso sollte das je aufgerufen werden???
				return;

			electricResource = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

			if(activatedSoundFilePath != "")
			{
				if(soundSound == null)
					soundSound = new SoundSource(part, "connector");
				soundSound.Setup(activatedSoundFilePath, true);
			}

			switch(attachType)
			{
			case AttachType.Ground:
				break; // do nothing here -> done after getting off-rails

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
*/					{
						Logger.Log("OnLoad(Core) Unable to get saved docked part!", Logger.Level.Fatal);
						attachType = AttachType.None;
					}
				}
				break;
			}

		//	AttachContextMenu();

			UpdateUI();
		}

		public IEnumerator WaitAndInitPartAttach()
		{
			yield return new WaitForEndOfFrame();

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

end:;
		}

		public void OnDestroy()
		{
		//	DetachContextMenu();

			GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
			GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);

			GameEvents.onJointBreak.Remove(OnJointBroken);

			if(attachType == AttachType.Ground)
				DetachGround();
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

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
			}
		}

		public override void OnLoad(ConfigNode node)
		{
			base.OnLoad(node);

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
			}
		}

		public void OnVesselGoOnRails(Vessel v)
		{
			if(part.vessel != v)
				return;

			if(attachType == AttachType.Ground)
				DetachGround();

			part.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
		}

		public void OnVesselGoOffRails(Vessel v)
		{
			if(part.vessel != v)
				return;

			if(attachType == AttachType.Ground)
				AttachGround(groundBreakForce);

			if(activated && (attachType == AttachType.None))
				part.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
		}

// FEHLER, Reihenfolge??
		void OnCollisionEnter(Collision collision)
		{
			if(!activated || (attachType != AttachType.None))
				return;

			// don't attach if inpact force is too low
			if(collision.relativeVelocity.magnitude < forceNeeded)
				return;

// Kollision mit dem da -> collision.gameObject und collision.rigidbody beim GetContact(0) z.B. -> wozu machen wir den ganzen Mist hier? wieso nehmen wir nicht das, mit dem wir kollidiert sind?

			float shorterDist = Mathf.Infinity;
			bool nearestHitFound = false;
			Part nearestHitPart = null;
			RaycastHit nearestHit = new RaycastHit();

			Vector3 rayDirection = this.part.transform.TransformDirection(rayDir);
			
			// get all raycast hits in front of the grapple
			var nearestHits = new List<RaycastHit>(Physics.RaycastAll(part.transform.position, rayDirection, rayLenght, 557059));
    
			foreach(RaycastHit hit in nearestHits)
			{
				if(hit.collider == this.part.collider)
					continue;

				// test if we can only connect to ground
				if(!(partAttach || dockedAttach) && hit.rigidbody && hit.rigidbody.GetComponent<Part>())
					continue;

				if(!groundAttach && (!hit.rigidbody || !hit.rigidbody.GetComponent<Part>()))
					continue;

				// find closest
				float tmpShorterDist = Vector3.Distance(part.transform.position, hit.point);
				if(tmpShorterDist <= shorterDist)
				{
					shorterDist = tmpShorterDist;
					nearestHit = hit;
					if(nearestHit.rigidbody)
						nearestHitPart = nearestHit.rigidbody.GetComponent<Part>();
					nearestHitFound = true;
				}
			}

			if(!nearestHitFound)
			{
				Logger.Log("AttachOnCollision - Nothing to attach in front of grapple");
				return;
			}

			if(nearestHitPart)
			{
				Logger.Log("AttachOnCollision - grappleAttachOnPart=true");
				Logger.Log("AttachOnCollision - Attaching to part : " + nearestHitPart.partInfo.title); 
  
				AttachPartGrapple(nearestHitPart);
		    }
			else
			{
				Logger.Log("AttachOnCollision - Attaching to static : " + nearestHit.collider.name);

				AttachGroundGrapple();
			}

			if(!activated || (attachType != AttachType.None))
				part.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;
		}

		private void OnJointBroken(EventReport report)
		{
			if((report.origin == part) || (report.origin == attachedPart))
				StartCoroutine(WaitAndCheckJoint());
		}

		private IEnumerator WaitAndCheckJoint()
		{
			yield return new WaitForFixedUpdate();

			switch(attachType)
			{
			case AttachType.Ground:
			case AttachType.Part:
				if(attachJoint == null)
					DetachGrapple();
				break;

		// FEHLER, AttachType.Docked fehlt -> test auf brechen ist unklar
			}
		}


		public virtual void OnPartDie()
		{
			DetachGrapple(); // FEHLER, oder nur Detach()? ... KAS hatte früher sogar nur einfach den Status gelöscht
		}

		////////////////////////////////////////
		// Functions

		public void AttachGround(float breakForce = 10)
		{
			if(attachType != AttachType.None)
				return;

			Logger.Log("JointToStatic(Base) Create kinematic rigidbody");

			if(attachedGround)
				Destroy(attachedGround); // FEHLER, darf nicht sein

			attachedGround = new GameObject("GroundAnchor");
			var objRigidbody = attachedGround.AddComponent<Rigidbody>();
			objRigidbody.isKinematic = true;
			attachedGround.transform.position = part.transform.position;
			attachedGround.transform.rotation = part.transform.rotation;

			Logger.Log("JointToStatic(Base) Create fixed joint on the kinematic rigidbody");
    
			if(attachJoint)
				Destroy(attachJoint); // FEHLER, darf nicht sein

			attachJoint = part.gameObject.AddComponent<ConfigurableJoint>();

			attachJoint.breakForce = breakForce;
			attachJoint.breakTorque = breakForce;
			attachJoint.connectedBody = objRigidbody;

			attachJoint.xMotion = attachJoint.yMotion = attachJoint.zMotion = ConfigurableJointMotion.Limited;
			attachJoint.angularXMotion = attachJoint.angularYMotion = attachJoint.angularZMotion = ConfigurableJointMotion.Limited;

			attachJoint.xDrive = attachJoint.yDrive = attachJoint.zDrive = new JointDrive { maximumForce = breakForce, positionSpring = breakForce, positionDamper = 0f };
			attachJoint.angularXDrive = attachJoint.angularYZDrive = new JointDrive { maximumForce = breakForce, positionSpring = breakForce, positionDamper = 0f };
		}

		public void DetachGround()
		{
			if(attachType != AttachType.Ground)
				return;

			if(attachJoint)
				Destroy(attachJoint);

			if(attachedGround)
				Destroy(attachedGround);

			attachJoint = null;
			attachedGround = null;
		}

		public void AttachGroundGrapple()
		{
			if(!activated)
				return;

			AttachGround(groundBreakForce);
			attachType = AttachType.Ground;

			UpdateUI();

			// Sound
			AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(attachGroundSoundFilePath), part.transform.position);
		}


		public void AttachPart(Part attachToPart, float breakForce)
		{
			if(attachType != AttachType.None)
				return;

			attachedPart = attachToPart;

			attachedVesselId = attachedPart.vessel.id;
			attachedPartId = attachedPart.flightID;

			attachedBreakForce = breakForce;


			Logger.Log("AttachPart create joint on " + part.partInfo.title + " with " + attachToPart.partInfo.title);

			attachJoint = part.gameObject.AddComponent<ConfigurableJoint>();
				// FEHLER, umdrehen, wenn -> Masse gross, klein bla bla... kennen wir ja

			attachJoint.connectedBody = attachToPart.Rigidbody;
			attachJoint.breakForce = breakForce;
			attachJoint.breakTorque = breakForce;

			attachJoint.xMotion = attachJoint.yMotion = attachJoint.zMotion = ConfigurableJointMotion.Limited;
			attachJoint.angularXMotion = attachJoint.angularYMotion = attachJoint.angularZMotion = ConfigurableJointMotion.Limited;

			attachJoint.xDrive = attachJoint.yDrive = attachJoint.zDrive = new JointDrive { maximumForce = breakForce, positionSpring = breakForce, positionDamper = 0f };
			attachJoint.angularXDrive = attachJoint.angularYZDrive = new JointDrive { maximumForce = breakForce, positionSpring = breakForce, positionDamper = 0f };

			// FEHLER, die haben jeweils noch geprüft, ob wir packed sind... also ein Part... muss ich das? kann ich überhaupt in einer Situation, in der ich packed bin, diese Funktion hier auslösen???
		}

		public void DetachPart()
		{
			if(attachType != AttachType.Part)
				return;

			if(attachJoint)
				Destroy(attachJoint);
			attachedPart = null;
		}

		public void AttachPartGrapple(Part attachToPart)
		{
			if(!activated)
				return;

			AttachPart(attachToPart, partBreakForce);
			attachType = AttachType.Part;

			UpdateUI();

			// Sound
			AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(attachPartSoundFilePath), part.transform.position);
		}


		private void ExecuteDocking(Part dockedPart, Part hostPart)
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

// FEHLER, man müsste hier das Zeug korrekt setzen, bevor man dockt... das wollen wir aber mit einem Latch oder so machen... -> und es ist sowieso fraglich wie wir das bei einem re-docking täten (wenn ein weniger dominanter node schon gedockt wäre)
// das hier ist daher mal bloss für's Docking mit... was auch immer... grappler ohne Absicht was Schlaues sein zu wollen
dockedPart.vessel.SetPosition(dockedPart.vessel.transform.position, true);
dockedPart.vessel.SetRotation(dockedPart.vessel.transform.rotation);
hostPart.vessel.SetPosition(hostPart.vessel.transform.position, true);
hostPart.vessel.SetRotation(hostPart.vessel.transform.rotation);

			dockedPart.vessel.IgnoreGForces(10);
			dockedPart.Couple(hostPart);

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
		}

		public void AttachDocked(float breakForce)
		{
			if(attachType != AttachType.Part)
				return;

			if(attachJoint)
				Destroy(attachJoint);

			if(part.vessel != attachedPart.vessel)
			{
				Debug.Log("Docking to vessel " + attachedPart.vessel.vesselName, gameObject);

				if(Vessel.GetDominantVessel(vessel, attachedPart.vessel) == vessel)
					ExecuteDocking(attachedPart, part);
				else
					ExecuteDocking(part, attachedPart);
			}
			else
			{
				sameVesselDockJoint = PartJoint.Create(part, attachedPart, referenceNode, null, attachedPart.attachMode);
		//		GameEvents.onSameVesselDock.Fire(new GameEvents.FromToAction<ModuleDockingNode, ModuleDockingNode>(this, node)); -> na ja, wir sind keine ModuleDockingNode... also, hätte es einen Sinn das zu posten? oder lassen wir's lieber? oder posten wir was anderes? *hmm* mal überlegen dann
			}

			attachType = AttachType.Docked;

			UpdateUI();
		}

		public void DetachDocked()
		{
			if(attachType != AttachType.Docked)
				return;

			// FEHLER, hier erkennen, wenn wir noch dual-attached sind... oder... weiter aussen und dann 'ne andere Fkt. bauen... wie auch immer...

			if(sameVesselDockJoint)
			{
				sameVesselDockJoint.DestroyJoint();
				sameVesselDockJoint = null;
			}
			else
			{
				if(attachedPart.parent == part)
					attachedPart.Undock(vesselInfo);
				else
					part.Undock(attachedVesselInfo);
			}

			attachedPart = null;

			attachType = AttachType.None; // FEHLER, hier sehen wir das Problem -> ich will NUR die Verbindung weg, nicht das hier... -> sauberer Mist das -> neu machen
		}


		public void Detach()
		{
			switch(attachType)
			{
			case AttachType.Ground: DetachGround(); break;
			case AttachType.Part:   DetachPart(); break;
			case AttachType.Docked: DetachDocked(); break;
			}

			UpdateUI();
		}

		public void DetachGrapple()
		{
			if(attachType == AttachType.None)
				return;

			Detach();
			attachType = AttachType.None;

			IsActive = false;

			UpdateUI();

			// Sound
			AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(detachSoundFilePath), part.transform.position);
		}


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
			double amountToConsume;

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

			double amountConsumed = part.RequestResource(electricResource.id, amountToConsume);

			LastPowerDrawRate = amountConsumed / TimeWarp.fixedDeltaTime;

			return amountConsumed == amountToConsume;
		}

		////////////////////////////////////////
		// Update-Functions

		public void FixedUpdate()
		{
	//		DrawRelative(1, part.transform.position, part.transform.TransformDirection(rayDir));

			if(activated)
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
			}
		}

		////////////////////////////////////////
		// Properties

		////////////////////////////////////////
		// Status

		public bool IsActive
		{
			get { return activated; }
			set
			{
				if(activated == value)
					return;

				activated = value;

				if(HighLogic.LoadedSceneIsFlight)
				{
					if(!activated && (attachType != AttachType.None))
						Detach();

					if(activated)
					{
						if(activatingSoundFilePath != "")
							AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(activatingSoundFilePath), part.transform.position);
					}
					else
					{
						if(soundSound != null)
							soundSound.Stop();

						if(deactivatingSoundFilePath != "")
							AudioSource.PlayClipAtPoint(GameDatabase.Instance.GetAudioClip(deactivatingSoundFilePath), part.transform.position);
					}
				}

				if(activated && (attachType == AttachType.None))
					part.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;
				else
					part.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

				UpdateUI();
			}
		}

		////////////////////////////////////////
		// Settings

		[KSPField(isPersistant = true)] public bool activated = false;

		////////////////////////////////////////
		// Characteristics - values 'by design'

		[KSPField(isPersistant = false)] public Vector3 rayDir = Vector3.down;
		[KSPField(isPersistant = false)] public float rayLenght = 1;

		[KSPField(isPersistant = false)] public float forceNeeded = 5;

		[KSPField(isPersistant = false)] public float groundBreakForce = 15;
		[KSPField(isPersistant = false)] public float partBreakForce = 10;

		[KSPField(isPersistant = false)] public bool groundAttach = false;
		[KSPField(isPersistant = false)] public bool partAttach = true;
		[KSPField(isPersistant = false)] public bool dockedAttach = true;			// FEHLER, temp, das hier wäre normal false als default

		[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Electric Charge required", guiUnits = "u/s")]
		public float electricChargeRequiredIdle = 0.3f;

		[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Electric Charge required (connected)", guiUnits = "u/s")]
		public float electricChargeRequiredConnected = 0.5f;

		public float ElectricChargeRequiredIdle
		{
			get { return electricChargeRequiredIdle; }
		}

		public float ElectricChargeRequiredConnected
		{
			get { return electricChargeRequiredConnected; }
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
				Events["ContextMenuActivateToggle"].guiName = activated ? "Deactivate" : "Activate";
				Events["ContextMenuActivateToggle"].guiActive = (attachType == AttachType.None);

				Events["ContextMenuDetach"].guiActive = (attachType != AttachType.None);
				Events["ContextMenuDetach"].guiActiveUnfocused = (attachType != AttachType.None);

				Events["ContextMenuDock"].guiActive = (attachType == AttachType.Part);
				Events["ContextMenuDock"].guiActiveUnfocused = (attachType  == AttachType.Part);

				switch(attachType)
				{
				case AttachType.None:   state = "Idle"; break;
				case AttachType.Ground: state = "Ground attached"; break;
				case AttachType.Part:   state = "Attached to " + attachedPart.partInfo.title; break;
				case AttachType.Docked: state = "Docked to " + attachedPart.partInfo.title; break;
				}

				Fields["electricChargeRequiredIdle"].guiActive = (electricChargeRequiredIdle != 0f);
				Fields["electricChargeRequiredConnected"].guiActive = (electricChargeRequiredConnected != 0f);
				Fields["LastPowerDrawRate"].guiActive = (electricChargeRequiredIdle != 0f) || (electricChargeRequiredConnected != 0f);
			}
			else
			{
				Fields["electricChargeRequiredIdle"].guiActiveEditor = (electricChargeRequiredIdle != 0f);
				Fields["electricChargeRequiredConnected"].guiActiveEditor = (electricChargeRequiredConnected != 0f);
			}
		}

		[KSPEvent(active = true, guiActive = true, guiActiveUnfocused = false, guiName = "Activate")]
		public void ContextMenuActivateToggle()
		{
			IsActive = !IsActive;
		}

		[KSPEvent(active = true, guiActive = true, guiActiveUnfocused = false, guiName = "Dock")]
		public void ContextMenuDock()
		{
			AttachDocked(partBreakForce);
		}

		[KSPEvent(active = true, guiActive = false, guiActiveUnfocused = false, guiName = "Detach")]
		public void ContextMenuDetach()
		{
			DetachGrapple();
		}

		////////////////////////////////////////
		// Actions

		[KSPAction("Activate")]
		public void ActivateAction(KSPActionParam param)
		{
			IsActive = true;
		}

		[KSPAction("Deactivate")]
		public void DeactivateAction(KSPActionParam param)
		{
			IsActive = false;
		}

		[KSPAction("Detach")]
		public void DetachAction(KSPActionParam param)
		{
			if(!part.packed)
				DetachGrapple();
		}

		////////////////////////////////////////
		// IRescalable

		// Tweakscale support
		[KSPEvent(guiActive = false, active = true)]
		void OnPartScaleChanged(BaseEventDetails data)
		{
			OnRescale(new ScalingFactor(data.Get<float>("factorAbsolute"), data.Get<float>("factorRelative")));
		}

		public void OnRescale(ScalingFactor factor)
		{
			ModuleIRAttachment prefab = part.partInfo.partPrefab.GetComponent<ModuleIRAttachment>();

			part.mass = prefab.part.mass * Mathf.Pow(factor.absolute.linear, scaleMass);

			forceNeeded = prefab.forceNeeded * factor.absolute.linear;
 			partBreakForce = partBreakForce * factor.relative.linear;
 			groundBreakForce = groundBreakForce * factor.relative.linear;

			electricChargeRequiredIdle = prefab.electricChargeRequiredIdle * Mathf.Pow(factor.absolute.linear, scaleElectricChargeRequired);
			electricChargeRequiredConnected = prefab.electricChargeRequiredConnected * Mathf.Pow(factor.absolute.linear, scaleElectricChargeRequired);

			UpdateUI();
		}

		////////////////////////////////////////
		// IModuleInfo

		string IModuleInfo.GetModuleTitle()
		{
			return "Robotic Connector";
		}

		string IModuleInfo.GetInfo()
		{
			StringBuilder sb = new StringBuilder();
			if(groundAttach)
				sb.AppendFormat("Attach strength (ground): {0:F0}\n", groundBreakForce);
			if(partAttach || dockedAttach)
				sb.AppendFormat("Attach strength (part): {0:F0}\n", partBreakForce);
			sb.AppendFormat("Impact force required: {0:F0}\n", forceNeeded);

			if((electricChargeRequiredIdle != 0f) || (electricChargeRequiredConnected != 0f))
			{
				if(sb.Length > 0)
					sb.Append("\n\n");

				sb.Append("<b><color=orange>Requires:</color></b>\n");
				
				if(electricChargeRequiredIdle != 0f)
					sb.AppendFormat("- <b>Electric Charge:</b> {0:F0}/sec\n", electricChargeRequiredIdle);

				if(electricChargeRequiredConnected != 0f)
					sb.AppendFormat("- <b>Electric Charge:</b> {0:F0}/sec\n  (when connected)\n", electricChargeRequiredConnected);
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

    if (action == "Store" || action == "AttachStart" || action == "DropEnd") {
      DetachGrapple();
    }
    if (action == "AttachEnd") {
      DetachGrapple();
      if (tgtPart == null) {
        AttachStaticGrapple();
      }
    }
  }
*/

		////////////////////////////////////////
		// Debug

		private LineDrawer[] al = new LineDrawer[13];
		private Color[] alColor = new Color[13];

		private void DebugInit()
		{
			for(int i = 0; i < 13; i++)
				al[i] = new LineDrawer();

			alColor[0] = Color.red;
			alColor[1] = Color.green;
			alColor[2] = Color.yellow;
			alColor[3] = Color.magenta;	// axis
			alColor[4] = Color.blue;	// secondaryAxis
			alColor[5] = Color.white;
			alColor[6] = new Color(33.0f / 255.0f, 154.0f / 255.0f, 193.0f / 255.0f);
			alColor[7] = new Color(154.0f / 255.0f, 193.0f / 255.0f, 33.0f / 255.0f);
			alColor[8] = new Color(193.0f / 255.0f, 33.0f / 255.0f, 154.0f / 255.0f);
			alColor[9] = new Color(193.0f / 255.0f, 33.0f / 255.0f, 255.0f / 255.0f);
			alColor[10] = new Color(244.0f / 255.0f, 238.0f / 255.0f, 66.0f / 255.0f);
			alColor[11] = new Color(244.0f / 255.0f, 170.0f / 255.0f, 66.0f / 255.0f);
			alColor[12] = new Color(247.0f / 255.0f, 186.0f / 255.0f, 74.0f / 255.0f);
		}

		public void DrawRelative(int idx, Vector3 p_from, Vector3 p_vector)
		{
			al[idx].DrawLineInGameView(p_from, p_from + p_vector, alColor[idx]);
		}
	}
}
