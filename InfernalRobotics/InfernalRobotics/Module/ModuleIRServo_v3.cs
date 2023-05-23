using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using KSP.IO;
using UnityEngine;

using InfernalRobotics_v3.Command;
using InfernalRobotics_v3.Effects;
using InfernalRobotics_v3.Interfaces;
using InfernalRobotics_v3.Servo;
using InfernalRobotics_v3.Utility;

using InfernalRobotics_v3.Gui;

namespace InfernalRobotics_v3.Module
{
	/*
	 * Remarks:
	 * 
	 * The values for maximumForce and positionSpring in the joints should not be too large. In case of small parts,
	 * too large values could cause unity to return NaN when calculating physics, which leads to a crash of the system.
	 * KSP uses the PhysicsGlobals.JointForce value as a maximum (currently 1E+20f).
	 */

	public class ModuleIRServo_v3 : PartModule, IServo, IJointLockState, IPartMassModifier, IPartCostModifier, IModuleInfo, IResourceConsumer, IConstruction
	{
		static private bool constantsLoaded = false;

		static private float resetPrecisionRotational = 4f;
		static private float resetPrecisionTranslational = 2f;

		////////////////////////////////////////
		// Data

		public bool isInitialized = false;

		private ConfigurableJoint Joint = null;

		[KSPField(isPersistant = false), SerializeField]
		private Vector3 axis = Vector3.right;	// x-axis of the joint
		[KSPField(isPersistant = false), SerializeField]
		private Vector3 pointer = Vector3.up;	// towards child (if possible), but always perpendicular to axis

		// true, if servo is attached reversed
		[KSPField(isPersistant = true)]
		public bool swap = false;

		[KSPField(isPersistant = false), SerializeField]
		private string movingMesh = "";
		[KSPField(isPersistant = false), SerializeField]
		private string fixedMesh = "";
		[KSPField(isPersistant = false), SerializeField]
		private string middleMeshes = "";

		[KSPField(isPersistant = false), SerializeField]
		private string fixedMeshNode = "bottom|srfAttach";

		private Transform movingMeshTransform = null;
		private Transform fixedMeshTransform = null;
		private Transform fixedMeshTransformParent;

		private GameObject fixedMeshAnchor = null;

		private Transform[] middleMeshesTransform = null;

		// internal information on how to calculate/read-out the current rotation/translation
		private Vector3 rot_jointup, rot_connectedup;

		private Vector3 trans_connectedzero;
		private float trans_zero;

		private float jointconnectedzero;

		/*
		 * position is an internal value and always relative to the current orientation
		 * of the joint (swap or not swap)
		 * all interface functions returning and expecting values do the swap internally
		 * and return and expect external values
		*/

		// position relative to current zero-point of joint
		[KSPField(isPersistant = true)]
		public float commandedPosition = 0f;
		private float position = 0f;

		private float lastUpdatePosition;

		// correction values for position
		// (required, since joints are always built straight, i.e. they always have their zero or
		// neutral points where they are created and they cannot be built angled)
		[KSPField(isPersistant = true)]
		public float correction_0 = 0f;
		[KSPField(isPersistant = true)]
		public float correction_1 = 0f;

		// correction values for displayed position
		private float commandedPositionCorrection = 0f;
		private float positionCorrection = 0f;

		// Limit-Joint (extra joint used for limits only, built dynamically, needed because of unity limitations)
		private ConfigurableJoint LimitJoint = null;
		private bool bLowerLimitJoint;
		private bool bUseDynamicLimitJoint = false;

		// Stability-Joints (extra joints used for stability of translational joints)
		[KSPField(isPersistant = false), SerializeField]
		private bool bBuildStabilityJoint = true;

		private ConfigurableJoint[] StabilityJoint = { null, null };
			// FEHLER, wir brauchen aktuell nur noch 1 davon -> dem evtl. das Drive so setzen, dass es einen Damper hat? damit's Schwingungen auffangen könnte?
			// ja und evtl. doch public machen, damit einer das verändern kann? mal sehen halt... das wird ja nochmal aktualisiert
			// List davon bauen ... und, save-load? für extra-joints?
			// und das verlängern? hmm... ja gut, evtl. auch eher in anderem Modul?
			// weitere stabilityJoints bauen -> nicht an parent, sondern an andere Punkte (für multi-Rail-Idee)

		// Motor (works with position relative to current zero-point of joint, like position)
		private Interpolator ip;
		private Interceptors.Limiter lm;
		private ILimiter ilm;

		[KSPField(isPersistant = false), SerializeField]
		private float friction = 0.5f;

		// Modes
		[KSPField(isPersistant = false), SerializeField]
		private string availableModeS = "";

		private List<ModeType> availableModes = null;

		private void ParseAvailableModes()
		{
			string[] modeChunks = availableModeS.Split('|');
			availableModes = new List<ModeType>();
			foreach(string chunk in modeChunks)
			{
				if(chunk == "Servo")
					availableModes.Add(ModeType.servo);
				else if(chunk == "Rotor")
					availableModes.Add(ModeType.rotor);
				else
					Logger.Log("[servo] unknown mode " + chunk + " found for part " + part.partInfo.name, Logger.Level.Debug);
			}

			if(availableModes.Count == 0)
				availableModes.Add(ModeType.servo);
			else
			{
				availableModes.Sort();
				for(int i = 1; i < availableModes.Count; i++)
				{
					if(availableModes[i - 1] == availableModes[i])
						availableModes.RemoveAt(i--);
				}
			}

			List<string> m = new List<string>();
			for(int i = 0; i < availableModes.Count; i++)
			{
				switch(availableModes[i])
				{
				case ModeType.servo: m.Add("Servo"); break;
				case ModeType.rotor: m.Add("Rotor"); break;
				}
			}

			if(HighLogic.LoadedSceneIsFlight)
				((UI_ChooseOption)Fields["modeIndex"].uiControlFlight).options = m.ToArray();
			else
				((UI_ChooseOption)Fields["modeIndex"].uiControlEditor).options = m.ToArray();
		}

		[KSPField(isPersistant = false), SerializeField]
		private string availableInputModeS = "";

		private List<InputModeType> availableInputModes = null;

		private void ParseAvailableInputModes()
		{
			string[] inputModeChunks = availableInputModeS.Split('|');
			availableInputModes = new List<InputModeType>();
			foreach(string chunk in inputModeChunks)
			{
				if(chunk == "Manual")
					availableInputModes.Add(InputModeType.manual);
				else if(chunk == "Control")
					availableInputModes.Add(InputModeType.control);
				else if(chunk == "Linked")
					availableInputModes.Add(InputModeType.linked);
				else if(chunk == "Tracking")
					availableInputModes.Add(InputModeType.tracking);
				else
					Logger.Log("[servo] unknown inputmode " + chunk + " found for part " + part.partInfo.name, Logger.Level.Debug);
			}

			if(availableInputModes.Count == 0)
				availableInputModes.Add(InputModeType.manual);
			else
			{
				availableInputModes.Sort();
				for(int i = 1; i < availableInputModes.Count; i++)
				{
					if(availableInputModes[i - 1] == availableInputModes[i])
						availableInputModes.RemoveAt(i--);
				}
			}

			List<string> m = new List<string>();
			for(int i = 0; i < availableInputModes.Count; i++)
			{
				switch(availableInputModes[i])
				{
				case InputModeType.manual: m.Add("Manual"); break;
				case InputModeType.control: m.Add("Control"); break;
				case InputModeType.linked: m.Add("Linked"); break;
				case InputModeType.tracking: m.Add("Tracking"); break;
				}
			}

			if(HighLogic.LoadedSceneIsFlight)
				((UI_ChooseOption)Fields["inputModeIndex"].uiControlFlight).options = m.ToArray();
			else
				((UI_ChooseOption)Fields["inputModeIndex"].uiControlEditor).options = m.ToArray();
		}

		// Electric Power
		private PartResourceDefinition electricResource = null;

		private bool hasElectricPower;

		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Power Consumption", guiFormat = "F1", guiUnits = "mu/s")]
		private float LastPowerDrawRate;

		// Sound
		[KSPField(isPersistant = false), SerializeField]
		private float soundPitch = 1.0f;
		[KSPField(isPersistant = false), SerializeField]
		private float soundVolume = 0.5f;
		[KSPField(isPersistant = false), SerializeField]
		private string soundFilePath = "";
		private SoundSource soundSound = null;

		// Lights
		static int lightColorId = 0;
		static Color lightColorOff, lightColorLocked, lightColorIdle, lightColorMoving, lightColorRotor, lightColorControl, lightColorTracking;

		private int lightStatus = -3;
		private Renderer lightRenderer;

		// Environment
		private bool isOnRails = false;

		////////////////////////////////////////
		// Data (servo)

		// Presets
		[KSPField(isPersistant = true)]
		public string presetsS = "";

		private void ParsePresetPositions()
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

		private void SerializePresets()
		{
			if(PresetPositions != null) // only for security -> otherwise KSP will crash
				presetsS = PresetPositions.Aggregate(string.Empty, (current, s) => current + (s + "|")).Trim('|');
		}

		// Link-Mode
		[KSPField(isPersistant = true)]
		public uint LinkedInputSourceId = 0;

		[KSPField(isPersistant = true)]
		public uint LinkedInputPartId = 0;

		[KSPField(isPersistant = true)]
		public uint LinkedInputPartFlightId = 0;

		private ModuleIRServo_v3 LinkedInputPart = null;


		private List<ModuleIRServo_v3> LinkedInputParts = new List<ModuleIRServo_v3>();

		public void Link(ModuleIRServo_v3 s)
		{ LinkedInputParts.Add(s); }
		public void Unlink(ModuleIRServo_v3 s)
		{ LinkedInputParts.Remove(s); }

		////////////////////////////////////////
		// Constructor

		public ModuleIRServo_v3()
		{
			if(!isFreeMoving)
			{
				ip = new Interpolator();
				lm = new Interceptors.Limiter();
				ilm = lm;

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
			DebugInit();

			isInitialized = false;

			LoadConstants();

			if(lightColorId == 0)
			{
				lightColorId = Shader.PropertyToID("_EmissiveColor");
				lightColorOff = new Color(0, 0, 0, 0);
				lightColorLocked = new Color(1, 0, 0, 1);
				lightColorIdle = new Color(1, 0.76f, 0, 1);
				lightColorMoving = new Color(0, 1, 0, 1);
				lightColorRotor = new Color(1, 0, 0.76f, 1);
				lightColorControl = new Color(0, 1, 0.76f, 1);
				lightColorTracking = new Color(0.2f, 0.2f, 1f, 1);
			}

			electricResource = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

			if(consumedResources == null)
				consumedResources = new List<PartResourceDefinition>();
			else
				consumedResources.Clear();

			consumedResources.Add(electricResource);

			GameEvents.onVesselCreate.Add(OnVesselCreate);
			GameEvents.onVesselWasModified.Add(OnVesselWasModified);

			GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
			GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);

			GameEvents.onPhysicsEaseStart.Add(OnEaseStart);
			GameEvents.onPhysicsEaseStop.Add(OnEaseStop);

		//	GameEvents.onJointBreak.Add(OnJointBreak); -> currently we use OnVesselWasModified

			if(HighLogic.LoadedSceneIsEditor)
			{
				GameEvents.onEditorStarted.Add(OnEditorStarted);
				GameEvents.onEditorPartPlaced.Add(OnEditorPartPlaced);
			}
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			// Renderer for lights
			lightRenderer = part.gameObject.GetComponentInChildren<Renderer>(); // do this before workaround

			if(state == StartState.Editor)
			{
				EditorInitialize();

				InitializeValues();

				if(LinkedInputPartId != 0)
				{
					LinkedInputPartFlightId = 0;

					for(int i = 0; i < EditorLogic.fetch.ship.parts.Count; i++)
					{
						ModuleIRServo_v3 servo = EditorLogic.fetch.ship.parts[i].GetComponent<ModuleIRServo_v3>();

						if((servo != null) && (servo.LinkedInputSourceId == LinkedInputPartId))
						{ LinkedInputPart = servo; LinkedInputPart.Link(this); break; }
					}
				}

				isInitialized = true;
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
	
				StartCoroutine(WaitAndInitialize()); // calling Initialize1 in OnStartFinished should work too

				if(LinkedInputPartFlightId != 0)
				{
					for(int i = 0; i < vessel.parts.Count; i++)
					{
						if(vessel.parts[i].flightID == LinkedInputPartFlightId)
						{ LinkedInputPart = vessel.parts[i].GetComponent<ModuleIRServo_v3>(); LinkedInputPart.Link(this); break; }
					}
				}
				else if(LinkedInputPartId != 0)
				{
					for(int i = 0; i < vessel.parts.Count; i++)
					{
						ModuleIRServo_v3 servo = vessel.parts[i].GetComponent<ModuleIRServo_v3>();

						if((servo != null) && (servo.LinkedInputSourceId == LinkedInputPartId))
						{ LinkedInputPart = servo; LinkedInputPartFlightId = LinkedInputPart.part.flightID; LinkedInputPart.Link(this); break; }
					}
				}
			}

			AttachContextMenu();

			UpdateUI();

			if(HighLogic.LoadedSceneIsFlight && CollisionManager4.Instance)
				CollisionManager4.Instance.RegisterServo(this);
		}

		public IEnumerator WaitAndInitialize()
		{
			if(part.parent)
			{
				while(!part.attachJoint || !part.attachJoint.Joint)
					yield return null;
			}

			if(part.attachJoint && part.attachJoint.Joint && (Joint != part.attachJoint.Joint))
				Initialize1();

			UpdateUI();
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

		//	GameEvents.onJointBreak.Remove(OnJointBreak); -> currently we use OnVesselWasModified

			GameEvents.onEditorStarted.Remove(OnEditorStarted);
			GameEvents.onEditorPartPlaced.Remove(OnEditorPartPlaced);

			if(CollisionManager4.Instance) // -> remove always, just to be sure
				CollisionManager4.Instance.UnregisterServo(this);

			if(LimitJoint)
				Destroy(LimitJoint);

			if(StabilityJoint[0])
				Destroy(StabilityJoint[0]);
			if(StabilityJoint[1])
				Destroy(StabilityJoint[1]);

			if(LinkedInputPart)
				LinkedInputPart.Unlink(this);
		}

		public override void OnSave(ConfigNode config)
		{
			ModuleIRController.OnSave(config);

			base.OnSave(config);

			SerializePresets();
		}

		public override void OnLoad(ConfigNode config)
		{
			ModuleIRController.OnLoad(config, part.vessel);

			base.OnLoad(config);

			if((part.partInfo != null) && (part.partInfo.partPrefab != null))
				OnRescale(new ScalingFactor(scalingFactor));
		}

		public void OnVesselGoOnRails(Vessel v)
		{
			if(part.vessel != v)
				return;

			isOnRails = true;
		}

		public void OnVesselGoOffRails(Vessel v)
		{
			if(part.vessel != v)
				return;

			isOnRails = false;

			Initialize2();

			if(Joint)
			{
				if(isRotational)
					Joint.targetRotation = Quaternion.AngleAxis(-(commandedPosition + lockPosition), Vector3.right); // rotate always around x axis
				else
					Joint.targetPosition = Vector3.right * (trans_zero - (commandedPosition + lockPosition)); // move always along x axis
			}
		}

		private FixedJoint easeJoint;

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

		////////////////////////////////////////
		// Editor

		[KSPField(isPersistant = true)]
		public bool editorRotated = false;

		public float CommandedPositionS
		{
			get
			{
				return (swap ? -commandedPosition : commandedPosition) + zeroNormal + correction_1 - correction_0;
			}
		}

		public void OnEditorAttached(bool detachedAsRoot)
		{
			float _detachPosition = swap ? -CommandedPositionS : CommandedPositionS;

			if(swap != FindSwap())
			{
				swap = !swap;

				commandedPosition = -commandedPosition;

				_detachPosition = 0;
			}

			fixedMeshTransform = KSPUtil.FindInPartModel(transform, swap ? movingMesh : fixedMesh);
			movingMeshTransform = KSPUtil.FindInPartModel(transform, swap ? fixedMesh : movingMesh);

			if(detachedAsRoot)
			{
				EditorInitialize();

				MoveChildren(-_detachPosition);
				editorRotated = false;

				if(isRotational)
					transform.Rotate(axis, _detachPosition);
				else
					transform.Translate(axis.normalized * _detachPosition);
			}

			FixChildrenAttachement();
		}

		// Remarks: on detaching objects KSP tends to re-orient them (happens because the parent of the transform is set to null)

		public void OnEditorDetached(bool detachedAsRoot)
		{
			if(detachedAsRoot)
			{
				float _detachPosition = swap ? -CommandedPositionS : CommandedPositionS;

				EditorInitialize();

				MoveChildren(_detachPosition);
				editorRotated = true;

				if(isRotational)
					transform.Rotate(axis, -_detachPosition);
				else
					transform.Translate(axis.normalized * -_detachPosition);
			}
		}

		public void OnEditorCopied()
		{
			if(!editorRotated)
			{
				MoveChildren(commandedPosition); // FEHLER, CommandedPositionS ??
				editorRotated = true;
			}
		}

		public void OnEditorRootSelected()
		{
			if(swap != FindSwap())
			{
				swap = !swap;

				commandedPosition = -commandedPosition;

				if(part.children.Count != 0)
				{
					float _detachPosition = swap ? CommandedPositionS : -CommandedPositionS;

					MoveChildren(_detachPosition);

					if(isRotational)
						transform.Rotate(axis, -_detachPosition);
					else
						transform.Translate(axis.normalized * -_detachPosition);
				}

				EditorInitialize();	// FEHLER, wirklich? oder ist das zuviel?
			}
		}

		public Quaternion CalculateNeutralRotation(Quaternion currentRotation)
		{
			return Quaternion.AngleAxis(swap ? CommandedPositionS : -CommandedPositionS, currentRotation * axis) * currentRotation;
		}

		public Quaternion CalculateFinalRotation(Quaternion neutralRotation)
		{
			return Quaternion.AngleAxis(swap ? -CommandedPositionS : CommandedPositionS, neutralRotation * axis) * neutralRotation;
		}

		public void _RotateBack(AttachNode node)
		{
			AttachNode prefabNode = null;

			if((part.srfAttachNode != null)
			&& (part.partInfo.partPrefab.srfAttachNode.id == node.id))
				prefabNode = part.partInfo.partPrefab.srfAttachNode;
			else
			{
				for(int i = 0; i < part.partInfo.partPrefab.attachNodes.Count; i++)
				{
					if(part.partInfo.partPrefab.attachNodes[i].id == node.id)
					{ prefabNode = part.partInfo.partPrefab.attachNodes[i]; break; }
				}
			}

			if(node.icon != null)
				node.icon.transform.localScale = Vector3.one * prefabNode.radius * ((prefabNode.size == 0) ? ((float)prefabNode.size + 0.5f) : prefabNode.size);

			ResetAttachNode(node, prefabNode, scalingFactor);
		}

		public void OnEditorStarted()
		{
			FixChildrenAttachement();
		}

		public void OnEditorPartPlaced(Part potentialChild)
		{
			if(potentialChild && (potentialChild.parent == part))
				FixChildrenAttachement();

			// we need to fix this special value
			Events["RemoveFromSymmetry2"].guiActiveEditor = (part.symmetryCounterparts.Count > 0);
		}

		////////////////////////////////////////
		// Functions

		private void doModulo(float p_delta)
		{
			commandedPosition += p_delta;
			commandedPositionCorrection -= p_delta;
			position += p_delta;
			positionCorrection -= p_delta;

			ip.doModulo(p_delta);
		}

		private void updateDisplayPosition()
		{
			float pos = Position;

			if(pos >= 360f)
				positionCorrection -= 360f;
			else if(pos <= -360f)
				positionCorrection += 360f;
		}

		private void updateDisplayCommandedPosition()
		{
			float pos = CommandedPosition;

			if(pos >= 360f)
				commandedPositionCorrection -= 360f;
			else if(pos <= -360f)
				commandedPositionCorrection += 360f;

			if((positionCorrection + 340f < commandedPositionCorrection)
			|| (positionCorrection - 340f > commandedPositionCorrection))
			{
				float f;

				if(!isInverted)
					f = (swap ? -position : position) + zeroNormal + correction_1 - correction_0 + commandedPositionCorrection;
				else
					f = (swap ? position : -position) + zeroInvert - correction_1 + correction_0 - commandedPositionCorrection;

				if((f > -360f) && (f < 360f))
					positionCorrection = commandedPositionCorrection;
			}
		}

		private static bool CompareValueAbsolute(float a, float b)
		{ return Mathf.Abs(Mathf.Abs(a) - Mathf.Abs(b)) >= 0.05; }

		private static void LoadConstants()
		{
			if(constantsLoaded)
				return;

			PluginConfiguration config = PluginConfiguration.CreateForType<ModuleIRServo_v3>();
			config.load();

			resetPrecisionRotational = config.GetValue<float>("PrecisionRotational", 4f);
			resetPrecisionTranslational = config.GetValue<float>("PrecisionTranslational", 2f);

			constantsLoaded = true;
		}

		// corrects all the values to valid values
		private void InitializeValues()
		{
			_minPositionLimit = Mathf.Clamp(_minPositionLimit, minPosition, maxPosition);
			_maxPositionLimit = Mathf.Clamp(_maxPositionLimit, minPosition, maxPosition);

			minmaxPositionLimit.x = MinPositionLimit;
			minmaxPositionLimit.y = MaxPositionLimit;
			requestedPosition = CommandedPosition;

			speedLimit = Mathf.Clamp(speedLimit, 0.1f, MaxSpeed);

			ip.maxAcceleration = accelerationLimit * factorAcceleration;
			ip.maxSpeed = speedLimit * factorSpeed;

			if(availableModes == null)
				ParseAvailableModes();

			if(availableInputModes == null)
				ParseAvailableInputModes();

			ParsePresetPositions();

			UpdateMaxPowerDrawRate();
		}

		private bool IsFixedMeshNode(string id)
		{
			string[] nodeIds = fixedMeshNode.Split('|');
			foreach(string nodeId in nodeIds)
			{
				if(id == nodeId)
					return true;
			}

			return false;
		}

		private bool FindSwap()
		{
			AttachNode nodeToParent = part.FindAttachNodeByPart(part.parent); // always exists

			if(nodeToParent == null)
				return false; // should never happen -> no idea if swapped or not

			return !IsFixedMeshNode(nodeToParent.id);
		}

		private void InitializeMeshes(bool bCorrectMeshPositions)
		{
			// find non rotating mesh
			fixedMeshTransform = KSPUtil.FindInPartModel(transform, swap ? movingMesh : fixedMesh);
			movingMeshTransform = KSPUtil.FindInPartModel(transform, swap ? fixedMesh : movingMesh);

			// find middle meshes (only for translational joints) -> the meshes that will be shown between the moving and fixed mesh
			if(!isRotational && (middleMeshes.Length > 0))
			{
				string[] middleMeshesChunks = middleMeshes.Split('|');
				List<Transform> _middleMeshesTransform = new List<Transform>();
				for(int i = 0; i < middleMeshesChunks.Length; i++)
					_middleMeshesTransform.Add(KSPUtil.FindInPartModel(transform, middleMeshesChunks[i]));
				middleMeshesTransform = _middleMeshesTransform.ToArray();

				if(swap)
					middleMeshesTransform.Reverse();
			}

// FEHLER, das hier umbauen, dass wir das jederzeit einfach neu setzen können (also nicht relativ setzen müssen), weil -> dann könnte ich auch mit verbogenen Elementen arbeiten und mich da dynamisch dran anpassen...
// zudem bräuchte es dann den bCorrectMeshPositions nicht mehr... dazu muss ich mir dann aber wohl die Original-Positionen merken... könnte ich zwar, sobald ich den nicht-fixen Mesh hole... oder?
			if(bCorrectMeshPositions)
			{
				if(isRotational)
				{
					fixedMeshTransform.rotation *= Quaternion.AngleAxis(-(swap ? correction_0 : correction_1), axis);
					movingMeshTransform.rotation *= Quaternion.AngleAxis(-(swap ? correction_1 : correction_0), axis);
				}
				else
				{
					fixedMeshTransform.Translate(axis.normalized * (-(swap ? correction_0 : correction_1)));
					movingMeshTransform.Translate(axis.normalized * (-(swap ? correction_1 : correction_0)));
				}
			}

			if(HighLogic.LoadedSceneIsFlight)
			{
// FEHLER, wozu eigentlich?
				fixedMeshAnchor = GameObject.CreatePrimitive(PrimitiveType.Cube);
				fixedMeshAnchor.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
				fixedMeshAnchor.SetActive(true);

				DestroyImmediate(fixedMeshAnchor.GetComponent<Collider>());
				fixedMeshAnchor.GetComponent<Renderer>().enabled = false;

				Rigidbody rb = fixedMeshAnchor.AddComponent<Rigidbody>();
				rb.mass = 1e-6f;
				rb.useGravity = false;

				fixedMeshAnchor.transform.position = fixedMeshTransform.parent.position;
				fixedMeshAnchor.transform.rotation = fixedMeshTransform.parent.rotation;
				fixedMeshAnchor.transform.parent = ((Joint.gameObject == part.gameObject) ? Joint.transform : Joint.connectedBody.transform);

				fixedMeshTransform.parent = fixedMeshAnchor.transform;

				FixedJoint fj = fixedMeshAnchor.AddComponent<FixedJoint>();
				fj.connectedBody = ((Joint.gameObject == part.gameObject) ? Joint.connectedBody : part.rb);
			}
		}

		private void InitializeDrive()
		{
			// [https://docs.nvidia.com/gameworks/content/gameworkslibrary/physx/guide/Manual/Joints.html]
			// force = spring * (targetPosition - position) + damping * (targetVelocity - velocity)

			if(mode == ModeType.servo)
			{
				JointDrive drive = new JointDrive
				{
					maximumForce = isLocked ? PhysicsGlobals.JointForce : (isFreeMoving ? 1e-20f : (forceLimit * factorForce)),
					positionSpring = hasSpring ? jointSpring : 60000f,
					positionDamper = hasSpring ? jointDamping : 0f
				};

				if(isRotational)	Joint.angularXDrive = drive;
				else				Joint.xDrive = drive;
			}
			else
			{
				Joint.angularXDrive = new JointDrive
					{
						maximumForce = isLocked ? PhysicsGlobals.JointForce : (forceLimit * factorForce),
						positionSpring = 1e-12f,
						positionDamper = jointDamping
					};
			}
		}

		private void InitializeLimits()
		{
			if(mode == ModeType.servo)
			{
				float min =
					swap ? (hasPositionLimit ? -_maxPositionLimit : -maxPosition) : (hasPositionLimit ? _minPositionLimit : minPosition);
				float max =
					swap ? (hasPositionLimit ? -_minPositionLimit : -minPosition) : (hasPositionLimit ? _maxPositionLimit : maxPosition);

				if(isRotational)
				{
					bUseDynamicLimitJoint = (hasPositionLimit || hasMinMaxPosition) && (max - min > 140);

					if(!bUseDynamicLimitJoint && (hasPositionLimit || hasMinMaxPosition))
					{
						// we only use (unity-)limits on this joint for parts with a small range (because of the 177° limits in unity)

						SoftJointLimit lowAngularXLimit = new SoftJointLimit() { limit = -max - (swap ? correction_1-correction_0 : correction_0-correction_1) };
						SoftJointLimit highAngularXLimit = new SoftJointLimit() { limit = -min - (swap ? correction_1-correction_0 : correction_0-correction_1) };

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

					trans_zero = -jointconnectedzero + (swap ? max - halfrange : min + halfrange);

					Vector3 _axis = Joint.transform.InverseTransformVector(
						part.transform.TransformVector(axis)); // FEHLER, beschreiben wieso -> joint inverse (nicht part, nur config-joint)
					Joint.connectedAnchor = Joint.connectedBody.transform.InverseTransformPoint(
						Joint.transform.TransformPoint(_axis.normalized * (trans_zero - position)));

					Joint.targetPosition = Vector3.right * (trans_zero - commandedPosition); // move always along x axis

					Joint.linearLimit = new SoftJointLimit{ limit = halfrange };

					// add stability joints
					if(bBuildStabilityJoint)
					{
						for(int i = 0; i < 1 /*2*/; i++)
						{
							if(StabilityJoint[i])
								continue;

							StabilityJoint[i] = gameObject.AddComponent<ConfigurableJoint>();
							// FEHLER, hier das mit dem schwereren RigidBody wählen... denke doch, oder? -> tut ksp zwar auch nicht... *hmm* -> aber KJR schon

							StabilityJoint[i].breakForce = Joint.breakForce;
							StabilityJoint[i].breakTorque = Joint.breakTorque;
							StabilityJoint[i].connectedBody = Joint.connectedBody;

							StabilityJoint[i].axis = axis;
							StabilityJoint[i].secondaryAxis = pointer;

							StabilityJoint[i].rotationDriveMode = RotationDriveMode.XYAndZ;

							StabilityJoint[i].angularXDrive = StabilityJoint[i].angularYZDrive =
							StabilityJoint[i].yDrive = StabilityJoint[i].zDrive =
							StabilityJoint[i].xDrive = new JointDrive { maximumForce = 0f, positionSpring = 0f, positionDamper = 0f };

							StabilityJoint[i].angularXMotion = ConfigurableJointMotion.Limited;
							StabilityJoint[i].angularYMotion = ConfigurableJointMotion.Limited;
							StabilityJoint[i].angularZMotion = ConfigurableJointMotion.Limited;
							StabilityJoint[i].xMotion = ConfigurableJointMotion.Free;
							StabilityJoint[i].yMotion = ConfigurableJointMotion.Limited;
							StabilityJoint[i].zMotion = ConfigurableJointMotion.Limited;

							StabilityJoint[i].autoConfigureConnectedAnchor = false;
							StabilityJoint[i].anchor = Joint.anchor;


							StabilityJoint[i].connectedAnchor =
								Joint.connectedBody.transform.InverseTransformPoint(
									StabilityJoint[i].transform.position
									+ Vector3.Project(StabilityJoint[i].connectedBody.transform.position - StabilityJoint[i].transform.position, StabilityJoint[i].transform.TransformDirection(StabilityJoint[i].axis)));

							StabilityJoint[i].configuredInWorldSpace = false;
						}
					}
				}

				min += (swap ? correction_1-correction_0 : correction_0-correction_1);
				max += (swap ? correction_1-correction_0 : correction_0-correction_1);

				bool isModulo = isRotational && !hasMinMaxPosition && !hasPositionLimit
					&& ((mode == ModeType.servo) && (inputMode != InputModeType.control));

				ip.isModulo = isModulo;
				ip.minPosition = min;
				ip.maxPosition = max;
			}
			else
			{
				if(LimitJoint)
				{
					Destroy(LimitJoint);
					LimitJoint = null;
				}
			}
		}

		private void Initialize1()
		{
			InitializeValues();

			bool bCorrectMeshPositions = (Joint == null);

			Joint = part.attachJoint.Joint;

			// detect attachment mode and calculate correction angles
			if(swap != FindSwap())
			{
				swap = !swap;

				if(swap)
					correction_1 += (commandedPosition + lockPosition);
				else
					correction_0 += (commandedPosition + lockPosition);
			}
			else
			{
				if(swap)
					correction_0 += (commandedPosition + lockPosition);
				else
					correction_1 += (commandedPosition + lockPosition);
			}
			commandedPosition = -lockPosition;


			commandedPositionCorrection = 0f;

			while(CommandedPosition >= 360f)
				commandedPositionCorrection += -360f;
			while(CommandedPosition <= -360f)
				commandedPositionCorrection += 360f;

			positionCorrection = 0f;

			while(Position >= 360f)
				positionCorrection += -360f;
			while(Position <= -360f)
				positionCorrection += 360f;


			requestedPosition = CommandedPosition;

			position = 0f;
			lastUpdatePosition = 0f;

			// reset workaround
			if(fixedMeshTransform)
				fixedMeshTransform.parent = fixedMeshTransformParent;

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
				Joint.autoConfigureConnectedAnchor = false;

				// set anchor
				Joint.anchor = Vector3.zero;

				// correct connectedAnchor
				Joint.connectedAnchor = Joint.connectedBody.transform.InverseTransformPoint(Joint.transform.TransformPoint(Vector3.zero));

				// set axis
				Joint.axis = axis;
				Joint.secondaryAxis = pointer;
			}
			else
			{
				Joint.autoConfigureConnectedAnchor = false;

				// set anchor
				Joint.anchor = Joint.transform.InverseTransformPoint(Joint.connectedBody.transform.TransformPoint(Vector3.zero));

				// correct connectedAnchor
				Joint.connectedAnchor = Vector3.zero;

				// set axis
				Joint.axis = Joint.connectedBody.transform.InverseTransformDirection(part.transform.TransformDirection(axis));
				Joint.secondaryAxis = Joint.connectedBody.transform.InverseTransformDirection(part.transform.TransformDirection(pointer));
			}

			// determine best way to calculate real rotation
			if(isRotational)
			{
				bool useJointUp =
					Vector3.ProjectOnPlane(Joint.transform.up.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude >
					Vector3.ProjectOnPlane(Joint.transform.right.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude;

				bool useConnectedUp =
							Vector3.ProjectOnPlane(Joint.connectedBody.transform.up.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude >
							Vector3.ProjectOnPlane(Joint.connectedBody.transform.right.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude;

				rot_jointup = Joint.transform.InverseTransformVector(
					Vector3.ProjectOnPlane(useJointUp ? Joint.transform.up.normalized : Joint.transform.right.normalized, Joint.transform.TransformDirection(Joint.axis)).normalized);

				rot_connectedup = Joint.connectedBody.transform.InverseTransformVector(
					Vector3.ProjectOnPlane(useConnectedUp ? Joint.connectedBody.transform.up.normalized : Joint.connectedBody.transform.right.normalized, Joint.transform.TransformDirection(Joint.axis)).normalized);

				jointconnectedzero = -Vector3.SignedAngle(
					Joint.transform.TransformVector(rot_jointup), Joint.connectedBody.transform.TransformVector(rot_connectedup),
						Joint.transform.TransformDirection(Joint.axis));
			}
			else
			{
				jointconnectedzero = (swap ? correction_0 : correction_1);
				
				trans_connectedzero = Joint.connectedBody.transform.InverseTransformPoint(
					Joint.transform.TransformPoint(Joint.anchor) + (Joint.transform.TransformDirection(Joint.axis).normalized * (-jointconnectedzero + minPosition)));
			}

			Initialize2();
		}

		private void Initialize2()
		{
			if(mode == ModeType.servo)
			{
				ip.Initialize(commandedPosition, isRotational ? resetPrecisionRotational : resetPrecisionTranslational);

				ip.maxSpeed = speedLimit * factorSpeed;
				ip.maxAcceleration = accelerationLimit * factorAcceleration;
			}

			Joint.rotationDriveMode = RotationDriveMode.XYAndZ;

			// we don't modify *Motion, angular*Motion and the drives we don't need
				// -> KSP defaults are ok for us

			if(mode == ModeType.servo)
			{
				if(isRotational)
					Joint.angularXMotion = (isFreeMoving && !bUseDynamicLimitJoint) ? ConfigurableJointMotion.Limited : ConfigurableJointMotion.Free;
				else
					Joint.xMotion = ConfigurableJointMotion.Limited;

				Joint.targetAngularVelocity = Vector3.zero;
			}
			else
				Joint.angularXMotion = ConfigurableJointMotion.Free;

			InitializeDrive();

			InitializeLimits();
			
			Joint.enableCollision = false;
			Joint.enablePreprocessing = false;

			Joint.projectionMode = JointProjectionMode.None;

			FixChildrenAttachement();
		}

		private void BuildLimitJoint(bool p_bLowerLimitJoint, float p_min, float p_max)
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

			SoftJointLimit lowAngularXLimit, highAngularXLimit;

			if(p_bLowerLimitJoint)
			{
				lowAngularXLimit = new SoftJointLimit() { limit = -170 };
				highAngularXLimit = new SoftJointLimit() { limit = -(p_min - position) };
			}
			else
			{
				lowAngularXLimit = new SoftJointLimit() { limit = -(p_max - position) };
				highAngularXLimit = new SoftJointLimit() { limit = 170 };
			}

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

			bLowerLimitJoint = p_bLowerLimitJoint;
		}

		private void CopyJointSettings(ConfigurableJoint newJoint, ConfigurableJoint oldJoint)
		{
			newJoint.xMotion = oldJoint.xMotion;
			newJoint.yMotion = oldJoint.yMotion;
			newJoint.zMotion = oldJoint.zMotion;

			newJoint.angularXMotion = oldJoint.xMotion;
			newJoint.angularYMotion = oldJoint.yMotion;
			newJoint.angularZMotion = oldJoint.zMotion;

			newJoint.xDrive = oldJoint.xDrive;
			newJoint.yDrive = oldJoint.yDrive;
			newJoint.zDrive = oldJoint.zDrive;

			newJoint.rotationDriveMode = oldJoint.rotationDriveMode;
			newJoint.angularXDrive = oldJoint.angularXDrive;
			newJoint.angularYZDrive = oldJoint.angularYZDrive;

			newJoint.linearLimit = oldJoint.linearLimit;
			newJoint.linearLimitSpring = oldJoint.linearLimitSpring;

			newJoint.highAngularXLimit = oldJoint.highAngularXLimit;
			newJoint.lowAngularXLimit = oldJoint.lowAngularXLimit;
			newJoint.angularYLimit = oldJoint.angularYLimit;
			newJoint.angularZLimit = oldJoint.angularZLimit;
			newJoint.angularXLimitSpring = oldJoint.angularXLimitSpring;
			newJoint.angularYZLimitSpring = oldJoint.angularYZLimitSpring;

			newJoint.targetPosition = oldJoint.targetPosition;
			newJoint.targetVelocity = oldJoint.targetVelocity;
			newJoint.targetRotation = oldJoint.targetRotation;
			newJoint.targetAngularVelocity = oldJoint.targetAngularVelocity;

			newJoint.breakForce = oldJoint.breakForce;
			newJoint.breakTorque = oldJoint.breakTorque;
			newJoint.configuredInWorldSpace = oldJoint.configuredInWorldSpace;
		}

		private void FixChildrenAttachement()
		{
			foreach(Part child in part.children)
			{
				AttachNode nodeToChild = part.FindAttachNodeByPart(child);

				if(nodeToChild == null)
					continue; // ignore this one / should never happen

				bool bToFixedMesh = false;

				string[] nodeIds = fixedMeshNode.Split('|');
				foreach(string nodeId in nodeIds)
				{
					if(nodeToChild.id == nodeId)
						bToFixedMesh = true;
				}

				if(bToFixedMesh != swap)
				{
					if(HighLogic.LoadedSceneIsEditor)
						child.transform.parent = part.transform.parent;
					else
					{
						if(child.attachJoint.Host == child)
						{
							foreach(ConfigurableJoint joint in child.attachJoint.joints)
							{
								joint.connectedAnchor = part.parent.Rigidbody.transform.InverseTransformPoint(joint.connectedBody.transform.TransformPoint(joint.connectedAnchor));
								joint.connectedBody = part.parent.Rigidbody;
							}
						}
						else
						{
							List<ConfigurableJoint> joints = new List<ConfigurableJoint>();

							foreach(ConfigurableJoint oldJoint in child.attachJoint.joints)
							{
								ConfigurableJoint newJoint = part.parent.gameObject.AddComponent<ConfigurableJoint>();

								newJoint.axis = part.parent.partTransform.InverseTransformDirection(part.partTransform.TransformDirection(oldJoint.axis));
								newJoint.secondaryAxis = part.parent.partTransform.InverseTransformDirection(part.partTransform.TransformDirection(oldJoint.secondaryAxis));

								newJoint.connectedBody = oldJoint.connectedBody;

								newJoint.autoConfigureConnectedAnchor = false;
								newJoint.anchor = part.parent.partTransform.InverseTransformPoint(part.partTransform.TransformPoint(oldJoint.anchor));
								newJoint.connectedAnchor = oldJoint.connectedAnchor;

								CopyJointSettings(newJoint, oldJoint);

								joints.Add(newJoint);

								UnityEngine.Object.Destroy(oldJoint);
							}

							child.attachJoint.joints = joints;
						}
					}
				}
			}
		}

		private void UpdateMaxPowerDrawRate()
		{
			if(mode == ModeType.servo)
			{
				powerDrawRateBase = (4.5f / (factorForce * factorSpeed)) * electricChargeRequired;
					// why these factors? -> they seem to be a good value -> makes our consumption around the same as stock

				powerDrawRateBase *= (ForceLimit / maxForce) * factorForce;
				powerDrawRateBase *= (1f / maxSpeed);

				powerDrawRateBase *= (0.01f * (60f + motorSizeFactor * 0.4f));

				maxPowerDrawRate = powerDrawRateBase * speedLimit * factorSpeed;
			}
			else
			{
				powerDrawRateBase = (0.192f / factorSpeed) * electricChargeRequired;
					// why these factors? -> they seem to be a good value -> makes our consumption around the same as stock

				powerDrawRateBase *= (ForceLimit / maxForce) * factorForce;
				powerDrawRateBase *= (1f / maxSpeed);

				powerDrawRateBase *= (0.01f * (60f + motorSizeFactor * 0.4f));

				maxPowerDrawRate = powerDrawRateBase * 2.6f * speedLimit * factorSpeed;
			}

			if(maxPowerDrawRate >= 1f)
			{
				MaxPowerDrawRate = maxPowerDrawRate;
				Fields["MaxPowerDrawRate"].guiUnits = "u/s";
				Fields["MaxPowerDrawRate"].guiFormat = "F2";
			}
			else
			{
				MaxPowerDrawRate = maxPowerDrawRate * 1000f;
				Fields["MaxPowerDrawRate"].guiUnits = "mu/s";
				Fields["MaxPowerDrawRate"].guiFormat = "0";
			}
		}

		private bool UpdateAndConsumeElectricCharge()
		{
			if((electricChargeRequired == 0f) || isFreeMoving)
				return true;

			if(!hasElectricPower)
				return false;

			if(mode == ModeType.servo)
			{
				float fixedDeltaTime = TimeWarp.fixedDeltaTime;

				if(trackSun)
					fixedDeltaTime /= TimeWarp.CurrentRate;

				ip.ResetPosition(position);				// FEHLER, das müssten wir auch tun, wenn nichts läuft -> wenn der Joint überdehnt wird, kommt er sonst über 90° vom target weg und dann dreht die Engine durch -> daher das hier auch bei nicht-Bewegung tun!!!
				ip.PrepareUpdate(fixedDeltaTime);

				double amountToConsume = powerDrawRateBase * fixedDeltaTime * 0.5f * (ip.NewSpeed + ip.Speed);
// FEHLER, bei Beschleunigung zusätzlich Strom ziehen, dafür bei Bewegung nicht so sehr?

				double amountConsumed = part.RequestResource(electricResource.id, amountToConsume);

				LastPowerDrawRate = (float)(1000f * amountConsumed / fixedDeltaTime);

				if(LastPowerDrawRate >= 1000f)
				{
					LastPowerDrawRate /= 1000f;
					Fields["LastPowerDrawRate"].guiUnits = "u/s";
					Fields["LastPowerDrawRate"].guiFormat = "F2";
				}
				else
				{
					Fields["LastPowerDrawRate"].guiUnits = "mu/s";
					Fields["LastPowerDrawRate"].guiFormat = "0";
				}

				return (amountConsumed >= amountToConsume * 0.95) | (ip.TargetPosition != position);
			}
			else
			{
				double amountToConsume = powerDrawRateBase * TimeWarp.fixedDeltaTime * Math.Abs(Joint.targetAngularVelocity.x) * factorSpeed;

				double amountConsumed = part.RequestResource(electricResource.id, amountToConsume);

				LastPowerDrawRate = (float)(1000f * amountConsumed / TimeWarp.fixedDeltaTime);

				if(LastPowerDrawRate >= 1000f)
				{
					LastPowerDrawRate /= 1000f;
					Fields["LastPowerDrawRate"].guiUnits = "u/s";
					Fields["LastPowerDrawRate"].guiFormat = "F2";
				}
				else
				{
					Fields["LastPowerDrawRate"].guiUnits = "mu/s";
					Fields["LastPowerDrawRate"].guiFormat = "0";
				}

				return amountConsumed >= amountToConsume * 0.95;
			}
		}

		private bool IsStopping()
		{
			if(ip.IsStopping)
				return true;

			bool bRes = ip.Stop();

			// Stop changed the direction -> we need to recalculate this now
			ip.ResetPosition(position);
			ip.PrepareUpdate(TimeWarp.fixedDeltaTime);

			return bRes;
		}

		private void UpdateChildPositionRot(Part current, Quaternion relRot)
		{
			foreach(Part child in current.children)
			{
				child.orgPos = part.orgPos + relRot * (child.orgPos - part.orgPos);
				child.orgRot = (relRot * child.orgRot).normalized;
				UpdateChildPositionRot(child, relRot);
			}
		}

		private void UpdateChildPositionTrans(Part current, Vector3 relTrans)
		{
			foreach(Part child in current.children)
			{
				child.orgPos = child.orgPos + relTrans;
				UpdateChildPositionTrans(child, relTrans);
			}
		}

		// set original rotation to new rotation
		private void UpdatePosition()
		{
			if(Mathf.Abs(commandedPosition - lastUpdatePosition) < 0.005f)
				return;

			if(isRotational)
			{
				Quaternion relRot = Quaternion.AngleAxis(commandedPosition - lastUpdatePosition, part.orgRot * axis);

				part.orgRot = (relRot * part.orgRot).normalized;
				UpdateChildPositionRot(part, relRot);
			}
			else
			{
				Vector3 relTrans = part.orgRot * axis.normalized * (commandedPosition - lastUpdatePosition);

				part.orgPos = part.orgPos + relTrans;
				UpdateChildPositionTrans(part, relTrans);
			}

			lastUpdatePosition = commandedPosition;
		}

		private float TransformPosition(float position)
		{
			if(!isInverted)
				return (swap ? -1.0f : 1.0f) * (position - zeroNormal - correction_1 + correction_0);
			else
				return (swap ? 1.0f : -1.0f) * (position - zeroInvert + correction_1 - correction_0);
		}

		private float InverseTransformPosition(float position)
		{
			if(!isInverted)
				return (swap ? -position : position) + zeroNormal + correction_1 - correction_0;
			else
				return (swap ? position : -position) + zeroInvert - correction_1 + correction_0;
		}

		private void SetColor(int status)
		{
			if(isLocked)
			{ if(lightStatus != 0) { lightStatus = 0; lightRenderer.material.SetColor(lightColorId, lightColorLocked); } }
			else
			{
				switch(mode)
				{
					case ModeType.servo:
						switch(inputMode)
						{
							case InputModeType.manual:
							case InputModeType.linked:
								if(lightStatus != status) { lightStatus = status; lightRenderer.material.SetColor(lightColorId, (status == 1) ? lightColorIdle : lightColorMoving); }
								break;

							case InputModeType.control:
								if(lightStatus != 4) { lightStatus = 4; lightRenderer.material.SetColor(lightColorId, lightColorControl); }
								break;

							case InputModeType.tracking:
								if(lightStatus != 5) { lightStatus = 5; lightRenderer.material.SetColor(lightColorId, lightColorTracking); }
								break;
						}
						break;

					case ModeType.rotor:
						if(lightStatus != 3) { lightStatus = 3; lightRenderer.material.SetColor(lightColorId, lightColorRotor); }
						break;
				}
			}
		}

		public void MoveMeshes(float targetPosition)
		{
			if(isRotational)
			{
				fixedMeshTransform.Rotate(axis, targetPosition);
				movingMeshTransform.Rotate(axis, targetPosition);
			}
			else
			{
				fixedMeshTransform.Translate(axis.normalized * targetPosition);
				movingMeshTransform.Translate(axis.normalized * targetPosition);
			}
		}

		public void MoveChildren(float targetPosition)
		{
			if(isRotational)
			{
				Quaternion rot = Quaternion.AngleAxis(targetPosition, axis);

				foreach(Part child in part.children)
				{
					child.transform.localRotation = rot * child.transform.localRotation;
					child.transform.localPosition = rot * child.transform.localPosition;
				}
			}
			else
			{
				Vector3 trans = axis.normalized * targetPosition;

				foreach(Part child in part.children)
				{
					child.transform.localPosition += trans;
				}
			}
		}

		public void MoveAttachNode(AttachNode node, Quaternion rot)
		{
			node.offset = rot * node.offset;
			node.originalPosition = rot * node.originalPosition;
			node.originalOrientation = rot * node.originalOrientation;
			node.position = rot * node.position;
			node.orientation = rot * node.orientation;
			node.originalSecondaryAxis = rot * node.originalSecondaryAxis;
			node.secondaryAxis = rot * node.secondaryAxis;
		}

		public void MoveAttachNode(AttachNode node, Vector3 trans)
		{
			node.originalPosition += trans;
			node.originalOrientation += trans;
			node.position += trans;
			node.orientation += trans;
			node.originalSecondaryAxis += trans;
			node.secondaryAxis += trans;
		}
		
		public void MoveAttachNodes(float targetPosition, bool bOnFixedSide)
		{
			if(isRotational)
			{
				Quaternion rot = Quaternion.AngleAxis(targetPosition, axis);

				foreach(AttachNode node in part.attachNodes)
				{
					if(bOnFixedSide == IsFixedMeshNode(node.id))
						MoveAttachNode(node, rot);
				}

				if(bOnFixedSide == IsFixedMeshNode(part.srfAttachNode.id))
					MoveAttachNode(part.srfAttachNode, rot);
			}
			else
			{
				Vector3 trans = axis.normalized * targetPosition;

				foreach(AttachNode node in part.attachNodes)
				{
					if(bOnFixedSide == IsFixedMeshNode(node.id))
						MoveAttachNode(node, trans);
				}

				if(bOnFixedSide == IsFixedMeshNode(part.srfAttachNode.id))
					MoveAttachNode(part.srfAttachNode, trans);
			}
		}

		public void MoveAttachNodes(float targetPosition)
		{
			if(isRotational)
			{
				Quaternion rot = Quaternion.AngleAxis(targetPosition, axis);

				foreach(AttachNode node in part.attachNodes)
					MoveAttachNode(node, rot);

				MoveAttachNode(part.srfAttachNode, rot);
			}
			else
			{
				Vector3 trans = axis.normalized * targetPosition;

				foreach(AttachNode node in part.attachNodes)
					MoveAttachNode(node, trans);

				MoveAttachNode(part.srfAttachNode, trans);
			}
		}

		private void ResetAttachNode(AttachNode node, AttachNode baseNode, float factor)
        {
			node.radius = baseNode.radius * factor;

			node.position = baseNode.position * factor;
			node.originalPosition = baseNode.originalPosition * factor;

			node.orientation = baseNode.orientation;
			node.originalOrientation = baseNode.originalOrientation;

			node.originalSecondaryAxis = baseNode.originalSecondaryAxis;
			node.secondaryAxis = baseNode.secondaryAxis;
        }

		void ResetAttachNodes()
		{
			for(int i = 0; i < part.attachNodes.Count; i++)
			{
				AttachNode node = part.attachNodes[i];

				AttachNode prefabNode = null;
				for(int j = 0; j < part.partInfo.partPrefab.attachNodes.Count; j++)
				{
					if(part.partInfo.partPrefab.attachNodes[j].id == node.id)
					{ prefabNode = part.partInfo.partPrefab.attachNodes[j]; break; }
				}

				if(node.icon != null)
					node.icon.transform.localScale = Vector3.one * prefabNode.radius * ((prefabNode.size == 0) ? ((float)prefabNode.size + 0.5f) : prefabNode.size);

                ResetAttachNode(node, prefabNode, scalingFactor);
			}

			if(part.srfAttachNode != null)
				ResetAttachNode(part.srfAttachNode, part.partInfo.partPrefab.srfAttachNode, scalingFactor);
		}

		////////////////////////////////////////
		// Update-Functions

		public void FixedUpdate()
		{
			if(!HighLogic.LoadedSceneIsFlight)
			{
				if(HighLogic.LoadedSceneIsEditor)
				{
					if(LinkedInputPart != null)
					{
						float requestedCommandedPosition = LinkedInputPart.CommandedPosition;

						if(commandedPosition != requestedCommandedPosition)
							EditorSetTo(requestedCommandedPosition);
					}

					// ?? Bug in KSP ?? we need to reset this on every frame, because highliting the parent part (in some situations) sets this to another value
					lightRenderer.SetPropertyBlock(part.mpb);

					SetColor(1);

					ProcessShapeUpdates();
				}

				return;
			}

// FEHLER, oder könnte das mit part.started geprüft werden? ... na, weiss nicht so recht... wobei, evtl. sollten wir das sonst zusätzlich prüfen?
			if(!part || !part.vessel || !part.vessel.rootPart || !Joint)
				return;

			if(isOnRails && !trackSun)
				return;

			if(part.State == PartStates.DEAD) 
				return;

			// ?? Bug in KSP ?? we need to reset this on every frame, because highliting the parent part (in some situations) sets this to another value
			lightRenderer.SetPropertyBlock(part.mpb);

			// determine current position and update variables
			// activate (dynamic) limits if needed
			
			if(isRotational)
			{
				if(ip.isModulo)
				{
					if(commandedPosition > 270f) doModulo(-360f);
					else if(commandedPosition < -270f) doModulo(360f);
				}

				// read new position
				float newPosition =
					-Vector3.SignedAngle(
						Joint.transform.TransformVector(rot_jointup), Joint.connectedBody.transform.TransformVector(rot_connectedup),
							Joint.transform.TransformDirection(Joint.axis))
					- jointconnectedzero;

				if(!float.IsNaN(newPosition))
				{
					if(newPosition < position - 270f) newPosition += 360f;
					else if(newPosition > position + 270f) newPosition -= 360f;

					position = newPosition;

					if(ip.isModulo)
						updateDisplayPosition();

					if((isFreeMoving || (mode == ModeType.rotor)) && !isLocked)
					{
						commandedPosition = Mathf.Clamp(position, _minPositionLimit, _maxPositionLimit);

						if(ip.isModulo)
							updateDisplayCommandedPosition();

						if(!requestedPositionIsDefined)
							requestedPosition = CommandedPosition;

						Joint.targetRotation = Quaternion.AngleAxis(-commandedPosition, Vector3.right); // rotate always around x axis!!
					}

					if((mode == ModeType.servo) && bUseDynamicLimitJoint)
					{
						float min = swap ? ((hasPositionLimit ? -_maxPositionLimit : -maxPosition) + correction_1 - correction_0) : ((hasPositionLimit ? _minPositionLimit : minPosition) + correction_0 - correction_1);
						float max = swap ? ((hasPositionLimit ? -_minPositionLimit : -minPosition) + correction_1 - correction_0) : ((hasPositionLimit ? _maxPositionLimit : maxPosition) + correction_0 - correction_1);

						if(min + 30 > position)
						{
							if(!bLowerLimitJoint || !LimitJoint)
								BuildLimitJoint(true, min, max);
						}
						else if(max - 30 < position)
						{
							if(bLowerLimitJoint || !LimitJoint)
								BuildLimitJoint(false, min, max);
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

				float newPosition = v2.magnitude;

				if(!float.IsNaN(newPosition) && !float.IsInfinity(newPosition))
				{
					if(swap)
						newPosition = minPosition - newPosition - jointconnectedzero;
					else
						newPosition = minPosition + newPosition - jointconnectedzero;

					// here we could further (manually) dampen the movement if we wish

					position = newPosition;

					if(isFreeMoving)
						Joint.targetPosition = Vector3.right * (trans_zero - position); // move always along x axis
				}
			}

			CurrentPosition = Position;

			// process current input

			if(mode == ModeType.servo)
			{
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

						if(ip.isModulo)
							updateDisplayCommandedPosition();

						if(!requestedPositionIsDefined)
							requestedPosition = CommandedPosition;

						if(isRotational)
							Joint.targetRotation = Quaternion.AngleAxis(-commandedPosition, Vector3.right); // rotate always around x axis
						else
							Joint.targetPosition = Vector3.right * (trans_zero - commandedPosition); // move always along x axis
					}
					else if(!IsStopping())
						ip.Stop(); // no power, we need to stop

					if(lightStatus != -1)
						SetColor(2);
				}
				else
				{
					soundSound.Stop();
					LastPowerDrawRate = 0f;
					Fields["LastPowerDrawRate"].guiUnits = "mu/s";
					Fields["LastPowerDrawRate"].guiFormat = "0";

					if(lightStatus != -1)
						SetColor(1);
				}

				if((inputMode == InputModeType.control) && hasElectricPower)
				{
					float newDeflection =
						  vessel.ctrlState.pitch * pitchControl
						+ vessel.ctrlState.roll * rollControl
						+ vessel.ctrlState.yaw * yawControl
						+ vessel.ctrlState.mainThrottle * throttleControl
						+ vessel.ctrlState.X * xControl
						+ vessel.ctrlState.Y * yControl
						+ vessel.ctrlState.Z * zControl;

					newDeflection *= 0.0001f * controlDeflectionRange;

					MoveTo(MinPosition + (controlNeutralPosition * 0.01f + Mathf.Clamp(newDeflection, -controlDeflectionRange, controlDeflectionRange)) * (MaxPosition - MinPosition), DefaultSpeed);
				}

				if((inputMode == InputModeType.tracking) && hasElectricPower)
				{
					if(trackSun)
						TrackMove();
				}
			}
			else
			{
				if(UpdateAndConsumeElectricCharge())
				{
					soundSound.Play();

					float newSpeed;

					if(isLocked)
						newSpeed = 0f;
					else
					{
						newSpeed = baseSpeed
							+ vessel.ctrlState.pitch * pitchSpeed
							+ vessel.ctrlState.roll * rollSpeed
							+ vessel.ctrlState.yaw * yawSpeed
							+ vessel.ctrlState.mainThrottle * throttleSpeed
							+ vessel.ctrlState.X * xSpeed
							+ vessel.ctrlState.Y * ySpeed
							+ vessel.ctrlState.Z * zSpeed;

						newSpeed = _isRunning * Mathf.Clamp(newSpeed, -100f, 100f) * 0.01f * 2.6f * speedLimit;

						if(isInverted)
							newSpeed *= -1.0f;
					}

					if(Math.Abs(Joint.targetAngularVelocity.x - newSpeed) > rotorAcceleration)
					{
						if(Joint.targetAngularVelocity.x > newSpeed)
							newSpeed = Joint.targetAngularVelocity.x - rotorAcceleration;
						else
							newSpeed = Joint.targetAngularVelocity.x + rotorAcceleration;
					}

					Joint.targetAngularVelocity = Vector3.right * newSpeed;
				}
				else
				{
					soundSound.Stop();

					float newSpeed = 0f;

					if(Math.Abs(Joint.targetAngularVelocity.x - newSpeed) > rotorAcceleration)
					{
						if(Joint.targetAngularVelocity.x > newSpeed)
							newSpeed = Joint.targetAngularVelocity.x - rotorAcceleration;
						else
							newSpeed = Joint.targetAngularVelocity.x + rotorAcceleration;
					}

					Joint.targetAngularVelocity = Vector3.right * newSpeed;
				}

				if(lightStatus != -1)
					SetColor(2);
			}

			// perform updates of children and other modules

			UpdatePosition();

			ProcessShapeUpdates();
		}

		public void Update()
		{
			if(!part || !part.vessel || !part.vessel.rootPart || !Joint)
				return;

			if(isOnRails)
				return;

			if(soundSound != null)
			{
				float pitchMultiplier;
				
				if(mode == ModeType.servo)
					pitchMultiplier = Math.Max(Math.Abs(CommandedSpeed / factorSpeed), 0.05f);
				else
					pitchMultiplier = Math.Max(Math.Abs(Joint.targetAngularVelocity.x) * 0.04f, 0.05f);

				if(pitchMultiplier > 1)

					pitchMultiplier = (float)Math.Sqrt(pitchMultiplier);

				soundSound.Update(soundVolume, soundPitch * pitchMultiplier);
			}

			if(HighLogic.LoadedSceneIsFlight)
			{
				double amount, maxAmount;
				part.GetConnectedResourceTotals(electricResource.id, electricResource.resourceFlowMode, out amount, out maxAmount);

				hasElectricPower = (amount > 0);

				if(!hasElectricPower)
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

		public void LateUpdate()
		{
			if((isRotational) && (middleMeshesTransform != null))
			{
				Vector3 localPosition = transform.InverseTransformPoint(fixedMeshTransform.position);
				Vector3 distance = localPosition - movingMeshTransform.localPosition;
				float fraction = 1f / (float)(middleMeshesTransform.Length + 1);
				for(int i = 0; i < middleMeshesTransform.Length; i++)
					middleMeshesTransform[i].localPosition = localPosition + distance * ((i + 1) * fraction);
			}
		}

		public Vector3 GetAxis()
		{ return Joint.transform.TransformDirection(Joint.axis).normalized; }

		public Vector3 GetSecAxis()
		{ return Joint.transform.TransformDirection(Joint.secondaryAxis).normalized; }

		public Vector3 GetAnchor()
		{ return Joint.transform.TransformPoint(Joint.anchor); }
		
		////////////////////////////////////////
		// Properties

		[KSPField(isPersistant = true)]
		public string servoName = "";

		private void onChanged_servoName(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().Name = servoName;
		}

		public string Name
		{
			get { return servoName; }
			set { if(object.Equals(servoName, value)) return; servoName = value; onChanged_servoName(null); }
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
			set
			{
				part.SetHighlight(value, false);

				for(int i = 0; i < part.symmetryCounterparts.Count; i++)
					part.symmetryCounterparts[i].SetHighlight(value, false);
			}
		}

		private readonly IPresetable presets;

		public IPresetable Presets
		{
			get { return presets; }
		}

		[KSPField(isPersistant = true)]
		public string groupName = "Default Group";

		private void onChanged_groupName(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().GroupName = groupName;
		}

		public string GroupName
		{
			get { return groupName; }
			set { if(object.Equals(groupName, value)) return; groupName = value; onChanged_groupName(null); }
		}

		////////////////////////////////////////
		// Status

		public bool IsReversed
		{
			get { return swap; }
		}

		public float TargetPosition
		{
			get { return ip.TargetPosition; }
		}

		public float TargetSpeed
		{
			get { return ip.TargetSpeed / factorSpeed; }
		}

		public float CommandedPosition
		{
			get
			{
				if(!isInverted)
					return (swap ? -commandedPosition : commandedPosition) + zeroNormal + correction_1 - correction_0 + commandedPositionCorrection;
				else
					return (swap ? commandedPosition : -commandedPosition) + zeroInvert - correction_1 + correction_0 - commandedPositionCorrection;
			}
		}

		public float CommandedSpeed
		{
			get { return ip.Speed / factorSpeed; }
		}

		// real position (corrected, when swapped or inverted)
		public float Position
		{
			get
			{
				if(!isInverted)
					return (swap ? -position : position) + zeroNormal + correction_1 - correction_0 + positionCorrection;
				else
					return (swap ? position : -position) + zeroInvert - correction_1 + correction_0 - positionCorrection;
			}
		}

		// Returns true if servo is currently moving
		public bool IsMoving
		{
			get { return ip.IsMoving; }
		}

		////////////////////////////////////////
		// Settings

		[KSPField(isPersistant = true)]
		public ModeType mode = ModeType.servo;

		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Mode"),
			UI_ChooseOption(suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public int modeIndex = 0;

		private void onChanged_modeIndex(object o)
		{
			mode = availableModes[modeIndex];

// FEHLER, evtl. anderen Preis setzen für anderen Modus?

			if(mode != ModeType.servo)
				IsLimitted = false;

			if(Joint)
				Initialize2();
			
			UpdateMaxPowerDrawRate();

			UpdateUI();

			// we need a bigger update when the mode changes
			if(Gui.WindowManager.Instance != null)
				Gui.WindowManager.Instance.Invalidate();
		}

		public ModeType Mode
		{
			get { return mode; }
			set
			{
				if(object.Equals(mode, value))
					return;

				if(!availableModes.Contains(value))
					return;

				if(HighLogic.LoadedSceneIsFlight && (IsMoving || (Joint.targetAngularVelocity.x > 0.005f)))
					return;

				modeIndex = availableModes.IndexOf(value);

				onChanged_modeIndex(null);
			}
		}

		[KSPField(isPersistant = true)]
		public InputModeType inputMode = InputModeType.manual;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "InuputMode"),
			UI_ChooseOption(suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public int inputModeIndex = 0;

		private void onChanged_inputModeIndex(object o)
		{
			inputMode = availableInputModes[inputModeIndex];

			if(inputMode != InputModeType.linked)
			{
				if(LinkedInputPart != null)
				{
					LinkedInputPartId = 0;
					LinkedInputPartFlightId = 0;
					LinkedInputPart.Unlink(this);
					LinkedInputPart = null;
				}
			}

			if(inputMode != InputModeType.tracking)
				trackSun = false;

			UpdateUI();

			// we need a bigger update when the inputMode changes
			if(Gui.WindowManager.Instance != null)
				Gui.WindowManager.Instance.Invalidate();
		}

		public InputModeType InputMode
		{
			get { return inputMode; }
			set
			{
				if(object.Equals(inputMode, value))
					return;

				if(!availableInputModes.Contains(value))
					return;

				inputModeIndex = availableInputModes.IndexOf(value);

				onChanged_inputModeIndex(null);
			}
		}

		[KSPField(isPersistant = true)]
		public float lockPosition = 0f;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Lock"),
			UI_Toggle(enabledText = "Engaged", disabledText = "Disengaged", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public bool isLocked = false;

		private void onChanged_isLocked(object o)
		{
			if(HighLogic.LoadedSceneIsFlight)
			{
				GameEvents.onRoboticPartLockChanging.Fire(part, isLocked);

				if(isLocked)
				{
					Stop();

					lockPosition = position - commandedPosition;
				}
				else
					lockPosition = 0f;

				if(isRotational)
					Joint.targetRotation = Quaternion.AngleAxis(-(commandedPosition + lockPosition), Vector3.right); // rotate always around x axis
				else
					Joint.targetPosition = Vector3.right * (trans_zero - (commandedPosition + lockPosition)); // move always along x axis

				InitializeDrive();

				vessel.CycleAllAutoStrut();
				GameEvents.onRoboticPartLockChanged.Fire(part, isLocked);
			}

			UpdateUI();
		}

		public bool IsLocked
		{
			get { return isLocked; }
			set
			{
				if(object.Equals(isLocked, value))
					return;

				isLocked = value;

				onChanged_isLocked(null);
			}
		}

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Invert Direction"),
			UI_Toggle(enabledText = "Inverted", disabledText = "Normal", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
		public bool isInverted = false;

		private void onChanged_isInverted(object o)
		{
			minmaxPositionLimit.x = MinPositionLimit;
			minmaxPositionLimit.y = MaxPositionLimit;

			if(part.symmetryCounterparts.Count == 0)
				requestedPosition = CommandedPosition;
			else
				onChanged_requestedPosition(null);

			UpdateUI();
		}

		public bool IsInverted
		{
			get { return isInverted; }
			set { if(object.Equals(isInverted, value)) return; isInverted = value; onChanged_isInverted(null); }
		}

		////////////////////////////////////////
		// Settings (servo)

		public List<float> PresetPositions
		{
			get;
			set;
		}

		public void AddPresetPosition(float position)
		{
			PresetPositions.Add(position);

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().PresetPositions = new List<float>(PresetPositions);
		}

		public void RemovePresetPositionsAt(int presetIndex)
		{
			PresetPositions.RemoveAt(presetIndex);

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().PresetPositions = new List<float>(PresetPositions);
		}

		public void SortPresetPositions(IComparer<float> sorter = null)
		{
			if(sorter != null)
				PresetPositions.Sort(sorter);
			else
				PresetPositions.Sort();

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().PresetPositions = new List<float>(PresetPositions);
		}

// FEHLER, werden die je genutzt? und wenn nicht -> werden sie überall korrekt angewendet? weil, wären sie immer 0, würde das ja nicht auffallen...
		[KSPField(isPersistant = false), SerializeField]
		public float zeroNormal = 0;
		[KSPField(isPersistant = false), SerializeField]
		public float zeroInvert = 0;

		[KSPField(isPersistant = true)]
		public float defaultPosition = 0f;

		private void onChanged_defaultPosition(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().DefaultPosition = defaultPosition;
		}

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
				if(object.Equals(DefaultPosition, value))
					return;

				if(!isInverted)
					defaultPosition = Mathf.Clamp(value, _minPositionLimit, _maxPositionLimit);
				else
					defaultPosition = Mathf.Clamp(zeroInvert - value, _minPositionLimit, _maxPositionLimit);

				onChanged_defaultPosition(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Force", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0.1f),
			UI_FloatRange(minValue = 0.1f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float forceLimit = 1f;

		private void onChanged_forceLimit(object o)
		{
			UpdateMaxPowerDrawRate();

			if(Joint)
				InitializeDrive();
		}

		public float ForceLimit
		{
			get { return forceLimit; }
			set
			{
				value = Mathf.Clamp(value, 0.1f, MaxForce);

				if(object.Equals(forceLimit, value))
					return;

				forceLimit = value;

				onChanged_forceLimit(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Acceleration", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0.1f),
			UI_FloatRange(minValue = 0.1f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float accelerationLimit = 4f;

		private void onChanged_accelerationLimit(object o)
		{
			ip.maxAcceleration = accelerationLimit * factorAcceleration;
		}

		public float AccelerationLimit
		{
			get { return accelerationLimit; }
			set
			{
				value = Mathf.Clamp(value, 0.1f, MaxAcceleration);

				if(object.Equals(accelerationLimit, value))
					return;

				accelerationLimit = value;

				onChanged_accelerationLimit(null);
			}
		}

		[KSPField(isPersistant = true)]
		public float defaultSpeed = 0f;

		public float DefaultSpeed
		{
			get { return defaultSpeed < 0.1f ? speedLimit : Mathf.Clamp(defaultSpeed, 0.1f, speedLimit); }
			set { defaultSpeed = value; }
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max Speed", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0.1f),
			UI_FloatRange(minValue = 0.1f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float speedLimit = 1f;

		private void onChanged_speedLimit(object o)
		{
			UpdateMaxPowerDrawRate();

			ip.maxSpeed = speedLimit * factorSpeed;
		}

		public float SpeedLimit
		{
			get { return speedLimit; }
			set
			{
				value = Mathf.Clamp(value, 0.1f, MaxSpeed);

				if(object.Equals(speedLimit, value))
					return;

				speedLimit = value;

				onChanged_speedLimit(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Force", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0f),
			UI_FloatRange(minValue = 0f, stepIncrement = 0.1f, suppressEditorShipModified = true, scene = UI_Scene.Editor, affectSymCounterparts = UI_Scene.All)]
		public float jointSpring = PhysicsGlobals.JointForce;

		private void onChanged_jointSpring(object o)
		{
			if(Joint)
				InitializeDrive();
		}

		public float SpringPower 
		{
			get { return jointSpring; }
			set { if(object.Equals(jointSpring, value)) return; jointSpring = value; onChanged_jointSpring(null); }
		}

		[KSPAxisField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Damping Force", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0f),
			UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, scene = UI_Scene.Editor, affectSymCounterparts = UI_Scene.All)]
		public float jointDamping = 5f;

		private void onChanged_jointDamping(object o)
		{
			if(Joint)
				InitializeDrive();
		}

		public float DampingPower 
		{
			get { return jointDamping; }
			set { if(object.Equals(jointDamping, value)) return; jointDamping = value; onChanged_jointDamping(null); }
		}

		[KSPField(isPersistant = false), SerializeField]
		private float electricChargeRequired = 2.5f;

		private float powerDrawRateBase;

		private float maxPowerDrawRate;

		[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Max Power Consumption", guiFormat = "F1", guiUnits = "mu/s")]
		private float MaxPowerDrawRate;

		[KSPAxisField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Motor Size", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = 20f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = 20f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float motorSizeFactor = 100f;

		private void onChanged_motorSizeFactor(object o)
		{
			ForceLimit = forceLimit;
			RotorAcceleration = rotorAcceleration;
			AccelerationLimit = accelerationLimit;
			SpeedLimit = speedLimit;

			ModuleIRServo_v3 prefab = part.partInfo.partPrefab.GetComponent<ModuleIRServo_v3>();

			part.mass = prefab.part.mass * Mathf.Pow(scalingFactor, scaleMass) * (0.01f * (75f + motorSizeFactor * 0.25f));

			electricChargeRequired = prefab.electricChargeRequired * Mathf.Pow(scalingFactor, scaleElectricChargeRequired) * (0.01f * (80f + motorSizeFactor * 0.2f));

			UpdateMaxPowerDrawRate();

			UpdateUI();

			GameEvents.onEditorShipModified.Fire(EditorLogic.fetch.ship);
		}

		public float MotorSizeFactor
		{
			get { return motorSizeFactor; }
			set { if(object.Equals(motorSizeFactor, value)) return; motorSizeFactor = value; onChanged_motorSizeFactor(null); }
		}

		// limits set by the user

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Limits"),
			UI_Toggle(enabledText = "Engaged", disabledText = "Disengaged", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public bool hasPositionLimit = false;

		private void onChanged_hasPositionLimit(object o)
		{
			if(hasPositionLimit)
			{
				if(!canHaveLimits
				|| (!isFreeMoving && IsMoving)
				|| (mode != ModeType.servo))
				{
					hasPositionLimit = false;
					return;
				}

				// we do update the limits, when we are not between them
				if(CommandedPosition < MinPositionLimit)
					MinPositionLimit = CommandedPosition;

				if(CommandedPosition > MaxPositionLimit)
					MaxPositionLimit = CommandedPosition;
			}

			if(Joint)
				InitializeLimits();

			UpdateUI();
		}

		public bool IsLimitted
		{
			get { return hasPositionLimit; }
			set { if(object.Equals(hasPositionLimit, value)) return; hasPositionLimit = value; onChanged_hasPositionLimit(null); }
		}

		public void ToggleLimits()
		{ IsLimitted = !IsLimitted; }

		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Limits", guiFormat = "F2", guiUnits = ""),
			UI_MinMaxRange(minValueX = 0f, minValueY = 0.5f, maxValueX = 179.5f, maxValueY = 180f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private Vector2 minmaxPositionLimit = new Vector2(-360f, 360f);

		private void onChanged_minmaxPositionLimit(object o)
		{
			MinPositionLimit = minmaxPositionLimit.x;
			MaxPositionLimit = minmaxPositionLimit.y;

			UpdateUI();
		}

		[KSPField(isPersistant = true)]
		public float _minPositionLimit = -360f;

		public float MinPositionLimit
		{
			get
			{
				if(!isInverted)
					return _minPositionLimit;
				else
					return zeroInvert - _maxPositionLimit;
			}
			set
			{
			retry:
				if(!isInverted)
				{
					value = Mathf.Clamp(value, minPosition, _maxPositionLimit);

					if(object.Equals(_minPositionLimit, value))
						return;

					_minPositionLimit = value;
				}
				else
				{
					value = Mathf.Clamp(zeroInvert - value, _minPositionLimit, maxPosition);

					if(object.Equals(_maxPositionLimit, value))
						return;

					_maxPositionLimit = value;
				}

				if(CommandedPosition < MinPositionLimit)
				{
					value = CommandedPosition;
					goto retry;
				}

				minmaxPositionLimit.x = MinPositionLimit;

				if(Joint)
					InitializeLimits();

				for(int i = 0; i < part.symmetryCounterparts.Count; i++)
					part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().MinPositionLimit = MinPositionLimit;
			}
		}

		[KSPField(isPersistant = true)]
		public float _maxPositionLimit = 360f;

		public float MaxPositionLimit
		{
			get
			{
				if(!isInverted)
					return _maxPositionLimit;
				else
					return zeroInvert - _minPositionLimit;
			}
			set
			{
			retry:
				if(!isInverted)
				{
					value = Mathf.Clamp(value, _minPositionLimit, maxPosition);

					if(object.Equals(_maxPositionLimit, value))
						return;

					_maxPositionLimit = value;
				}
				else
				{
					value = Mathf.Clamp(zeroInvert - value, minPosition, _maxPositionLimit);

					if(object.Equals(_minPositionLimit, value))
						return;
	
					_minPositionLimit = value;
				}

				if(CommandedPosition > MaxPositionLimit)
				{
					value = CommandedPosition;
					goto retry;
				}

				minmaxPositionLimit.y = MaxPositionLimit;

				if(Joint)
					InitializeLimits();

				for(int i = 0; i < part.symmetryCounterparts.Count; i++)
					part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().MaxPositionLimit = MaxPositionLimit;
			}
		}

		////////////////////////////////////////
		// Settings (servo - control input)

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Deflection Range", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float controlDeflectionRange = 0f;

		public float ControlDeflectionRange
		{
			get { return controlDeflectionRange; }
			set
			{
				value = Mathf.Clamp(value, 0f, 100f);

				if(object.Equals(controlDeflectionRange, value))
					return;

				controlDeflectionRange = value;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Neutral Position", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float controlNeutralPosition = 0f;

		public float ControlNeutralPosition
		{
			get
			{
				if(!isInverted)
					return controlNeutralPosition;
				else
					return 100 - controlNeutralPosition;
			}
			set
			{
				if(!isInverted)
					value = Mathf.Clamp(value, 0f, 100f);
				else
					value = Mathf.Clamp(100 - value, 0f, 100f);

				if(object.Equals(controlNeutralPosition, value))
					return;

				controlNeutralPosition = value;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Pitch Control", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float pitchControl = 0f;

		public float PitchControl
		{
			get { return pitchControl; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(pitchControl, value))
					return;

				pitchControl = value;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Roll Control", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float rollControl = 0f;

		public float RollControl
		{
			get { return rollControl; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(rollControl, value))
					return;

				rollControl = value;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Yaw Control", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float yawControl = 0f;

		public float YawControl
		{
			get { return yawControl; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(yawControl, value))
					return;

				yawControl = value;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Throttle Control", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float throttleControl = 0f;

		public float ThrottleControl
		{
			get { return throttleControl; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(throttleControl, value))
					return;

				throttleControl = value;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "X Control", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float xControl = 0f;

		public float XControl
		{
			get { return xControl; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(xControl, value))
					return;

				xControl = value;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Y Control", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float yControl = 0f;

		public float YControl
		{
			get { return yControl; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(yControl, value))
					return;

				yControl = value;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Z Control", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float zControl = 0f;

		public float ZControl
		{
			get { return zControl; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(zControl, value))
					return;

				zControl = value;
			}
		}

		////////////////////////////////////////
		// Settings (servo - link input)

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Link Input")]
		public void LinkInput()
		{
			if(LinkedInputPart != null)
			{
				LinkedInputPartId = 0;
				LinkedInputPartFlightId = 0;
				LinkedInputPart.Unlink(this);
				LinkedInputPart = null;

				UpdateUI();
			}
			else
			{
				GameObject go = new GameObject("PartSelectorHelper");
				Utility.PartSelector Selector = go.AddComponent<InfernalRobotics_v3.Utility.PartSelector>();

				Selector.onSelectedCallback = onSelectedLinkInput;

				if(HighLogic.LoadedSceneIsFlight)
					Selector.AddAllPartsOfType<ModuleIRServo_v3>(vessel);
				else if(HighLogic.LoadedSceneIsEditor)
					Selector.AddAllPartsOfType<ModuleIRServo_v3>(EditorLogic.fetch.ship);

				Selector.StartSelection();
			}
		}

		////////////////////////////////////////
		// Settings (servo - track input)

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Track Sun"),
			UI_Toggle(enabledText = "Engaged", disabledText = "Disengaged", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public bool trackSun = false;

		private void onChanged_trackSun(object o)
		{
			UpdateUI();
		}

		public bool TrackSun
		{
			get { return trackSun; }
			set { if(object.Equals(trackSun, value)) return; trackSun = value; onChanged_trackSun(null); }
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Track Angle", guiFormat = "0", guiUnits = "°",
			axisMode = KSPAxisMode.Incremental, minValue = -180f, maxValue = 180f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -180f, maxValue = 180f, stepIncrement = 1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float trackAngle = 0f;

		public float TrackAngle
		{
			get { return trackAngle; }
			set { trackAngle = value; }
		}

		////////////////////////////////////////
		// Settings (rotor)

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Acceleration", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0.1f),
			UI_FloatRange(minValue = 0.1f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float rotorAcceleration = 4f;

		private void onChanged_rotorAcceleration(object o)
		{
			if(Joint)
				InitializeDrive();
		}

		public float RotorAcceleration
		{
			get { return rotorAcceleration; }
			set
			{
				value = Mathf.Clamp(value, 0.1f, MaxAcceleration);

				if(object.Equals(rotorAcceleration, value))
					return;

				rotorAcceleration = value;

				onChanged_rotorAcceleration(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Base Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float baseSpeed;

		public float BaseSpeed
		{
			get { return baseSpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(baseSpeed, value))
					return;

				baseSpeed = value;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Pitch Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float pitchSpeed = 0f;

		public float PitchSpeed
		{
			get { return pitchSpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(pitchSpeed, value))
					return;

				pitchSpeed = value;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Roll Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float rollSpeed = 0f;

		public float RollSpeed
		{
			get { return rollSpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(rollSpeed, value))
					return;

				rollSpeed = value;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Yaw Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float yawSpeed = 0f;

		public float YawSpeed
		{
			get { return yawSpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(yawSpeed, value))
					return;

				yawSpeed = value;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Throttle Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float throttleSpeed = 0f;

		public float ThrottleSpeed
		{
			get { return throttleSpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(throttleSpeed, value))
					return;

				throttleSpeed = value;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "X Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float xSpeed = 0f;

		public float XSpeed
		{
			get { return xSpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(xSpeed, value))
					return;

				xSpeed = value;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Y Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float ySpeed = 0f;

		public float YSpeed
		{
			get { return ySpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(ySpeed, value))
					return;

				ySpeed = value;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Z Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		public float zSpeed = 0f;

		public float ZSpeed
		{
			get { return zSpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(zSpeed, value))
					return;

				zSpeed = value;
			}
		}

		////////////////////////////////////////
		// Input (servo)

		public void MoveLeft(float targetSpeed)
		{
			Move(float.NegativeInfinity, targetSpeed);
		}

		public void MoveCenter(float targetSpeed)
		{
			Move(DefaultPosition - CommandedPosition, targetSpeed);
		}

		public void MoveRight(float targetSpeed)
		{
			Move(float.PositiveInfinity, targetSpeed);
		}

		public void Move(float deltaPosition, float targetSpeed)
		{
			if(isOnRails || isLocked || isFreeMoving)
				return;

			MoveExecute(deltaPosition, targetSpeed);

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().MoveExecute(deltaPosition, targetSpeed);

			for(int i = 0; i < LinkedInputParts.Count; i++)
				LinkedInputParts[i].MoveExecute(deltaPosition, targetSpeed);
		}

		// FEHLER, temp, Idee für IK, daher auch kein Symmetry/Link
		public void PrecisionMove(float deltaPosition, float targetSpeed, float _acceleration)
		{
			if(isOnRails || isLocked || isFreeMoving)
				return;

			if(swap)
				deltaPosition = -deltaPosition;

			if(isInverted)
				deltaPosition = -deltaPosition;

			float targetPosition = commandedPosition + deltaPosition;

			ip.maxAcceleration = _acceleration * factorAcceleration;

			ip.SetCommand(targetPosition, Mathf.Clamp(targetSpeed, 0f, speedLimit) * factorSpeed);

			requestedPositionIsDefined = false;
		}

		// FEHLER, temp, Idee für IK, daher auch kein Symmetry/Link
		public void RegisterLimiter(ILimiter _ilm)
		{
			if(_ilm != null)
				ilm = _ilm;
			else
			{
				ilm = lm;

				ip.maxAcceleration = accelerationLimit * factorAcceleration;
			}
		}

		private void TrackMove()
		{
			if(isLocked)
				return;

			Vector3 toSun = Planetarium.fetch.Sun.transform.position - Joint.transform.position;

			if(Vector3.Angle(toSun, part.transform.TransformVector(axis)) < 5f)
				return; // axis points almost to the sun, we cannot track it like this

			toSun = Vector3.ProjectOnPlane(toSun, part.transform.TransformVector(axis));

			float deltaPosition = Vector3.SignedAngle(Quaternion.AngleAxis(trackAngle, part.transform.TransformVector(axis)) * part.transform.TransformVector(pointer), toSun, part.transform.TransformVector(axis));

			if(swap)
				deltaPosition = -deltaPosition;

			if(isInverted)
				deltaPosition = -deltaPosition;

			float targetPosition = commandedPosition + deltaPosition;

			MoveToPositionExecute(targetPosition, DefaultSpeed);

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().MoveToPositionExecute(targetPosition, DefaultSpeed);

			for(int i = 0; i < LinkedInputParts.Count; i++)
				LinkedInputParts[i].MoveExecute(targetPosition, DefaultSpeed);
		}

		private void MoveExecute(float deltaPosition, float targetSpeed)
		{
			if(swap)
				deltaPosition = -deltaPosition;

			if(isInverted)
				deltaPosition = -deltaPosition;

			float targetPosition = commandedPosition + deltaPosition;

			float _targetSpeed = Mathf.Clamp(targetSpeed, 0.1f, speedLimit);
			float _acceleration = accelerationLimit;

			if(!ilm.SetCommand(ref targetPosition, ref _targetSpeed, ref _acceleration))
				ip.SetCommand(targetPosition, _targetSpeed * factorSpeed);
			else
			{
				ip.maxAcceleration = _acceleration * factorAcceleration;
				ip.SetCommand(targetPosition, _targetSpeed * factorSpeed);
			}

			requestedPositionIsDefined = false;
		}

		public void MoveTo(float targetPosition)
		{
			MoveTo(targetPosition, DefaultSpeed);
		}

		public void MoveTo(float targetPosition, float targetSpeed)
		{
			if(isOnRails || isLocked || isFreeMoving)
				return;

			MoveToPositionExecute(targetPosition, targetSpeed);

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().MoveToPositionExecute(targetPosition, targetSpeed);

			for(int i = 0; i < LinkedInputParts.Count; i++)
				LinkedInputParts[i].MoveToPositionExecute(targetPosition, targetSpeed);
		}

		public void MoveToPosition(float targetPosition, float targetSpeed)
		{
			if(isOnRails || isLocked || isFreeMoving)
				return;

			MoveToPositionExecute(targetPosition, targetSpeed);

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().MoveToPositionExecute(targetPosition, targetSpeed);

			for(int i = 0; i < LinkedInputParts.Count; i++)
				LinkedInputParts[i].MoveToPositionExecute(targetPosition, targetSpeed);
		}

		private void MoveToPositionExecute(float targetPosition, float targetSpeed)
		{
			MoveToPositionExecuteInternal(targetPosition, targetSpeed);

			float targetPositionSet = ip.TargetPosition;

			if(!isInverted)
				requestedPosition = (swap ? -targetPositionSet : targetPositionSet) + zeroNormal + correction_1 - correction_0;
			else
				requestedPosition = (swap ? targetPositionSet : -targetPositionSet) + zeroInvert - correction_1 + correction_0;
		}

		private void MoveToPositionExecuteInternal(float targetPosition, float targetSpeed)
		{
			if(!isInverted)
				targetPosition = (swap ? -1.0f : 1.0f) * (targetPosition - zeroNormal - correction_1 + correction_0);
			else
				targetPosition = (swap ? 1.0f : -1.0f) * (targetPosition - zeroInvert + correction_1 - correction_0);

			float _targetSpeed = Mathf.Clamp(targetSpeed, 0.1f, speedLimit);
			float _acceleration = accelerationLimit;

			if(!ilm.SetCommand(ref targetPosition, ref _targetSpeed, ref _acceleration))
				ip.SetCommand(targetPosition, _targetSpeed * factorSpeed);
			else
			{
				ip.maxAcceleration = _acceleration * factorAcceleration;
				ip.SetCommand(targetPosition, _targetSpeed * factorSpeed);
			}

			requestedPositionIsDefined = true;
		}

		public void Stop()
		{
			if(isFreeMoving)
				return;

			ip.Stop();
			requestedPositionIsDefined = false;

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
			{
				ModuleIRServo_v3 servo = part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>();

				servo.ip.Stop();
				servo.requestedPositionIsDefined = false;
			}

			for(int i = 0; i < LinkedInputParts.Count; i++)
			{
				LinkedInputParts[i].ip.Stop();
				LinkedInputParts[i].requestedPositionIsDefined = false;
			}
		}

		// Relax Mode (used to prevent breaking while latching of LEE and GF)

		public void SetRelaxMode(float relaxFactor)
		{
			if(mode != ModeType.servo)
				return;

			JointDrive currentDrive = isRotational ? Joint.angularXDrive : Joint.xDrive;

			JointDrive drive = new JointDrive
			{
				maximumForce = currentDrive.maximumForce,
				positionSpring = 0.04f * relaxFactor * currentDrive.positionSpring,
				positionDamper = currentDrive.positionDamper
			};

			if(isRotational)	Joint.angularXDrive = drive;
			else				Joint.xDrive = drive;
		}

		public void ResetRelaxMode()
		{
			if(mode != ModeType.servo)
				return;

			InitializeDrive();
		}

		// returns true, if you need to call it again / the joint is not relaxed

		public bool RelaxStep()
		{
			if(mode != ModeType.servo)
				return false;

			float f = Math.Abs(CommandedPosition - Position);
			if(isRotational && (f > 180f))
				f -= 360f;

			MoveTo(Position, DefaultSpeed);

			return f >= 0.01f;
		}

		////////////////////////////////////////
		// Input (rotor)

		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Motor"),
			UI_Toggle(enabledText = "Engaged", disabledText = "Disengaged", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private bool isRunning = false;

		private void onChanged_isRunning(object o)
		{
			StartCoroutine(ChangeIsRunning(isRunning ? 1f : 0f));
		}

		public bool IsRunning
		{
			get { return isRunning; }
			set { if(object.Equals(isRunning, value)) return; isRunning = value; onChanged_isRunning(null); }
		}

		[KSPField(isPersistant = true)]
		public float _isRunning = 0f;

		private IEnumerator ChangeIsRunning(float target)
		{
			float s = _isRunning;
			int cnt = (int)(4 * BaseSpeed);

			List<ModuleIRServo_v3> servos = new List<ModuleIRServo_v3>();

			servos.Add(this);
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				servos.Add(part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>());

			for(int i = 0; i < cnt; i++)
			{
				for(int j = 0; j < servos.Count; j++)
					servos[j]._isRunning = (s / cnt) * (cnt - i) + (target / cnt) * i;

				yield return new WaitForFixedUpdate();
			}

			for(int j = 0; j < servos.Count; j++)
				servos[j]._isRunning = target;
		}

		////////////////////////////////////////
		// Characteristics - values 'by design' of the joint

		[KSPField(isPersistant = false), SerializeField]
		private bool isRotational = false;

		public bool IsRotational
		{
			get { return isRotational; }
		}

		[KSPField(isPersistant = false), SerializeField]
		private bool hasMinMaxPosition = false;
		[KSPField(isPersistant = false), SerializeField]
		private float minPosition = 0;
		[KSPField(isPersistant = false), SerializeField]
		private float maxPosition = 360;

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

		[KSPField(isPersistant = false), SerializeField]
		private bool isFreeMoving = false;

		public bool IsFreeMoving
		{
			get { return isFreeMoving; }
		}

		public bool IsServo
		{
			get { return mode == ModeType.servo; }
		}

		[KSPField(isPersistant = false), SerializeField]
		private bool canHaveLimits = true;

		public bool CanHaveLimits
		{
			get { return canHaveLimits; }
		}

		[KSPField(isPersistant = false), SerializeField]
		private float maxForce = 30f;

		public float MaxForce
		{
			get { return maxForce * (0.01f * motorSizeFactor); }
		}

		[KSPField(isPersistant = false), SerializeField]
		private float maxAcceleration = 10;

		public float MaxAcceleration
		{
			get { return maxAcceleration * (0.01f * motorSizeFactor); }
		}

		[KSPField(isPersistant = false), SerializeField]
		private float maxSpeed = 100;

		public float MaxSpeed
		{
			get { return maxSpeed * (0.01f * motorSizeFactor); }
		}

		public float ElectricChargeRequired
		{
			get { return maxPowerDrawRate; }
		}

		[KSPField(isPersistant = false), SerializeField]
		private bool hasSpring = false;

		public bool HasSpring
		{
			get { return hasSpring; }
		}

		// Factors (mainly for UI)

		[KSPField(isPersistant = false), SerializeField]
		private float factorForce = 1.0f;
		[KSPField(isPersistant = false), SerializeField]
		private float factorSpeed = 1.0f;
		[KSPField(isPersistant = false), SerializeField]
		private float factorAcceleration = 1.0f;

		////////////////////////////////////////
		// Editor

		private void EditorInitialize()
		{
			requestedPosition = CommandedPosition;

			float _commandedPosition = swap ? -CommandedPositionS : CommandedPositionS;

			position = 0f;
			lastUpdatePosition = 0f;

			if(part.parent != null)
				swap = FindSwap();

			InitializeMeshes(false);

			fixedMeshTransform.localPosition = Vector3.zero;
			fixedMeshTransform.localRotation = Quaternion.identity;
			movingMeshTransform.localPosition = Vector3.zero;
			movingMeshTransform.localRotation = Quaternion.identity;

			ResetAttachNodes();

			if(part.parent == null)
			{
				if(isRotational)
					movingMeshTransform.Rotate(axis, _commandedPosition);
				else
					movingMeshTransform.Translate(axis.normalized * _commandedPosition);

				MoveAttachNodes(_commandedPosition, swap);
			}
			else
			{
				if(isRotational)
					fixedMeshTransform.Rotate(axis, -_commandedPosition);
				else
					fixedMeshTransform.Translate(axis.normalized * (-_commandedPosition));

				MoveAttachNodes(-_commandedPosition, !swap);
			}
		}
	
		public void EditorMiniInit()
		{
			// find non rotating mesh
			fixedMeshTransform = KSPUtil.FindInPartModel(transform, swap ? movingMesh : fixedMesh);
			movingMeshTransform = KSPUtil.FindInPartModel(transform, swap ? fixedMesh : movingMesh);
		}

		public void EditorReset()
		{
			if(!HighLogic.LoadedSceneIsEditor)
				return;

			IsInverted = false;
			IsLimitted = false;

			EditorSetToPosition(0f);
		}

		public void EditorMoveLeft(float targetSpeed)
		{
			EditorMove(float.NegativeInfinity, targetSpeed);
		}

		public void EditorMoveCenter(float targetSpeed)
		{
			EditorSetTo(DefaultPosition);
		}

		public void EditorMoveRight(float targetSpeed)
		{
			EditorMove(float.PositiveInfinity, targetSpeed);
		}

		public void EditorMove(float targetPosition, float targetSpeed)
		{
			float movement = Mathf.Clamp(targetSpeed, 0.1f, speedLimit) * factorSpeed * Time.deltaTime;

			if(Math.Abs(targetPosition - Position) > movement)
			{
				if(targetPosition < Position)
					movement = -movement;

				if(!isInverted)
					targetPosition = Position + movement;
				else
					targetPosition = Position + movement;
			}

			EditorSetTo(targetPosition);
		}

		public void EditorSetTo(float targetPosition)
		{
			EditorSetToPosition(targetPosition);

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().EditorSetToPosition(targetPosition);
		}

		// sets the position and rotates the joint and its meshes
		// (all correction values should be zero here and could be ignored... I still didn't "optimize" it)
		private void EditorSetToPosition(float targetPosition)
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
					swap
					? Mathf.Clamp(targetPosition, -_maxPositionLimit, -_minPositionLimit)
					: Mathf.Clamp(targetPosition, _minPositionLimit, _maxPositionLimit);
			}
			else if(hasMinMaxPosition)
			{
				targetPosition =
					swap
					? Mathf.Clamp(targetPosition, -maxPosition, -minPosition)
					: Mathf.Clamp(targetPosition, minPosition, maxPosition);
			}

			if(isRotational)
			{
				if(!hasMinMaxPosition && !hasPositionLimit) // then it is "modulo" -> from -360 to +360
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

			EditorSetToPositionExecute(targetPosition);
		}

		private void EditorSetToPositionExecute(float targetPosition)
		{
			float deltaPosition = targetPosition - commandedPosition;

			if(isRotational)
			{
				if(transform.parent)
				{
					fixedMeshTransform.Rotate(axis, -deltaPosition);
					MoveAttachNodes(-deltaPosition, !swap);

					transform.Rotate(axis, deltaPosition);
				}
				else
				{
					movingMeshTransform.Rotate(axis, deltaPosition);
					MoveAttachNodes(deltaPosition, swap);

					MoveChildren(deltaPosition);
				}
			}
			else
			{
				if(transform.parent)
				{
					fixedMeshTransform.Translate(axis.normalized * (-deltaPosition));
					MoveAttachNodes(-deltaPosition, !swap);

					transform.Translate(axis.normalized * deltaPosition);
				}
				else
				{
					movingMeshTransform.Translate(axis.normalized * deltaPosition);
					MoveAttachNodes(deltaPosition, swap);

					MoveChildren(deltaPosition);
				}
			}

			position = commandedPosition = targetPosition;

			requestedPosition = CommandedPosition;
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
				Quaternion.AngleAxis(!isInverted ? zeroNormal : zeroInvert, swap ? rAxis : -rAxis)		// inversion for inverted joints -> like this the Aid doesn't have to invert values itself
				* Quaternion.LookRotation(swap ? rAxis : -rAxis, swap ? -rPointer : rPointer);			// normal rotation
		}

		////////////////////////////////////////
		// Context Menu

		private void AttachContextMenu()
		{
			Fields["isInverted"].OnValueModified += onChanged_isInverted;

			Fields["isLocked"].OnValueModified += onChanged_isLocked;
			Fields["modeIndex"].OnValueModified += onChanged_modeIndex;
			Fields["inputModeIndex"].OnValueModified += onChanged_inputModeIndex;

			Fields["hasPositionLimit"].OnValueModified += onChanged_hasPositionLimit;
			Fields["minmaxPositionLimit"].OnValueModified += onChanged_minmaxPositionLimit;

			Fields["forceLimit"].OnValueModified += onChanged_forceLimit;
			Fields["accelerationLimit"].OnValueModified += onChanged_accelerationLimit;
			Fields["speedLimit"].OnValueModified += onChanged_speedLimit;

			Fields["jointSpring"].OnValueModified += onChanged_jointSpring;
			Fields["jointDamping"].OnValueModified += onChanged_jointDamping;

			Fields["motorSizeFactor"].OnValueModified += onChanged_motorSizeFactor;

			Fields["rotorAcceleration"].OnValueModified += onChanged_rotorAcceleration;

			Fields["isRunning"].OnValueModified += onChanged_isRunning;

			Fields["requestedPosition"].OnValueModified += onChanged_requestedPosition;

			Fields["trackSun"].OnValueModified += onChanged_trackSun;

			Fields["activateCollisions"].OnValueModified += onChanged_activateCollisions;
		}

		private void DetachContextMenu()
		{
			Fields["isInverted"].OnValueModified -= onChanged_isInverted;

			Fields["isLocked"].OnValueModified -= onChanged_isLocked;
			Fields["modeIndex"].OnValueModified -= onChanged_modeIndex;
			Fields["inputModeIndex"].OnValueModified -= onChanged_inputModeIndex;

			Fields["hasPositionLimit"].OnValueModified -= onChanged_hasPositionLimit;
			Fields["minmaxPositionLimit"].OnValueModified -= onChanged_minmaxPositionLimit;

			Fields["forceLimit"].OnValueModified -= onChanged_forceLimit;
			Fields["accelerationLimit"].OnValueModified -= onChanged_accelerationLimit;
			Fields["speedLimit"].OnValueModified -= onChanged_speedLimit;
			
			Fields["jointSpring"].OnValueModified -= onChanged_jointSpring;
			Fields["jointDamping"].OnValueModified -= onChanged_jointDamping;

			Fields["motorSizeFactor"].OnValueModified -= onChanged_motorSizeFactor;

			Fields["rotorAcceleration"].OnValueModified -= onChanged_rotorAcceleration;

			Fields["isRunning"].OnValueModified -= onChanged_isRunning;

			Fields["requestedPosition"].OnValueModified -= onChanged_requestedPosition;

			Fields["trackSun"].OnValueModified -= onChanged_trackSun;

			Fields["activateCollisions"].OnValueModified -= onChanged_activateCollisions;
		}

		private void UpdateUI(bool bRebuildUI = false)
		{
			part.Events["RemoveFromSymmetry"].active = false;

			((BaseAxisField)Fields["forceLimit"]).incrementalSpeed = MaxForce / 10f;
			((BaseAxisField)Fields["forceLimit"]).maxValue = MaxForce;

			((BaseAxisField)Fields["accelerationLimit"]).incrementalSpeed = MaxAcceleration / 10f;
			((BaseAxisField)Fields["accelerationLimit"]).maxValue = MaxAcceleration;

			((BaseAxisField)Fields["speedLimit"]).incrementalSpeed = MaxSpeed / 10f;
			((BaseAxisField)Fields["speedLimit"]).maxValue = MaxSpeed;

			((BaseAxisField)Fields["rotorAcceleration"]).incrementalSpeed = MaxAcceleration / 10f;
			((BaseAxisField)Fields["rotorAcceleration"]).maxValue = MaxAcceleration;

			((BaseAxisField)Fields["requestedPosition"]).ignoreClampWhenIncremental = !hasMinMaxPosition && !hasPositionLimit;
			((BaseAxisField)Fields["requestedPosition"]).minValue = hasPositionLimit ? MinPositionLimit : MinPosition;
			((BaseAxisField)Fields["requestedPosition"]).maxValue = hasPositionLimit ? MaxPositionLimit : MaxPosition;
			((BaseAxisField)Fields["requestedPosition"]).incrementalSpeed = isRotational ? 30f : 0.3f;


			if(HighLogic.LoadedSceneIsFlight)
			{
				Fields["inputModeIndex"].guiActive = (((UI_ChooseOption)Fields["inputModeIndex"].uiControlFlight).options.Length > 1) && (mode == ModeType.servo);

				Fields["forceLimit"].guiActive = !isFreeMoving;
				((UI_FloatRange)Fields["forceLimit"].uiControlFlight).maxValue = MaxForce;

				Fields["accelerationLimit"].guiActive = (mode == ModeType.servo) && !isFreeMoving;
				((UI_FloatRange)Fields["accelerationLimit"].uiControlFlight).maxValue = MaxAcceleration;
 
				Fields["speedLimit"].guiActive = !isFreeMoving;
				((UI_FloatRange)Fields["speedLimit"].uiControlFlight).maxValue = MaxSpeed;

				Fields["jointSpring"].guiActive = false;
				Fields["jointDamping"].guiActive = (mode == ModeType.rotor);

				Fields["LastPowerDrawRate"].guiActive = !isFreeMoving;

				Fields["hasPositionLimit"].guiActive = (mode == ModeType.servo) && canHaveLimits;

				Fields["minmaxPositionLimit"].guiActive = (mode == ModeType.servo) && hasPositionLimit;
				((UI_MinMaxRange)Fields["minmaxPositionLimit"].uiControlFlight).minValueX = MinPosition;
				((UI_MinMaxRange)Fields["minmaxPositionLimit"].uiControlFlight).maxValueX = MaxPosition;
				((UI_MinMaxRange)Fields["minmaxPositionLimit"].uiControlFlight).minValueY = MinPosition;
				((UI_MinMaxRange)Fields["minmaxPositionLimit"].uiControlFlight).maxValueY = MaxPosition;

				if(Math.Abs(MaxPosition - MinPosition) < 20)
				{
					Fields["minmaxPositionLimit"].guiFormat = "F2";
					((UI_MinMaxRange)Fields["minmaxPositionLimit"].uiControlFlight).stepIncrement = 0.01f;
				}
				else
				{
					Fields["minmaxPositionLimit"].guiFormat = "F1";
					((UI_MinMaxRange)Fields["minmaxPositionLimit"].uiControlFlight).stepIncrement = 0.1f;
				}

				// control

				Fields["controlDeflectionRange"].guiActive = (mode == ModeType.servo) && (inputMode == InputModeType.control);
				Fields["controlNeutralPosition"].guiActive = (mode == ModeType.servo) && (inputMode == InputModeType.control);

				Fields["pitchControl"].guiActive = (mode == ModeType.servo) && (inputMode == InputModeType.control);
				Fields["rollControl"].guiActive = (mode == ModeType.servo) && (inputMode == InputModeType.control);
				Fields["yawControl"].guiActive = (mode == ModeType.servo) && (inputMode == InputModeType.control);
				Fields["throttleControl"].guiActive = (mode == ModeType.servo) && (inputMode == InputModeType.control);

				Fields["xControl"].guiActive = (mode == ModeType.servo) && (inputMode == InputModeType.control);
				Fields["yControl"].guiActive = (mode == ModeType.servo) && (inputMode == InputModeType.control);
				Fields["zControl"].guiActive = (mode == ModeType.servo) && (inputMode == InputModeType.control);

				// link

				Events["LinkInput"].guiName = LinkedInputPart == null ? "Link Input" : "Unlink Input from " + LinkedInputPart.ToString();
				Events["LinkInput"].guiActive = (mode == ModeType.servo) && (inputMode == InputModeType.linked);

				// track

				Fields["trackSun"].guiActive = (mode == ModeType.servo) && (inputMode == InputModeType.tracking);
				Fields["trackAngle"].guiActive = (mode == ModeType.servo) && (inputMode == InputModeType.tracking) && trackSun;

				// rotor

				Fields["baseSpeed"].guiActive = (mode == ModeType.rotor);
				Fields["pitchSpeed"].guiActive = (mode == ModeType.rotor);
				Fields["rollSpeed"].guiActive = (mode == ModeType.rotor);
				Fields["yawSpeed"].guiActive = (mode == ModeType.rotor);
				Fields["throttleSpeed"].guiActive = (mode == ModeType.rotor);

				Fields["xSpeed"].guiActive = (mode == ModeType.rotor);
				Fields["ySpeed"].guiActive = (mode == ModeType.rotor);
				Fields["zSpeed"].guiActive = (mode == ModeType.rotor);

				Fields["rotorAcceleration"].guiActive = (mode == ModeType.rotor);
				((UI_FloatRange)Fields["rotorAcceleration"].uiControlFlight).maxValue = MaxAcceleration;

				Fields["isRunning"].guiActive = (mode == ModeType.rotor);


				Fields["requestedPosition"].guiActive = (mode == ModeType.servo) && (inputMode == InputModeType.manual) && !IsLocked && !isFreeMoving;
				Fields["CurrentPosition"].guiActive = (mode == ModeType.servo);

				((UI_FloatRange)Fields["requestedPosition"].uiControlFlight).minValue = hasPositionLimit ? MinPositionLimit : MinPosition;
				((UI_FloatRange)Fields["requestedPosition"].uiControlFlight).maxValue = hasPositionLimit ? MaxPositionLimit : MaxPosition;

				if(Math.Abs((hasPositionLimit ? MaxPositionLimit : MaxPosition) - (hasPositionLimit ? MinPositionLimit : MinPosition)) < 20)
				{
					Fields["requestedPosition"].guiFormat = "F2";
					((UI_FloatRange)Fields["requestedPosition"].uiControlFlight).stepIncrement = 0.01f;

					Fields["CurrentPosition"].guiFormat = "F2";
				}
				else
				{
					Fields["requestedPosition"].guiFormat = "F1";
					((UI_FloatRange)Fields["requestedPosition"].uiControlFlight).stepIncrement = 0.1f;

					Fields["CurrentPosition"].guiFormat = "F1";
				}

				Events["RemoveFromSymmetry2"].guiActive = (part.symmetryCounterparts.Count > 0);
			}
			else if(HighLogic.LoadedSceneIsEditor)
			{
				Fields["modeIndex"].guiActiveEditor = ((UI_ChooseOption)Fields["modeIndex"].uiControlEditor).options.Length > 1;
				Fields["inputModeIndex"].guiActiveEditor = (((UI_ChooseOption)Fields["inputModeIndex"].uiControlEditor).options.Length > 1) && (mode == ModeType.servo);

				Fields["forceLimit"].guiActiveEditor = !isFreeMoving;
				((UI_FloatRange)Fields["forceLimit"].uiControlEditor).maxValue = MaxForce;

				Fields["accelerationLimit"].guiActiveEditor = (mode == ModeType.servo) && !isFreeMoving;
				((UI_FloatRange)Fields["accelerationLimit"].uiControlEditor).maxValue = MaxAcceleration;
 
				Fields["speedLimit"].guiActiveEditor = !isFreeMoving;
				((UI_FloatRange)Fields["speedLimit"].uiControlEditor).maxValue = MaxSpeed;

				Fields["jointSpring"].guiActiveEditor = hasSpring && isFreeMoving;
				Fields["jointDamping"].guiActiveEditor = (mode == ModeType.rotor) || (hasSpring && isFreeMoving);

				Fields["MaxPowerDrawRate"].guiActiveEditor = !isFreeMoving;
				Fields["motorSizeFactor"].guiActiveEditor = !isFreeMoving;

				Fields["hasPositionLimit"].guiActiveEditor = (mode == ModeType.servo) && canHaveLimits;

				Fields["minmaxPositionLimit"].guiActiveEditor = hasPositionLimit;
				((UI_MinMaxRange)Fields["minmaxPositionLimit"].uiControlEditor).minValueX = MinPosition;
				((UI_MinMaxRange)Fields["minmaxPositionLimit"].uiControlEditor).maxValueX = MaxPosition;
				((UI_MinMaxRange)Fields["minmaxPositionLimit"].uiControlEditor).minValueY = MinPosition;
				((UI_MinMaxRange)Fields["minmaxPositionLimit"].uiControlEditor).maxValueY = MaxPosition;

				if(Math.Abs(MaxPosition - MinPosition) < 20)
				{
					Fields["minmaxPositionLimit"].guiFormat = "F2";
					((UI_MinMaxRange)Fields["minmaxPositionLimit"].uiControlEditor).stepIncrement = 0.01f;
				}
				else
				{
					Fields["minmaxPositionLimit"].guiFormat = "F1";
					((UI_MinMaxRange)Fields["minmaxPositionLimit"].uiControlEditor).stepIncrement = 0.1f;
				}

				// control

				Fields["controlDeflectionRange"].guiActiveEditor = (mode == ModeType.servo) && (inputMode == InputModeType.control);
				Fields["controlNeutralPosition"].guiActiveEditor = (mode == ModeType.servo) && (inputMode == InputModeType.control);

				Fields["pitchControl"].guiActiveEditor = (mode == ModeType.servo) && (inputMode == InputModeType.control);
				Fields["rollControl"].guiActiveEditor = (mode == ModeType.servo) && (inputMode == InputModeType.control);
				Fields["yawControl"].guiActiveEditor = (mode == ModeType.servo) && (inputMode == InputModeType.control);
				Fields["throttleControl"].guiActiveEditor = (mode == ModeType.servo) && (inputMode == InputModeType.control);

				Fields["xControl"].guiActiveEditor = (mode == ModeType.servo) && (inputMode == InputModeType.control);
				Fields["yControl"].guiActiveEditor = (mode == ModeType.servo) && (inputMode == InputModeType.control);
				Fields["zControl"].guiActiveEditor = (mode == ModeType.servo) && (inputMode == InputModeType.control);

				// link

				Events["LinkInput"].guiName = LinkedInputPart == null ? "Link Input" : "Unlink Input from " + LinkedInputPart.ToString();
				Events["LinkInput"].guiActiveEditor = (mode == ModeType.servo) && (inputMode == InputModeType.linked);

				// track

				Fields["trackSun"].guiActiveEditor = (mode == ModeType.servo) && (inputMode == InputModeType.tracking);
				Fields["trackAngle"].guiActiveEditor = (mode == ModeType.servo) && (inputMode == InputModeType.tracking) && trackSun;

				// rotor

				Fields["baseSpeed"].guiActiveEditor = (mode == ModeType.rotor);
				Fields["pitchSpeed"].guiActiveEditor = (mode == ModeType.rotor);
				Fields["rollSpeed"].guiActiveEditor = (mode == ModeType.rotor);
				Fields["yawSpeed"].guiActiveEditor = (mode == ModeType.rotor);
				Fields["throttleSpeed"].guiActiveEditor = (mode == ModeType.rotor);

				Fields["xSpeed"].guiActiveEditor = (mode == ModeType.rotor);
				Fields["ySpeed"].guiActiveEditor = (mode == ModeType.rotor);
				Fields["zSpeed"].guiActiveEditor = (mode == ModeType.rotor);

				Fields["rotorAcceleration"].guiActiveEditor = (mode == ModeType.rotor);
				((UI_FloatRange)Fields["rotorAcceleration"].uiControlEditor).maxValue = MaxAcceleration;

				Fields["isRunning"].guiActiveEditor = false; // (mode == ModeType.rotor);


				Fields["requestedPosition"].guiActiveEditor = (mode == ModeType.servo) && (inputMode == InputModeType.manual);
			//	Fields["CurrentPosition"].guiActiveEditor = (mode == ModeType.servo); -> we don't update it -> add this before uncommenting this line

				((UI_FloatRange)Fields["requestedPosition"].uiControlEditor).minValue = hasPositionLimit ? MinPositionLimit : MinPosition;
				((UI_FloatRange)Fields["requestedPosition"].uiControlEditor).maxValue = hasPositionLimit ? MaxPositionLimit : MaxPosition;

				if(Math.Abs((hasPositionLimit ? MaxPositionLimit : MaxPosition) - (hasPositionLimit ? MinPositionLimit : MinPosition)) < 20)
				{
					Fields["requestedPosition"].guiFormat = "F2";
					((UI_FloatRange)Fields["requestedPosition"].uiControlEditor).stepIncrement = 0.01f;

					Fields["CurrentPosition"].guiFormat = "F2";
				}
				else
				{
					Fields["requestedPosition"].guiFormat = "F1";
					((UI_FloatRange)Fields["requestedPosition"].uiControlEditor).stepIncrement = 0.1f;

					Fields["CurrentPosition"].guiFormat = "F1";
				}

				Events["RemoveFromSymmetry2"].guiActiveEditor = (part.symmetryCounterparts.Count > 0);
			}

			UIPartActionWindow[] partWindows = FindObjectsOfType<UIPartActionWindow>();
			foreach(UIPartActionWindow partWindow in partWindows)
			{
				if(partWindow.part == part)
				{
					// FEHLER, superdoof, später viel gezielter arbeiten
					for(int i = 0; i < partWindow.ListItems.Count; i++)
						partWindow.ListItems[i].UpdateItem();

					if(bRebuildUI)
						partWindow.displayDirty = true; // -> this would rebuild the window, we don't need that -> except for those UI elements with bugs -> FEHLER, diese Elemente später überschreiben
				}
			}
		}

		////////////////////////////////////////
		// Actions

		[KSPAction("Toggle Lock")]
		public void LockToggleAction(KSPActionParam param)
		{ IsLocked = !IsLocked; }

		////////////////////////////////////////
		// Actions (servo)

		[KSPAction("Move To Previous Preset")]
		public void MovePrevPresetAction(KSPActionParam param)
		{
			if(Presets != null)
				Presets.MovePrev(DefaultSpeed);
		}

		[KSPAction("Move To Next Preset")]
		public void MoveNextPresetAction(KSPActionParam param)
		{
			if(Presets != null)
				Presets.MoveNext(DefaultSpeed);
		}

		[KSPAction("Move -")]
		public void MoveMinusAction(KSPActionParam param)
		{
			switch (param.type)
			{
			case KSPActionType.Activate:
				MoveLeft(DefaultSpeed);
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
				MoveCenter(DefaultSpeed);
				break;

			case KSPActionType.Deactivate:
				Stop();
				break;
			}
		}

		[KSPAction("Move +")]
		public void MovePlusAction(KSPActionParam param)
		{
			switch(param.type)
			{
			case KSPActionType.Activate:
				MoveRight(DefaultSpeed);
				break;

			case KSPActionType.Deactivate:
				Stop();
				break;
			}
		}

		////////////////////////////////////////
		// Axis (servo)

		private bool requestedPositionIsDefined = true;

		[KSPAxisField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Target Position", guiFormat = "F2",
			axisMode = KSPAxisMode.Incremental),
			UI_FloatRange(suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float requestedPosition;

		private void onChanged_requestedPosition(object o)
		{
			if(HighLogic.LoadedSceneIsEditor)
			{
				EditorSetTo(requestedPosition);
				return;
			}

			if(isOnRails || isLocked || isFreeMoving || (LinkedInputPart != null))
			{
				requestedPosition = CommandedPosition;
				return;
			}

			MoveToPositionExecuteInternal(requestedPosition, DefaultSpeed);

			for(int i = 0; i < LinkedInputParts.Count; i++)
				LinkedInputParts[i].MoveToPositionExecute(requestedPosition, DefaultSpeed);
		}

		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Current Position", guiFormat = "F1")]
		private float CurrentPosition;

		////////////////////////////////////////
		// Actions (rotor)

		[KSPAction("Toggle Motor")]
		public void MotorToggleAction(KSPActionParam param)
		{ IsRunning = !IsRunning; }

		////////////////////////////////////////
		// special functions

		private void onSelectedLinkInput(Part p)
		{
			ModuleIRServo_v3 servo = p.GetComponent<ModuleIRServo_v3>();

			if(servo.LinkedInputSourceId == 0)
			{
				if(HighLogic.LoadedSceneIsEditor)
				{
					// if we want to link something in the editor, we need a unique id
					// we generate this here (this is not used when we are already in flight,
					// because there we can use the flightID of the part)
					// (info: the persistentId of the Part does change from time to time)

					servo.LinkedInputSourceId = (uint)Guid.NewGuid().GetHashCode();

					for(int i = 0; i < EditorLogic.fetch.ship.parts.Count; i++)
					{
						ModuleIRServo_v3 _servo = EditorLogic.fetch.ship.parts[i].GetComponent<ModuleIRServo_v3>();

						if((_servo != null) && (_servo != servo) && (_servo.LinkedInputSourceId == servo.LinkedInputSourceId))
						{
							servo.LinkedInputSourceId = (uint)Guid.NewGuid().GetHashCode();
							i = 0;
						}
					}
				}
			}

			LinkedInputPartId = servo.LinkedInputSourceId;
			LinkedInputPartFlightId = p.flightID;
			LinkedInputPart = servo;
			LinkedInputPart.Link(this);

			requestedPositionIsDefined = false;

			UpdateUI();
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Remove from Symmetry")]
		public void RemoveFromSymmetry2()
		{
			List<Part> parts = new List<Part>(part.symmetryCounterparts);

			part.CleanSymmetryReferences();

			if(HighLogic.LoadedSceneIsFlight)
			{
				// we need to fix this special value
				Events["RemoveFromSymmetry2"].guiActive = false;
				for(int i = 0; i < parts.Count; i++)
					parts[i].GetComponent<ModuleIRServo_v3>().Events["RemoveFromSymmetry2"].guiActive = (parts[i].symmetryCounterparts.Count > 0);

				Controller.Instance.RebuildServoGroupsFlight();
			}
			else if(HighLogic.LoadedSceneIsEditor)
			{
				// we need to fix this special value
				Events["RemoveFromSymmetry2"].guiActiveEditor = false;
				for(int i = 0; i < parts.Count; i++)
					parts[i].GetComponent<ModuleIRServo_v3>().Events["RemoveFromSymmetry2"].guiActiveEditor = (parts[i].symmetryCounterparts.Count > 0);

				Controller.Instance.RebuildServoGroupsEditor();
			}
		}

		[KSPField(isPersistant = true, advancedTweakable = true, guiActive = true, guiActiveEditor = true, guiName = "IR Same Vessel Interaction"),
			UI_Toggle(enabledText = "Yes", disabledText = "No")]
		public bool activateCollisions = false;

		private void onChanged_activateCollisions(object o)
		{
			GameEvents.OnCollisionIgnoreUpdate.Fire();
		}

		////////////////////////////////////////
		// IRescalable

		public float scalingFactor = 1f;

		[KSPField(isPersistant = false), SerializeField]
		private float scaleMass = 1.0f;
		[KSPField(isPersistant = false), SerializeField]
		private float scaleElectricChargeRequired = 2.0f;

		// Tweakscale support
		[KSPEvent(guiActive = false, active = true)]
		void OnPartScaleChanged(BaseEventDetails data)
		{
			OnRescale(new ScalingFactor(data.Get<float>("factorAbsolute")));
		}

		public void OnRescale(ScalingFactor factor)
		{
			float _abs = factor.absolute.linear;
			float _rel = factor.absolute.linear / scalingFactor;

			ModuleIRServo_v3 prefab = part.partInfo.partPrefab.GetComponent<ModuleIRServo_v3>();

			part.mass = prefab.part.mass * Mathf.Pow(_abs, scaleMass) * (0.01f * (75f + motorSizeFactor * 0.25f));

			maxForce = prefab.maxForce * _abs;
 			ForceLimit = ForceLimit * _rel;

			electricChargeRequired = prefab.electricChargeRequired * Mathf.Pow(_abs, scaleElectricChargeRequired) * (0.01f * (80f + motorSizeFactor * 0.2f));

 			if(!isRotational)
			{
				minPosition = prefab.minPosition * _abs;
				maxPosition = prefab.maxPosition * _abs;

				float _MinPositionLimit = MinPositionLimit;
				float _MaxPositionLimit = MaxPositionLimit;

				zeroNormal = prefab.zeroNormal * _abs;
				zeroInvert = prefab.zeroInvert * _abs;

				MinPositionLimit = _MinPositionLimit * _rel;
				MaxPositionLimit = _MaxPositionLimit * _rel;

				factorAcceleration = prefab.factorAcceleration * _abs;
				factorSpeed = prefab.factorSpeed * _abs;

				float deltaPosition = commandedPosition;

				commandedPosition *= _rel;
				deltaPosition = commandedPosition - deltaPosition;
				transform.Translate(axis.normalized * deltaPosition);

// FEHLER, temp, mal sehen wieso das nötig ist
if(fixedMeshTransform != null)
				{
			fixedMeshTransform.localPosition = Vector3.zero;
			fixedMeshTransform.Translate(axis.normalized * (-commandedPosition));
				}
			}

			scalingFactor = factor.absolute.linear;

			UpdateMaxPowerDrawRate();

			UpdateUI();
		}

		////////////////////////////////////////
		// IJointLockState (AutoStrut support)

		bool IJointLockState.IsJointUnlocked()
		{
			return !isLocked;
		}

		////////////////////////////////////////
		// IPartMassModifier

		public float GetModuleMass(float defaultMass, ModifierStagingSituation situation)
		{
			return part.partInfo.partPrefab.mass * (Mathf.Pow(scalingFactor, scaleMass) * (0.01f * (75f + motorSizeFactor * 0.25f)) - 1f);
		}

		public ModifierChangeWhen GetModuleMassChangeWhen()
		{
			return ModifierChangeWhen.FIXED;
		}

		////////////////////////////////////////
		// IPartCostModifier

		public float GetModuleCost(float defaultCost, ModifierStagingSituation situation)
		{
			return part.partInfo.cost * ((0.2f + 0.8f * scalingFactor) * (0.01f * (50f + motorSizeFactor * 0.5f)) - 1f);
		}

		public ModifierChangeWhen GetModuleCostChangeWhen()
		{
			return ModifierChangeWhen.FIXED;
		}

		////////////////////////////////////////
		// IModuleInfo

		string IModuleInfo.GetModuleTitle()
		{
			return "Robotics";
		}

		string IModuleInfo.GetInfo()
		{
			if(isFreeMoving)
				return "free moving";

			if(availableModes == null)
				ParseAvailableModes();

			if(availableInputModes == null)
				ParseAvailableInputModes();

			string info = "";

			for(int i = 0; i < availableModes.Count; i++)
			{
				switch(availableModes[i])
				{
				case ModeType.servo:
					for(int j = 0; j < availableInputModes.Count; j++)
					{
						switch(availableInputModes[j])
						{
						case InputModeType.manual:
							if(info.Length > 0) info += "\n";
							info += "servo mode";
							break;
						case InputModeType.control:
							if(info.Length > 0) info += "\n";
							info += "control mode";
							break;
						case InputModeType.linked:
							break;
						case InputModeType.tracking:
							if(info.Length > 0) info += "\n";
							info += "sun tracking mode";
							break;
						}
					}
					break;

				case ModeType.rotor:
					if(info.Length > 0) info += "\n";
					info += "rotor mode";
					break;
				}
			}

			if(info.Length > 0)
				info += "\n\n";

			info += "<b><color=orange>Requires:</color></b>\n- <b>Electric Charge: </b>when moving";

			return info;
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
		// IResourceConsumer

		private List<PartResourceDefinition> consumedResources;

		public List<PartResourceDefinition> GetConsumedResources()
		{
			return consumedResources;
		}

		////////////////////////////////////////
		// IConstruction

		public bool CanBeDetached()
		{
			return !IsMoving;
		}

		public bool CanBeOffset()
		{
			return !IsMoving;
		}

		public bool CanBeRotated()
		{
			return !IsMoving;
		}

		////////////////////////////////////////
		// Ferram Aerospace Research

		private int _far_counter = 60;
		private float _far_lastPosition = 0f;

		private void ProcessShapeUpdates()
		{
			if(--_far_counter > 0)
				return;

			if(Math.Abs(_far_lastPosition - position) >= 0.005f)
			{
				part.SendMessage("UpdateShapeWithAnims");
				foreach(var p in part.children)
					p.SendMessage("UpdateShapeWithAnims");

				_far_lastPosition = position;
			}

			_far_counter = 60;
		}

		////////////////////////////////////////
		// Debug

		private MultiLineDrawer ld;

		private void DebugInit()
		{
			ld = new MultiLineDrawer();
			ld.Create(null);
		}

		private void DrawPointer(int idx, Vector3 p_vector)
		{
			ld.Draw(idx, Vector3.zero, p_vector);
		}

		public void DrawRelative(int idx, Vector3 p_from, Vector3 p_vector)
		{
			ld.Draw(idx, p_from, p_from + p_vector);
		}

		private void DrawAxis(int idx, Transform p_transform, Vector3 p_vector, bool p_relative, Vector3 p_off)
		{
			ld.Draw(idx, p_transform.position + p_off, p_transform.position + p_off
				+ (p_relative ? p_transform.TransformDirection(p_vector) : p_vector));
		}

		private void DrawAxis(int idx, Transform p_transform, Vector3 p_vector, bool p_relative)
		{ DrawAxis(idx, p_transform, p_vector, p_relative, Vector3.zero); }

		// draw the limits without any correction
		private void DrawInitLimits(int idx)
		{
			if(!Joint)
				return;

		//	float low = Joint.lowAngularXLimit.limit;
		//	float high = Joint.highAngularXLimit.limit;

			float low = (swap ? -_maxPositionLimit : _minPositionLimit);
			float high = (swap ? -_minPositionLimit : _maxPositionLimit);

			DrawAxis(idx, Joint.transform,
				(swap ? -Joint.transform.up : Joint.transform.up), false);
			DrawAxis(idx + 1, Joint.transform,
				Quaternion.AngleAxis(-low, Joint.transform.TransformDirection(Joint.axis)) * (swap ? -Joint.transform.up : Joint.transform.up), false);
			DrawAxis(idx + 2, Joint.transform,
				Quaternion.AngleAxis(-high, Joint.transform.TransformDirection(Joint.axis)) * (swap ? -Joint.transform.up : Joint.transform.up), false);
		}
		
		// draw the limits
		private void DrawLimits(int idx, float pos)
		{
			if(!Joint)
				return;

		//	float low = Joint.lowAngularXLimit.limit;
		//	float high = Joint.highAngularXLimit.limit;

			float min = swap ? (hasPositionLimit ? -_maxPositionLimit : -maxPosition) : (hasPositionLimit ? _minPositionLimit : minPosition);
			float max = swap ? (hasPositionLimit ? -_minPositionLimit : -minPosition) : (hasPositionLimit ? _maxPositionLimit : maxPosition);

			float low = min + (swap ? correction_1-correction_0 : correction_0-correction_1);
			float high = max + (swap ? correction_1-correction_0 : correction_0-correction_1);

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

