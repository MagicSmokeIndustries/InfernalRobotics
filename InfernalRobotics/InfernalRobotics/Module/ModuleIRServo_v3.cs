using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;
using TweakScale;
using KerbalJointReinforcement;

using InfernalRobotics_v3.Effects;
using InfernalRobotics_v3.Control;
using InfernalRobotics_v3.Control.Servo;
using InfernalRobotics_v3.Command;

namespace InfernalRobotics_v3.Module
{
	public class ModuleIRServo_v3 : PartModule, IServo, IRescalable, IJointLockState, IKJRaware
	{
		private ConfigurableJoint Joint = null;

		[KSPField(isPersistant = false)] public Vector3 axis = Vector3.right;	// x-axis of the joint
		[KSPField(isPersistant = false)] public Vector3 pointer = Vector3.up;	// towards child (if possible), but always perpendicular to axis

		[KSPField(isPersistant = false)] public string fixedMesh = string.Empty;
		[KSPField(isPersistant = false)] public string movingMesh = string.Empty;

		private Transform fixedMeshTransform = null;

// IK >>
// FEHLER, neu -> pointer wird neu auch als secondaryAxis genutzt !!! -> wegen dem AID sinnvoll
		public Part pointerPart = null; // -> wird neu von aussen gesetzt...
	//	private bool ok = false;
	//	private bool pointAlongAxis = false;
// IK <<

		private bool isOnRails = false;

		// internal information on how to calculate/read-out the current rotation
		private bool jointup = true, connectedup = true;
		private float jointconnectedzero;

		// true, if servo is attached reversed
		[KSPField(isPersistant = true)] private bool swap = false;
public bool GetFuckingSwap() { return swap; } // FEHLER

		/*
		 * position is an internal value and always relative to the current orientation
		 * of the joint (swap or not swap)
		 * all interface functions returning and expecting values do the swap internally
		 * and return and expect external values
		*/

		// position relative to current zero-point of joint
		[KSPField(isPersistant = true)] private float position = 0.0f;

		// correction values for position
		[KSPField(isPersistant = true)] private float correction_0 = 0.0f;
		[KSPField(isPersistant = true)] private float correction_1 = 0.0f;

		// Limit-Joint (joint used for limits of uncontrolled parts)
		private ConfigurableJoint LimitJoint = null;
		private bool bLowerLimitJoint;
		private bool bUseLimitJoint = false;

		// Motor (works with position relative to current zero-point of joint, like position)
		Interpolator2 ip;

		[KSPField(isPersistant = false)] public float friction = 0.5f;

		// Electric Power
		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Current Draw", guiUnits = "EC/s")] 
		public float LastPowerDrawRate;

		PartResourceDefinition electricResource = null;

		// Sound
		[KSPField(isPersistant = false)] protected float soundPitch = 1.0f;
		[KSPField(isPersistant = false)] protected float soundVolume = 0.5f;
		[KSPField(isPersistant = false)] protected string soundFilePath = "MagicSmokeIndustries/Sounds/infernalRoboticMotor";
		protected SoundSource soundSound = null;

		// Lights
		static int lightColorId = 0;
		static Color lightColorOff, lightColorLocked, lightColorIdle, lightColorMoving;
		int lightStatus = -1;
		Renderer lightRenderer;

// >> FEHLER, neues Zeugs, was noch rein müsste...
/*
		[KSPField(isPersistant = false)] public string bottomNode = "bottom";		?
*/
/*		[KSPField(isPersistant = false)] public float keyRotateSpeed = 0;
		[KSPField(isPersistant = false)] public float keyTranslateSpeed = 0;

		//TODO: Move FAR related things to ServoController
		//these 3 are for sending messages to inform nuFAR of shape changes to the craft.
		protected const int shapeUpdateTimeout = 60; //it will send message every xx FixedUpdates
		protected int shapeUpdateCounter = 0;
		protected float lastPosition = 0f;

		protected const string ELECTRIC_CHARGE_RESOURCE_NAME = "ElectricCharge";

		public bool isStuck = false;
*/
// << FEHLER, neues Zeugs, was noch rein müsste...

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
				ip = new Interpolator2();

				motor = new ServoMotor(this);
				presets = new ServoPresets(this);
			}
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
				lightColorIdle = new Color(1, 1, 0, 1);
				lightColorMoving = new Color(0, 1, 0, 1);
			}

			GameEvents.onVesselCreate.Add(OnVesselCreate);
			GameEvents.onVesselWasModified.Add(OnVesselWasModified);

			GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
			GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);
		}

		public void OnDestroy()
		{
			GameEvents.onVesselCreate.Remove(OnVesselCreate);
			GameEvents.onVesselWasModified.Remove(OnVesselWasModified);

			GameEvents.onVesselGoOnRails.Remove(OnVesselGoOnRails);
			GameEvents.onVesselGoOffRails.Remove(OnVesselGoOffRails);

// FEHLER ??? entfernen aus liste... also echt jetzt

// FEHLER, supertemp
Group.Remove(this);
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			if(state == StartState.Editor)
			{
				part.OnEditorAttach = (Callback)Delegate.Combine(part.OnEditorAttach, new Callback(onEditorAttached));
				part.OnEditorDetach = (Callback)Delegate.Combine(part.OnEditorDetach, new Callback(onEditorDetached));

				ParsePresetPositions();

				try
				{
					InitializeMeshes(true);
				}
				catch(Exception)
				{}
			}
			else
			{
				if(soundSound == null)
					soundSound = new SoundSource(part, "motor");
				soundSound.Setup(soundFilePath, true);
	
				StartCoroutine(WaitAndInitialize()); // calling Initialize in OnStartFinished should work too, but KSP does it like this internally

				electricResource = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

// FEHLER ??? einfügen in liste
			}

			// Renderer for lights
			lightRenderer = part.gameObject.GetComponentInChildren<Renderer>();
		}

		public IEnumerator WaitAndInitialize()
		{
			while(!part.attachJoint || !part.attachJoint.Joint)
				yield return null;

			Initialize1();
		}

		public override void OnLoad(ConfigNode config)
		{
			base.OnLoad(config);

			InitializeValues(); // FEHLER, sind jetzt die Daten schon drin?

			if(HighLogic.LoadedSceneIsFlight)	// FEHLER, wirklich?
				Initialize1();
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			SerializePresets();
		}

		public void OnVesselGoOnRails(Vessel v)
		{
			isOnRails = true;
		}

		public void OnVesselGoOffRails(Vessel v)
		{
			isOnRails = false;

			if(part.vessel == v)
				Initialize2();
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
			}
		}

		public void onEditorAttached()
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

		// corrects all the values to valid values by setting them via their set-functions
		public void InitializeValues()
		{
			MinPositionLimit = minPositionLimit;
			MaxPositionLimit = maxPositionLimit;

			AccelerationLimit = accelerationLimit;

			Events["ToggleLimits"].guiActive = canHaveLimits;
			if(canHaveLimits)
				Events["ToggleLimits"].guiName = hasPositionLimit ? "Engage Limits" : "Disengage Limits";

			Fields["jointSpring"].guiActiveEditor = hasSpring && isFreeMoving;
			Fields["jointDamping"].guiActiveEditor = hasSpring && isFreeMoving;

			ParsePresetPositions();
		}

		public void InitializeMeshes(bool bCorrectMeshPositions)
		{
			// detect attachment mode and calculate correction angles
			if(swap != FindSwap())
			{
				swap = !swap;

				if(!swap)
					correction_0 += position;
				else
					correction_1 += position;
			}
			else
			{
				if(swap)
					correction_0 += position;
				else
					correction_1 += position;
			}
			position = 0.0f;

			// find non rotating mesh
			fixedMeshTransform = KSPUtil.FindInPartModel(transform, swap ? movingMesh : fixedMesh);

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
		}

		public void Initialize1()
		{
			if(!part.attachJoint)
				return; // FEHLER, schneller Bugfix -> kommt glaub ich aus dem Load raus der Mist Aufruf

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


			fixedMeshTransform.parent = Joint.connectedBody.transform;


			// set anchor
			Joint.anchor = Vector3.zero;

			// correct connectedAnchor
			Joint.connectedAnchor = Joint.connectedBody.transform.InverseTransformPoint(Joint.transform.TransformPoint(Joint.anchor));

			// set axis
			Joint.axis = axis;
			Joint.secondaryAxis = pointer;

			// determine best way to calculate real rotation
			if(isRotational)
			{
				jointup =
					Vector3.ProjectOnPlane(Joint.transform.up.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude >
					Vector3.ProjectOnPlane(Joint.transform.right.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude;

				connectedup =
					Vector3.ProjectOnPlane(Joint.connectedBody.transform.up.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude >
					Vector3.ProjectOnPlane(Joint.connectedBody.transform.right.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude;

				jointconnectedzero = to180(AngleSigned(
					jointup ? Joint.transform.up : Joint.transform.right,
					connectedup ? Joint.connectedBody.transform.up : Joint.connectedBody.transform.right,
					Joint.transform.TransformDirection(Joint.axis)));
			}
			else
				jointconnectedzero = (Joint.transform.position - Joint.connectedBody.transform.position).magnitude;

			Initialize2();
		}

		public void Initialize2()
		{
			InitializeDrive();

			InitializeLimits();
			
			Joint.yMotion = ConfigurableJointMotion.Locked;
			Joint.zMotion = ConfigurableJointMotion.Locked;
			Joint.angularYMotion = ConfigurableJointMotion.Locked;
			Joint.angularZMotion = ConfigurableJointMotion.Locked;

			if(isRotational)
			{
				Joint.xMotion = ConfigurableJointMotion.Locked;
				Joint.angularXMotion = (isFreeMoving && !bUseLimitJoint) ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;
			}
			else
			{
				Joint.xMotion = ConfigurableJointMotion.Free;
				Joint.angularXMotion = ConfigurableJointMotion.Locked;
			}

			Joint.enableCollision = false;
			Joint.enablePreprocessing = false;

			Joint.projectionMode = JointProjectionMode.None;
		}

		public void InitializeDrive()
		{
			if(isRotational)
			{
				Joint.rotationDriveMode = RotationDriveMode.XYAndZ;
				Joint.angularXDrive = new JointDrive
				{
					maximumForce = isFreeMoving ? 0.0f : torqueLimit * 20, // FEHLER, temp mit 20... mal sehen halt ob das Schütteln aufhört
					positionSpring = isFreeMoving ? jointSpring : 1e30f,
					positionDamper = isFreeMoving ? jointDamping : 0.0f
				};
			}
			else
			{
				Joint.xDrive = new JointDrive
				{
					maximumForce = isFreeMoving ? 0.0f : torqueLimit * 20, // FEHLER, temp mit 20... mal sehen halt ob das Schütteln aufhört
					positionSpring = isFreeMoving ? jointSpring : 1e30f,
					positionDamper = isFreeMoving ? jointDamping : 0.0f
				};
			}
		}
	
		public void InitializeLimits()
		{
			float min =
				swap ? (hasPositionLimit ? -maxPositionLimit : -maxPosition) : (hasPositionLimit ? minPositionLimit : minPosition);
			float max =
				swap ? (hasPositionLimit ? -minPositionLimit : -minPosition) : (hasPositionLimit ? maxPositionLimit : maxPosition);

			if(isRotational)
			{
				bUseLimitJoint = isFreeMoving && (max - min > 90);

				if(isFreeMoving && !bUseLimitJoint)
				{
					// we only use (unity-)limits on this joint for uncontrolled parts with a small range

					SoftJointLimit lowAngularXLimit = new SoftJointLimit() { limit = -to360(max + (!swap ? correction_0-correction_1 : correction_1-correction_0)) };
					SoftJointLimit highAngularXLimit = new SoftJointLimit() { limit = -to360(min + (!swap ? correction_0-correction_1 : correction_1-correction_0 )) };

					Joint.lowAngularXLimit = lowAngularXLimit;
					Joint.highAngularXLimit = highAngularXLimit;
					Joint.lowAngularXLimit = lowAngularXLimit;

					Joint.angularXMotion = ConfigurableJointMotion.Limited;
				}
				else
					Joint.angularXMotion = ConfigurableJointMotion.Free;
			}
	//		else
	//		{
	//			setting Joint.linearLimit does not make sense at all
	//		}

			ip.Initialize(position, !hasMinMaxPosition && !hasPositionLimit,
				to360(min + (!swap ? correction_0-correction_1 : correction_1-correction_0)),
				to360(max + (!swap ? correction_0-correction_1 : correction_1-correction_0)),
				speedLimit * factorSpeed * groupSpeedFactor, accelerationLimit * factorAcceleration);
		}
	
		public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
		{
			// negativ, because unity is left handed and we would have to correct this always

			return -Mathf.Atan2(
				Vector3.Dot(n.normalized, Vector3.Cross(v1.normalized, v2.normalized)),
				Vector3.Dot(v1.normalized, v2.normalized)) * Mathf.Rad2Deg;
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

		// set original rotation to current rotation
		public void UpdatePos()
		{
			part.UpdateOrgPosAndRot(part.vessel.rootPart);
			foreach(Part child in part.FindChildParts<Part>(true))
				child.UpdateOrgPosAndRot(vessel.rootPart);

// FEHLER, eine Idee... mal sehen wie's kommt	
				// Vektor bestimmen, entlang welchem ich zeige (aktuell) -> also von mir zum Kind
if(pointerPart) // FEHLER, temp, translational haben das noch nicht gesetzt
			IsTarget = pointerPart.transform.position - transform.position;
			IsPosition = transform.position; // damit's gleich heisst
			IsRotation = transform.rotation;
		}

		public void Update()
		{
			if(soundSound != null)
			{
				float pitchMultiplier = Math.Max(Math.Abs(Speed / factorSpeed), 0.05f);
					// FEHLER, Division, blöd

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

		public void FixedUpdate()
		{
			if(!HighLogic.LoadedSceneIsFlight)
				return;

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

					float targetPosition = ip.GetPosition();

					if(isRotational)
						Joint.targetRotation = Quaternion.AngleAxis(-targetPosition, Vector3.right); // rotate always around x axis!!
					else
						Joint.targetPosition = Vector3.right * -targetPosition; // move always along x axis!!

	//				position = targetPosition; // we correct this later
						// FEHLER, verdammte Scheisse... das ist doch Müll hier... wozu soll ich den anders setzen, hä?
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

			UpdatePos();

			if(isRotational)
			{
				// read new position
				float newPosition =
					AngleSigned(
						jointup ? Joint.transform.up : Joint.transform.right,
						connectedup ? Joint.connectedBody.transform.up : Joint.connectedBody.transform.right,
						Joint.transform.TransformDirection(Joint.axis))
					- jointconnectedzero;

				// correct value into a plausible range
				float newPositionCorrected = newPosition - zeroNormal - correction_1 + correction_0;
				float positionCorrected = position - zeroNormal - correction_1 + correction_0;

				if(newPositionCorrected < positionCorrected)
				{
					if((positionCorrected - newPositionCorrected) > (newPositionCorrected + 360f - positionCorrected))
						newPosition += 360f;
				}
				else
				{
					if((newPositionCorrected - positionCorrected) > (positionCorrected - newPositionCorrected + 360f))
						newPosition -= 360f;
				}

				// set new position
				position = newPosition;

				if(bUseLimitJoint)
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
							SoftJointLimit highAngularXLimit = new SoftJointLimit() { limit = -(min - position) };

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

							SoftJointLimit lowAngularXLimit = new SoftJointLimit() { limit = -(max - position)};
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

		//		DrawAxis(8, part.attachJoint.Joint.transform, part.attachJoint.Joint.transform.up, false);
		//		DrawAxis(9, part.attachJoint.Joint.connectedBody.transform, part.attachJoint.Joint.connectedBody.transform.up, false);

	// FEHLER, temp, der Scheissdreck muss aktuell IMMER angezeigt werden		
				al[3].DrawLineInGameView(
					IsPosition,
					IsPosition + IsTarget,
					alColor[3]);
			}
			else
				position = (Joint.transform.position - Joint.connectedBody.transform.position).magnitude - jointconnectedzero;

			// show axis
		//	DrawAxis(3, Joint.transform, Joint.axis, true);
		//	DrawAxis(4, Joint.transform, Joint.secondaryAxis, true);

			// show limits
		//	DrawLimits(8, position);
	
		}

		private bool UpdateAndConsumeElectricCharge()
		{
			if((electricChargeRequired == 0f) || isFreeMoving)
				return true;

			ip.PrepareUpdate(TimeWarp.fixedDeltaTime);

			float amountToConsume = electricChargeRequired * TimeWarp.fixedDeltaTime;

			amountToConsume *= TorqueLimit / MaxTorque;
			amountToConsume *= (ip.NewSpeed + ip.Speed) / (2 * MaxSpeed);

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

		////////////////////////////////////////
		// inverse kinematics (einige Teile stecken noch woanders)

		// man müsste jetzt 'ne Gruppe bauen um die vollständig berechnen zu können... und dann alles anzuzeigen...
		static List<ModuleIRServo_v3> Group = new List<ModuleIRServo_v3>();

		public Vector3 IsPosition;		// aktuelle Position (eigentlich transform.position)
		public Quaternion IsRotation;	// aktuelle Rotation (eigentlich transform.rotation)
		public Vector3 IsTarget;		// aktuelle Richtung, in der wir zeigen

		public class Stat
		{
			public bool bRestricted = false;
			public Vector3 Position;	// absolut
			public Quaternion Rotation;	// absolut -> inklusive meiner Drehung, also das, was dann für die Kinder relevant ist
			public Vector3 Target;		// relativ zu Position
		};

		public List<Stat> aStat = new List<Stat>();
		public Stat lastStat;
		public Stat _lastRestrictedStat; // restricted
		public Stat lastRestrictedStat() // FEHLER, erste Idee mal... weiss nicht, ob "last" bedeuten müsste -> NICHT cur...
		{
			int i = aStat.Count - 1;
			while(!aStat[i].bRestricted) --i;
			return aStat[i];
		}
		public Stat curStat;

		public void AddStat()
		{
			lastStat = curStat;
			if(curStat.bRestricted)
				_lastRestrictedStat = curStat;
			curStat = new Stat();
			aStat.Add(curStat);
		}

		public void Plot(int colorIdx, int type)
		{
/*			if(type == 0)
			{
				al[colorIdx].DrawLineInGameView(
					IsPosition,
					IsPosition + IsTarget,
					alColor[colorIdx]);
			}
			else
			{*/
				al[colorIdx].DrawLineInGameView(
					aStat[type].Position,
					aStat[type].Position + aStat[type].Target,
					alColor[colorIdx]);
//			}
		}


		public Vector3 GetAxis()
		{ return Joint.transform.TransformDirection(Joint.axis).normalized; }

		public Vector3 GetSecAxis()
		{ return Joint.transform.TransformDirection(Joint.secondaryAxis).normalized; }

/*
		[KSPEvent(guiActive = false, guiActiveEditor = false, guiName = "berechne mal")]
		public void berechneMal()
		{
			// FEHLER, den Controller anrufen... neu

			ServoController.bMove = !ServoController.bMove;
		}
*/

		[KSPEvent(guiActive = true, guiActiveEditor = false, guiName = "test test")]
		public void testtest()
		{
		}

		////////////////////////////////////////
		// IRescalable

		public void OnRescale(ScalingFactor factor)
		{
			electricChargeRequired *= factor.relative.quadratic;

 			if(isRotational)
				return;

			minPosition *= factor.relative.linear;
			maxPosition *= factor.relative.linear;

			minPositionLimit *= factor.relative.linear;
			maxPositionLimit *= factor.relative.linear;

			factorAcceleration *= factor.relative.linear;
			factorSpeed *= factor.relative.linear;

			zeroNormal *= factor.relative.linear;
			zeroInvert *= factor.relative.linear;

			float deltaPosition = position;

			position *= factor.relative.linear;

			deltaPosition = position - deltaPosition;

			transform.Translate(axis.normalized * deltaPosition);

			InitializeLimits();
		}

		////////////////////////////////////////
		// IJointLockState

		bool IJointLockState.IsJointUnlocked()
		{
			return !isLocked;
		}

		////////////////////////////////////////
		// KSPEvents, KSPActions

		public bool FindSwap()
		{
			AttachNode nodeToParent = part.FindAttachNodeByPart(part.parent); // always exists

		//	int idx = 0;
		//	while((idx < part.attachNodes.Count)
		//		&& (part.attachNodes[idx].position != nodeToParent.position)) ++idx;
			// -> first method -> but second one seems to be more robust

			int idx = 0;
			while((idx < part.attachNodes.Count)
				&& (part.attachNodes[idx].originalPosition != nodeToParent.originalPosition)) ++idx;

			return ((idx < part.attachNodes.Count) && (part.attachNodes[idx].id != "bottom"));
		}
		
		void SetLock(bool bLock)
		{
			isLocked = bLock;

			Events["MotionLockToggle"].guiName = isLocked ? "Disengage Lock" : "Engage Lock";

			if(isLocked)
				Stop();
		}

		private void UpdateUI()
		{
			Events["ToggleLimits"].guiActive = hasMinMaxPosition;
			Events["ToggleLimits"].guiActiveEditor = hasMinMaxPosition;
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

		private readonly IMotor motor;

		public IMotor Motor
		{
			get { return motor; }
		}

		private readonly IPresetable presets;

		public IPresetable Presets
		{
			get { return presets; }
		}

		[KSPField(isPersistant = true)] public string groupName = "";

		public string GroupName
		{
			get { return groupName; }
			set { groupName = value; }
		}

		////////////////////////////////////////
		// Status

		// Gets the current position (corrected, when swapped or inverted)
		public float Position
		{
			get
			{
				if(!isInverted)
					return zeroNormal + ((correction_1 - correction_0) + (swap ? -position : position));
				else
					return zeroInvert - ((correction_1 - correction_0) + (swap ? -position : position));
			}
		}

		public float Speed
		{
			get { return ip.Speed; }
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
			set { SetLock(value); }
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Engage Lock", active = true)]
		public void MotionLockToggle()
		{
			SetLock(!isLocked);
		}

		[KSPAction("Toggle Lock")]
		public void MotionLockToggle(KSPActionParam param)
		{
			SetLock(!isLocked);
		}

		////////////////////////////////////////
		// Settings

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Invert Axis")]
		public void InvertAxisToggle()
		{
			isInverted = !isInverted;
			Events["InvertAxisToggle"].guiName = isInverted ? "Un-invert Axis" : "Invert Axis";
		}

		[KSPField(isPersistant = true)] public bool isInverted = false;

		public bool IsInverted
		{
			get { return isInverted; }
			set { isInverted = value; }
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
					return zeroNormal;
				else
					return zeroInvert;
			}
			set
			{
				if(isInverted)
					value = zeroInvert - value;

				defaultPosition = Mathf.Clamp(value, minPosition, maxPosition);
			}
		}

			// limits set by the user
		[KSPField(isPersistant = true)] public bool hasPositionLimit = false;

		[KSPField(isPersistant = true)/*, guiActive = true, guiActiveEditor = true, guiName = "Min", guiFormat = "F2", guiUnits = "")/*, 
			UI_FloatEdit(minValue = -360f, maxValue = 360f, incrementSlide = 0.01f, scene = UI_Scene.All, sigFigs = 2)*/]
		public float minPositionLimit = 0;

		[KSPField(isPersistant = true)/*, guiActive = true, guiActiveEditor = true, guiName = "Max", guiFormat = "F2", guiUnits = "")/*, 
			UI_FloatEdit(minValue = -360f, maxValue = 360f, incrementSlide = 0.01f, scene = UI_Scene.All, sigFigs = 2)*/]
		public float maxPositionLimit = 360;

		public bool IsLimitted
		{
			get { return hasPositionLimit; }
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Engage Limits", active = false)]
		public void ToggleLimits()
		{
			if(!canHaveLimits)
				return;

			hasPositionLimit = !hasPositionLimit;

			if(Joint)
				InitializeLimits();

			Events["ToggleLimits"].guiName = hasPositionLimit ? "Engage Limits" : "Disengage Limits";
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
			}
		}

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Torque", guiFormat = "0.00"),
			UI_FloatEdit(minValue = 0f, maxValue=30f, incrementSlide = 0.05f, incrementSmall=0.5f, incrementLarge=1f, scene = UI_Scene.All, sigFigs = 2)]
		public float torqueLimit = 1f;

		public float TorqueLimit
		{
			get { return torqueLimit; }
			set
			{
				torqueLimit = Mathf.Clamp(value, 0.1f, maxTorque);
				if(Joint)
					InitializeDrive();
			}
		}

// FEHLER, wozu? der geht sowieso immer auf Voll-Speed? -> das mal noch klären hier...
		public float DefaultSpeed
		{
			get { return SpeedLimit; }
			set {}
		}

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Speed", guiFormat = "0.00"), 
			UI_FloatEdit(minValue = 0f, incrementSlide = 0.05f, incrementSmall=0.5f, incrementLarge=1f, sigFigs = 2)]
		public float speedLimit = 1f;

		public float SpeedLimit
		{
			get { return speedLimit; }
			set
			{
				speedLimit = Mathf.Clamp(value, 0.01f, maxSpeed);
				ip.maxSpeed = speedLimit * factorSpeed * groupSpeedFactor;
			}
		}

		public float groupSpeedFactor = 1f;

		public float GroupSpeedFactor
		{
			get { return groupSpeedFactor; }
			set
			{
				groupSpeedFactor = (value < 0.1f) ? 0.1f : value;
				ip.maxSpeed = speedLimit * factorSpeed * groupSpeedFactor;
			}
		}


		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Acceleration", guiFormat = "0.00"), 
			UI_FloatEdit(minValue = 0.05f, incrementSlide = 0.05f, incrementSmall=0.5f, incrementLarge=1f, sigFigs = 2)]
		public float accelerationLimit = 4f;

		public float AccelerationLimit
		{
			get { return accelerationLimit; }
			set
			{
				accelerationLimit = Mathf.Clamp(value, 0.1f, maxAcceleration);
				ip.maxAcceleration = accelerationLimit * factorAcceleration;
			}
		}

		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Force", guiFormat = "0.00"), 
			UI_FloatEdit(minValue = 0.0f, incrementSlide = 0.05f, incrementSmall=0.5f, incrementLarge=1f, sigFigs = 2)]
				public float jointSpring = 0;
		public float SpringPower 
		{
			get { return jointSpring; }
			set { jointSpring = value; }
		}

		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Damping Force", guiFormat = "0.00"), 
			UI_FloatEdit(minValue = 0.0f, incrementSlide = 0.05f, incrementSmall=0.5f, incrementLarge=1f, sigFigs = 2)]
				public float jointDamping = 0;
		public float DampingPower 
		{
			get { return jointDamping; }
			set { jointDamping = value; }
		}

		////////////////////////////////////////
		// Characteristics - values 'by design' of the joint

		[KSPField(isPersistant = false)] public bool isRotational = false;

		public bool IsRotational
		{
			get { return isRotational; }
		}

		[KSPField(isPersistant = true)] public bool hasMinMaxPosition = false;
		[KSPField(isPersistant = true)] public float minPosition = 0;
		[KSPField(isPersistant = true)] public float maxPosition = 360;

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

		[KSPField(isPersistant = true)] public bool isFreeMoving = false;

		public bool IsFreeMoving
		{
			get { return isFreeMoving; }
		}

		[KSPField(isPersistant = true)] public bool canHaveLimits = true;

		public bool CanHaveLimits
		{
			get { return canHaveLimits; }
		}

		[KSPField(isPersistant = true)] public float maxTorque = 30f;

		public float MaxTorque
		{
			get { return maxTorque; }
		}

		[KSPField(isPersistant = true)] public float maxSpeed = 100;

		public float MaxSpeed
		{
			get { return maxSpeed; }
		}

		[KSPField(isPersistant = true)] public float maxAcceleration = 10;

		public float MaxAcceleration
		{
			get { return maxAcceleration; }
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

		[KSPField(isPersistant = true)] public float factorSpeed = 1.0f;
		[KSPField(isPersistant = true)] public float factorAcceleration = 1.0f;

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

		public void Move(float deltaPosition, float targetSpeed)
		{
			if(isOnRails || isLocked || isFreeMoving)
				return;

			if(swap)
				deltaPosition = -deltaPosition;

			if(isInverted)
				deltaPosition = -deltaPosition;

			ip.SetCommand(to360(position + deltaPosition), targetSpeed * factorSpeed * groupSpeedFactor);
		}

		public void MoveTo(float targetPosition, float targetSpeed)
		{
			if(isOnRails || isLocked || isFreeMoving)
				return;

			if(!isInverted)
				targetPosition = (swap ? -1.0f : 1.0f) * (targetPosition - zeroNormal - correction_1 + correction_0);
			else
				targetPosition = (swap ? 1.0f : -1.0f) * (targetPosition - zeroInvert + correction_1 - correction_0);

			ip.SetCommand(targetPosition, targetSpeed * factorSpeed * groupSpeedFactor);
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
				Motor.MoveRight();
			else if(KeyPressed(reverseKey))
				Motor.MoveLeft();
			else if(KeyUnPressed(forwardKey) || KeyUnPressed(reverseKey))
				Motor.Stop();
		}

		////////////////////////////////////////
		// Editor

		public void EditorReset()
		{
			EditorSetPosition(0f);
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

			float deltaPosition = targetPosition - position;

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

			position = targetPosition;
		}

		public void CopyPresetsToSymmetry()
		{
			Logger.Log("CopyPresetsToSymmetry", Logger.Level.Debug);

			foreach(Part p in part.symmetryCounterparts)
			{
				ModuleIRServo_v3 servo = p.GetComponent<ModuleIRServo_v3>();

				servo.PresetPositions = new List<float>();
				servo.PresetPositions.AddRange(PresetPositions);

				servo.defaultPosition = defaultPosition; //force sync the default position as well
			}
		}

		public void CopyLimitsToSymmetry()
		{
			Logger.Log("CopyLimitsToSymmetry", Logger.Level.Debug);

			foreach(Part p in part.symmetryCounterparts)
			{
				ModuleIRServo_v3 servo = p.GetComponent<ModuleIRServo_v3>();

				servo.hasPositionLimit = hasPositionLimit;
				servo.MinPositionLimit = MinPositionLimit;
				servo.MaxPositionLimit = MaxPositionLimit;
			}
		}

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
				+ (p_relative ? p_transform.TransformVector(p_vector) : p_vector), alColor[idx]);
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

