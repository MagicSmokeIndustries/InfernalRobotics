using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;
using TweakScale;

using InfernalRobotics_v3.Command;
using InfernalRobotics_v3.Effects;
using InfernalRobotics_v3.Interfaces;
using InfernalRobotics_v3.Servo;
using InfernalRobotics_v3.Utility;


namespace InfernalRobotics_v3.Module
{
	/*
	 * Remarks:
	 *		The values for maximumForce and positionSpring in the joints should not be too large. In case of small parts,
	 *		too large values could cause unity to return NaN when calculating physics, which leads to a crash of the system.
	 *		KSP uses the PhysicsGlobals.JointForce value as a maximum (currently 1E+20f).
	 */

	public class ModuleIRServo_v3 : PartModule, IServo, IRescalable, IJointLockState
	{
		private ModuleIRMovedPart MovedPart;

		private ConfigurableJoint Joint = null;

		[KSPField(isPersistant = false)] public Vector3 axis = Vector3.right;	// x-axis of the joint
		[KSPField(isPersistant = false)] public Vector3 pointer = Vector3.up;	// towards child (if possible), but always perpendicular to axis

		[KSPField(isPersistant = false)] public string fixedMesh = string.Empty;
		[KSPField(isPersistant = false)] public string movingMesh = string.Empty;

		private Transform fixedMeshTransform = null;
		private Transform fixedMeshTransformParent; // FEHLER, supertemp, weiss nicht, ob das nicht immer transform wäre??

		private bool isOnRails = false;

		// internal information on how to calculate/read-out the current rotation/translation
		private bool rot_jointup = true, rot_connectedup = true;

		private Vector3 trans_connectedzero;
		private float trans_zero;

		private float jointconnectedzero;

		// true, if servo is attached reversed
		[KSPField(isPersistant = true)] private bool swap = false;

		/*
		 * position is an internal value and always relative to the current orientation
		 * of the joint (swap or not swap)
		 * all interface functions returning and expecting values do the swap internally
		 * and return and expect external values
		*/

		// position relative to current zero-point of joint
		[KSPField(isPersistant = true)] private float commandedPosition = 0.0f;
		private float position = 0.0f;

		private float lastUpdatePosition;

		// correction values for position
		[KSPField(isPersistant = true)] private float correction_0 = 0.0f;
		[KSPField(isPersistant = true)] private float correction_1 = 0.0f;

		// Limit-Joint (extra joint used for limits only, built dynamically, needed because of unity limitations)
		private ConfigurableJoint LimitJoint = null;
		private bool bLowerLimitJoint;
		private bool bUseDynamicLimitJoint = false;

		// Stability-Joints (extra joints used for stability of translational joints)
		[KSPField(isPersistant = false)] public bool bUseStabilityJoints = true;
		private ConfigurableJoint[] StabilityJoint = { null, null };

		// Collisions
		[KSPField(isPersistant = true)] public bool activateCollisions = false;

		// Motor (works with position relative to current zero-point of joint, like position)
		Interpolator ip;

		[KSPField(isPersistant = false)] public float friction = 0.5f;

		// Electric Power
		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Current Draw", guiUnits = "EC/s")]
		private float LastPowerDrawRate;

		PartResourceDefinition electricResource = null;

		// Sound
		[KSPField(isPersistant = false)] public float soundPitch = 1.0f;
		[KSPField(isPersistant = false)] public float soundVolume = 0.5f;
		[KSPField(isPersistant = false)] public string soundFilePath = "MagicSmokeIndustries/Sounds/infernalRoboticMotor";
		protected SoundSource soundSound = null;

		// Lights
		static int lightColorId = 0;
		static Color lightColorOff, lightColorLocked, lightColorIdle, lightColorMoving;
		int lightStatus = -1;
		Renderer lightRenderer;

		// Presets
		[KSPField(isPersistant = true)] public string presetsS = "";

		public void ParsePresetPositions()
		{
			string[] positionChunks = presetsS.Split('|');
			PresetPositions = new List<float>();
			foreach(string chunk in positionChunks)
			{
				float tmp;
				if(float.TryParse(chunk, out tmp))
					PresetPositions.Add(tmp);
			}
		}

		public void SerializePresets()
		{
			if(PresetPositions != null) // only for security -> otherwise KSP will crash
				presetsS = PresetPositions.Aggregate(string.Empty, (current, s) => current + (s + "|"));
		}

		public ModuleIRServo_v3()
		{
			DebugInit();

			if(!isFreeMoving)
			{
				ip = new Interpolator();

				presets = new ServoPresets(this);
			}
		}

		public IServo servo
		{
			get { return this; }
		}

		////////////////////////////////////////
		// Callbacks

		public override void OnAwake()
		{
			if(lightColorId == 0)
			{
				lightColorId = Shader.PropertyToID("_EmissiveColor");
				lightColorOff = new Color(0, 0, 0, 0);
				lightColorLocked = new Color(1, 0, 0, 1);
				lightColorIdle = new Color(1, 0.76f, 0, 1);
				lightColorMoving = new Color(0, 1, 0, 1);
			}

// FEHLER, wieso nicht im onStart?
			GameEvents.onVesselCreate.Add(OnVesselCreate);
			GameEvents.onVesselWasModified.Add(OnVesselWasModified);

			GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
			GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);

			GameEvents.onPhysicsEaseStart.Add(OnEaseStart);
			GameEvents.onPhysicsEaseStop.Add(OnEaseStop);

//			GameEvents.onJointBreak.Add(OnJointBreak); FEHLER weiss nicht ob nötig, ich mach's mit OnVesselWasModified
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			// Renderer for lights
			lightRenderer = part.gameObject.GetComponentInChildren<Renderer>(); // do this before workaround

			if(state == StartState.Editor)
			{
				part.OnEditorAttach = (Callback)Delegate.Combine(part.OnEditorAttach, new Callback(onEditorAttached));
				part.OnEditorDetach = (Callback)Delegate.Combine(part.OnEditorDetach, new Callback(onEditorDetached));

				try
				{
					InitializeMeshes(true);
				}
				catch(Exception)
				{}

				InitializeValues();
			}
			else
			{
				// workaround (set the parent of one mesh to the connected body makes joints a lot stronger... maybe a bug?)
				fixedMeshTransform = KSPUtil.FindInPartModel(transform, fixedMesh);
fixedMeshTransformParent = fixedMeshTransform.parent;
				if(part.parent)
					fixedMeshTransform.parent = part.parent.transform;

	
				if(soundSound == null)
					soundSound = new SoundSource(part, "motor");
				soundSound.Setup(soundFilePath, true);
	
				StartCoroutine(WaitAndInitialize()); // calling Initialize in OnStartFinished should work too, but KSP does it like this internally

				electricResource = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

// FEHLER ??? einfügen in liste
			}

			AttachContextMenu();

			UpdateUI();

			if(HighLogic.LoadedSceneIsFlight && CollisionManager4.Instance && activateCollisions)
				CollisionManager4.Instance.RegisterServo(this);
		}

		public IEnumerator WaitAndInitialize()
		{
			if(part.parent)
			{
				while(!part.attachJoint || !part.attachJoint.Joint)
					yield return null;
			}

			Initialize1();

			MovedPart = ModuleIRMovedPart.InitializePart(part);
		}

		public void OnDestroy()
		{
			DetachContextMenu();

			GameEvents.onVesselCreate.Remove(OnVesselCreate);
			GameEvents.onVesselWasModified.Remove(OnVesselWasModified);

			GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
			GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);

			GameEvents.onPhysicsEaseStart.Remove(OnEaseStart);
			GameEvents.onPhysicsEaseStop.Remove(OnEaseStop);

//			GameEvents.onJointBreak.Remove(OnJointBreak); FEHLER weiss nicht ob nötig, ich mach's mit OnVesselWasModified

			if(HighLogic.LoadedSceneIsFlight && CollisionManager4.Instance /* && activateCollisions -> removet it always, just to be sure*/)
				CollisionManager4.Instance.UnregisterServo(this);

			if(LimitJoint)
				Destroy(LimitJoint);

			if(StabilityJoint[0])
				Destroy(StabilityJoint[0]);
			if(StabilityJoint[1])
				Destroy(StabilityJoint[1]);

// FEHLER ??? entfernen aus liste... also echt jetzt
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			SerializePresets();
		}

		public override void OnLoad(ConfigNode config)
		{
			base.OnLoad(config);

			if(HighLogic.LoadedSceneIsFlight)
			{
		//		commandedPosition = position + force; FEHLER, könnte man hier tun... aber wegen der inversion ist es fraglich, ob das sinnvoll ist und -> alles andere was hier stand ist wohl auch sinnlos hier
			}
			else
				InitializeValues(); // FEHLER, sind jetzt die Daten schon drin? -> ja, unklar, ob das nötig ist hier -> Initialize1 ruft's auf, darum hab ich's hierher gepackt -> die Frage ist nur, ob das der Editor braucht

			UpdateUI();
		}

		public void OnVesselGoOnRails(Vessel v)
		{
			if(part.vessel == v)
				isOnRails = true;
		}

// FEHLER, müsste ich bei GoOnRails gewisse Joints locken? weil... ich nicht nur den AttachJoint nutze? und der das selber tut?
// wobei... soll ich mich drauf verlassen, dass er es tut? :-) tja, mal klären... später dann

		public void OnVesselGoOffRails(Vessel v)
		{
			if(part.vessel == v)
			{
				isOnRails = false;

				Initialize2();

				if(Joint)
				{
					commandedPosition = position; // FEHLER, fraglich... aber gut... vorerst mal -> trotzdem nochmal überlegen

					if(isRotational)
						Joint.targetRotation = Quaternion.AngleAxis(-commandedPosition, Vector3.right); // rotate always around x axis!!
					else
						Joint.targetPosition = Vector3.right * (trans_zero - commandedPosition); // move always along x axis!!
				}
			}
		}

		FixedJoint easeJoint;

		public void OnEaseStart(Vessel v)
		{
			if((part.vessel == v) && (Joint))
			{
				easeJoint = Joint.gameObject.AddComponent<FixedJoint>();
				easeJoint.connectedBody = Joint.connectedBody;

				easeJoint.breakForce = float.PositiveInfinity;
				easeJoint.breakTorque = float.PositiveInfinity;
			}
		}

		public void OnEaseStop(Vessel v)
		{
			if((part.vessel == v) && (Joint))
			{
				Destroy(easeJoint);
			}
		}

		public void OnVesselCreate(Vessel v)
		{
			if(part.vessel == v)
			{
				if(part.attachJoint && part.attachJoint.Joint && (Joint != part.attachJoint.Joint))
					Initialize1();
			}
		}

		public void OnVesselWasModified(Vessel v)
		{
			if(part.vessel == v)
			{
				if(part.attachJoint && part.attachJoint.Joint && (Joint != part.attachJoint.Joint))
					Initialize1();
				else // FEHLER, Idee... evtl. wurde ich abgehängt?? -> was mach ich bei "break" oder "die"?
				{
					if((part.parent == null) && (fixedMeshTransform != null))
						fixedMeshTransform.parent = fixedMeshTransformParent;
				}
			}
		}

		public void onEditorAttached()	// FEHLER, das ist zwar jetzt richtig, weil die Teils immer auf 0 stehen, wenn sie nicht attached sind, aber... mal ehrlich... das müsste sauberer werden
		{
			swap = FindSwap();
			fixedMeshTransform = KSPUtil.FindInPartModel(transform, swap ? movingMesh : fixedMesh);
		}

		public void onEditorDetached()
		{
			/* Remarks:
			 * KSP does send onEditorDetached without sending a onEditorAttached for symmetry-objects
			 * in this case we don't have a fixedMeshTransform and don't need to do anything
			 * 
			 * it is possible that this could also be because of an error -> in this case we wouldn't
			 * detect this anymore... no idea if this could be a problem
			 * */
			if(fixedMeshTransform == null)
				return;

			EditorReset();
		}

		////////////////////////////////////////
		// Functions

		// corrects all the values to valid values
		public void InitializeValues()
		{
			if(!isInverted)
			{
				_gui_minPositionLimit = minPositionLimit = Mathf.Clamp(minPositionLimit, minPosition, maxPositionLimit);
				_gui_maxPositionLimit = maxPositionLimit = Mathf.Clamp(maxPositionLimit, minPositionLimit, maxPosition);
			}
			else
			{
				_gui_minPositionLimit = maxPositionLimit = Mathf.Clamp(maxPositionLimit, minPositionLimit, maxPosition);
				_gui_maxPositionLimit = minPositionLimit = Mathf.Clamp(minPositionLimit, minPosition, maxPositionLimit);
			}

			_gui_torqueLimit = torqueLimit = Mathf.Clamp(torqueLimit, 0.1f, maxTorque);

			_gui_accelerationLimit = accelerationLimit = Mathf.Clamp(accelerationLimit, 0.05f, maxAcceleration);
			ip.maxAcceleration = accelerationLimit * factorAcceleration;

			_gui_speedLimit = speedLimit = Mathf.Clamp(speedLimit, 0.05f, maxSpeed);
			ip.maxSpeed = speedLimit * factorSpeed * groupSpeedFactor;

			ParsePresetPositions();
		}

		public bool FindSwap()
		{
			AttachNode nodeToParent = part.FindAttachNodeByPart(part.parent); // always exists

			if(nodeToParent == null)
				return false;

		//	int idx = 0;
		//	while((idx < part.attachNodes.Count)
		//		&& (part.attachNodes[idx].position != nodeToParent.position)) ++idx;
			// -> first method -> but second one seems to be more robust

			int idx = 0;
			while((idx < part.attachNodes.Count)
				&& (part.attachNodes[idx].originalPosition != nodeToParent.originalPosition)) ++idx;

			return ((idx < part.attachNodes.Count) && (part.attachNodes[idx].id != "bottom"));
		}

		public void InitializeMeshes(bool bCorrectMeshPositions)
		{
			// detect attachment mode and calculate correction angles
			if(swap != FindSwap())
			{
				swap = !swap;

				if(!swap)
					correction_0 += commandedPosition;
				else
					correction_1 += commandedPosition;
			}
			else
			{
				if(swap)
					correction_0 += commandedPosition;
				else
					correction_1 += commandedPosition;
			}
			commandedPosition = 0.0f;
			position = 0.0f;
			lastUpdatePosition = 0.0f;

			// reset workaround
			if(fixedMeshTransform)
				fixedMeshTransform.parent = fixedMeshTransformParent;

			// find non rotating mesh
			fixedMeshTransform = KSPUtil.FindInPartModel(transform, swap ? movingMesh : fixedMesh);

// FEHLER, das hier umbauen, dass wir das jederzeit einfach neu setzen können (also nicht relativ setzen müssen), weil -> dann könnte ich auch mit verbogenen Elementen arbeiten und mich da dynamisch dran anpassen...
// zudem bräuchte es dann den bCorrectMeshPositions nicht mehr... dazu muss ich mir dann aber wohl die Original-Positionen merken... könnte ich zwar, sobald ich den nicht-fixen Mesh hole... oder?
			if(bCorrectMeshPositions)
			{
				if(isRotational)
				{
					fixedMeshTransform.rotation *= Quaternion.AngleAxis(-(swap ? correction_0 : correction_1), axis);
					KSPUtil.FindInPartModel(transform, swap ? fixedMesh : movingMesh).rotation *= Quaternion.AngleAxis(-(swap ? correction_1 : correction_0), axis);
				}
				else
				{
					fixedMeshTransform.Translate(axis.normalized * (-(swap ? correction_0 : correction_1)));
					KSPUtil.FindInPartModel(transform, swap ? fixedMesh : movingMesh).Translate(axis.normalized * (-(swap ? correction_1 : correction_0)));
				}
			}

			fixedMeshTransform.parent = ((Joint.gameObject == part.gameObject) ? Joint.connectedBody.transform : Joint.transform);
// FEHLER, das speichern, damit wir das wiederherstellen können, wenn der Joint bricht... oder abgehängt wird...also seinen Parent verliert... beim brechen müsste man wohl... mehrere Teils bauen aus dem hier? und je ein Teil davon anzeigen dann... sowas in der Art halt -> ist aber eher ein Detail
		}

		public void InitializeDrive()
		{
			JointDrive drive = new JointDrive
			{
				maximumForce = isFreeMoving ? 1e-20f : torqueLimit * factorTorque,
				positionSpring = hasSpring ? jointSpring : PhysicsGlobals.JointForce,
				positionDamper = hasSpring ? jointDamping : 0.0f
			};

			if(isRotational)	Joint.angularXDrive = drive;
			else				Joint.xDrive = drive;
		}
	
		public void InitializeLimits()
		{
			float min =
				swap ? (hasPositionLimit ? -maxPositionLimit : -maxPosition) : (hasPositionLimit ? minPositionLimit : minPosition);
			float max =
				swap ? (hasPositionLimit ? -minPositionLimit : -minPosition) : (hasPositionLimit ? maxPositionLimit : maxPosition);

			if(isRotational)
			{
				bUseDynamicLimitJoint = (hasPositionLimit || hasMinMaxPosition) && (max - min > 140);

				if(!bUseDynamicLimitJoint && (hasPositionLimit || hasMinMaxPosition))
				{
					// we only use (unity-)limits on this joint for parts with a small range (because of the 177° limits in unity)

					SoftJointLimit lowAngularXLimit = new SoftJointLimit() { limit = -to360(max + (!swap ? correction_0-correction_1 : correction_1-correction_0)) };
					SoftJointLimit highAngularXLimit = new SoftJointLimit() { limit = -to360(min + (!swap ? correction_0-correction_1 : correction_1-correction_0 )) };

					Joint.lowAngularXLimit = lowAngularXLimit;
					Joint.highAngularXLimit = highAngularXLimit;
					Joint.lowAngularXLimit = lowAngularXLimit;

					Joint.angularXMotion = ConfigurableJointMotion.Limited;

					if(LimitJoint)
					{
						Destroy(LimitJoint);
						LimitJoint = null;
					}
				}
				else
					Joint.angularXMotion = ConfigurableJointMotion.Free;
			}
			else
			{
				bUseDynamicLimitJoint = false;

				float halfrange = Mathf.Abs((max - min) / 2);

				trans_zero = !swap ? halfrange + min - correction_1 : correction_0 - halfrange - max;

Vector3 _axis = Joint.transform.InverseTransformVector(part.transform.TransformVector(axis)); // FEHLER, beschreiben wieso -> joint inverse (nicht part, nur config-joint)
				Joint.connectedAnchor = Joint.connectedBody.transform.InverseTransformPoint(
					Joint.transform.TransformPoint(_axis.normalized * (trans_zero - position)));

				Joint.targetPosition = Vector3.right * (trans_zero - commandedPosition); // move always along x axis!!

				Joint.linearLimit = new SoftJointLimit{ limit = halfrange };

				// add stability joints
				if(bUseStabilityJoints)
					for(int i = 0; i < 2; i++)
					{
						if(StabilityJoint[i])
							continue;

						StabilityJoint[i] = gameObject.AddComponent<ConfigurableJoint>();

						StabilityJoint[i].breakForce = Joint.breakForce;
						StabilityJoint[i].breakTorque = Joint.breakTorque;
						StabilityJoint[i].connectedBody = Joint.connectedBody;

						StabilityJoint[i].axis = axis;
						StabilityJoint[i].secondaryAxis = pointer;

						StabilityJoint[i].rotationDriveMode = RotationDriveMode.XYAndZ;

						StabilityJoint[i].xDrive = new JointDrive
						{ maximumForce = 0, positionSpring = 0, positionDamper = 0 };

						StabilityJoint[i].angularXMotion = ConfigurableJointMotion.Locked;
						StabilityJoint[i].angularYMotion = ConfigurableJointMotion.Locked;
						StabilityJoint[i].angularZMotion = ConfigurableJointMotion.Locked;
						StabilityJoint[i].xMotion = ConfigurableJointMotion.Free;
						StabilityJoint[i].yMotion = ConfigurableJointMotion.Locked;
						StabilityJoint[i].zMotion = ConfigurableJointMotion.Locked;

						StabilityJoint[i].autoConfigureConnectedAnchor = false;
						StabilityJoint[i].anchor = Joint.anchor;
						StabilityJoint[i].connectedAnchor =
							Joint.connectedBody.transform.InverseTransformPoint(
								Joint.connectedBody.transform.TransformPoint(Joint.connectedAnchor)
								+ Joint.transform.TransformDirection(_axis.normalized * ((i > 0) ? halfrange : -halfrange)));

						StabilityJoint[i].configuredInWorldSpace = false;
					}
			}

			ip.Initialize(position, !hasMinMaxPosition && !hasPositionLimit,
				to360(min + (!swap ? correction_0-correction_1 : correction_1-correction_0)),
				to360(max + (!swap ? correction_0-correction_1 : correction_1-correction_0)),
				speedLimit * factorSpeed * groupSpeedFactor, accelerationLimit * factorAcceleration);
		}

		public void Initialize1()
		{
			InitializeValues();

			bool bCorrectMeshPositions = (Joint == null);

			Joint = part.attachJoint.Joint;

			InitializeMeshes(bCorrectMeshPositions);

			for(int i = 0; i < part.transform.childCount; i++)
			{
				Transform child = part.transform.GetChild(i);
				var tmp = child.GetComponent<MeshCollider>();
				if(tmp != null)
				{
					tmp.material.dynamicFriction = tmp.material.staticFriction = friction;
					tmp.material.frictionCombine = PhysicMaterialCombine.Maximum;
				}
			}

			if(Joint.gameObject == part.gameObject)
			{
				// set anchor
				Joint.anchor = Vector3.zero;

				// correct connectedAnchor
				Joint.autoConfigureConnectedAnchor = false;
				Joint.connectedAnchor = Joint.connectedBody.transform.InverseTransformPoint(Joint.transform.TransformPoint(Vector3.zero));

				// set axis
				Joint.axis = axis;
				Joint.secondaryAxis = pointer;
			}
			else
			{
				// set anchor
				Joint.anchor = Joint.transform.InverseTransformPoint(Joint.connectedBody.transform.TransformPoint(Vector3.zero));

				// correct connectedAnchor
				Joint.autoConfigureConnectedAnchor = false;
				Joint.connectedAnchor = Vector3.zero;

				// set axis
				Joint.axis = Joint.connectedBody.transform.InverseTransformDirection(part.transform.TransformDirection(axis));
				Joint.secondaryAxis = Joint.connectedBody.transform.InverseTransformDirection(part.transform.TransformDirection(pointer));
			}

			// determine best way to calculate real rotation
			if(isRotational)
			{
				rot_jointup =
					Vector3.ProjectOnPlane(Joint.transform.up.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude >
					Vector3.ProjectOnPlane(Joint.transform.right.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude;

				rot_connectedup =
					Vector3.ProjectOnPlane(Joint.connectedBody.transform.up.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude >
					Vector3.ProjectOnPlane(Joint.connectedBody.transform.right.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude;

				jointconnectedzero = -Vector3.SignedAngle(
					rot_jointup ? Joint.transform.up : Joint.transform.right,
					rot_connectedup ? Joint.connectedBody.transform.up : Joint.connectedBody.transform.right,
					Joint.transform.TransformDirection(Joint.axis));
			}
			else
			{
				jointconnectedzero = (!swap ? correction_1 : -correction_0) - minPosition;
				
				trans_connectedzero = Joint.anchor - Joint.axis.normalized * (position + jointconnectedzero);
				trans_connectedzero = Joint.transform.TransformPoint(trans_connectedzero);
				trans_connectedzero = Joint.connectedBody.transform.InverseTransformPoint(trans_connectedzero);
			}

			Initialize2();
		}

		public void Initialize2()
		{
			Joint.rotationDriveMode = RotationDriveMode.XYAndZ;

			// we don't modify *Motion, angular*Motion and the drives we don't need
				// -> KSP defaults are ok for us

			if(isRotational)
				Joint.angularXMotion = (isFreeMoving && !bUseDynamicLimitJoint) ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;
			else
				Joint.xMotion = ConfigurableJointMotion.Limited;

			InitializeDrive();

			InitializeLimits();
			
			Joint.enableCollision = false;
			Joint.enablePreprocessing = false;

			Joint.projectionMode = JointProjectionMode.None;
		}

		public static float to180(float v)
		{
			while(v > 180f) v -= 360f;
			while(v < -180f) v += 360f;
			return v;
		}

		public static float to360(float v)
		{
			while(v > 360f) v -= 360f;
			while(v < -360f) v += 360f;
			return v;
		}

		private bool UpdateAndConsumeElectricCharge()
		{
			if((electricChargeRequired == 0f) || isFreeMoving)
				return true;

			ip.PrepareUpdate(TimeWarp.fixedDeltaTime);

			float amountToConsume = electricChargeRequired * TimeWarp.fixedDeltaTime;

			amountToConsume *= TorqueLimit / MaxTorque;
			amountToConsume *= (ip.NewSpeed + ip.Speed) / (2 * maxSpeed * factorSpeed);

			float amountConsumed = part.RequestResource(electricResource.id, amountToConsume);

			LastPowerDrawRate = amountConsumed / TimeWarp.fixedDeltaTime;

			return amountConsumed == amountToConsume;
		}

		private bool IsStopping()
		{
			if(ip.IsStopping)
				return true;

			bool bRes = ip.Stop();

			// Stop changed the direction -> we need to recalculate this now
			ip.PrepareUpdate(TimeWarp.fixedDeltaTime);

			return bRes;
		}

		// set original rotation to new rotation
		public void UpdatePosition()
		{
			if(Mathf.Abs(commandedPosition - lastUpdatePosition) < 0.005f)
				return;

			if(isRotational)
				MovedPart.lastRot = Quaternion.AngleAxis(commandedPosition, MovedPart.relAxis);
			else
				MovedPart.lastTrans = MovedPart.relAxis * commandedPosition;

			MovedPart.UpdatePosition();

			lastUpdatePosition = commandedPosition;
		}

		////////////////////////////////////////
		// Update-Functions

		public void FixedUpdate()
		{
			if(!HighLogic.LoadedSceneIsFlight)
			{
				if(HighLogic.LoadedSceneIsEditor)
					ProcessShapeUpdates();

				return;
			}

			// FEHLER, wir fangen hier einiges ab -> aber wieso kann das passieren?? -> klären, ob das ein Bug von mir ist
			if(!part || !part.vessel || !part.vessel.rootPart || !Joint)
				return;

			if(isOnRails)
				return;

			if(part.State == PartStates.DEAD) 
				return;

			if(ip.IsMoving)
			{
				// verify if enough electric charge is available and consume it
				// or if that's not possible, command a stop and ask, if we still have a movement
				// in case there is a movement, do all the updating of the positions and play the sound

				if(UpdateAndConsumeElectricCharge() || IsStopping())
				{
					soundSound.Play();

					ip.Update();

					commandedPosition = ip.GetPosition();

					if(isRotational)
						Joint.targetRotation = Quaternion.AngleAxis(-commandedPosition, Vector3.right); // rotate always around x axis!!
					else
						Joint.targetPosition = Vector3.right * (trans_zero - commandedPosition); // move always along x axis!!
				}

				if(lightStatus != -1)
				{
					if(lightStatus != 2)
					{ lightStatus = 2; lightRenderer.material.SetColor(lightColorId, lightColorMoving); }
				}
			}
			else
			{
				soundSound.Stop();
				LastPowerDrawRate = 0f;

				if(lightStatus != -1)
				{
					if(isLocked)
					{ if(lightStatus != 0) { lightStatus = 0; lightRenderer.material.SetColor(lightColorId, lightColorLocked); } }
					else
					{ if(lightStatus != 1) { lightStatus = 1; lightRenderer.material.SetColor(lightColorId, lightColorIdle); } }
				}
			}

			if(isRotational)
			{
				// read new position
				float newPosition =
					-Vector3.SignedAngle(
						rot_jointup ? Joint.transform.up : Joint.transform.right,
						rot_connectedup ? Joint.connectedBody.transform.up : Joint.connectedBody.transform.right,
						Joint.transform.TransformDirection(Joint.axis))
					- jointconnectedzero;

				if(!float.IsNaN(newPosition))
				{
					// correct value into a plausible range -> FEHLER, unschön, dass es zwei Schritte braucht -> nochmal prüfen auch wird -90 als 270 angezeigt nach dem Laden?
					float newPositionCorrected = newPosition - zeroNormal - correction_1 + correction_0;
					float positionCorrected = position - zeroNormal - correction_1 + correction_0;

				//	DebugString("nP: " + newPosition.ToString() + ", nPC: " + newPositionCorrected.ToString() + ", pC: " + positionCorrected.ToString());

					if(newPositionCorrected < positionCorrected)
					{
						if((positionCorrected - newPositionCorrected) > (newPositionCorrected + 360f - positionCorrected))
						{
							newPosition += 360f;
							newPositionCorrected = newPosition - zeroNormal - correction_1 + correction_0;
						}
					}
					else
					{
						if((newPositionCorrected - positionCorrected) > (positionCorrected - newPositionCorrected + 360f))
						{
							newPosition -= 360f;
							newPositionCorrected = newPosition - zeroNormal - correction_1 + correction_0;
						}
					}

					while(newPositionCorrected < -360f)
					{
						newPosition += 360f;
						newPositionCorrected += 360f;
					}

					while(newPositionCorrected > 360f)
					{
						newPosition -= 360f;
						newPositionCorrected -= 360f;
					}

					// manuell dämpfen der Bewegung
					//if(jointDamping != 0)
					//	part.AddTorque(-(newPosition - position) * jointDamping * 0.001 * (Vector3d)GetAxis());
						// -> das funktioniert super aber ich probier noch was anderes

					// set new position
					position = newPosition;

					// Feder bei uncontrolled hat keinen Sinn... das wär nur bei Motoren sinnvoll... und dafür ist das Dämpfen bei Motoren wiederum nicht sehr sinnvoll...
					// ausser ... man macht's wie das alte IR... setzt die Spring auf fast nix und wendet dann eine Kraft an und eine Dämpfung...
					// -> genau das machen wir jetzt mal hier ...

					if(isFreeMoving)
						Joint.targetRotation = Quaternion.AngleAxis(-position, Vector3.right); // rotate always around x axis!!

					if(bUseDynamicLimitJoint)
					{
						float min =
							swap ? (hasPositionLimit ? -maxPositionLimit : -maxPosition) : (hasPositionLimit ? minPositionLimit : minPosition);
						float max =
							swap ? (hasPositionLimit ? -minPositionLimit : -minPosition) : (hasPositionLimit ? maxPositionLimit : maxPosition);

						if(min + 30 > position)
						{
							if(!bLowerLimitJoint || !LimitJoint)
							{
								if(LimitJoint)
									Destroy(LimitJoint);

								LimitJoint = gameObject.AddComponent<ConfigurableJoint>();

								LimitJoint.breakForce = Joint.breakForce;
								LimitJoint.breakTorque = Joint.breakTorque;
								LimitJoint.connectedBody = Joint.connectedBody;

								LimitJoint.axis = axis;
								LimitJoint.secondaryAxis = pointer;

								LimitJoint.rotationDriveMode = RotationDriveMode.XYAndZ;
								LimitJoint.angularXDrive = new JointDrive
								{ maximumForce = 0, positionSpring = 0, positionDamper = 0 };

								SoftJointLimit lowAngularXLimit = new SoftJointLimit() { limit = -170 };
								SoftJointLimit highAngularXLimit = new SoftJointLimit() { limit = -(min - position + (!swap? correction_0-correction_1 : correction_1-correction_0)) };

								LimitJoint.lowAngularXLimit = lowAngularXLimit;
								LimitJoint.highAngularXLimit = highAngularXLimit;
								LimitJoint.lowAngularXLimit = lowAngularXLimit;

								LimitJoint.angularXMotion = ConfigurableJointMotion.Limited;
								LimitJoint.angularYMotion = ConfigurableJointMotion.Locked;
								LimitJoint.angularZMotion = ConfigurableJointMotion.Locked;
								LimitJoint.xMotion = ConfigurableJointMotion.Locked;
								LimitJoint.yMotion = ConfigurableJointMotion.Locked;
								LimitJoint.zMotion = ConfigurableJointMotion.Locked;

								LimitJoint.autoConfigureConnectedAnchor = false;
								LimitJoint.anchor = Joint.anchor;
								LimitJoint.connectedAnchor = Joint.connectedAnchor;

								LimitJoint.configuredInWorldSpace = false;

								bLowerLimitJoint = true;
							}
						}
						else if(max - 30 < position)
						{
							if(bLowerLimitJoint || !LimitJoint)
							{
								if(LimitJoint)
									Destroy(LimitJoint);

								LimitJoint = gameObject.AddComponent<ConfigurableJoint>();

								LimitJoint.breakForce = Joint.breakForce;
								LimitJoint.breakTorque = Joint.breakTorque;
								LimitJoint.connectedBody = Joint.connectedBody;

								LimitJoint.axis = axis;
								LimitJoint.secondaryAxis = pointer;

								LimitJoint.rotationDriveMode = RotationDriveMode.XYAndZ;
								LimitJoint.angularXDrive = new JointDrive
								{ maximumForce = 0, positionSpring = 0, positionDamper = 0 };

								SoftJointLimit lowAngularXLimit = new SoftJointLimit() { limit = -(max - position - (!swap ? correction_1-correction_0 : correction_0-correction_1))};
								SoftJointLimit highAngularXLimit = new SoftJointLimit() { limit = 170 };

								LimitJoint.lowAngularXLimit = lowAngularXLimit;
								LimitJoint.highAngularXLimit = highAngularXLimit;
								LimitJoint.lowAngularXLimit = lowAngularXLimit;

								LimitJoint.angularXMotion = ConfigurableJointMotion.Limited;
								LimitJoint.angularYMotion = ConfigurableJointMotion.Locked;
								LimitJoint.angularZMotion = ConfigurableJointMotion.Locked;
								LimitJoint.xMotion = ConfigurableJointMotion.Locked;
								LimitJoint.yMotion = ConfigurableJointMotion.Locked;
								LimitJoint.zMotion = ConfigurableJointMotion.Locked;

								LimitJoint.autoConfigureConnectedAnchor = false;
								LimitJoint.anchor = Joint.anchor;
								LimitJoint.connectedAnchor = Joint.connectedAnchor;

								LimitJoint.configuredInWorldSpace = false;

								bLowerLimitJoint = false;
							}
						}
						else if(LimitJoint)
						{
							Destroy(LimitJoint);
							LimitJoint = null;
						}
					}
				}
			}
			else
			{
				Vector3 v =
					Joint.transform.TransformPoint(Joint.anchor) -
					Joint.connectedBody.transform.TransformPoint(trans_connectedzero);

				Vector3 v2 = Vector3.Project(v, Joint.transform.TransformDirection(Joint.axis));

if(float.IsNaN(v2.magnitude) || float.IsInfinity(v2.magnitude))
	position = position + jointconnectedzero;
else
				position = v2.magnitude;
				position -= jointconnectedzero; // FEHLER, wieso geht das nicht direkt in oberer Zeile??

				if(swap)
					position = -position;

				// manuell dämpfen der Bewegung
					// siehe Rotation, für das hier hab ich das nie ausprobiert

				// Feder bei uncontrolled hat keinen Sinn... das wär nur bei Motoren sinnvoll... und dafür ist das Dämpfen bei Motoren wiederum nicht sehr sinnvoll...
				// ausser ... man macht's wie das alte IR... setzt die Spring auf fast nix und wendet dann eine Kraft an und eine Dämpfung...
				// -> genau das machen wir jetzt mal hier ...

				if(isFreeMoving)
					Joint.targetPosition = Vector3.right * (trans_zero - position); // move always along x axis!!
			}

			UpdatePosition();

			ProcessShapeUpdates();
		}

		public void Update()
		{
			if(soundSound != null)
			{
				float pitchMultiplier = Math.Max(Math.Abs(CommandedSpeed / factorSpeed), 0.05f);

				if(pitchMultiplier > 1)
					pitchMultiplier = (float)Math.Sqrt(pitchMultiplier);

				soundSound.Update(soundVolume, soundPitch * pitchMultiplier);
			}

			if(HighLogic.LoadedSceneIsFlight)
			{
				CheckInputs();


				double amount, maxAmount;
				part.GetConnectedResourceTotals(electricResource.id, electricResource.resourceFlowMode, out amount, out maxAmount);

				if(amount == 0)
				{
					if(lightStatus != -1)
					{
						lightStatus = -1;
						lightRenderer.material.SetColor(lightColorId, lightColorOff);
					}
				}
				else if(lightStatus == -1) lightStatus = -2;
			}
		}

		public Vector3 GetAxis()
		{ return Joint.transform.TransformDirection(Joint.axis).normalized; }

		public Vector3 GetSecAxis()
		{ return Joint.transform.TransformDirection(Joint.secondaryAxis).normalized; }

		////////////////////////////////////////
		// IRescalable

		public void OnRescale(ScalingFactor factor)
		{
			ModuleIRServo_v3 prefab = part.partInfo.partPrefab.GetComponent<ModuleIRServo_v3>();

			part.mass = prefab.part.mass * Mathf.Pow(factor.absolute.linear, scaleMass);

			maxTorque = prefab.maxTorque * factor.absolute.linear;
 			TorqueLimit = TorqueLimit * factor.relative.linear;

			electricChargeRequired = prefab.electricChargeRequired * Mathf.Pow(factor.absolute.linear, scaleElectricChargeRequired);

 			if(!isRotational)
			{
				minPosition = prefab.minPosition * factor.absolute.linear;
				maxPosition = prefab.maxPosition * factor.absolute.linear;

				float _minPositionLimit = MinPositionLimit;
				float _maxPositionLimit = MaxPositionLimit;

				zeroNormal = prefab.zeroNormal * factor.absolute.linear;
				zeroInvert = prefab.zeroInvert * factor.absolute.linear;

				MinPositionLimit = _minPositionLimit * factor.relative.linear;
				MaxPositionLimit = _maxPositionLimit * factor.relative.linear;

				factorAcceleration = prefab.factorAcceleration * factor.absolute.linear;
				factorSpeed = prefab.factorSpeed * factor.absolute.linear;

				float deltaPosition = commandedPosition;

				commandedPosition *= factor.relative.linear;
				deltaPosition = commandedPosition - deltaPosition;
				transform.Translate(axis.normalized * deltaPosition);
			}

			UpdateUI();
		}

		////////////////////////////////////////
		// IJointLockState (auto strut support)

		bool IJointLockState.IsJointUnlocked()
		{
			return !isLocked;
		}

		////////////////////////////////////////
		// Ferram Aerospace Research

		protected int _far_counter = 60;
		protected float _far_lastPosition = 0f;

		protected void ProcessShapeUpdates()
		{
			if(--_far_counter > 0)
				return;

			if(Math.Abs(_far_lastPosition - position) >= 0.005)
			{
				part.SendMessage("UpdateShapeWithAnims");
				foreach(var p in part.children)
					p.SendMessage("UpdateShapeWithAnims");

				_far_lastPosition = position;
			}

			_far_counter = 60;
		}

		////////////////////////////////////////
		// Properties

		[KSPField(isPersistant = true)] public string servoName = "";

		public string Name
		{
			get { return servoName; }
			set { servoName = value; }
		}

		public uint UID
		{
			get { return part.craftID; }
		}

		public Part HostPart
		{
			get { return part; }
		}

		public bool Highlight
		{
			set { part.SetHighlight(value, false); }
		}

		private readonly IPresetable presets;

		public IPresetable Presets
		{
			get { return presets; }
		}

		[KSPField(isPersistant = true)] public string groupName = "New Group";

		public string GroupName
		{
			get { return groupName; }
			set { groupName = value; }
		}

		////////////////////////////////////////
		// Status

		public float TargetPosition
		{
			get { return ip.TargetPosition; }
		}

		public float TargetSpeed
		{
			get { return ip.TargetSpeed; }
		}

		public float CommandedPosition
		{
			get
			{
				if(!isInverted)
					return (swap ? -commandedPosition : commandedPosition) + zeroNormal + correction_1 - correction_0;
				else
					return (swap ? commandedPosition : -commandedPosition) + zeroInvert - correction_1 + correction_0;
			}
		}

		public float CommandedSpeed
		{
			get { return ip.Speed; }
		}

		// real position (corrected, when swapped or inverted)
		public float Position
		{
			get
			{
				if(!isInverted)
					return (swap ? -position : position) + zeroNormal + correction_1 - correction_0;
				else
					return (swap ? position : -position) + zeroInvert - correction_1 + correction_0;
			}
		}

		// Returns true if servo is currently moving
		public bool IsMoving
		{
			get { return ip.IsMoving; }
		}

		[KSPField(isPersistant = true)] public bool isLocked = false;

		public bool IsLocked
		{
			get { return isLocked; }
			set
			{
				isLocked = value;

				if(isLocked)
					Stop();

				if(vessel) // not set in editor
				{
					// AutoStrut
					vessel.CycleAllAutoStrut();

					// KJR
					Type KJRManagerType = null;

					AssemblyLoader.loadedAssemblies.TypeOperation (t => {
						if(t.FullName == "KerbalJointReinforcement.KJRManager") { KJRManagerType = t; } });

					if(KJRManagerType != null)
					{
						object o = FlightGlobals.FindObjectOfType(KJRManagerType);
			
						if(o != null)
							KJRManagerType.GetMethod("CycleAllAutoStrut").Invoke(o, new object[] { vessel });
					}
				}

				UpdateUI();
			}
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Engage Lock", active = true)]
		public void LockToggle()
		{
			IsLocked = !IsLocked;
		}

		////////////////////////////////////////
		// Settings

		[KSPField(isPersistant = true)] public bool isInverted = false;

		public bool IsInverted
		{
			get { return isInverted; }
			set
			{
				isInverted = value;

				UpdateUI();
			}
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Invert Axis")]
		public void InvertAxisToggle()
		{
			IsInverted = !IsInverted;
		}

		public List<float> PresetPositions
		{
			get;
			set;
		}

		[KSPField(isPersistant = true)] public float zeroNormal = 0;
		[KSPField(isPersistant = true)] public float zeroInvert = 0;

		[KSPField(isPersistant = true)] public float defaultPosition = 0;

		// default position, to be used for Revert/MoveCenter (can be outside minLimit<->maxLimit)
		public float DefaultPosition
		{
			get
			{
				if(!isInverted)
					return defaultPosition;
				else
					return zeroInvert - defaultPosition;
			}
			set
			{
				if(!isInverted)
					defaultPosition = Mathf.Clamp(value, minPositionLimit, maxPositionLimit);
				else
					defaultPosition = Mathf.Clamp(zeroInvert - value, minPositionLimit, maxPositionLimit);
			}
		}

			// limits set by the user
		[KSPField(isPersistant = true)] public bool hasPositionLimit = false;

		[KSPField(isPersistant = true)]
		public float minPositionLimit = 0;

		[KSPField(isPersistant = true)]
		public float maxPositionLimit = 360;

		public bool IsLimitted
		{
			get { return hasPositionLimit; }
			set
			{
				if(!canHaveLimits)
					return;

				// we can only activate the limits when we are inside the limits and when the motor is stopped
				if(value && !hasPositionLimit)
				{
					if(((position - correction_0 + correction_1) < (!swap ? minPositionLimit : -maxPositionLimit))
					|| ((position - correction_0 + correction_1) > (!swap ? maxPositionLimit : -minPositionLimit)))
						return;

					if(!isFreeMoving && IsMoving)
						return;
				}

				hasPositionLimit = value;

				if(Joint)
					InitializeLimits();

				UpdateUI();
			}
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Engage Limits", active = true)]
		public void ToggleLimits()
		{
			IsLimitted = !IsLimitted;
		}

		public float MinPositionLimit
		{
			get
			{
				if(!isInverted)
					return minPositionLimit;
				else
					return zeroInvert - maxPositionLimit;
			}
			set
			{
				if(!isInverted)
					minPositionLimit = Mathf.Clamp(value, minPosition, maxPositionLimit);
				else
					maxPositionLimit = Mathf.Clamp(zeroInvert - value, minPositionLimit, maxPosition);

				if(Joint)
					InitializeLimits();

				_gui_minPositionLimit = MinPositionLimit;
				UpdateUI();
			}
		}

		public float MaxPositionLimit
		{
			get
			{
				if(!isInverted)
					return maxPositionLimit;
				else
					return zeroInvert - minPositionLimit;
			}
			set
			{
				if(!isInverted)
					maxPositionLimit = Mathf.Clamp(value, minPositionLimit, maxPosition);
				else
					minPositionLimit = Mathf.Clamp(zeroInvert - value, minPosition, maxPositionLimit);

				if(Joint)
					InitializeLimits();

				_gui_maxPositionLimit = MaxPositionLimit;
				UpdateUI();
			}
		}

		[KSPField(isPersistant = true)]
		public float torqueLimit = 1f;

		public float TorqueLimit
		{
			get { return torqueLimit; }
			set
			{
				torqueLimit = Mathf.Clamp(value, 0.1f, maxTorque);

				if(Joint)
					InitializeDrive();

				_gui_torqueLimit = TorqueLimit;
				UpdateUI();
			}
		}

		[KSPField(isPersistant = true)]
		public float accelerationLimit = 4f;

		public float AccelerationLimit
		{
			get { return accelerationLimit; }
			set
			{
				accelerationLimit = Mathf.Clamp(value, 0.05f, maxAcceleration);
				
				ip.maxAcceleration = accelerationLimit * factorAcceleration;

				_gui_accelerationLimit = AccelerationLimit;
				UpdateUI();
			}
		}

// FEHLER, wozu? der geht sowieso immer auf Voll-Speed? -> das mal noch klären hier...
		public float DefaultSpeed
		{
			get { return SpeedLimit; }
			set {}
		}

		[KSPField(isPersistant = true)]
		public float speedLimit = 1f;

		public float SpeedLimit
		{
			get { return speedLimit; }
			set
			{
				speedLimit = Mathf.Clamp(value, 0.05f, maxSpeed);

				ip.maxSpeed = speedLimit * factorSpeed * groupSpeedFactor;

				_gui_speedLimit = SpeedLimit;
				UpdateUI();
			}
		}

		public float groupSpeedFactor = 1f;

		public float GroupSpeedFactor
		{
			get { return groupSpeedFactor; }
			set
			{
				groupSpeedFactor = Mathf.Clamp(value, 0.1f, maxSpeed / speedLimit);

				ip.maxSpeed = speedLimit * factorSpeed * groupSpeedFactor;
			}
		}

		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Force", guiFormat = "0.00"), 
			UI_FloatEdit(minValue = 0.0f, incrementSlide = 0.05f, incrementSmall=0.5f, incrementLarge=1f, sigFigs = 2)]
		public float jointSpring = PhysicsGlobals.JointForce;

		public float SpringPower 
		{
			get { return jointSpring; }
			set { jointSpring = value; UpdateUI(); }
		}

		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Damping Force", guiFormat = "0.00"), 
			UI_FloatEdit(minValue = 0.0f, incrementSlide = 0.05f, incrementSmall=0.5f, incrementLarge=1f, sigFigs = 2)]
		public float jointDamping = 0;

		public float DampingPower 
		{
			get { return jointDamping; }
			set { jointDamping = value; UpdateUI(); }
		}

		[KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Activate Collisions", active = true)]
		public void ActivateCollisions()
		{
			activateCollisions = !activateCollisions;

			Events["ActivateCollisions"].guiName = activateCollisions ? "Deactivate Collisions" : "Activate Collisions";
		}

		////////////////////////////////////////
		// Characteristics - values 'by design' of the joint

		[KSPField(isPersistant = false)] public bool isRotational = false;

		public bool IsRotational
		{
			get { return isRotational; }
		}

		[KSPField(isPersistant = false)] public bool hasMinMaxPosition = false;
		[KSPField(isPersistant = false)] public float minPosition = 0;
		[KSPField(isPersistant = false)] public float maxPosition = 360;

		public float MinPosition
		{
			get
			{
				if(!isInverted)
					return minPosition;
				else
					return zeroInvert - maxPosition;
			}
		}

		public float MaxPosition
		{
			get
			{
				if(!isInverted)
					return maxPosition;
				else
					return zeroInvert - minPosition;
			}
		}

		[KSPField(isPersistant = false)] public bool isFreeMoving = false;

		public bool IsFreeMoving
		{
			get { return isFreeMoving; }
		}

		[KSPField(isPersistant = false)] public bool canHaveLimits = true;

		public bool CanHaveLimits
		{
			get { return canHaveLimits; }
		}

		[KSPField(isPersistant = false)] public float maxTorque = 30f;

		public float MaxTorque
		{
			get { return maxTorque; }
		}

		[KSPField(isPersistant = false)] public float maxAcceleration = 10;

		public float MaxAcceleration
		{
			get { return maxAcceleration; }
		}

		[KSPField(isPersistant = false)] public float maxSpeed = 100;

		public float MaxSpeed
		{
			get { return maxSpeed; }
		}

		[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Electric Charge required", guiUnits = "EC/s")]
		public float electricChargeRequired = 2.5f;

		public float ElectricChargeRequired
		{
			get { return electricChargeRequired; }
		}

		[KSPField(isPersistant = true)] public bool hasSpring = false;

		public bool HasSpring
		{
			get { return hasSpring; }
		}

		////////////////////////////////////////
		// Factors (mainly for UI)

		[KSPField(isPersistant = true)] public float factorTorque = 1.0f;
		[KSPField(isPersistant = true)] public float factorSpeed = 1.0f;
		[KSPField(isPersistant = true)] public float factorAcceleration = 1.0f;

		////////////////////////////////////////
		// Scaling

		[KSPField(isPersistant = true)] public float scaleMass = 1.0f;
		[KSPField(isPersistant = true)] public float scaleElectricChargeRequired = 2.0f;

		////////////////////////////////////////
		// Input

		[KSPField(isPersistant = true)] public string forwardKey;
		[KSPField(isPersistant = true)] public string reverseKey;

		public string ForwardKey
		{
			get { return forwardKey; }
			set	{ forwardKey = value.ToLower(); }
		}

		public string ReverseKey
		{
			get { return reverseKey; }
			set { reverseKey = value.ToLower(); }
		}

		public void MoveLeft()
		{
			MoveTo(float.NegativeInfinity, DefaultSpeed);
		}

		public void MoveCenter()
		{
			MoveTo(DefaultPosition, DefaultSpeed);
		}

		public void MoveRight()
		{
			MoveTo(float.PositiveInfinity, DefaultSpeed);
		}

		public void Move(float deltaPosition, float targetSpeed)
		{
			if(isOnRails || isLocked || isFreeMoving)
				return;

			if(swap)
				deltaPosition = -deltaPosition;

			if(isInverted)
				deltaPosition = -deltaPosition;

			ip.SetCommand(to360(position + deltaPosition), targetSpeed * factorSpeed * groupSpeedFactor, false);
		}

		public void MoveTo(float targetPosition)
		{
			MoveTo(targetPosition, DefaultSpeed);
		}

		public void MoveTo(float targetPosition, float targetSpeed)
		{
			if(isOnRails || isLocked || isFreeMoving)
				return;

			if(!isInverted)
				targetPosition = (swap ? -1.0f : 1.0f) * (targetPosition - zeroNormal - correction_1 + correction_0);
			else
				targetPosition = (swap ? 1.0f : -1.0f) * (targetPosition - zeroInvert + correction_1 - correction_0);

			ip.SetCommand(targetPosition, targetSpeed * factorSpeed * groupSpeedFactor, false);
		}

		public void Stop()
		{
			if(isFreeMoving)
				return;

			ip.Stop();
		}

		protected bool KeyPressed(string key)
		{
			return (key != "" && vessel == FlightGlobals.ActiveVessel
					&& InputLockManager.IsUnlocked(ControlTypes.LINEAR)
					&& Input.GetKey(key));
		}

		protected bool KeyUnPressed(string key)
		{
			return (key != "" && vessel == FlightGlobals.ActiveVessel
					&& InputLockManager.IsUnlocked(ControlTypes.LINEAR)
					&& Input.GetKeyUp(key));
		}

		protected void CheckInputs()
		{
			if(KeyPressed(forwardKey))
				MoveRight();
			else if(KeyPressed(reverseKey))
				MoveLeft();
			else if(KeyUnPressed(forwardKey) || KeyUnPressed(reverseKey))
				Stop();
		}

		////////////////////////////////////////
		// Editor

		public void EditorReset()
		{
			if(!HighLogic.LoadedSceneIsEditor)
				return;

			IsInverted = false;

			EditorSetPosition(0f);
		}

		public void EditorMoveLeft()
		{
			EditorMove(float.NegativeInfinity);
		}

		public void EditorMoveCenter()
		{
			EditorMove(DefaultPosition);
		}

		public void EditorMoveRight()
		{
			EditorMove(float.PositiveInfinity);
		}

		public void EditorMove(float targetPosition)
		{
			float movement = speedLimit * factorSpeed * groupSpeedFactor * Time.deltaTime;

			if(Math.Abs(targetPosition - Position) > movement)
			{
				if(targetPosition < Position)
					movement = -movement;

				if(!isInverted)
					targetPosition = Position + movement;
				else
					targetPosition = Position + movement;
			}

			EditorSetPosition(targetPosition);
		}

		public void EditorSetTo(float targetPosition)
		{
			EditorSetPosition(targetPosition);
		}

		// sets the position and rotates the joint and its meshes
		private void EditorSetPosition(float targetPosition)
		{
			if(!HighLogic.LoadedSceneIsEditor)
				return;

			if(!isInverted)
				targetPosition = (swap ? -1.0f : 1.0f) * (targetPosition - zeroNormal);
			else
				targetPosition = (swap ? 1.0f : -1.0f) * (targetPosition - zeroInvert);

			if(hasPositionLimit)
			{
				targetPosition =
					!swap
					? Mathf.Clamp(targetPosition, minPositionLimit, maxPositionLimit)
					: Mathf.Clamp(targetPosition, -maxPositionLimit, -minPositionLimit);
			}
			else if(hasMinMaxPosition)
			{
				targetPosition =
					!swap
					? Mathf.Clamp(targetPosition, minPosition, maxPosition)
					: Mathf.Clamp(targetPosition, -maxPosition, -minPosition);
			}

			if(isRotational)
			{
				if(!hasMinMaxPosition && !hasPositionLimit) // dann ist es "Modulo" -> von -360 bis +360
				{
					while(targetPosition < -360f)
						targetPosition += 360f;
					while(targetPosition > 360f)
						targetPosition -= 360f;
				}
			}

			if(!isInverted)
				targetPosition += (swap ? -1.0f : 1.0f) * (correction_0 - correction_1);
			else
				targetPosition += (swap ? 1.0f : -1.0f) * (correction_1 - correction_0);

			float deltaPosition = targetPosition - commandedPosition;

			if(isRotational)
			{
				fixedMeshTransform.Rotate(axis, -deltaPosition, Space.Self);
				transform.Rotate(axis, deltaPosition, Space.Self);
			}
			else
			{
				fixedMeshTransform.Translate(axis.normalized * (-deltaPosition));
				transform.Translate(axis.normalized * deltaPosition);
			}

			position = commandedPosition = targetPosition;
		}

		public void CopyPresetsToSymmetry()
		{
			if(!HighLogic.LoadedSceneIsEditor)
				return;

			Logger.Log("CopyPresetsToSymmetry", Logger.Level.Debug);

			foreach(Part p in part.symmetryCounterparts)
			{
				ModuleIRServo_v3 servo = p.GetComponent<ModuleIRServo_v3>();

				servo.PresetPositions = new List<float>();
				servo.PresetPositions.AddRange(PresetPositions);

				servo.defaultPosition = defaultPosition;
			}
		}

		public void CopyLimitsToSymmetry()
		{
			if(!HighLogic.LoadedSceneIsEditor)
				return;

			Logger.Log("CopyLimitsToSymmetry", Logger.Level.Debug);

			foreach(Part p in part.symmetryCounterparts)
			{
				ModuleIRServo_v3 servo = p.GetComponent<ModuleIRServo_v3>();

				servo.hasPositionLimit = hasPositionLimit;
				servo.MinPositionLimit = MinPositionLimit;
				servo.MaxPositionLimit = MaxPositionLimit;
			}
		}

// FEHLER, was'n des???
		public void DoTransformStuff(Transform trf)
		{
			trf.position = fixedMeshTransform.position;
			trf.parent = fixedMeshTransform;

			// FEHLER, hier noch berücksichtigen, wenn die Teils mit Winkel geladen wurden, also nicht ganz gerade stehen

			Vector3 rAxis = fixedMeshTransform.TransformDirection(axis);
			Vector3 rPointer = fixedMeshTransform.TransformDirection(pointer);

			if(isInverted)
				rAxis = -rAxis;

			trf.rotation =
				Quaternion.AngleAxis(!isInverted ? zeroNormal : zeroInvert, !swap ? -rAxis : rAxis)		// inversion for inverted joints -> like this the Aid doesn't have to invert values itself
				* Quaternion.LookRotation(!swap ? -rAxis : rAxis, !swap ? rPointer : -rPointer);		// normal rotation
		}

		////////////////////////////////////////
		// Context Menu

		private void AttachContextMenu()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				Fields["_gui_minPositionLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_minPositionLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onMinPositionLimitChanged));

				Fields["_gui_maxPositionLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_maxPositionLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onMaxPositionLimitChanged));

				Fields["_gui_torqueLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_torqueLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onTorqueLimitChanged));

				Fields["_gui_accelerationLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_accelerationLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onAccelerationLimitChanged));

				Fields["_gui_speedLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_speedLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onSpeedLimitChanged));
			}
			else
			{
				Fields["_gui_minPositionLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_minPositionLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onMinPositionLimitChanged));

				Fields["_gui_maxPositionLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_maxPositionLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onMaxPositionLimitChanged));

				Fields["_gui_torqueLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_torqueLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onTorqueLimitChanged));

				Fields["_gui_accelerationLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_accelerationLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onAccelerationLimitChanged));

				Fields["_gui_speedLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Combine(
					Fields["_gui_speedLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onSpeedLimitChanged));
			}
		}

		private void DetachContextMenu()
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				Fields["_gui_minPositionLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_minPositionLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onMinPositionLimitChanged));

				Fields["_gui_maxPositionLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_maxPositionLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onMaxPositionLimitChanged));

				Fields["_gui_torqueLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_torqueLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onTorqueLimitChanged));

				Fields["_gui_accelerationLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_accelerationLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onAccelerationLimitChanged));

				Fields["_gui_speedLimit"].uiControlFlight.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_speedLimit"].uiControlFlight.onFieldChanged, new Callback<BaseField, object>(onSpeedLimitChanged));
			}
			else if(HighLogic.LoadedSceneIsEditor)
			{
				Fields["_gui_minPositionLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_minPositionLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onMinPositionLimitChanged));

				Fields["_gui_maxPositionLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_maxPositionLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onMaxPositionLimitChanged));

				Fields["_gui_torqueLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_torqueLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onTorqueLimitChanged));

				Fields["_gui_accelerationLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_accelerationLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onAccelerationLimitChanged));

				Fields["_gui_speedLimit"].uiControlEditor.onFieldChanged = (Callback<BaseField, object>)Delegate.Remove(
					Fields["_gui_speedLimit"].uiControlEditor.onFieldChanged, new Callback<BaseField, object>(onSpeedLimitChanged));
			}
		}

		private void UpdateUI()
		{
			Events["InvertAxisToggle"].guiName = isInverted ? "Un-invert Axis" : "Invert Axis";
			Events["LockToggle"].guiName = isLocked ? "Disengage Lock" : "Engage Lock";

			if(canHaveLimits)
				Events["ToggleLimits"].guiName = hasPositionLimit ? "Disengage Limits" : "Engage Limits";

			if(HighLogic.LoadedSceneIsFlight)
			{
				Events["ToggleLimits"].guiActive = canHaveLimits;

				Fields["_gui_minPositionLimit"].guiActive = hasPositionLimit;
				Fields["_gui_maxPositionLimit"].guiActive = hasPositionLimit;

				((UI_FloatEdit)Fields["_gui_minPositionLimit"].uiControlFlight).minValue = (!isInverted ? minPosition : minPositionLimit);
				((UI_FloatEdit)Fields["_gui_minPositionLimit"].uiControlFlight).maxValue = (!isInverted ? maxPositionLimit : maxPosition);

				((UI_FloatEdit)Fields["_gui_maxPositionLimit"].uiControlFlight).minValue = (!isInverted ? minPositionLimit : minPosition);
				((UI_FloatEdit)Fields["_gui_maxPositionLimit"].uiControlFlight).maxValue = (!isInverted ? maxPosition : maxPositionLimit);

				Fields["_gui_torqueLimit"].guiActive = !isFreeMoving;
				((UI_FloatEdit)Fields["_gui_torqueLimit"].uiControlFlight).maxValue = maxTorque;

				Fields["_gui_accelerationLimit"].guiActive = !isFreeMoving;
				((UI_FloatEdit)Fields["_gui_accelerationLimit"].uiControlFlight).maxValue = maxAcceleration;
 
				Fields["_gui_speedLimit"].guiActive = !isFreeMoving;
				((UI_FloatEdit)Fields["_gui_speedLimit"].uiControlFlight).maxValue = maxSpeed;
			}
			else if(HighLogic.LoadedSceneIsEditor)
			{
				Events["ToggleLimits"].guiActiveEditor = canHaveLimits;

				Fields["_gui_minPositionLimit"].guiActiveEditor = hasPositionLimit;
				Fields["_gui_maxPositionLimit"].guiActiveEditor = hasPositionLimit;

				((UI_FloatEdit)Fields["_gui_minPositionLimit"].uiControlEditor).minValue = (!isInverted ? minPosition : minPositionLimit);
				((UI_FloatEdit)Fields["_gui_minPositionLimit"].uiControlEditor).maxValue = (!isInverted ? maxPositionLimit : maxPosition);

				((UI_FloatEdit)Fields["_gui_maxPositionLimit"].uiControlEditor).minValue = (!isInverted ? minPositionLimit : minPosition);
				((UI_FloatEdit)Fields["_gui_maxPositionLimit"].uiControlEditor).maxValue = (!isInverted ? maxPosition : maxPositionLimit);

				Fields["_gui_torqueLimit"].guiActiveEditor = !isFreeMoving;
				((UI_FloatEdit)Fields["_gui_torqueLimit"].uiControlEditor).maxValue = maxTorque;

				Fields["_gui_accelerationLimit"].guiActiveEditor = !isFreeMoving;
				((UI_FloatEdit)Fields["_gui_accelerationLimit"].uiControlEditor).maxValue = maxAcceleration;
 
				Fields["_gui_speedLimit"].guiActiveEditor = !isFreeMoving;
				((UI_FloatEdit)Fields["_gui_speedLimit"].uiControlEditor).maxValue = maxSpeed;

				Fields["jointSpring"].guiActiveEditor = hasSpring && isFreeMoving;
				Fields["jointDamping"].guiActiveEditor = hasSpring && isFreeMoving;

				Events["ActivateCollisions"].guiName = activateCollisions ? "Deactivate Collisions" : "Activate Collisions";
			}

			UIPartActionWindow[] partWindows = FindObjectsOfType<UIPartActionWindow>();
			foreach(UIPartActionWindow partWindow in partWindows)
			{
				if(partWindow.part == part)
					partWindow.displayDirty = true;
			}
		}

		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Min", guiFormat = "F2", guiUnits = ""),
			UI_FloatEdit(minValue = -360f, maxValue = 360f, incrementSlide = 0.05f, incrementSmall=1f, incrementLarge=10f, scene = UI_Scene.All, sigFigs = 2)]
		private float _gui_minPositionLimit;

		private bool CompareValueAbsolute(float a, float b)
		{ return Mathf.Abs(Mathf.Abs(a) - Mathf.Abs(b)) >= 0.05; }

		public void onMinPositionLimitChanged(BaseField bf, object o)
		{
			if(CompareValueAbsolute(MinPositionLimit, _gui_minPositionLimit)) MinPositionLimit = _gui_minPositionLimit;
			foreach(Part p in part.symmetryCounterparts)
			{
				ModuleIRServo_v3 s = p.GetComponent<ModuleIRServo_v3>();
				if(CompareValueAbsolute(s.MinPositionLimit, s._gui_minPositionLimit)) s.MinPositionLimit = s._gui_minPositionLimit;
			}
		}

		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Max", guiFormat = "F2", guiUnits = ""),
			UI_FloatEdit(minValue = -360f, maxValue = 360f, incrementSlide = 0.05f, incrementSmall=1f, incrementLarge=10f, scene = UI_Scene.All, sigFigs = 2)]
		private float _gui_maxPositionLimit;

		public void onMaxPositionLimitChanged(BaseField bf, object o)
		{
			if(CompareValueAbsolute(MaxPositionLimit, _gui_maxPositionLimit)) MaxPositionLimit = _gui_maxPositionLimit;
			foreach(Part p in part.symmetryCounterparts)
			{
				ModuleIRServo_v3 s = p.GetComponent<ModuleIRServo_v3>();
				if(CompareValueAbsolute(s.MaxPositionLimit, s._gui_maxPositionLimit)) s.MaxPositionLimit = s._gui_maxPositionLimit;
			}
		}

		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Torque", guiFormat = "0.00"),
			UI_FloatEdit(minValue = 0.1f, maxValue=30f, incrementSlide = 0.05f, incrementSmall=1f, incrementLarge=5f, scene = UI_Scene.All, sigFigs = 2)]
		private float _gui_torqueLimit;

		public void onTorqueLimitChanged(BaseField bf, object o)
		{
			if(CompareValueAbsolute(TorqueLimit, _gui_torqueLimit)) TorqueLimit = _gui_torqueLimit;
			foreach(Part p in part.symmetryCounterparts)
			{
				ModuleIRServo_v3 s = p.GetComponent<ModuleIRServo_v3>();
				if(CompareValueAbsolute(s.TorqueLimit, s._gui_torqueLimit)) s.TorqueLimit = s._gui_torqueLimit;
			}
		}

		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Acceleration", guiFormat = "0.00"), 
			UI_FloatEdit(minValue = 0.05f, incrementSlide = 0.05f, incrementSmall=1f, incrementLarge=5f, sigFigs = 2)]
		private float _gui_accelerationLimit;

		public void onAccelerationLimitChanged(BaseField bf, object o)
		{
			if(CompareValueAbsolute(AccelerationLimit, _gui_accelerationLimit)) AccelerationLimit = _gui_accelerationLimit;
			foreach(Part p in part.symmetryCounterparts)
			{
				ModuleIRServo_v3 s = p.GetComponent<ModuleIRServo_v3>();
				if(CompareValueAbsolute(s.AccelerationLimit, s._gui_accelerationLimit)) s.AccelerationLimit = s._gui_accelerationLimit;
			}
		}

		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Speed", guiFormat = "0.00"), 
			UI_FloatEdit(minValue = 0.05f, incrementSlide = 0.05f, incrementSmall=1f, incrementLarge=5f, sigFigs = 2)]
		private float _gui_speedLimit;

		public void onSpeedLimitChanged(BaseField bf, object o)
		{
			if(CompareValueAbsolute(SpeedLimit, _gui_speedLimit)) SpeedLimit = _gui_speedLimit;
			foreach(Part p in part.symmetryCounterparts)
			{
				ModuleIRServo_v3 s = p.GetComponent<ModuleIRServo_v3>();
				if(CompareValueAbsolute(s.SpeedLimit, s._gui_speedLimit)) s.SpeedLimit = s._gui_speedLimit;
			}
		}

		////////////////////////////////////////
		// Actions

		[KSPAction("Toggle Lock")]
		public void MotionLockToggle(KSPActionParam param)
		{ LockToggle(); }

		[KSPAction("Move To Next Preset")]
		public void MoveNextPresetAction(KSPActionParam param)
		{
			if(Presets != null)
				Presets.MoveNext();
		}

		[KSPAction("Move To Previous Preset")]
		public void MovePrevPresetAction(KSPActionParam param)
		{
			if(Presets != null)
				Presets.MovePrev();
		}

		[KSPAction("Move +")]
		public void MovePlusAction(KSPActionParam param)
		{
			switch(param.type)
			{
			case KSPActionType.Activate:
				MoveRight();
				break;

			case KSPActionType.Deactivate:
				Stop();
				break;
			}
		}

		[KSPAction("Move -")]
		public void MoveMinusAction(KSPActionParam param)
		{
			switch (param.type)
			{
			case KSPActionType.Activate:
				MoveLeft();
				break;

			case KSPActionType.Deactivate:
				Stop();
				break;
			}
		}

		[KSPAction("Move Center")]
		public void MoveCenterAction(KSPActionParam param)
		{
			switch (param.type)
			{
			case KSPActionType.Activate:
				MoveCenter();
				break;

			case KSPActionType.Deactivate:
				Stop();
				break;
			}
		}

		////////////////////////////////////////
		// Debug

		private LineDrawer[] al = new LineDrawer[13];
		private Color[] alColor = new Color[13];

		private String[] astrDebug;
		private int istrDebugPos;

		private void DebugInit()
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

			astrDebug = new String[10240];
			istrDebugPos = 0;
		}

		private void DebugString(String s)
		{
			astrDebug[istrDebugPos] = s;
			istrDebugPos = (istrDebugPos + 1) % 10240;
		}

		private void DrawPointer(int idx, Vector3 p_vector)
		{
			al[idx].DrawLineInGameView(Vector3.zero, p_vector, alColor[idx]);
		}

// FEHLER, temp public, ich such was und brauch das als Anzeige
		public void DrawRelative(int idx, Vector3 p_from, Vector3 p_vector)
		{
			al[idx].DrawLineInGameView(p_from, p_from + p_vector, alColor[idx]);
		}

		private void DrawAxis(int idx, Transform p_transform, Vector3 p_vector, bool p_relative, Vector3 p_off)
		{
			al[idx].DrawLineInGameView(p_transform.position + p_off, p_transform.position + p_off
				+ (p_relative ? p_transform.TransformDirection(p_vector) : p_vector), alColor[idx]);
		}

		private void DrawAxis(int idx, Transform p_transform, Vector3 p_vector, bool p_relative)
		{ DrawAxis(idx, p_transform, p_vector, p_relative, Vector3.zero); }

		// FEHLER, spezielle und evtl. temporäre Hilfsfunktionen
		
			// zeichnet die Limits wie sie wären, wenn niemand was korrigieren würde
		private void DrawInitLimits(int idx)
		{
			if(!Joint)
				return;

			float low = Joint.lowAngularXLimit.limit;
			float high = Joint.highAngularXLimit.limit;

				// weil das ja "init" ist, gehen wir zurück auf die Werte, die es ohne Korrektur wäre
			low = (swap ? -maxPositionLimit : minPositionLimit);
			high = (swap ? -minPositionLimit : maxPositionLimit);

			DrawAxis(idx, Joint.transform,
				(swap ? -Joint.transform.up : Joint.transform.up), false);
			DrawAxis(idx + 1, Joint.transform,
				Quaternion.AngleAxis(-low, Joint.transform.TransformDirection(Joint.axis)) * (swap ? -Joint.transform.up : Joint.transform.up), false);
			DrawAxis(idx + 2, Joint.transform,
				Quaternion.AngleAxis(-high, Joint.transform.TransformDirection(Joint.axis)) * (swap ? -Joint.transform.up : Joint.transform.up), false);
		}

		private void DrawLimits(int idx, float pos)
		{
			if(!Joint)
				return;

		//	float low = Joint.lowAngularXLimit.limit;
		//	float high = Joint.highAngularXLimit.limit;

			float min = swap ? (hasPositionLimit ? -maxPositionLimit : -maxPosition) : (hasPositionLimit ? minPositionLimit : minPosition);
			float max = swap ? (hasPositionLimit ? -minPositionLimit : -minPosition) : (hasPositionLimit ? maxPositionLimit : maxPosition);

			float low = to360(min + (!swap ? correction_0-correction_1 : correction_1-correction_0));
			float high = to360(max + (!swap ? correction_0-correction_1 : correction_1-correction_0 ));

			Vector3 v;
			
			float cor = swap ? correction_0 : correction_1;

			Vector3 u = swap ? -Joint.transform.up : Joint.transform.up;

			v = Quaternion.AngleAxis(-cor - pos, Joint.transform.TransformDirection(Joint.axis)) * u;
			DrawAxis(idx, Joint.transform,
				v, false, Joint.transform.TransformDirection(Joint.axis) * 0.2f);

			v = Quaternion.AngleAxis(low - pos, Joint.transform.TransformDirection(Joint.axis)) * u;
			DrawAxis(idx + 1, Joint.transform,
				v, false, Joint.transform.TransformDirection(Joint.axis) * 0.2f);

			v = Quaternion.AngleAxis(high - pos, Joint.transform.TransformDirection(Joint.axis)) * u;
			DrawAxis(idx + 2, Joint.transform,
				v, false, Joint.transform.TransformDirection(Joint.axis) * 0.2f);
		}
	}
}

