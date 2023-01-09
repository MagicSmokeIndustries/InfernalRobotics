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

	public class ModuleIRServo_v3 : PartModule, IServo, IJointLockState, IModuleInfo, IRescalable
//, IResourceConsumer, IConstruction FEHLER, hinzufügen
	{
		static private bool constantsLoaded = false;
		static private float resetPrecisionRotational = 4f;
		static private float resetPrecisionTranslational = 2f;

		////////////////////////////////////////
		// Data

		private ModuleIRMovedPart MovedPart;

		private ConfigurableJoint Joint = null;

		[KSPField(isPersistant = false), SerializeField]
		private Vector3 axis = Vector3.right;	// x-axis of the joint
		[KSPField(isPersistant = false), SerializeField]
		private Vector3 pointer = Vector3.up;	// towards child (if possible), but always perpendicular to axis

		// true, if servo is attached reversed
		[KSPField(isPersistant = true)]
		private bool swap = false;

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
		private bool rot_jointup = true, rot_connectedup = true;
private Quaternion rot_jointup_q, rot_connectedup_q; // FEHLER, neuer Versuch
private Vector3 rot_jointup_, rot_connectedup_;

		private Vector3 trans_connectedzero;
		private float trans_zero;

		private float jointconnectedzero;
private float jointconnectedzero2; // FEHLER, Versuch

		/*
		 * position is an internal value and always relative to the current orientation
		 * of the joint (swap or not swap)
		 * all interface functions returning and expecting values do the swap internally
		 * and return and expect external values
		*/

		// position relative to current zero-point of joint
		[KSPField(isPersistant = true)]
		private float commandedPosition = 0.0f;
		private float position = 0.0f;

		private float lastUpdatePosition;

		// correction values for position
		// (required, since joints are always built straight, i.e. they always have their zero or
		// neutral points where they are created and they cannot be built angled)
		[KSPField(isPersistant = true)]
		private float correction_0 = 0.0f;
		[KSPField(isPersistant = true)]
		private float correction_1 = 0.0f;

		// correction values for user interaction
		private float jumpCorrectionCommandedPosition = 0.0f;
		private float jumpCorrectionPosition = 0.0f;

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
//List davon bauen ... und, save-load? für extra-joints?
//			und das verlängern? hmm... ja gut, evtl. auch eher in anderem Modul?

// FEHLER, weitere stabilityJoints bauen -> nicht an parent, sondern an andere Punkte (für multi-Rail-Idee) -> mal sehen wie's käme... evtl. erst nach der Release zwar
//---

		// Motor (works with position relative to current zero-point of joint, like position)
		Interpolator ip;

		float targetPositionSet;
		float targetSpeedSet;
			// FEHLER, die zwei Werte nochmal prüfen

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
		PartResourceDefinition electricResource = null;

		private bool hasElectricPower;

		[KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Current Draw", guiFormat = "F1", guiUnits = "mu/s")]
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
		int lightStatus = -3;
		Renderer lightRenderer;

		// Environment
		private bool isOnRails = false;

		////////////////////////////////////////
		// Data (servo)

		// Presets
		[KSPField(isPersistant = true)]
		private string presetsS = "";

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
				presetsS = PresetPositions.Aggregate(string.Empty, (current, s) => current + (s + "|"));
		}

		// Link-Mode
		[KSPField(isPersistant = true)]
		private uint LinkedInputPartId = 0;

		[KSPField(isPersistant = true)]
		private uint LinkedInputPartFlightId = 0;

		private ModuleIRServo_v3 LinkedInputPart = null;

		////////////////////////////////////////
		// Constructor

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

			GameEvents.onVesselCreate.Add(OnVesselCreate);
			GameEvents.onVesselWasModified.Add(OnVesselWasModified);

			GameEvents.onVesselGoOnRails.Add(OnVesselGoOnRails);
			GameEvents.onVesselGoOffRails.Add(OnVesselGoOffRails);

			GameEvents.onPhysicsEaseStart.Add(OnEaseStart);
			GameEvents.onPhysicsEaseStop.Add(OnEaseStop);

		//	GameEvents.onJointBreak.Add(OnJointBreak); -> currently we use OnVesselWasModified

			if(HighLogic.LoadedSceneIsEditor)
			{
				GameEvents.onEditorPartPlaced.Add(OnEditorPartPlaced);
				GameEvents.onEditorStarted.Add(OnEditorStarted);
			}
		}

		public override void OnStart(StartState state)
		{
			base.OnStart(state);

			// Renderer for lights
			lightRenderer = part.gameObject.GetComponentInChildren<Renderer>(); // do this before workaround

			if(state == StartState.Editor)
			{
				part.OnEditorAttach = (Callback)Delegate.Combine(part.OnEditorAttach, new Callback(OnEditorAttached));
				part.OnEditorDetach = (Callback)Delegate.Combine(part.OnEditorDetach, new Callback(OnEditorDetached));

				try
				{
					InitializeMeshes(true);
				}
				catch(Exception)
				{}

				InitializeValues();

				if(LinkedInputPartId != 0)
				{
					if(LinkedInputPartFlightId != 0)
						LinkedInputPartFlightId = 0;

					for(int i = 0; i < EditorLogic.fetch.ship.parts.Count; i++)
					{
						if(EditorLogic.fetch.ship.parts[i].persistentId == LinkedInputPartId)
						{ LinkedInputPart = EditorLogic.fetch.ship.parts[i].GetComponent<ModuleIRServo_v3>(); break; }
					}
				}
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

				electricResource = PartResourceLibrary.Instance.GetDefinition("ElectricCharge");

				if(LinkedInputPartId != 0)
				{
					if(LinkedInputPartFlightId != 0)
					{
						for(int i = 0; i < vessel.parts.Count; i++)
						{
							if(vessel.parts[i].flightID == LinkedInputPartFlightId)
							{ LinkedInputPart = vessel.parts[i].GetComponent<ModuleIRServo_v3>(); break; }
						}
					}
					else
					{
						for(int i = 0; i < vessel.parts.Count; i++)
						{
							if(vessel.parts[i].persistentId == LinkedInputPartId)
							{ LinkedInputPart = vessel.parts[i].GetComponent<ModuleIRServo_v3>(); LinkedInputPartFlightId = LinkedInputPart.part.flightID; break; }
						}
					}
				}
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

			if(part.attachJoint && part.attachJoint.Joint && (Joint != part.attachJoint.Joint))
				Initialize1();
	
			// initialize all objects we move (caputre their relative positions)
			if(part.attachJoint && part.attachJoint.Joint)
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

		//	GameEvents.onJointBreak.Remove(OnJointBreak); -> currently we use OnVesselWasModified

// FEHLER, scene ist Mist beim Destroy -> ist aber egal, das Zeug einfach immer tun
//			if(HighLogic.LoadedSceneIsEditor)
			{
				GameEvents.onEditorPartPlaced.Remove(OnEditorPartPlaced);
				GameEvents.onEditorStarted.Remove(OnEditorStarted);
			}

			if(/*HighLogic.LoadedSceneIsFlight &&*/ CollisionManager4.Instance /* && activateCollisions -> remove it always, just to be sure*/)
				CollisionManager4.Instance.UnregisterServo(this);

			if(LimitJoint)
				Destroy(LimitJoint);

			if(StabilityJoint[0])
				Destroy(StabilityJoint[0]);
			if(StabilityJoint[1])
				Destroy(StabilityJoint[1]);
		}

		public override void OnSave(ConfigNode node)
		{
			base.OnSave(node);

			SerializePresets();
		}

		public override void OnLoad(ConfigNode config)
		{
			base.OnLoad(config);

			if(HighLogic.LoadedSceneIsEditor)
				InitializeValues(); // FEHLER, sind jetzt die Daten schon drin? -> ja, unklar, ob das nötig ist hier -> Initialize1 ruft's auf, darum hab ich's hierher gepackt -> die Frage ist nur, ob das der Editor braucht

			UpdateUI();
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
					Joint.targetRotation = Quaternion.AngleAxis(-(commandedPosition + lockPosition), Vector3.right); // rotate always around x axis!!
				else
					Joint.targetPosition = Vector3.right * (trans_zero - (commandedPosition + lockPosition)); // move always along x axis!!
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

				// initialize all objects we move (caputre their relative positions)
				if(part.attachJoint && part.attachJoint.Joint)
					MovedPart = ModuleIRMovedPart.InitializePart(part);
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

				// initialize all objects we move (caputre their relative positions)
				if(part.attachJoint && part.attachJoint.Joint)
					MovedPart = ModuleIRMovedPart.InitializePart(part);
						// FEHLER, jeweils überall noch ein -> sonst rausschmeissen das Modul einbauen? wär das nötig?/besser??
			}
		}

		public void OnEditorAttached()	// FEHLER, das ist zwar jetzt richtig, weil die Teils immer auf 0 stehen, wenn sie nicht attached sind, aber... mal ehrlich... das müsste sauberer werden
		{
			swap = FindSwap();
			fixedMeshTransform = KSPUtil.FindInPartModel(transform, swap ? movingMesh : fixedMesh);

			FixChildrenAttachement();
		}

		public void OnEditorDetached()
		{
			/*
			 * Remarks:
			 * 
			 * KSP does send onEditorDetached without sending a onEditorAttached for symmetry-objects
			 * in this case we don't have a fixedMeshTransform and don't need to do anything
			 * 
			 * it is possible that this could also be because of an error -> in this case we wouldn't
			 * detect this anymore... no idea if this could be a problem
			 */
			if(fixedMeshTransform == null)
				return;

			EditorReset();
		}

		public void OnEditorPartPlaced(Part potentialChild)
		{
			if(potentialChild && (potentialChild.parent == part))
				FixChildrenAttachement();

			// we need to fix this special value
			Events["RemoveFromSymmetry2"].guiActiveEditor = (part.symmetryCounterparts.Count > 0);
		}

		public void OnEditorStarted()
		{
			FixChildrenAttachement();
		}

		////////////////////////////////////////
		// Functions

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
		}

		// corrects all the values to valid values
		private void InitializeValues()
		{
			_minPositionLimit = Mathf.Clamp(_minPositionLimit, minPosition, maxPosition);
			_maxPositionLimit = Mathf.Clamp(_maxPositionLimit, minPosition, maxPosition);

			minmaxPositionLimit.x = MinPositionLimit;
			minmaxPositionLimit.y = MaxPositionLimit;
			requestedPosition = CommandedPosition;

			ip.maxAcceleration = accelerationLimit * factorAcceleration;
			ip.maxSpeed = Mathf.Clamp(speedLimit * groupSpeedFactor, 0.1f, maxSpeed) * factorSpeed;

			if(availableModes == null)
				ParseAvailableModes();

			if(availableInputModes == null)
				ParseAvailableInputModes();

			ParsePresetPositions();

// FEHLER inputmodes setzen -> je nach av-modes -> fehlt noch sowas von...
		}

		private bool FindSwap()
		{
			AttachNode nodeToParent = part.FindAttachNodeByPart(part.parent); // always exists

			if(nodeToParent == null)
				return false; // should never happen -> no idea if swapped or not

			string[] nodeIds = fixedMeshNode.Split('|');
			foreach(string nodeId in nodeIds)
			{
				if(nodeToParent.id == nodeId)
					return false;
			}

			return true;
		}

		private void InitializeMeshes(bool bCorrectMeshPositions)
		{
			// detect attachment mode and calculate correction angles
			if(swap != FindSwap())
			{
				swap = !swap;

				if(!swap)
					correction_0 += (commandedPosition + lockPosition);
				else
					correction_1 += (commandedPosition + lockPosition);
			}
			else
			{
				if(swap)
					correction_0 += (commandedPosition + lockPosition);
				else
					correction_1 += (commandedPosition + lockPosition);
			}
			commandedPosition = -lockPosition;
			requestedPosition = CommandedPosition;

			position = 0.0f;
			lastUpdatePosition = 0.0f;

			// reset workaround
			if(fixedMeshTransform)
				fixedMeshTransform.parent = fixedMeshTransformParent;

			// find non rotating mesh
			fixedMeshTransform = KSPUtil.FindInPartModel(transform, swap ? movingMesh : fixedMesh);

			// find middle meshes (only for translational joints) -> the meshes that will be shown between the moving and fixed mesh
			if(!isRotational && (middleMeshes.Length > 0))
			{
				movingMeshTransform = KSPUtil.FindInPartModel(transform, swap ? fixedMesh : movingMesh);

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
					KSPUtil.FindInPartModel(transform, swap ? fixedMesh : movingMesh).rotation *= Quaternion.AngleAxis(-(swap ? correction_1 : correction_0), axis);
				}
				else
				{
					fixedMeshTransform.Translate(axis.normalized * (-(swap ? correction_0 : correction_1)));
					KSPUtil.FindInPartModel(transform, swap ? fixedMesh : movingMesh).Translate(axis.normalized * (-(swap ? correction_1 : correction_0)));
				}
			}


			fixedMeshAnchor = GameObject.CreatePrimitive(PrimitiveType.Cube);
			fixedMeshAnchor.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
			fixedMeshAnchor.SetActive(true);

			DestroyImmediate(fixedMeshAnchor.GetComponent<Collider>());
			fixedMeshAnchor.GetComponent<Renderer>().enabled = false;

			Rigidbody rb = fixedMeshAnchor.AddComponent<Rigidbody>();
			rb.mass = 1e-6f;
			rb.useGravity = false;

			Transform tp = ((Joint.gameObject == part.gameObject) ? Joint.connectedBody.transform : Joint.transform);
			fixedMeshAnchor.transform.position = fixedMeshTransform.parent.position;
			fixedMeshAnchor.transform.rotation = fixedMeshTransform.parent.rotation;
			fixedMeshAnchor.transform.parent = ((Joint.gameObject == part.gameObject) ? Joint.transform : Joint.connectedBody.transform);

			fixedMeshTransform.parent = fixedMeshAnchor.transform;

			FixedJoint fj = fixedMeshAnchor.AddComponent<FixedJoint>();
			fj.connectedBody = ((Joint.gameObject == part.gameObject) ? Joint.connectedBody : part.rb);
		}

		private void InitializeDrive()
		{
			// [https://docs.nvidia.com/gameworks/content/gameworkslibrary/physx/guide/Manual/Joints.html]
			// force = spring * (targetPosition - position) + damping * (targetVelocity - velocity)

			if(mode != ModeType.rotor)
			{
				JointDrive drive = new JointDrive
				{
					maximumForce = isLocked ? PhysicsGlobals.JointForce : (isFreeMoving ? 1e-20f : forceLimit * factorForce),
					positionSpring = hasSpring ? jointSpring : 60000f,
					positionDamper = hasSpring ? jointDamping : 0.0f
				};
				// FEHLER, evtl. sollten wir doch mit dem Damper-Wert arbeiten? damit nicht alles total ohne Reibung dreht... also z.B. bei isFreeMoving den Wert auf 100 oder so setzen? -> oder konfigurierbar bzw. dann das forceLimit oder friction oder so nehmen?

				if(isRotational)	Joint.angularXDrive = drive;
				else				Joint.xDrive = drive;
			}
			else
			{
				Joint.angularXDrive = new JointDrive
					{
						maximumForce = PhysicsGlobals.JointForce,
						positionSpring = 1e-12f,
						positionDamper = jointDamping				// FEHLER, na ja... was soll ich tun sonst? sonst kann das ja keiner konfigurieren? wobei... eben... na egal mal
					};
			}
		}

		private void InitializeLimits()
		{
			if(mode != ModeType.rotor)
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

					if(!swap)
						trans_zero = -jointconnectedzero + min + halfrange;
					else
						trans_zero = -jointconnectedzero + max - halfrange; // FEHLER, echt jetzt? hier und beim auslesen muss ich jointconnectedzero negieren?
							// FEHLER, vereinfachbar?? bzw. richtig? zuerstmal...

	Vector3 _axis = Joint.transform.InverseTransformVector(part.transform.TransformVector(axis)); // FEHLER, beschreiben wieso -> joint inverse (nicht part, nur config-joint)
					Joint.connectedAnchor = Joint.connectedBody.transform.InverseTransformPoint(
						Joint.transform.TransformPoint(_axis.normalized * (trans_zero - position)));

					Joint.targetPosition = Vector3.right * (trans_zero - commandedPosition); // move always along x axis!!

					Joint.linearLimit = new SoftJointLimit{ limit = halfrange };

bool bUseStabilityJoints = true; // FEHLER, das wieder global vermerken aber die Schnittstelle verändern und eine Funktion bauen zum bau dieser Joints... also echt jetzt

					// add stability joints
					if(bUseStabilityJoints)
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
							StabilityJoint[i].xDrive = new JointDrive
							{ maximumForce = 0f, positionSpring = 0f, positionDamper = 0f };

		//					new JointDrive
		//					{ maximumForce = PhysicsGlobals.JointForce, positionSpring = 60000f, positionDamper = 0f };

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

				min += (!swap ? correction_0-correction_1 : correction_1-correction_0);
				max += (!swap ? correction_0-correction_1 : correction_1-correction_0);

				bool isModulo = isRotational && !hasMinMaxPosition && !hasPositionLimit
					&& ((mode == ModeType.servo) && (inputMode != InputModeType.control));

				ip.Initialize(commandedPosition, isModulo,
					isModulo ? to360(min) : min,
					isModulo ? to360(max) : max,
					(mode == ModeType.servo) ? (Mathf.Clamp(speedLimit * groupSpeedFactor, 0.1f, maxSpeed) * factorSpeed) : (maxSpeed * factorSpeed),
					(mode == ModeType.servo) ? (accelerationLimit * factorAcceleration) : (maxAcceleration * factorAcceleration),
					isRotational ? resetPrecisionRotational : resetPrecisionTranslational);

				targetPositionSet = ip.TargetPosition;
				targetSpeedSet = ip.TargetSpeed;

//				if(CompareValueAbsolute(requestedPosition, targetPositionSet))
//					requestedPosition = targetPositionSet;
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
				rot_jointup =
					Vector3.ProjectOnPlane(Joint.transform.up.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude >
					Vector3.ProjectOnPlane(Joint.transform.right.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude;

rot_jointup_q =
		Quaternion.Inverse(Joint.transform.rotation) *
				Quaternion.FromToRotation(Joint.transform.up.normalized,
					Vector3.ProjectOnPlane(rot_jointup ? Joint.transform.up.normalized : Joint.transform.right.normalized,
						Joint.transform.TransformDirection(Joint.axis)).normalized);

rot_jointup_ = Joint.transform.InverseTransformVector(
	Vector3.ProjectOnPlane(rot_jointup ? Joint.transform.up.normalized : Joint.transform.right.normalized, Joint.transform.TransformDirection(Joint.axis)).normalized);

		rot_connectedup =
					Vector3.ProjectOnPlane(Joint.connectedBody.transform.up.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude >
					Vector3.ProjectOnPlane(Joint.connectedBody.transform.right.normalized, Joint.transform.TransformDirection(Joint.axis)).magnitude;

rot_connectedup_q =
		Quaternion.Inverse(Joint.connectedBody.transform.rotation) *
				Quaternion.FromToRotation(Joint.connectedBody.transform.up.normalized,
					Vector3.ProjectOnPlane(rot_connectedup ? Joint.connectedBody.transform.up.normalized : Joint.connectedBody.transform.right.normalized,
						Joint.transform.TransformDirection(Joint.axis)).normalized);

rot_connectedup_ = Joint.connectedBody.transform.InverseTransformVector(
	Vector3.ProjectOnPlane(rot_connectedup ? Joint.connectedBody.transform.up.normalized : Joint.connectedBody.transform.right.normalized, Joint.transform.TransformDirection(Joint.axis)).normalized);

				jointconnectedzero = -Vector3.SignedAngle(
					rot_jointup ? Joint.transform.up : Joint.transform.right,
					rot_connectedup ? Joint.connectedBody.transform.up : Joint.connectedBody.transform.right,
					Joint.transform.TransformDirection(Joint.axis));

jointconnectedzero = -Vector3.SignedAngle(
	Joint.transform.rotation * rot_jointup_q * Joint.transform.up, Joint.connectedBody.transform.rotation * rot_connectedup_q * Joint.connectedBody.transform.up,
		Joint.transform.TransformDirection(Joint.axis));

jointconnectedzero = -Vector3.SignedAngle(
					Joint.transform.TransformVector(rot_jointup_), Joint.connectedBody.transform.TransformVector(rot_connectedup_),
						Joint.transform.TransformDirection(Joint.axis));
			}
			else
			{
				jointconnectedzero = (swap ? correction_0 : correction_1); // - minPosition;
				
				trans_connectedzero = Joint.connectedBody.transform.InverseTransformPoint(
					Joint.transform.TransformPoint(Joint.anchor) + (Joint.transform.TransformDirection(Joint.axis).normalized * (-jointconnectedzero + minPosition)));
			}

			Initialize2();
		}

		private void Initialize2()
		{
			Joint.rotationDriveMode = RotationDriveMode.XYAndZ;

			// we don't modify *Motion, angular*Motion and the drives we don't need
				// -> KSP defaults are ok for us

			if(mode != ModeType.rotor)
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

UpdateUI(); // FEHLER, quick bugfix -> Werte werden beim Start viel zu spät initialisiert -> das nochmal überarbeiten
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
				highAngularXLimit = new SoftJointLimit() { limit = -(p_min - position + (!swap? correction_0-correction_1 : correction_1-correction_0)) };
			}
			else
			{
				lowAngularXLimit = new SoftJointLimit() { limit = -(p_max - position - (!swap ? correction_1-correction_0 : correction_0-correction_1))};
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
// FEHLER, neueste Idee -> wenn einer an unseren Joints dran ist, die sich nicht bewegen sollen, dann hängen wir sie um -> wobei... evtl. auch nicht... evtl. hängen wir nur die attach-Points um... aber, mal sehen -> kommt etwas später
			foreach(Part child in part.children)
			{
				AttachNode nodeToChild = part.FindAttachNodeByPart(child);

				if(nodeToChild == null)
					continue; // ignore this one - FEHLER, nur mal zur Sicherheit, bin nicht sicher, ob's überhaupt passieren könnte

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

		private static float to180(float v)
		{
			while(v > 180f) v -= 360f;
			while(v < -180f) v += 360f;
			return v;
		}

		private static float to360(float v)
		{
			while(v > 360f) v -= 360f;
			while(v < -360f) v += 360f;
			return v;
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
	fixedDeltaTime /= TimeWarp.CurrentRate; // FEHLER, Test

				ip.ResetPosition(position);
				ip.PrepareUpdate(fixedDeltaTime);

				double amountToConsume = 60f * electricChargeRequired * fixedDeltaTime; // why 60? seems to be a good value... -> makes our consumption around the same as stock

				amountToConsume *= ForceLimit / MaxForce;
				amountToConsume *= (ip.NewSpeed + ip.Speed) / (2 * maxSpeed * factorSpeed);

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

				//			return amountConsumed >= 0.0;
				bool bR = amountConsumed >= amountToConsume * 0.95; //-> FEHLER, überlegen, früher war's ==, scheint aber nicht mehr zu gehen mit neuem KSP

if(!bR)
{
					if (ip.TargetPosition != position)
						bR = true;		
}
				return bR;
			}
			else
			{
				double amountToConsume = 0.9f * electricChargeRequired * TimeWarp.fixedDeltaTime; // why 0.9? seems to be a good value...

				amountToConsume *= Math.Abs(Joint.targetAngularVelocity.x) / (2 * maxSpeed);
					// like this we consume half of the maximum possible when running at full speed

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

				//			return amountConsumed >= 0.0;
				return amountConsumed >= amountToConsume * 0.95; //-> FEHLER, überlegen, früher war's ==, scheint aber nicht mehr zu gehen mit neuem KSP
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

		// set original rotation to new rotation
		private void UpdatePosition()
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
				return (swap ? -(position + jumpCorrectionCommandedPosition) : (position + jumpCorrectionCommandedPosition)) + zeroNormal + correction_1 - correction_0;
			else
				return (swap ? (position + jumpCorrectionCommandedPosition) : -(position + jumpCorrectionCommandedPosition)) + zeroInvert - correction_1 + correction_0;
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
						float requestedCommandedPosition = TransformPosition(LinkedInputPart.InverseTransformPosition(LinkedInputPart.commandedPosition));

						if(commandedPosition != requestedCommandedPosition)
							EditorSetPosition(requestedCommandedPosition);
					}

					// ?? Bug in KSP ?? we need to reset this on every frame, because highliting the parent part (in some situations) sets this to another value
					lightRenderer.SetPropertyBlock(part.mpb);

					SetColor(1);

					ProcessShapeUpdates();
				}

				return;
			}

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
/*
				// FEHLER, xtreme-Debugging, ich such was
	DrawAxis(1, Joint.transform, rot_jointup ? Joint.transform.up : Joint.transform.right, false);
	DrawAxis(2, Joint.connectedBody.transform, rot_connectedup ? Joint.connectedBody.transform.up : Joint.connectedBody.transform.right, false, Joint.transform.position - Joint.connectedBody.transform.position);
	DrawAxis(3, Joint.transform, Joint.axis, true);

DrawAxis(6, Joint.transform, rot_jointup_q * Joint.transform.up, false);
DrawAxis(7, Joint.connectedBody.transform, rot_connectedup_q * Joint.connectedBody.transform.up, false, Joint.transform.position - Joint.connectedBody.transform.position);
*/

				// read new position
				float newPosition =
					-Vector3.SignedAngle(
						rot_jointup ? Joint.transform.up : Joint.transform.right,
						rot_connectedup ? Joint.connectedBody.transform.up : Joint.connectedBody.transform.right,
						Joint.transform.TransformDirection(Joint.axis))
					- jointconnectedzero;

newPosition =
				-Vector3.SignedAngle(
//					Joint.transform.rotation * rot_jointup_q * Joint.transform.up, Joint.connectedBody.transform.rotation * rot_connectedup_q * Joint.connectedBody.transform.up,
					Joint.transform.TransformVector(rot_jointup_), Joint.connectedBody.transform.TransformVector(rot_connectedup_),
						Joint.transform.TransformDirection(Joint.axis))
				- jointconnectedzero;

float newPosition2 =
				Joint.transform.localEulerAngles.x
				- jointconnectedzero2;

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
					//	part.AddTorque(-(newPosition - position) * jointDamping * 0.001f * (Vector3d)GetAxis());
						// -> das funktioniert super aber ich probier noch was anderes

					// set new position
					if(!hasMinMaxPosition && (Math.Abs(position - newPosition) >= 180f))
					{
						if(newPosition < position)
						{ jumpCorrectionPosition += 360f; if(Math.Abs(Position) > 360f) jumpCorrectionPosition -= 360f; }
						else
						{ jumpCorrectionPosition -= 360f; if(Math.Abs(Position) > 360f) jumpCorrectionPosition += 360f; }
					}

					position = newPosition;

					// Feder bei uncontrolled hat keinen Sinn... das wär nur bei Motoren sinnvoll... und dafür ist das Dämpfen bei Motoren wiederum nicht sehr sinnvoll...
					// ausser ... man macht's wie das alte IR... setzt die Spring auf fast nix und wendet dann eine Kraft an und eine Dämpfung...
					// -> genau das machen wir jetzt mal hier ...

					if((isFreeMoving || (mode == ModeType.rotor)) && !isLocked)
					{
						float newCommandedPosition = Mathf.Clamp(position, _minPositionLimit, _maxPositionLimit);

						if(!hasMinMaxPosition && (Math.Abs(commandedPosition - newCommandedPosition) >= 180f))
						{
							if(newCommandedPosition < commandedPosition)
							{ jumpCorrectionCommandedPosition += 360f; if(Math.Abs(CommandedPosition) > 360f) jumpCorrectionCommandedPosition -= 360f; }
							else
							{ jumpCorrectionCommandedPosition -= 360f; if(Math.Abs(CommandedPosition) > 360f) jumpCorrectionCommandedPosition += 360f; }
						}

						commandedPosition = newCommandedPosition;
						if(!requestedPositionIsDefined)
							requestedPosition = CommandedPosition;

						Joint.targetRotation = Quaternion.AngleAxis(-commandedPosition, Vector3.right); // rotate always around x axis!!
					}

					if((mode == ModeType.servo) && bUseDynamicLimitJoint)
					{
						float min = swap ? (hasPositionLimit ? -_maxPositionLimit : -maxPosition) : (hasPositionLimit ? _minPositionLimit : minPosition);
						float max = swap ? (hasPositionLimit ? -_minPositionLimit : -minPosition) : (hasPositionLimit ? _maxPositionLimit : maxPosition);

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

				if(!float.IsNaN(newPosition) && !float.IsInfinity(newPosition)) // FEHLER, nur zur Sicherheit -> xtreme-debugging?
				{
					if(swap)
						newPosition = -(newPosition + jointconnectedzero); // minus, because magnitude is always positive
					else
						newPosition = newPosition - jointconnectedzero;

					newPosition += minPosition; // FEHLER, unklar... ich hab trans_connectedzero verändert und jetzt muss ich das noch korrigieren... stimmt das? weiss ned... mal sehen

					// manuell dämpfen der Bewegung
					// siehe Rotation, für das hier hab ich das nie ausprobiert

					position = newPosition;

					// Feder bei uncontrolled hat keinen Sinn... das wär nur bei Motoren sinnvoll... und dafür ist das Dämpfen bei Motoren wiederum nicht sehr sinnvoll...
					// ausser ... man macht's wie das alte IR... setzt die Spring auf fast nix und wendet dann eine Kraft an und eine Dämpfung...
					// -> genau das machen wir jetzt mal hier ...

					if(isFreeMoving)
						Joint.targetPosition = Vector3.right * (trans_zero - position); // move always along x axis!!

					if(middleMeshesTransform != null)
					{
						Vector3 localPosition = transform.InverseTransformPoint(fixedMeshTransform.position);
						Vector3 distance = localPosition - movingMeshTransform.localPosition;
						float fraction = 1f / (float)(middleMeshesTransform.Length + 1);
						for(int i = 0; i < middleMeshesTransform.Length; i++)
							middleMeshesTransform[i].localPosition = localPosition + distance * ((i + 1) * fraction);
					}
				}
			}

			// process current input

			if(mode == ModeType.servo)
			{
				if(inputMode == InputModeType.linked)
				{
					if(LinkedInputPart != null)
						ip.SetCommand(TransformPosition(LinkedInputPart.InverseTransformPosition(LinkedInputPart.ip.TargetPosition)), LinkedInputPart.ip.TargetSpeed, false);
				}

				if(ip.IsMoving)
				{
					// verify if enough electric charge is available and consume it
					// or if that's not possible, command a stop and ask, if we still have a movement
					// in case there is a movement, do all the updating of the positions and play the sound

					if(UpdateAndConsumeElectricCharge() || IsStopping())
					{
						soundSound.Play();

						ip.Update();

						float newCommandedPosition = ip.GetPosition();

						if(!hasMinMaxPosition && (Math.Abs(commandedPosition - newCommandedPosition) >= 180f))
						{
							if(newCommandedPosition < commandedPosition)
							{ jumpCorrectionCommandedPosition += 360f; if(Math.Abs(CommandedPosition) > 360f) jumpCorrectionCommandedPosition -= 360f; }
							else
							{ jumpCorrectionCommandedPosition -= 360f; if(Math.Abs(CommandedPosition) > 360f) jumpCorrectionCommandedPosition += 360f; }
						}

						commandedPosition = newCommandedPosition;
						if(!requestedPositionIsDefined)
							requestedPosition = CommandedPosition;

						if(isRotational)
							Joint.targetRotation = Quaternion.AngleAxis(-commandedPosition, Vector3.right); // rotate always around x axis!!
						else
							Joint.targetPosition = Vector3.right * (trans_zero - commandedPosition); // move always along x axis!!
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
						newSpeed = 0.0f;
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

						newSpeed *= 0.01f * 5f * maxSpeed; // FEHLER, SpeedLimit nutzen? und das dann anzeigen im Rotor-Modus?

						newSpeed = Mathf.Clamp(_isRunning * newSpeed, -5f * maxSpeed, 5f * maxSpeed);

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

					float newSpeed = 0.0f;

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
				
				if(mode != ModeType.rotor)
					pitchMultiplier = Math.Max(Math.Abs(CommandedSpeed / factorSpeed), 0.05f);
				else
					pitchMultiplier = Math.Max(Math.Abs(Joint.targetAngularVelocity.x) * 0.04f, 0.05f);

				if(pitchMultiplier > 1)

					pitchMultiplier = (float)Math.Sqrt(pitchMultiplier);

				soundSound.Update(soundVolume, soundPitch * pitchMultiplier);
			}

			if(HighLogic.LoadedSceneIsFlight)
			{
				if(mode == ModeType.servo) // FEHLER, komisch, das prüfen wir hier, die anderen Modi im Fixed??
					CheckInputs();

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

		public Vector3 GetAxis()
		{ return Joint.transform.TransformDirection(Joint.axis).normalized; }

		public Vector3 GetSecAxis()
		{ return Joint.transform.TransformDirection(Joint.secondaryAxis).normalized; }

		////////////////////////////////////////
		// Properties

// FEHLER sym?
		[KSPField(isPersistant = true)]
		public string servoName = "";

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
		private string groupName = "New Group";

// FEHLER, fraglich, wie das im Gui auszusehen hat... lösen wir erstmal den Rest :-)
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
			get { return targetPositionSet; }
		}

		public float TargetSpeed
		{
			get { return targetSpeedSet; }
		}

		public float CommandedPosition
		{
			get
			{
				if(!isInverted)
					return (swap ? -(commandedPosition + jumpCorrectionCommandedPosition) : (commandedPosition + jumpCorrectionCommandedPosition)) + zeroNormal + correction_1 - correction_0;
				else
					return (swap ? (commandedPosition + jumpCorrectionCommandedPosition) : -(commandedPosition + jumpCorrectionCommandedPosition)) + zeroInvert - correction_1 + correction_0;
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
					return (swap ? -(position + jumpCorrectionPosition) : (position + jumpCorrectionPosition)) + zeroNormal + correction_1 - correction_0;
				else
					return (swap ? (position + jumpCorrectionPosition) : -(position + jumpCorrectionPosition)) + zeroInvert - correction_1 + correction_0;
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
		private ModeType mode = ModeType.servo;

		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Mode"),
			UI_ChooseOption(suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private int modeIndex = 0;

		private void onChanged_modeIndex(object o)
		{
/*			if(HighLogic.LoadedSceneIsFlight && (IsMoving || (Joint.targetAngularVelocity.x > 0.005f)))
			{
				ScreenMessages.PostScreenMessage(new ScreenMessage("Cannot change mode while in motion!", 3f, ScreenMessageStyle.UPPER_CENTER));
				modeIndex = availableModes.IndexOf(mode);
				UpdateUI(true);
				return;
			}*/ // FEHLER, kann man sowieso nicht mehr wechseln im Flug -> ist ein "anderer" Motor -> anderer Verbrauch etc. -> evtl. sogar anderen Preis machen... mal sehen

			mode = availableModes[modeIndex];

			if(mode != ModeType.servo)
				IsLimitted = false;

			if(Joint)
				Initialize2(); // FEHLER, evtl. nochmal aufräumen... das stimmt zwar, ist aber... na ja... :-) nicht mehr so super sauber wie auch schon mal
					// das Teil da setzt auch die neuen Accel/Speed maxima vom Interpolator

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().Mode = mode;

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
		private InputModeType inputMode = InputModeType.manual;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "InuputMode"),
			UI_ChooseOption(suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private int inputModeIndex = 0;

		private void onChanged_inputModeIndex(object o)
		{
			inputMode = availableInputModes[inputModeIndex];

	//		if(inputMode == InputModeType.tracking)
	//			IsLimitted = false;
	//		else
			if(inputMode != InputModeType.tracking)
				trackSun = false;

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().InputMode = inputMode;

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
		private float lockPosition = 0.0f;

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Lock"),
			UI_Toggle(enabledText = "Engaged", disabledText = "Disengaged", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private bool isLocked = false;

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
					lockPosition = 0.0f;

				if(isRotational)
					Joint.targetRotation = Quaternion.AngleAxis(-(commandedPosition + lockPosition), Vector3.right); // rotate always around x axis!!
				else
					Joint.targetPosition = Vector3.right * (trans_zero - (commandedPosition + lockPosition)); // move always along x axis!!

				InitializeDrive();

				vessel.CycleAllAutoStrut();
				GameEvents.onRoboticPartLockChanged.Fire(part, isLocked);
			}

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().IsLocked = isLocked;

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

		////////////////////////////////////////
		// Settings (servo)

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Invert Direction"),
			UI_Toggle(enabledText = "Inverted", disabledText = "Normal", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.None)]
		private bool isInverted = false;

		private void onChanged_isInverted(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().IsInverted = isInverted;

			minmaxPositionLimit.x = MinPositionLimit;
			minmaxPositionLimit.y = MaxPositionLimit;
			requestedPosition = CommandedPosition;

			UpdateUI();
		}

		public bool IsInverted
		{
			get { return isInverted; }
			set { if(object.Equals(isInverted, value)) return; isInverted = value; onChanged_isInverted(null); }
		}

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

		[KSPField(isPersistant = true)]
		private float zeroNormal = 0;
		[KSPField(isPersistant = true)]
		private float zeroInvert = 0;

		[KSPField(isPersistant = true)]
		private float defaultPosition = 0f;

		private void onChanged_defaultPosition(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().DefaultPosition = defaultPosition;

//			UpdateUI();
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
		private float forceLimit = 1f;

		private void onChanged_forceLimit(object o)
		{
			if(Joint)
				InitializeDrive();
	
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().ForceLimit = forceLimit;

//			UpdateUI(); // FEHLER, viel weniger aufrufen das Zeug, das ist einfach nur dämlich
		}

		public float ForceLimit
		{
			get { return forceLimit; }
			set
			{
				value = Mathf.Clamp(value, 0.1f, maxForce);

				if(object.Equals(forceLimit, value))
					return;

				forceLimit = value;

				onChanged_forceLimit(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Acceleration", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0.1f),
			UI_FloatRange(minValue = 0.1f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float accelerationLimit = 4f;

		private void onChanged_accelerationLimit(object o)
		{
			ip.maxAcceleration = accelerationLimit * factorAcceleration;

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().AccelerationLimit = accelerationLimit;

//			UpdateUI();
		}

		public float AccelerationLimit
		{
			get { return accelerationLimit; }
			set
			{
				value = Mathf.Clamp(value, 0.1f, maxAcceleration);

				if(object.Equals(accelerationLimit, value))
					return;

				accelerationLimit = value;

				onChanged_accelerationLimit(null);
			}
		}

		[KSPField(isPersistant = true)]
		private float defaultSpeed = 0f;

		public float DefaultSpeed
		{
			get { return defaultSpeed < 0.1f ? SpeedLimit : Mathf.Clamp(defaultSpeed, 0.1f, SpeedLimit); }
			set { defaultSpeed = value; }
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max Speed", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0.1f),
			UI_FloatRange(minValue = 0.1f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float speedLimit = 1f;

		private void onChanged_speedLimit(object o)
		{
			ip.maxSpeed = Mathf.Clamp(speedLimit * groupSpeedFactor, 0.1f, maxSpeed) * factorSpeed;

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().SpeedLimit = speedLimit;

//			UpdateUI();
		}

		public float SpeedLimit
		{
			get { return speedLimit; }
			set
			{
				value = Mathf.Clamp(value, 0.1f, maxSpeed);

				if(object.Equals(speedLimit, value))
					return;

				speedLimit = value;

				onChanged_speedLimit(null);
			}
		}

		private float groupSpeedFactor = 1f;

		public float GroupSpeedFactor
		{
			get { return groupSpeedFactor; }
			set
			{
				if(object.Equals(groupSpeedFactor, value))
					return;

				groupSpeedFactor = value;

				ip.maxSpeed = Mathf.Clamp(speedLimit * groupSpeedFactor, 0.1f, maxSpeed) * factorSpeed;

				for(int i = 0; i < part.symmetryCounterparts.Count; i++)
					part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().GroupSpeedFactor = groupSpeedFactor;
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Spring Force", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0.0f),
			UI_FloatRange(minValue = 0.0f, stepIncrement = 0.1f, suppressEditorShipModified = true, scene = UI_Scene.Editor, affectSymCounterparts = UI_Scene.All)]
		private float jointSpring = PhysicsGlobals.JointForce;

		private void onChanged_jointSpring(object o)
		{
			if(Joint)
				InitializeDrive();
	
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().SpringPower = jointSpring;

//			UpdateUI();
		}

		public float SpringPower 
		{
			get { return jointSpring; }
			set { if(object.Equals(jointSpring, value)) return; jointSpring = value; onChanged_jointSpring(null); }
		}

		[KSPAxisField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "Damping Force", guiFormat = "F1",
			axisMode = KSPAxisMode.Incremental, minValue = 0.0f),
			UI_FloatRange(minValue = 0.0f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, scene = UI_Scene.Editor, affectSymCounterparts = UI_Scene.All)]
		private float jointDamping = 5f; // FEHLER, war 0, aber Rotor muss es auf was stehen... die anderen ignorieren's glaub ich... daher mal zur Sicherheit 5 sonst dreht der ewig? weiss nicht, ich probier's mal

		private void onChanged_jointDamping(object o)
		{
			if(Joint)
				InitializeDrive();
	
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().DampingPower = jointDamping;

//			UpdateUI();
		}

		public float DampingPower 
		{
			get { return jointDamping; }
			set { if(object.Equals(jointDamping, value)) return; jointDamping = value; onChanged_jointDamping(null); }
		}

		[KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Electric Charge required", guiUnits = "u/s"), SerializeField]
		private float electricChargeRequired = 2.5f;

		// limits set by the user

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Limits"),
			UI_Toggle(enabledText = "Engaged", disabledText = "Disengaged", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private bool hasPositionLimit = false;

		private void onChanged_hasPositionLimit(object o)
		{
			if(hasPositionLimit)
			{
				if(!canHaveLimits
				|| (!isFreeMoving && IsMoving)
				|| (mode != ModeType.servo)
			/*	|| (inputMode == InputModeType.tracking)*/)
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

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().IsLimitted = hasPositionLimit;

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
		private float _minPositionLimit = -360f;

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

//				UpdateUI();
			}
		}

		[KSPField(isPersistant = true)]
		private float _maxPositionLimit = 360f;

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

//				UpdateUI();
			}
		}

		[KSPField(isPersistant = true)]
		private string forwardKey = "";
		[KSPField(isPersistant = true)]
		private string reverseKey = "";

		public string ForwardKey
		{
			get { return forwardKey; }
			set
			{
				forwardKey = value.ToLower();

				for(int i = 0; i < part.symmetryCounterparts.Count; i++)
					part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().forwardKey = forwardKey;
			}
		}

		public string ReverseKey
		{
			get { return reverseKey; }
			set
			{
				reverseKey = value.ToLower();

				for(int i = 0; i < part.symmetryCounterparts.Count; i++)
					part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().reverseKey = reverseKey;
			}
		}

		////////////////////////////////////////
		// Settings (servo - control input)

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Deflection Range", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float controlDeflectionRange = 0f;

		private void onChanged_controlDeflectionRange(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().controlDeflectionRange = controlDeflectionRange;

//			UpdateUI();
		}

		public float ControlDeflectionRange
		{
			get { return controlDeflectionRange; }
			set
			{
				value = Mathf.Clamp(value, 0f, 100f);

				if(object.Equals(controlDeflectionRange, value))
					return;

				controlDeflectionRange = value;

				onChanged_controlDeflectionRange(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Neutral Position", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = 0f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = 0f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float controlNeutralPosition = 0f;

		private void onChanged_controlNeutralPosition(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().controlNeutralPosition = controlNeutralPosition;

//			UpdateUI();
		}

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

				onChanged_controlNeutralPosition(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Pitch Control", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float pitchControl = 0f;

		private void onChanged_pitchControl(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().PitchControl = pitchControl;

//			UpdateUI();
		}

		public float PitchControl
		{
			get { return pitchControl; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(pitchControl, value))
					return;

				pitchControl = value;

				onChanged_pitchControl(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Roll Control", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float rollControl = 0f;

		private void onChanged_rollControl(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().RollControl = rollControl;

//			UpdateUI();
		}

		public float RollControl
		{
			get { return rollControl; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(rollControl, value))
					return;

				rollControl = value;

				onChanged_rollControl(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Yaw Control", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float yawControl = 0f;

		private void onChanged_yawControl(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().YawControl = yawControl;

//			UpdateUI();
		}

		public float YawControl
		{
			get { return yawControl; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(yawControl, value))
					return;

				yawControl = value;

				onChanged_yawControl(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Throttle Control", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float throttleControl = 0f;

		private void onChanged_throttleControl(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().ThrottleControl = throttleControl;

//			UpdateUI();
		}

		public float ThrottleControl
		{
			get { return throttleControl; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(throttleControl, value))
					return;

				throttleControl = value;

				onChanged_throttleControl(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "X Control", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float xControl = 0f;

		private void onChanged_xControl(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().XControl = xControl;

//			UpdateUI();
		}

		public float XControl
		{
			get { return xControl; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(xControl, value))
					return;

				xControl = value;

				onChanged_xControl(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Y Control", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float yControl = 0f;

		private void onChanged_yControl(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().YControl = yControl;

//			UpdateUI();
		}

		public float YControl
		{
			get { return yControl; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(yControl, value))
					return;

				yControl = value;

				onChanged_yControl(null);

			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Z Control", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float zControl = 0f;

		private void onChanged_zControl(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().ZControl = zControl;

//			UpdateUI();
		}

		public float ZControl
		{
			get { return zControl; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(zControl, value))
					return;

				zControl = value;

				onChanged_zControl(null);
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
				LinkedInputPart = null;

				UpdateUI();
			}
			else
			{
				GameObject go = new GameObject("PartSelectorHelper");
				Selector = go.AddComponent<InfernalRobotics_v3.Utility.PartSelector>();

				Selector.onSelectedCallback = onSelectedLinkInput;

				if(HighLogic.LoadedSceneIsFlight)
					Selector.AddAllPartsOfType<ModuleIRServo_v3>(vessel);
				else if(HighLogic.LoadedSceneIsEditor)
				{
					foreach(Part p in EditorLogic.fetch.ship.parts) // FEHLER, blöd... egal jetzt... -> genau wie oben Fkt. bauen
					{
						if(p.GetComponent<ModuleIRServo_v3>() != null)
							Selector.AddPart(p);
					}
				}

				Selector.StartSelection();
			}
		}

		////////////////////////////////////////
		// Settings (servo - track input)

		[KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Track Sun"),
			UI_Toggle(enabledText = "Engaged", disabledText = "Disengaged", suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private bool trackSun = false;

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
		private float trackAngle = 0f;

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
		private float rotorAcceleration = 4f;

		private void onChanged_rotorAcceleration(object o)
		{
			if(Joint)
				InitializeDrive();

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().RotorAcceleration = rotorAcceleration;

//			UpdateUI();
		}

		public float RotorAcceleration
		{
			get { return rotorAcceleration; }
			set
			{
				value = Mathf.Clamp(value, 0.1f, maxAcceleration);

				if(object.Equals(rotorAcceleration, value))
					return;

				rotorAcceleration = value;

				onChanged_rotorAcceleration(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Base Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float baseSpeed;

		private void onChanged_baseSpeed(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().BaseSpeed = baseSpeed;

//			UpdateUI();
		}

		public float BaseSpeed
		{
			get { return baseSpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(baseSpeed, value))
					return;

				baseSpeed = value;

				onChanged_baseSpeed(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Pitch Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float pitchSpeed = 0f;

		private void onChanged_pitchSpeed(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().PitchSpeed = pitchSpeed;

//			UpdateUI();
		}

		public float PitchSpeed
		{
			get { return pitchSpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(pitchSpeed, value))
					return;

				pitchSpeed = value;

				onChanged_pitchSpeed(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Roll Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float rollSpeed = 0f;

		private void onChanged_rollSpeed(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().RollSpeed = rollSpeed;

//			UpdateUI();
		}

		public float RollSpeed
		{
			get { return rollSpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(rollSpeed, value))
					return;

				rollSpeed = value;

				onChanged_rollSpeed(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Yaw Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float yawSpeed = 0f;

		private void onChanged_yawSpeed(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().YawSpeed = yawSpeed;

//			UpdateUI();
		}

		public float YawSpeed
		{
			get { return yawSpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(yawSpeed, value))
					return;

				yawSpeed = value;

				onChanged_yawSpeed(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Throttle Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float throttleSpeed = 0f;

		private void onChanged_throttleSpeed(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().ThrottleSpeed = throttleSpeed;

//			UpdateUI();
		}

		public float ThrottleSpeed
		{
			get { return throttleSpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(throttleSpeed, value))
					return;

				throttleSpeed = value;

				onChanged_throttleSpeed(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "X Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float xSpeed = 0f;

		private void onChanged_xSpeed(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().XSpeed = xSpeed;

//			UpdateUI();
		}

		public float XSpeed
		{
			get { return xSpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(xSpeed, value))
					return;

				xSpeed = value;

				onChanged_xSpeed(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Y Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float ySpeed = 0f;

		private void onChanged_ySpeed(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().YSpeed = ySpeed;

//			UpdateUI();
		}

		public float YSpeed
		{
			get { return ySpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(ySpeed, value))
					return;

				ySpeed = value;

				onChanged_ySpeed(null);
			}
		}

		[KSPAxisField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Z Speed", guiFormat = "0", guiUnits = "%",
			axisMode = KSPAxisMode.Incremental, minValue = -100f, maxValue = 100f, incrementalSpeed = 1f),
			UI_FloatRange(minValue = -100f, maxValue = 100f, stepIncrement = 0.1f, suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float zSpeed = 0f;

		private void onChanged_zSpeed(object o)
		{
			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().ZSpeed = zSpeed;

//			UpdateUI();
		}

		public float ZSpeed
		{
			get { return zSpeed; }
			set
			{
				value = Mathf.Clamp(value, -100f, 100f);

				if(object.Equals(zSpeed, value))
					return;

				zSpeed = value;

				onChanged_zSpeed(null);
			}
		}

		////////////////////////////////////////
		// Input (servo)

		public void MoveLeft()
		{
			Move(float.NegativeInfinity, DefaultSpeed);
		}

		public void MoveCenter()
		{
			Move(DefaultPosition - CommandedPosition, DefaultSpeed);
		}

		public void MoveRight()
		{
			Move(float.PositiveInfinity, DefaultSpeed);
		}

		public void Move(float deltaPosition, float targetSpeed)
		{
			if(isOnRails || isLocked || isFreeMoving)
				return;

			if(swap)
				deltaPosition = -deltaPosition;

			if(isInverted)
				deltaPosition = -deltaPosition;

			float targetPosition = commandedPosition + deltaPosition;

			MoveExecute(targetPosition, targetSpeed);

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().MoveExecute(targetPosition, targetSpeed);
		}

		private void TrackMove()
		{
			if(isLocked)
				return;

			Vector3 toSun = Planetarium.fetch.Sun.transform.position - Joint.transform.position;

			if(Vector3.Angle(toSun, part.transform.TransformVector(axis)) < 5f)
				return; // axis points almost to the sun, we cannot track it like this

//DrawAxis(1, Joint.transform, toSun.normalized, false);

			toSun = Vector3.ProjectOnPlane(toSun, part.transform.TransformVector(axis));

//DrawAxis(2, Joint.transform, toSun.normalized, false);

			float deltaPosition = Vector3.SignedAngle(Quaternion.AngleAxis(trackAngle, part.transform.TransformVector(axis)) * part.transform.TransformVector(pointer), toSun, part.transform.TransformVector(axis));

			if(swap)
				deltaPosition = -deltaPosition;

			if(isInverted)
				deltaPosition = -deltaPosition;

			float targetPosition = commandedPosition + deltaPosition;

			MoveExecute(targetPosition, DefaultSpeed);

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().MoveExecute(targetPosition, DefaultSpeed);
		}

		private void MoveExecute(float targetPosition, float targetSpeed)
		{
			ip.SetCommand(targetPosition, Mathf.Clamp(targetSpeed * groupSpeedFactor, 0.1f, maxSpeed) * factorSpeed, false);

			targetPositionSet = ip.TargetPosition;
			targetSpeedSet = ip.TargetSpeed;

			requestedPositionIsDefined = false;

//			UpdateUI();
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

			MoveToExecute(targetPosition, targetSpeed);

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().MoveToExecute(targetPosition, targetSpeed);
		}

		private void MoveToExecute(float targetPosition, float targetSpeed)
		{
			ip.SetCommand(targetPosition, Mathf.Clamp(targetSpeed * groupSpeedFactor, 0.1f, maxSpeed) * factorSpeed, false);

			targetPositionSet = ip.TargetPosition;
			targetSpeedSet = ip.TargetSpeed;

			requestedPositionIsDefined = true;

			if(!isInverted)
				requestedPosition = to360((swap ? -targetPositionSet : targetPositionSet) + zeroNormal + correction_1 - correction_0);
			else
				requestedPosition = to360((swap ? targetPositionSet : -targetPositionSet) + zeroInvert - correction_1 + correction_0);

//			UpdateUI();
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
		}

		private bool KeyPressed(string key)
		{
			return (key != "" && vessel == FlightGlobals.ActiveVessel
					&& InputLockManager.IsUnlocked(ControlTypes.LINEAR)
					&& Input.GetKey(key));
		}

		private bool KeyUnPressed(string key)
		{
			return (key != "" && vessel == FlightGlobals.ActiveVessel
					&& InputLockManager.IsUnlocked(ControlTypes.LINEAR)
					&& Input.GetKeyUp(key));
		}

		private void CheckInputs()
		{
			if(KeyPressed(forwardKey))
				MoveRight();
			else if(KeyPressed(reverseKey))
				MoveLeft();
			else if(KeyUnPressed(forwardKey) || KeyUnPressed(reverseKey))
				Stop();
		}

		// Relax Mode (used to prevent breaking while latching of LEE and GF)

		public void SetRelaxMode(float relaxFactor)
		{
			Joint.angularXDrive = new JointDrive
			{
				maximumForce = Joint.angularXDrive.maximumForce,
				positionSpring = 0.04f * forceLimit * factorForce * relaxFactor,
				positionDamper = 0.4f * forceLimit * factorForce * relaxFactor
			};
		}

		public void ResetRelaxMode()
		{
			InitializeDrive();
		}

		public bool RelaxStep()
		{
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

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
				part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().isRunning = isRunning;

//			UpdateUI();
		}

		public bool IsRunning
		{
			get { return isRunning; }
			set { if(object.Equals(isRunning, value)) return; isRunning = value; onChanged_isRunning(null); }
		}

		[KSPField(isPersistant = true)]
		private float _isRunning = 0f;

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

// FEHLER, Idee -> minimierbar und dafür kostet er weniger / wiegt weniger?
		[KSPField(isPersistant = false), SerializeField]
		private float maxForce = 30f;

		public float MaxForce
		{
			get { return maxForce; }
		}

// FEHLER, Idee -> minimierbar und dafür kostet er weniger / wiegt weniger?
		[KSPField(isPersistant = false), SerializeField]
		private float maxAcceleration = 10;

		public float MaxAcceleration
		{
			get { return maxAcceleration; }
		}

// FEHLER, Idee -> minimierbar und dafür kostet er weniger / wiegt weniger?
		[KSPField(isPersistant = false), SerializeField]
		private float maxSpeed = 100;

		public float MaxSpeed
		{
			get { return maxSpeed; }
		}

		public float ElectricChargeRequired
		{
			get { return electricChargeRequired; }
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

		public void EditorReset()
		{
			if(!HighLogic.LoadedSceneIsEditor)
				return;

			IsInverted = false;
			IsLimitted = false;

			EditorSetPosition(0f);
		}

		public void EditorMoveLeft()
		{
			EditorMove(float.NegativeInfinity);
		}

		public void EditorMoveCenter()
		{
			EditorSetTo(DefaultPosition);
		}

		public void EditorMoveRight()
		{
			EditorMove(float.PositiveInfinity);
		}

		public void EditorMove(float targetPosition)
		{
			float movement = Mathf.Clamp(speedLimit * groupSpeedFactor, 0.1f, maxSpeed) * factorSpeed * Time.deltaTime;

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
		// (all correction values should be zero here and could be ignored... I still didn't "optimize" it)
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
					? Mathf.Clamp(targetPosition, _minPositionLimit, _maxPositionLimit)
					: Mathf.Clamp(targetPosition, -_maxPositionLimit, -_minPositionLimit);
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

			EditorSetPositionExecute(targetPosition);

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
					part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>().EditorSetPositionExecute(targetPosition);
		}

		public void EditorSetPositionExecute(float targetPosition)
		{
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
			requestedPosition = CommandedPosition;

//			UpdateUI();
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

			Fields["baseSpeed"].OnValueModified += onChanged_baseSpeed;
			Fields["pitchSpeed"].OnValueModified += onChanged_pitchSpeed;
			Fields["rollSpeed"].OnValueModified += onChanged_rollSpeed;
			Fields["yawSpeed"].OnValueModified += onChanged_yawSpeed;
			Fields["throttleSpeed"].OnValueModified += onChanged_throttleSpeed;
			Fields["xSpeed"].OnValueModified += onChanged_xSpeed;
			Fields["ySpeed"].OnValueModified += onChanged_ySpeed;
			Fields["zSpeed"].OnValueModified += onChanged_zSpeed;
			Fields["rotorAcceleration"].OnValueModified += onChanged_rotorAcceleration;

			Fields["isRunning"].OnValueModified += onChanged_isRunning;

			Fields["pitchControl"].OnValueModified += onChanged_pitchControl;
			Fields["rollControl"].OnValueModified += onChanged_rollControl;
			Fields["yawControl"].OnValueModified += onChanged_yawControl;
			Fields["throttleControl"].OnValueModified += onChanged_throttleControl;
			Fields["xControl"].OnValueModified += onChanged_xControl;
			Fields["yControl"].OnValueModified += onChanged_yControl;
			Fields["zControl"].OnValueModified += onChanged_zControl;
			Fields["controlDeflectionRange"].OnValueModified += onChanged_controlDeflectionRange;
			Fields["controlNeutralPosition"].OnValueModified += onChanged_controlNeutralPosition;

			Fields["requestedPosition"].OnValueModified += onChanged_targetPosition;

			Fields["trackSun"].OnValueModified += onChanged_trackSun;
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

			Fields["baseSpeed"].OnValueModified -= onChanged_baseSpeed;
			Fields["pitchSpeed"].OnValueModified -= onChanged_pitchSpeed;
			Fields["rollSpeed"].OnValueModified -= onChanged_rollSpeed;
			Fields["yawSpeed"].OnValueModified -= onChanged_yawSpeed;
			Fields["throttleSpeed"].OnValueModified -= onChanged_throttleSpeed;
			Fields["xSpeed"].OnValueModified -= onChanged_xSpeed;
			Fields["ySpeed"].OnValueModified -= onChanged_ySpeed;
			Fields["zSpeed"].OnValueModified -= onChanged_zSpeed;
			Fields["rotorAcceleration"].OnValueModified -= onChanged_rotorAcceleration;

			Fields["isRunning"].OnValueModified -= onChanged_isRunning;

			Fields["pitchControl"].OnValueModified -= onChanged_pitchControl;
			Fields["rollControl"].OnValueModified -= onChanged_rollControl;
			Fields["yawControl"].OnValueModified -= onChanged_yawControl;
			Fields["throttleControl"].OnValueModified -= onChanged_throttleControl;
			Fields["xControl"].OnValueModified -= onChanged_xControl;
			Fields["yControl"].OnValueModified -= onChanged_yControl;
			Fields["zControl"].OnValueModified -= onChanged_zControl;
			Fields["controlDeflectionRange"].OnValueModified -= onChanged_controlDeflectionRange;
			Fields["controlNeutralPosition"].OnValueModified -= onChanged_controlNeutralPosition;

			Fields["requestedPosition"].OnValueModified -= onChanged_targetPosition;

			Fields["trackSun"].OnValueModified -= onChanged_trackSun;
		}

		private void UpdateUI(bool bRebuildUI = false)
		{
			part.Events["RemoveFromSymmetry"].active = false;

			((BaseAxisField)Fields["forceLimit"]).incrementalSpeed = maxForce / 10f;
			((BaseAxisField)Fields["forceLimit"]).maxValue = maxForce;

			((BaseAxisField)Fields["accelerationLimit"]).incrementalSpeed = maxAcceleration / 10f;
			((BaseAxisField)Fields["accelerationLimit"]).maxValue = maxAcceleration;

			((BaseAxisField)Fields["speedLimit"]).incrementalSpeed = maxSpeed / 10f;
			((BaseAxisField)Fields["speedLimit"]).maxValue = maxSpeed;

			((BaseAxisField)Fields["rotorAcceleration"]).incrementalSpeed = maxAcceleration / 10f;
			((BaseAxisField)Fields["rotorAcceleration"]).maxValue = maxAcceleration;

			((BaseAxisField)Fields["requestedPosition"]).ignoreClampWhenIncremental = !hasMinMaxPosition && !hasPositionLimit;
			((BaseAxisField)Fields["requestedPosition"]).minValue = hasPositionLimit ? MinPositionLimit : MinPosition;
			((BaseAxisField)Fields["requestedPosition"]).maxValue = hasPositionLimit ? MaxPositionLimit : MaxPosition;
			((BaseAxisField)Fields["requestedPosition"]).incrementalSpeed = isRotational ? 30f : 0.3f;


			if(HighLogic.LoadedSceneIsFlight)
			{
			//	Fields["modeIndex"].guiActive = ((UI_ChooseOption)Fields["modeIndex"].uiControlFlight).options.Length > 1;
				Fields["inputModeIndex"].guiActive = (((UI_ChooseOption)Fields["inputModeIndex"].uiControlFlight).options.Length > 1) && (mode == ModeType.servo);

				Fields["forceLimit"].guiActive = !isFreeMoving;
				((UI_FloatRange)Fields["forceLimit"].uiControlFlight).maxValue = maxForce;

				Fields["accelerationLimit"].guiActive = (mode == ModeType.servo) && !isFreeMoving;
				((UI_FloatRange)Fields["accelerationLimit"].uiControlFlight).maxValue = maxAcceleration;
 
				Fields["speedLimit"].guiActive = (mode == ModeType.servo) && !isFreeMoving;
				((UI_FloatRange)Fields["speedLimit"].uiControlFlight).maxValue = maxSpeed;

				// FEHLER, nochmal klären diese Werte hier... aber gut...
				Fields["jointSpring"].guiActive = false;
				Fields["jointDamping"].guiActive = (mode == ModeType.rotor);

				Fields["hasPositionLimit"].guiActive = (mode == ModeType.servo) && canHaveLimits /* && (inputMode != InputModeType.tracking)*/;

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
				((UI_FloatRange)Fields["rotorAcceleration"].uiControlFlight).maxValue = maxAcceleration;

				Fields["isRunning"].guiActive = (mode == ModeType.rotor);


				Fields["requestedPosition"].guiActive = (mode == ModeType.servo) && (inputMode == InputModeType.manual) && !IsLocked;

				((UI_FloatRange)Fields["requestedPosition"].uiControlFlight).minValue = hasPositionLimit ? MinPositionLimit : MinPosition;
				((UI_FloatRange)Fields["requestedPosition"].uiControlFlight).maxValue = hasPositionLimit ? MaxPositionLimit : MaxPosition;

				if(Math.Abs((hasPositionLimit ? MaxPositionLimit : MaxPosition) - (hasPositionLimit ? MinPositionLimit : MinPosition)) < 20)
				{
					Fields["requestedPosition"].guiFormat = "F2";
					((UI_FloatRange)Fields["requestedPosition"].uiControlFlight).stepIncrement = 0.01f;
				}
				else
				{
					Fields["requestedPosition"].guiFormat = "F1";
					((UI_FloatRange)Fields["requestedPosition"].uiControlFlight).stepIncrement = 0.1f;
				}

				Events["RemoveFromSymmetry2"].guiActive = (part.symmetryCounterparts.Count > 0);
			}
			else if(HighLogic.LoadedSceneIsEditor)
			{
				Fields["modeIndex"].guiActiveEditor = ((UI_ChooseOption)Fields["modeIndex"].uiControlEditor).options.Length > 1;
				Fields["inputModeIndex"].guiActiveEditor = (((UI_ChooseOption)Fields["inputModeIndex"].uiControlEditor).options.Length > 1) && (mode == ModeType.servo);

				Fields["forceLimit"].guiActiveEditor = !isFreeMoving;
				((UI_FloatRange)Fields["forceLimit"].uiControlEditor).maxValue = maxForce;

				Fields["accelerationLimit"].guiActiveEditor = (mode == ModeType.servo) && !isFreeMoving;
				((UI_FloatRange)Fields["accelerationLimit"].uiControlEditor).maxValue = maxAcceleration;
 
				Fields["speedLimit"].guiActiveEditor = (mode == ModeType.servo) && !isFreeMoving;
				((UI_FloatRange)Fields["speedLimit"].uiControlEditor).maxValue = maxSpeed;

				// FEHLER, nochmal klären diese Werte hier... aber gut...
				Fields["jointSpring"].guiActiveEditor = hasSpring && isFreeMoving;
				Fields["jointDamping"].guiActiveEditor = (mode == ModeType.rotor);
			//	Fields["jointDamping"].guiActiveEditor = hasSpring && isFreeMoving; -> FEHLER war früher mal so... weiss nicht ob das gut wäre

				Fields["hasPositionLimit"].guiActiveEditor = (mode == ModeType.servo) && canHaveLimits /* && (inputMode != InputModeType.tracking)*/;

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
				((UI_FloatRange)Fields["rotorAcceleration"].uiControlEditor).maxValue = maxAcceleration;

				Fields["isRunning"].guiActiveEditor = false; // (mode == ModeType.rotor);


				Fields["requestedPosition"].guiActiveEditor = (mode == ModeType.servo) && (inputMode == InputModeType.manual);

				((UI_FloatRange)Fields["requestedPosition"].uiControlEditor).minValue = hasPositionLimit ? MinPositionLimit : MinPosition;
				((UI_FloatRange)Fields["requestedPosition"].uiControlEditor).maxValue = hasPositionLimit ? MaxPositionLimit : MaxPosition;

				if(Math.Abs((hasPositionLimit ? MaxPositionLimit : MaxPosition) - (hasPositionLimit ? MinPositionLimit : MinPosition)) < 20)
				{
					Fields["requestedPosition"].guiFormat = "F2";
					((UI_FloatRange)Fields["requestedPosition"].uiControlEditor).stepIncrement = 0.01f;
				}
				else
				{
					Fields["requestedPosition"].guiFormat = "F1";
					((UI_FloatRange)Fields["requestedPosition"].uiControlEditor).stepIncrement = 0.1f;
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
				Presets.MovePrev();
		}

		[KSPAction("Move To Next Preset")]
		public void MoveNextPresetAction(KSPActionParam param)
		{
			if(Presets != null)
				Presets.MoveNext();
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

		////////////////////////////////////////
		// Axis (servo)

		private bool requestedPositionIsDefined = true;

		[KSPAxisField(isPersistant = false, guiActive = true, guiActiveEditor = true, guiName = "Target Position", guiFormat = "F2",
			axisMode = KSPAxisMode.Incremental),
			UI_FloatRange(suppressEditorShipModified = true, affectSymCounterparts = UI_Scene.All)]
		private float requestedPosition;

		private void onChanged_targetPosition(object o)
		{
			if(HighLogic.LoadedSceneIsEditor)
			{
				EditorSetPosition(requestedPosition);
				return;
			}

			if(isOnRails || isLocked || isFreeMoving)
			{
requestedPosition = CommandedPosition; // FEHLER, weiss nicht, ob das korrekt ist (wegen free moving z.B.)?
				return;
			}

			if(LinkedInputPart != null)
			{
				requestedPosition = CommandedPosition;
				return;
			}

			float _requestedPosition = requestedPosition;

			if(!isInverted)
				_requestedPosition = (swap ? -1.0f : 1.0f) * (_requestedPosition - zeroNormal - correction_1 + correction_0);
			else
				_requestedPosition = (swap ? 1.0f : -1.0f) * (_requestedPosition - zeroInvert + correction_1 - correction_0);

			ip.SetCommand(_requestedPosition, Mathf.Clamp(DefaultSpeed * groupSpeedFactor, 0.1f, DefaultSpeed) * factorSpeed, false);
			requestedPositionIsDefined = true;

			for(int i = 0; i < part.symmetryCounterparts.Count; i++)
			{
				ModuleIRServo_v3 servo = part.symmetryCounterparts[i].GetComponent<ModuleIRServo_v3>();

				servo.requestedPosition = requestedPosition;
				servo.ip.SetCommand(_requestedPosition, Mathf.Clamp(DefaultSpeed * groupSpeedFactor, 0.1f, DefaultSpeed) * factorSpeed, false);
				servo.requestedPositionIsDefined = true;
			}
		}

		////////////////////////////////////////
		// Actions (rotor)

		[KSPAction("Toggle Motor")]
		public void MotorToggleAction(KSPActionParam param)
		{ IsRunning = !IsRunning; }

		////////////////////////////////////////
		// special functions

		private InfernalRobotics_v3.Utility.PartSelector Selector;

		private void onSelectedLinkInput(Part p)
		{
			LinkedInputPartId = p.persistentId;
			LinkedInputPartFlightId = p.flightID;
			LinkedInputPart = p.GetComponent<ModuleIRServo_v3>();

			requestedPositionIsDefined = false;

			UpdateUI();
		}

		[KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "#autoLOC_8003305")]
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

		[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = true, guiName = "#autoLOC_8002375"),
			UI_Toggle(enabledText = "#autoLOC_439839", disabledText = "#autoLOC_439840")]
		public bool activateCollisions = false;

		////////////////////////////////////////
		// IRescalable

		[KSPField(isPersistant = false), SerializeField]
		private float scaleMass = 1.0f;
		[KSPField(isPersistant = false), SerializeField]
		private float scaleElectricChargeRequired = 2.0f;

		public void OnRescale(ScalingFactor factor)
		{
			ModuleIRServo_v3 prefab = part.partInfo.partPrefab.GetComponent<ModuleIRServo_v3>();

			part.mass = prefab.part.mass * Mathf.Pow(factor.absolute.linear, scaleMass);

			maxForce = prefab.maxForce * factor.absolute.linear;
 			ForceLimit = ForceLimit * factor.relative.linear;

			electricChargeRequired = prefab.electricChargeRequired * Mathf.Pow(factor.absolute.linear, scaleElectricChargeRequired);

 			if(!isRotational)
			{
				minPosition = prefab.minPosition * factor.absolute.linear;
				maxPosition = prefab.maxPosition * factor.absolute.linear;

				float _MinPositionLimit = MinPositionLimit;
				float _MaxPositionLimit = MaxPositionLimit;

				zeroNormal = prefab.zeroNormal * factor.absolute.linear;
				zeroInvert = prefab.zeroInvert * factor.absolute.linear;

				MinPositionLimit = _MinPositionLimit * factor.relative.linear;
				MaxPositionLimit = _MaxPositionLimit * factor.relative.linear;

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
		// IJointLockState (AutoStrut support)

		bool IJointLockState.IsJointUnlocked()
		{
			return !isLocked;
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
			alColor[4] = Color.blue;	// secondaryAxis
			alColor[5] = Color.white;
			alColor[6] = new Color(33.0f / 255.0f, 154.0f / 255.0f, 193.0f / 255.0f);
			alColor[7] = new Color(154.0f / 255.0f, 193.0f / 255.0f, 33.0f / 255.0f);
			alColor[8] = new Color(193.0f / 255.0f, 33.0f / 255.0f, 154.0f / 255.0f);
			alColor[9] = new Color(193.0f / 255.0f, 33.0f / 255.0f, 255.0f / 255.0f);
			alColor[10] = new Color(244.0f / 255.0f, 238.0f / 255.0f, 66.0f / 255.0f);
			alColor[11] = new Color(244.0f / 255.0f, 170.0f / 255.0f, 66.0f / 255.0f);
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

		private void DrawRelative(int idx, Vector3 p_from, Vector3 p_vector)
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

		// draw the limits without any correction
		private void DrawInitLimits(int idx)
		{
			if(!Joint)
				return;

			float low = Joint.lowAngularXLimit.limit;
			float high = Joint.highAngularXLimit.limit;

				// weil das ja "init" ist, gehen wir zurück auf die Werte, die es ohne Korrektur wäre
			low = (swap ? -_maxPositionLimit : _minPositionLimit);
			high = (swap ? -_minPositionLimit : _maxPositionLimit);

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

