using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InfernalRobotics.Command;
using InfernalRobotics.Control.Servo;
using InfernalRobotics.Effects;
using InfernalRobotics.Gui;
using KSP.IO;
using UnityEngine;
using TweakScale;

namespace InfernalRobotics.Module
{
    public class ModuleIRServo : PartModule, IRescalable, IJointLockState
    {

        //BEGIN Servo&Utility related KSPFields
        [KSPField(isPersistant = true)] public string servoName = "";

        //sound related fields
        [KSPField(isPersistant = true)] public float pitchSet = 1f;
        [KSPField(isPersistant = true)] public float soundSet = .5f;
        [KSPField(isPersistant = false)] public string motorSndPath = "MagicSmokeIndustries/Sounds/infernalRoboticMotor";

        //node&mesh related fields
        [KSPField(isPersistant = false)] public string bottomNode = "bottom";
        [KSPField(isPersistant = false)] public string fixedMesh = string.Empty;
        [KSPField(isPersistant = false)] public float friction = 0.5f;

        [KSPField(isPersistant = false)] public bool invertSymmetry = true; //TODO: this is a candidate for removal
        //END Servo&Utility related KSPFields

        //BEGIN Mechanism related KSPFields
        [KSPField(isPersistant = true)] public bool freeMoving = false;
        [KSPField(isPersistant = true)] public bool isMotionLock = false;
        [KSPField(isPersistant = true)] public bool limitTweakable = false;
        [KSPField(isPersistant = true)] public bool limitTweakableFlag = false;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max", guiFormat = "F2", guiUnits = ""), 
            UI_FloatEdit(minValue = -360f, maxValue = 360f, incrementSlide = 0.01f, scene = UI_Scene.All, sigFigs = 2)] 
        public float maxTweak = 360;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Min", guiFormat = "F2", guiUnits = ""), 
            UI_FloatEdit(minValue = -360f, maxValue = 360f, incrementSlide = 0.01f, scene = UI_Scene.All, sigFigs = 2)] 
        public float minTweak = 0;

        /*[KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Spring Power", guiFormat = "0.00"), 
            UI_FloatEdit(minValue = 0.00f, incrementSlide = 0.05f, incrementSmall=1f, incrementLarge=10f, scene = UI_Scene.None, sigFigs = 2)]
        public float jointSpring = 0;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Damping", guiFormat = "0.00"), 
            UI_FloatEdit(minValue = 0.00f, incrementSlide = 0.05f, incrementSmall=1f, incrementLarge=10f, scene = UI_Scene.None, sigFigs = 2)]
        public float jointDamping = 0; 
        */
        [KSPField(isPersistant = false)]
        public float jointSpring = 0;
        [KSPField(isPersistant = false)]
        public float jointDamping = 0;

        bool isOnRails = true;

        [KSPField(isPersistant = true)] public bool rotateLimits = false;
        [KSPField(isPersistant = true)] public float rotateMax = 360;
        [KSPField(isPersistant = true)] public float rotateMin = 0;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Rotation")] public float rotation = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)] public float rotationDelta = 0;

        [KSPField(isPersistant = true)] public float translateMax = 3;
        [KSPField(isPersistant = true)] public float translateMin = 0;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Translation")] public float translation = 0f;
        [KSPField(isPersistant = false)] public float translationDelta = 0;

        [KSPField(isPersistant = true)] public float defaultPosition = 0;

        [KSPField(isPersistant = false)] public Vector3 rotateAxis = Vector3.forward;
        [KSPField(isPersistant = false)] public bool rotateJoint = false; 
        [KSPField(isPersistant = false)] public Vector3 rotatePivot = Vector3.zero;
        [KSPField(isPersistant = false)] public string rotateModel = "on";
        [KSPField(isPersistant = false)] public Vector3 zeroUp = Vector3.up; //must be orthogonal to rotate axis, indicates up direction at 0 rotation

        [KSPField(isPersistant = false)] public Vector3 translateAxis = Vector3.forward;
        [KSPField(isPersistant = false)] public bool translateJoint = false;


        //END Mechanism related KSPFields

        //BEGIN Motor related KSPFields
        [KSPField(isPersistant = true)] public float customSpeed = 1;
        [KSPField(isPersistant = true)] public bool invertAxis;
        [KSPField(isPersistant = true)] public float torqueMax = 30f;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Torque", guiFormat = "0.00"), 
            UI_FloatEdit(minValue = 0f, maxValue=30f, incrementSlide = 0.05f, incrementSmall=0.5f, incrementLarge=1f, scene = UI_Scene.All, sigFigs = 2)]
        public float torqueTweak = 1f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Speed", guiFormat = "0.00"), 
            UI_FloatEdit(minValue = 0f, incrementSlide = 0.05f, incrementSmall=0.5f, incrementLarge=1f, sigFigs = 2)]
        public float speedTweak = 1f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Acceleration", guiFormat = "0.00"), 
            UI_FloatEdit(minValue = 0.05f, incrementSlide = 0.05f, incrementSmall=0.5f, incrementLarge=1f, sigFigs = 2)]
        public float accelTweak = 4f;

        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Electric Charge required", guiUnits = "EC/s")] 
        public float electricChargeRequired = 2.5f;

        [KSPField(isPersistant = false, guiActive = true, guiActiveEditor = false, guiName = "Current Draw", guiUnits = "EC/s")] 
        public float LastPowerDrawRate;

        [KSPField(isPersistant = false)] public float keyRotateSpeed = 0;
        [KSPField(isPersistant = false)] public float keyTranslateSpeed = 0;

        //END Motor related KSPFields

        //Preset related fields
        [KSPField(isPersistant = true)] public string presetPositionsSerialized = "";
        //end Preset related 

        //Group related KSPFields
        [KSPField(isPersistant = true)] public string groupName = "";
        //end Group relsted KSPFields

        //Begin Input related KSPFields
        [KSPField(isPersistant = true)] public string forwardKey;
        [KSPField(isPersistant = true)] public string reverseKey;
        //End Input related KSPFields

        //TODO: Move FAR related things to ServoController
        //these 3 are for sending messages to inform nuFAR of shape changes to the craft.
        protected const int shapeUpdateTimeout = 60; //it will send message every xx FixedUpdates
        protected int shapeUpdateCounter = 0;
        protected float lastPosition = 0f;

        protected const string ELECTRIC_CHARGE_RESOURCE_NAME = "ElectricCharge";

        protected ConfigurableJoint joint;
        protected ConfigurableJoint savedJoint;
        protected Rigidbody jointRigidBody;

        protected SoundSource motorSound;
        protected bool failedAttachment = false;

        //TODO: candidate for refactoring. ModuleIRServo should not be doing this, this is a job for ServoController
        private static int globalCreationOrder = 0; 

        protected bool JointSetupDone { get; set; }
        protected List<Transform> MobileColliders { get; set; }
        public Transform ModelTransform { get; set; }
        protected Transform RotateModelTransform { get; set; }
        protected bool UseElectricCharge { get; set; }
        public bool UseTorque { get; set; }

        //Interpolator represents a controller, assuring smooth movements
        //TODO: replace or refactor to implement Motor
        public Interpolator Interpolator { get; set; }

        //Translator represents an interface to interact with the servo
        public Translator Translator { get; set; }

        public Transform FixedMeshTransform { get; set; }

        public float GroupElectricChargeRequired { get; set; }

        public int CreationOrder { get; set; }
        public UIPartActionWindow TweakWindow { get; set; }
        public bool TweakIsDirty { get; set; }

        public List<float> PresetPositions { get; set; }

        public float Position { get { return rotateJoint ? rotation : translation; } }
        public float MinPosition {get { return Interpolator.Initialised ? Interpolator.MinPosition : minTweak;}}
        public float MaxPosition {get { return Interpolator.Initialised ? Interpolator.MaxPosition : maxTweak;}}

        protected float lastRealPosition = 0f;
        public bool isStuck = false;
        protected float startPosition = 0f;

        public ModuleIRServo()
        {
            Interpolator = new Interpolator();
            Translator = new Translator();
            TweakIsDirty = false;
            UseElectricCharge = true;
            GroupElectricChargeRequired = electricChargeRequired;
            UseTorque = false;
            CreationOrder = 0;
            MobileColliders = new List<Transform>();
            JointSetupDone = false;
            forwardKey = "";
            reverseKey = "";

            //motorSound = new SoundSource(this.part, "motor");
        }

        //BEGIN All KSPEvents&KSPActions
        [KSPEvent (guiActive = true, guiActiveEditor = true, guiName = "Reattach FixedMesh", active = true)]
        public void ReattachFixedMesh ()
        {
            AttachToParent ();
        }
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Engage Limits", active = false)]
        public void LimitTweakableToggle()
        {
            if (!rotateJoint)
                return;
            limitTweakableFlag = !limitTweakableFlag;
            Events["LimitTweakableToggle"].guiName = limitTweakableFlag ? "Disengage Limits" : "Engage Limits";

            if (limitTweakableFlag)
            {
                //revert back to full range as in part.cfg
                minTweak = rotateMin;
                maxTweak = rotateMax;
            }
            else
            {
                //we need to convert part's minTweak and maxTweak to [-180,180] range to get consistent behavior with Interpolator
                minTweak = -180f;
                maxTweak = 180f;
            }
            SetupMinMaxTweaks ();
            TweakIsDirty = true;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Invert Axis")]
        public void InvertAxisToggle()
        {
            invertAxis = !invertAxis;
            Events["InvertAxisToggle"].guiName = invertAxis ? "Un-invert Axis" : "Invert Axis";
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Engage Lock", active = true)]
        public void MotionLockToggle()
        {
            SetLock(!isMotionLock);
        }
        [KSPAction("Toggle Lock")]
        public void MotionLockToggle(KSPActionParam param)
        {
            SetLock(!isMotionLock);
        }


        [KSPAction("Move To Next Preset")]
        public void MoveNextPresetAction(KSPActionParam param)
        {
            if (Translator.IsMoving())
                Translator.Stop();
            else
                MoveNextPreset();
        }

        [KSPAction("Move To Previous Preset")]
        public void MovePrevPresetAction(KSPActionParam param)
        {
            if (Translator.IsMoving())
                Translator.Stop();
            else
                MovePrevPreset();
        }


        [KSPAction("Move +")]
        public void MovePlusAction(KSPActionParam param)
        {
            switch (param.type)
            {
            case KSPActionType.Activate:
                Translator.Move(float.PositiveInfinity, customSpeed * speedTweak);
                break;
            case KSPActionType.Deactivate:
                Translator.Stop ();
                break;
            }
        }

        [KSPAction("Move -")]
        public void MoveMinusAction(KSPActionParam param)
        {
            switch (param.type)
            {
            case KSPActionType.Activate:
                Translator.Move(float.NegativeInfinity, customSpeed * speedTweak);
                break;
            case KSPActionType.Deactivate:
                Translator.Stop ();
                break;
            }
        }

        [KSPAction("Move Center")]
        public void MoveCenterAction(KSPActionParam param)
        {
            switch (param.type)
            {
            case KSPActionType.Activate:
                Translator.Move(Translator.ToExternalPos(defaultPosition), customSpeed * speedTweak);
                break;
            case KSPActionType.Deactivate:
                Translator.Stop ();
                break;
            }
        }

        //END all KSPEvents & KSPActions

        public float GetStepIncrement()
        {
            return rotateJoint ? 1f : 0.01f;
        }


        //TODO: this seems like obsolete, as it is only called once for all my parts and it is for legacy part (RotatronMK2)

        protected void ColliderizeChilds(Transform obj)
        {
            if (obj.name.StartsWith("node_collider")
                || obj.name.StartsWith("fixed_node_collider")
                || obj.name.StartsWith("mobile_node_collider"))
            {
                Logger.Log("Converting collider " + obj.name, Logger.Level.Debug);

                if (obj.GetComponent<MeshFilter>())
                {
                    var sharedMesh = Instantiate(obj.GetComponent<MeshFilter>().mesh) as Mesh;
                    Destroy(obj.GetComponent<MeshFilter>());
                    Destroy(obj.GetComponent<MeshRenderer>());
                    var meshCollider = obj.gameObject.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = sharedMesh;
                    meshCollider.convex = true;
                    obj.parent = part.transform;

                    if (obj.name.StartsWith("mobile_node_collider"))
                    {
                        MobileColliders.Add(obj);
                    }
                }
                else
                {
                    Logger.Log("Collider has no MeshFilter (yet?): skipping Colliderize", Logger.Level.Debug);
                }
            }
            for (int i = 0; i < obj.childCount; i++)
            {
                ColliderizeChilds(obj.GetChild(i));
            }
        }

        public void InitUITweakables()
        {
            limitTweakableFlag = limitTweakableFlag | rotateLimits;

            //try-catch block to avoid GUI exceptions on incorrectly setup parts
            try
            {
                Events["InvertAxisToggle"].guiName = invertAxis ? "Un-invert Axis" : "Invert Axis";
                Events["MotionLockToggle"].guiName = isMotionLock ? "Disengage Lock" : "Engage Lock";

                Fields["torqueTweak"].guiActive = UseTorque;
                Fields["torqueTweak"].guiActiveEditor = UseTorque;

                if (rotateJoint)
                {
                    minTweak = rotateMin;
                    maxTweak = rotateMax;

                    if (limitTweakable)
                    {
                        Events["LimitTweakableToggle"].active = true;
                        Events["LimitTweakableToggle"].guiName = limitTweakableFlag ? "Disengage Limits" : "Engage Limits";
                    }

                    if (freeMoving)
                    {
                        Events["InvertAxisToggle"].active = false;
                        Events["MotionLockToggle"].active = false;
                        Fields["minTweak"].guiActive = false;
                        Fields["minTweak"].guiActiveEditor = false;
                        Fields["maxTweak"].guiActive = false;
                        Fields["maxTweak"].guiActiveEditor = false;
                        Fields["torqueTweak"].guiActive = false;
                        Fields["speedTweak"].guiActive = false;
                        Fields["speedTweak"].guiActiveEditor = false;
                        Fields["accelTweak"].guiActive = false;
                        Fields["accelTweak"].guiActiveEditor = false;
                        //Fields["rotation"].guiActive = false;
                        //Fields["rotation"].guiActiveEditor = false;
                    }


                    Fields["translation"].guiActive = false;
                    Fields["translation"].guiActiveEditor = false;
                }
                else if (translateJoint)
                {
                    minTweak = translateMin;
                    maxTweak = translateMax;

                    Events["LimitTweakableToggle"].active = false;

                    Fields["rotation"].guiActive = false;
                    Fields["rotation"].guiActiveEditor = false;
                }

            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("InitUITweakables exception {0}", ex.Message), Logger.Level.Fatal);
            }
        }

        public override void OnAwake()
        {
            Logger.Log("[OnAwake] Start", Logger.Level.Debug);

            if (!rotateAxis.IsZero())
                rotateAxis.Normalize();
            if (!translateAxis.IsZero())
                translateAxis.Normalize();

            FindTransforms();

            if (ModelTransform == null)
                Logger.Log("[OnAwake] ModelTransform is null", Logger.Level.Warning);

            ColliderizeChilds(ModelTransform);
            
            InitUITweakables();

            if (motorSound == null)
                motorSound = new SoundSource(part, "motor");

            SetupMinMaxTweaks();
            ParsePresetPositions();

            Translator.Init(isMotionLock, new Servo(this), Interpolator);

            GameEvents.onVesselGoOnRails.Add (OnVesselGoOnRails);
            GameEvents.onVesselGoOffRails.Add (OnVesselGoOffRails);

            Logger.Log(string.Format("[OnAwake] End, rotateLimits={0}, minTweak={1}, maxTweak={2}, rotateJoint={0}", rotateLimits, minTweak, maxTweak), Logger.Level.Debug);
        }

        public void onDestroy()
        {
            GameEvents.onVesselGoOnRails.Remove (OnVesselGoOnRails);
            GameEvents.onVesselGoOffRails.Remove (OnVesselGoOffRails);
        }

        public void OnVesselGoOnRails (Vessel v)
        {
            if (v != vessel)
                return;

            Logger.Log ("[OnVesselGoOnRails] Reverting Joint", Logger.Level.Debug);

            part.attachJoint.Joint.angularXDrive = savedJoint.angularXDrive;
            part.attachJoint.Joint.angularYZDrive = savedJoint.angularYZDrive;
            part.attachJoint.Joint.xDrive = savedJoint.xDrive;
            part.attachJoint.Joint.yDrive = savedJoint.yDrive;
            part.attachJoint.Joint.zDrive = savedJoint.zDrive;
            part.attachJoint.Joint.enableCollision = false;

            if (joint) 
            {
                // lock all movement by default
                joint.xMotion = ConfigurableJointMotion.Locked;
                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Locked;
                joint.angularXMotion = ConfigurableJointMotion.Locked;
                joint.angularYMotion = ConfigurableJointMotion.Locked;
                joint.angularZMotion = ConfigurableJointMotion.Locked;
            }

            rotationDelta = rotation;
            translationDelta = translation;

            isOnRails = true;
        }


        public void OnVesselGoOffRails (Vessel v)
        {
            if (v != vessel)
                return;

            JointSetupDone = false;

            Logger.Log ("[OnVesselGoOffRails] Started for "+ part.name, Logger.Level.Debug);

            Logger.Log ("[OnVesselGoOffRails] Rebuilding Attachments", Logger.Level.Debug);
            BuildAttachments ();

            if (joint) 
            {
                Logger.Log ("[OnVesselGoOffRails] Resetting Joint", Logger.Level.Debug);
                DestroyImmediate (joint);
            }

            SetupJoints ();

            isOnRails = false;
        }

        public override void OnSave(ConfigNode node)
        {
            Logger.Log("[OnSave] Start", Logger.Level.Debug);

            base.OnSave(node);

            presetPositionsSerialized = SerializePresets();

            Logger.Log("[OnSave] End", Logger.Level.Debug);
        }


        public void ParsePresetPositions()
        {
            string[] positionChunks = presetPositionsSerialized.Split('|');
            PresetPositions = new List<float>();
            foreach (string chunk in positionChunks)
            {
                float tmp;
                if(float.TryParse(chunk,out tmp))
                {
                    PresetPositions.Add(tmp);
                }
            }
        }

        public string SerializePresets()
        {
            return PresetPositions.Aggregate(string.Empty, (current, s) => current + (s + "|"));
        }

        protected virtual void InitModule()
        {
            FindTransforms();

            if (ModelTransform == null)
                Logger.Log("[OnLoad] ModelTransform is null", Logger.Level.Warning);

            ColliderizeChilds(ModelTransform);
            
            if (HighLogic.LoadedSceneIsEditor)
            {
                //apply saved rotation/translation in reverse to a fixed mesh.
                if (rotateJoint)
                {
                    FixedMeshTransform.Rotate(rotateAxis, -rotation);
                }
                else
                {
                    FixedMeshTransform.Translate(translateAxis * translation);
                }
            }

            ParsePresetPositions();

            UpdateMinMaxTweaks();
        }

        public override void OnLoad(ConfigNode config)
        {
            Logger.Log("[OnLoad] Start", Logger.Level.Debug);

            //base.OnLoad (config);

            //save persistent rotation/translation data, because the joint will be initialized at current position.
            rotationDelta = rotation;
            translationDelta = translation;

            InitModule();

            Logger.Log("[OnLoad] End", Logger.Level.Debug);
        }
        /// <summary>
        /// GUI Related.
        /// Updates the minimum max tweaks.
        /// </summary>
        protected void UpdateMinMaxTweaks()
        {
            var isEditor = (HighLogic.LoadedSceneIsEditor);

            var rangeMinF = isEditor? (UI_FloatEdit) Fields["minTweak"].uiControlEditor :(UI_FloatEdit) Fields["minTweak"].uiControlFlight;
            var rangeMaxF = isEditor? (UI_FloatEdit) Fields["maxTweak"].uiControlEditor :(UI_FloatEdit) Fields["maxTweak"].uiControlFlight;

            var rangeTorqueF = isEditor? (UI_FloatEdit) Fields["torqueTweak"].uiControlEditor :(UI_FloatEdit) Fields["torqueTweak"].uiControlFlight;

            rangeMinF.minValue = rotateJoint ? rotateMin : translateMin;
            rangeMinF.maxValue = rotateJoint ? rotateMax : translateMax;
            rangeMaxF.minValue = rotateJoint ? rotateMin : translateMin;
            rangeMaxF.maxValue = rotateJoint ? rotateMax : translateMax;

            rangeTorqueF.minValue = 0f;
            rangeTorqueF.maxValue = torqueMax;

            Logger.Log (string.Format ("UpdateTweaks: rotateJoint = {0}, rotateMin={1}, rotateMax={2}, translateMin={3}, translateMax={4}",
                rotateJoint, rotateMin, rotateMax, translateMin, translateMax), Logger.Level.Debug);
        }

        /// <summary>
        /// GUI Related
        /// Setups the minimum max tweaks.
        /// </summary>
        protected void SetupMinMaxTweaks()
        {
            if (HighLogic.LoadedSceneIsEditor)
            {
                ((UI_FloatEdit)Fields["minTweak"].uiControlEditor).incrementSlide = GetStepIncrement();
                ((UI_FloatEdit)Fields["maxTweak"].uiControlEditor).incrementSlide = GetStepIncrement();
            }
            else
            {
                ((UI_FloatEdit)Fields["minTweak"].uiControlFlight).incrementSlide = GetStepIncrement();
                ((UI_FloatEdit)Fields["maxTweak"].uiControlFlight).incrementSlide = GetStepIncrement();
            }
            bool showTweakables = (translateJoint || (limitTweakableFlag && !freeMoving));
            Fields["minTweak"].guiActive = showTweakables;
            Fields["minTweak"].guiActiveEditor = showTweakables;
            Fields["maxTweak"].guiActive = showTweakables;
            Fields["maxTweak"].guiActiveEditor = showTweakables;

            UpdateMinMaxTweaks();

            Logger.Log ("SetupMinMaxTweaks finished, showTweakables = " + showTweakables + ", limitTweakableFlag = " + limitTweakableFlag, Logger.Level.Debug);
        }

        /// <summary>
        /// Core function, messes with Transforms.
        ///
        /// Ultimately called from OnStart (usually meanining start of Flight mode). 
        /// 
        /// Basically rotates/translates the fixed mesh into the opposite direction of saved rotation/translation.
        /// </summary>
        protected virtual void AttachToParent()
        {
            Transform fix = FixedMeshTransform;
            //first revert position to part position
            if (fix == null || part == null || part.transform == null || part.parent == null)
            {
                Logger.Log ("[AttachToParent] part, parent or transform is null", Logger.Level.Debug);
                return;
            }
            
            fix.position = part.transform.position;
            fix.rotation = part.transform.rotation;

            if (rotateJoint)
            {
                fix.RotateAround(part.transform.TransformPoint(rotatePivot), part.transform.TransformDirection(rotateAxis),
                    //(invertSymmetry ? ((part.symmetryCounterparts.Count != 1) ? -1 : 1) : -1) *
                    -rotationDelta);
            }
            else if (translateJoint)
            {
                fix.Translate(translateAxis.normalized*translationDelta, Space.Self);
            }

            fix.parent = part.parent.transform;
        }
        /// <summary>
        /// Updates colliders friction.
        /// </summary>
        /// <param name="obj">Transofrm</param>
        protected void ReparentFriction(Transform obj)
        {
            for (int i = 0; i < obj.childCount; i++)
            {
                Transform child = obj.GetChild(i);
                var tmp = child.GetComponent<MeshCollider>();
                if (tmp != null)
                {
                    tmp.material.dynamicFriction = tmp.material.staticFriction = friction;
                    tmp.material.frictionCombine = PhysicMaterialCombine.Maximum;
              
                }
                if (child.name.StartsWith("fixed_node_collider") && (part.parent != null))
                {
                    Logger.Log ("ReparentFriction: reparenting collider " + child.name, Logger.Level.Debug);
                    AttachToParent();
                }
            }
            if ((MobileColliders.Count > 0) && (RotateModelTransform != null))
            {
                foreach (Transform c in MobileColliders)
                {
                    c.parent = RotateModelTransform;
                }
            }
        }

        /// <summary>
        /// Core function.
        /// Builds the attachments.
        /// </summary>
        protected virtual void BuildAttachments()
        {
            if (part.parent == null)
            {
                Logger.Log ("BuildAttachments: part.parent is null", Logger.Level.Warning);
                if(!isMotionLock)
                    SetLock (true);
                failedAttachment = true;
                return;
            }

            if(FixedMeshTransform != null)
            {
                AttachToParent ();
            }

            //var node = part.FindAttachNodeByPart (part.parent);
            
            //if(translateJoint && (node == null || !(node.id.Contains(bottomNode) || part.attachMode == AttachModes.SRF_ATTACH)))
            //    translateAxis *= -1;
            
            ReparentFriction(part.transform);
            failedAttachment = false;
        }
        /// <summary>
        /// Extracts the Transforms from model, the only one apart from FixedMeshTransform that is used in code is RotateModelTransofrm
        /// </summary>
        protected void FindTransforms()
        {
            ModelTransform = part.transform.FindChild("model");
            RotateModelTransform = ModelTransform.FindChild(rotateModel);

            FixedMeshTransform = KSPUtil.FindInPartModel(transform, fixedMesh);
        }
            
        // mrblaq return an int to multiply by rotation direction based on GUI "invert" checkbox bool
        public int GetAxisInversion()
        {
            return (invertAxis ? -1 : 1);
        }

        //Called when the flight starts, or when the part is created in the editor. 
        //OnStart will be called before OnUpdate or OnFixedUpdate are ever called.
        public override void OnStart(StartState state)
        {
            
            Logger.Log("[MMT] OnStart Start", Logger.Level.Debug);

            UseElectricCharge = WindowManager.UseElectricCharge;

            limitTweakableFlag = limitTweakableFlag | rotateLimits;

            if (!float.IsNaN(Position))
                Interpolator.Position = Position;

            Translator.Init(isMotionLock, new Servo(this), Interpolator);

            if (vessel == null) //or we can check for state==StartState.Editor
            {
                Logger.Log(string.Format("[MMT] OnStart vessel is null"));
                return;
            }

            //basically beyond this point we are in Flight, because vessel is not null

            ConfigureInterpolator();

            if (motorSound==null)
                motorSound = new SoundSource(part, "motor");

            motorSound.Setup(motorSndPath, true);

            CreationOrder = globalCreationOrder++;

            FindTransforms();

            if (ModelTransform == null)
                Logger.Log("[MMT] OnStart ModelTransform is null", Logger.Level.Warning);

            BuildAttachments(); 

            if (limitTweakable)
            {
                Events["LimitTweakableToggle"].active = rotateJoint;
                Events["LimitTweakableToggle"].guiName = limitTweakableFlag ? "Disengage Limits" : "Engage Limits";
            }
            //it seems like we do need to call this one more time as OnVesselChange was called after Awake
            //for some reason it was not necessary for legacy parts, but needed for rework parts.
            SetupMinMaxTweaks();

            Logger.Log("[MMT] OnStart End, rotateLimits=" + rotateLimits + ", minTweak=" + minTweak + ", maxTweak=" + maxTweak + ", rotateJoint = " + rotateJoint, Logger.Level.Debug);
        }


        /// <summary>
        /// Initialise the Interpolator.
        /// </summary>
        public void ConfigureInterpolator()
        {
            // write interpolator configuration
            // (this should not change while it is active!!)
            if (Interpolator.Active)
                return;

            Interpolator.IsModulo = rotateJoint && !limitTweakableFlag;
            if (Interpolator.IsModulo)
            {
                Interpolator.Position = Interpolator.ReduceModulo(this.Position);
                Interpolator.MinPosition = -180;
                Interpolator.MaxPosition =  180;
            } 
            else
            {
                float min = Math.Min(minTweak, maxTweak);
                float max = Math.Max(minTweak, maxTweak);
                Interpolator.MinPosition = Math.Min(min, this.Position);
                Interpolator.MaxPosition = Math.Max(max, this.Position);
            }
            Interpolator.MaxAcceleration = accelTweak * Translator.GetSpeedUnit();
            Interpolator.Initialised = true;

            //Logger.Log("configureInterpolator:" + Interpolator, Logger.Level.Debug);
        }

        /// <summary>
        /// Core function. Sets up the joint.
        /// </summary>
        /// <returns><c>true</c>, if joint was setup, <c>false</c> otherwise.</returns>
        public virtual bool SetupJoints()
        {
            if (!rotateJoint && !translateJoint || part.attachJoint == null)
            {
                JointSetupDone = false;
                return false;
            }

            if (JointSetupDone)
                return false;

            savedJoint = part.attachJoint.Joint;

            // Catch reversed joint
            // Maybe there is a best way to do it?
            if (transform.position != part.attachJoint.Joint.connectedBody.transform.position)
            {
                joint = part.attachJoint.Joint.connectedBody.gameObject.AddComponent<ConfigurableJoint>();
                joint.connectedBody = part.attachJoint.Joint.GetComponent<Rigidbody> ();
            }
            else
            {
                joint = part.attachJoint.Joint.GetComponent<Rigidbody> ().gameObject.AddComponent<ConfigurableJoint>();
                joint.connectedBody = part.attachJoint.Joint.connectedBody;

            }

            joint.breakForce = 1e15f;
            joint.breakTorque = 1e15f;
            // And to default joint
            part.attachJoint.Joint.breakForce = 1e15f;
            part.attachJoint.Joint.breakTorque = 1e15f;
            part.attachJoint.SetBreakingForces(1e15f, 1e15f);

            // lock all movement by default
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Locked;
            joint.angularYMotion = ConfigurableJointMotion.Locked;
            joint.angularZMotion = ConfigurableJointMotion.Locked;

            joint.projectionDistance = 0f;
            joint.projectionAngle = 0f;
            joint.projectionMode = JointProjectionMode.PositionAndRotation;

            // Copy drives
            joint.linearLimit = part.attachJoint.Joint.linearLimit;
            joint.lowAngularXLimit = part.attachJoint.Joint.lowAngularXLimit;
            joint.highAngularXLimit = part.attachJoint.Joint.highAngularXLimit;
            joint.angularXDrive = part.attachJoint.Joint.angularXDrive;
            joint.angularYZDrive = part.attachJoint.Joint.angularYZDrive;
            joint.xDrive = part.attachJoint.Joint.xDrive;
            joint.yDrive = part.attachJoint.Joint.yDrive;
            joint.zDrive = part.attachJoint.Joint.zDrive;

            jointRigidBody = joint.GetComponent<Rigidbody> ();

            // Set anchor position
            joint.anchor =
                jointRigidBody.transform.InverseTransformPoint(joint.connectedBody.transform.position);
            joint.connectedAnchor = Vector3.zero;

            // Set correct axis
            joint.axis =
                jointRigidBody.transform.InverseTransformDirection(joint.connectedBody.transform.right);  //x axis
            joint.secondaryAxis =
                jointRigidBody.transform.InverseTransformDirection(joint.connectedBody.transform.up); //y axis

            joint.enableCollision = false;

            if (translateJoint)
            {
                //we need to get joint's translation along the translate axis
                var right = joint.axis; //x axis
                var up = joint.secondaryAxis; //y axis
                var forward = Vector3.Cross(joint.axis, joint.secondaryAxis).normalized; //z axis
                var r = Quaternion.LookRotation(forward, up);
                Vector3 f = r * (-translateAxis);

                startPosition = Vector3.Dot(jointRigidBody.transform.InverseTransformPoint(joint.connectedBody.transform.position) - joint.anchor, f);

                Logger.Log(servoName + ": right = " + right + ", forward = " + forward + ", up = " + up + ", trAxis=" + translateAxis + ", f=" + f + ", startposition=" + startPosition, Logger.Level.Debug);

                /*JointDrive drv = joint.xDrive;
                drv.maximumForce = UseTorque ? torqueTweak : float.PositiveInfinity;
                drv.positionSpring = jointSpring == 0f ? float.PositiveInfinity : jointSpring;
                drv.positionDamper = jointDamping;
                joint.xDrive = drv;
                joint.yDrive = drv;
                joint.zDrive = drv;
                */
                joint.xMotion = ConfigurableJointMotion.Free;
                joint.yMotion = ConfigurableJointMotion.Free;
                joint.zMotion = ConfigurableJointMotion.Free;

                /*
                if (jointSpring > 0)
                {
                    if (translateAxis == Vector3.right || translateAxis == Vector3.left)
                    {
                        joint.xMotion = ConfigurableJointMotion.Free;

                        //lock the other two axii
                        joint.yMotion = ConfigurableJointMotion.Locked;
                        joint.zMotion = ConfigurableJointMotion.Locked;

                    }
                        
                    if (translateAxis == Vector3.up || translateAxis == Vector3.down)
                    {
                        joint.yMotion = ConfigurableJointMotion.Free;
                        //lock the other two axii
                        joint.xMotion = ConfigurableJointMotion.Locked;
                        joint.zMotion = ConfigurableJointMotion.Locked;

                    }

                    if (translateAxis == Vector3.forward || translateAxis == Vector3.back)
                    {
                        joint.zMotion = ConfigurableJointMotion.Free;
                        //lock the other two axii
                        joint.yMotion = ConfigurableJointMotion.Locked;
                        joint.xMotion = ConfigurableJointMotion.Locked;

                    }
                        
                }
                */
            }

            if (rotateJoint)
            {
                startPosition = to180(AngleSigned(jointRigidBody.transform.up, joint.connectedBody.transform.up, joint.connectedBody.transform.right));

                joint.rotationDriveMode = RotationDriveMode.XYAndZ;
                joint.angularXMotion = ConfigurableJointMotion.Free;
                joint.angularYMotion = ConfigurableJointMotion.Free;
                joint.angularZMotion = ConfigurableJointMotion.Free;


                /*
                if(UseTorque)
                {
                    JointDrive tmp = joint.angularXDrive;
                    tmp.maximumForce = torqueTweak;
                    joint.angularXDrive = tmp;

                    tmp = joint.angularYZDrive;
                    tmp.maximumForce = torqueTweak;
                    joint.angularYZDrive = tmp;
                }
                */

                if (jointSpring > 0)
                {
                    if (rotateAxis == Vector3.right || rotateAxis == Vector3.left)
                    {
                        JointDrive drv = joint.angularXDrive;
                        drv.positionSpring = jointSpring;
                        drv.positionDamper = jointDamping;
                        joint.angularXDrive = drv;

                        /*Logger.Log("Got here");
                        var lowAngularXLimit = joint.lowAngularXLimit;
                        lowAngularXLimit.limit = -10f;
                        lowAngularXLimit.bounciness = 1;
                        joint.lowAngularXLimit = lowAngularXLimit;

                        var highAngularXLimit = joint.highAngularXLimit;
                        highAngularXLimit.limit = 10f;
                        highAngularXLimit.bounciness = 1;
                        joint.highAngularXLimit = highAngularXLimit;

                        var t = joint.angularXLimitSpring;
                        t.spring = 100f;
                        t.damper = 30f;
                        joint.angularXLimitSpring = t;
                        joint.angularXMotion = ConfigurableJointMotion.Limited;
                        */
                        joint.angularYMotion = ConfigurableJointMotion.Locked;
                        joint.angularZMotion = ConfigurableJointMotion.Locked;
                    }
                    else
                    {
                        JointDrive drv = joint.angularYZDrive;
                        drv.positionSpring = jointSpring;
                        drv.positionDamper = jointDamping;
                        joint.angularYZDrive = drv;

                        joint.angularXMotion = ConfigurableJointMotion.Locked;
                        joint.angularZMotion = ConfigurableJointMotion.Locked;
                    }
                }
            }

            // Reset default joint drives
            var resetDrv = new JointDrive
            {
                //mode = JointDriveMode.PositionAndVelocity,
                positionSpring = 0,
                positionDamper = 0,
                maximumForce = 0
            };

            part.attachJoint.Joint.angularXDrive = resetDrv;
            part.attachJoint.Joint.angularYZDrive = resetDrv;
            part.attachJoint.Joint.xDrive = resetDrv;
            part.attachJoint.Joint.yDrive = resetDrv;
            part.attachJoint.Joint.zDrive = resetDrv;
            part.attachJoint.Joint.enableCollision = false;



            JointSetupDone = true;
            return true;
        }

        public float to180(float v)
        {
            if (v > 180)    v -= 360;
            if (v <= -180)   v += 360;
            return v;
        }
        
        /// <summary>
        /// Determine the signed angle between two vectors, with normal 'n'
        /// as the rotation axis.
        /// </summary>
        public static float AngleSigned(Vector3 v1, Vector3 v2, Vector3 n)
        {
            return Mathf.Atan2(
                Vector3.Dot(n, Vector3.Cross(v1, v2)),
                Vector3.Dot(v1, v2)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Try to get the real rotation of the ConfigurableJoint as it could be different from Rotation due to number of factors 
        /// when we introduce the Torque parameter.
        /// So far little luck on that front and the Internet is of little help. Current issue is as soon as Rotation is blocked by 
        /// collision and lack of torque and then the obstacle is removed RealRotation calculated as above begins to differ from Rotation
        /// even when not constrained.
        /// </summary>
        /// <returns>The real rotation.</returns>
        public float GetRealRotation()
        {            
            Vector3 v1, v2, n;
            if(rotateAxis == Vector3.forward || rotateAxis == Vector3.back)
            {
                v1 = FixedMeshTransform.up;
                v2 = transform.up;
                n = FixedMeshTransform.right;
            }
            else if (rotateAxis == Vector3.up || rotateAxis == Vector3.down)
            {
                v1 = FixedMeshTransform.right;
                v2 = transform.right;
                n = FixedMeshTransform.up;
            }
            else
            {
                v1 = FixedMeshTransform.forward;
                v2 = transform.forward;
                n = FixedMeshTransform.right;
            }

            float retVal = to180(AngleSigned(v1, v2, n));

            return (float) Math.Round(retVal, 2);
        }

        public float GetRealTranslation()
        {
            float retVal = translation;

            if (joint!=null && jointRigidBody != null)
            {
                //we need to get joint's translation along the translate axis
                var right = joint.axis;
                var forward = Vector3.Cross(joint.axis, joint.secondaryAxis).normalized;
                var up = joint.secondaryAxis;
                var r = Quaternion.LookRotation(forward, up);

                Vector3 f = r * (-translateAxis);
                
                retVal = Vector3.Dot(jointRigidBody.transform.InverseTransformPoint(joint.connectedBody.transform.position) - joint.anchor, f) - startPosition + translationDelta;
            }

            return (float) Math.Round(retVal, 2);
        }

        protected virtual void UpdateJointSettings(float torque, float springPower, float dampingPower)
        {
            if (joint == null)
                return;

            JointDrive drv = joint.xDrive;
            drv.maximumForce = torque == 0f ? float.PositiveInfinity : UseTorque ? torque : float.PositiveInfinity;
            drv.positionSpring = springPower == 0f ? float.PositiveInfinity : springPower;
            drv.positionDamper = dampingPower == 0f ? float.PositiveInfinity : dampingPower;

            joint.xDrive = drv;
            joint.yDrive = drv;
            joint.zDrive = drv;

            drv = joint.angularXDrive;
            drv.maximumForce = torque == 0f ? float.PositiveInfinity : UseTorque ? torque: float.PositiveInfinity;
            drv.positionSpring = springPower == 0f ? float.PositiveInfinity : springPower;
            drv.positionDamper = dampingPower == 0f ? float.PositiveInfinity : dampingPower;

            joint.angularXDrive = drv;
            joint.angularYZDrive = drv;

            //for springy translating parts
            if(springPower > 0f || dampingPower > 0f)
                EnforceJointLimits ();
        }

        bool IJointLockState.IsJointUnlocked ()
        {
            return true;
        }

        /// <summary>
        /// Called every FixedUpdate, reads next target position and 
        /// updates the rotation/translation correspondingly.
        /// Marked for overhaul, use Motor instead.
        /// </summary>
        protected virtual void UpdatePosition()
        {
            Interpolator.Update(TimeWarp.fixedDeltaTime);

            float targetPos = Interpolator.GetPosition();
            float currentPos = rotateJoint ? rotation : translation;//rotateJoint ? GetRealRotation() : GetRealTranslation();

            if (lastRealPosition == 0f)
                lastRealPosition = currentPos;

            if (part.attachJoint != null && part.attachJoint.Joint != null)
            {
                part.attachJoint.Joint.xMotion = ConfigurableJointMotion.Free;
                part.attachJoint.Joint.yMotion = ConfigurableJointMotion.Free;
                part.attachJoint.Joint.zMotion = ConfigurableJointMotion.Free;

                part.attachJoint.Joint.angularXMotion = ConfigurableJointMotion.Free;
                part.attachJoint.Joint.angularYMotion = ConfigurableJointMotion.Free;
                part.attachJoint.Joint.angularZMotion = ConfigurableJointMotion.Free;
            }

            if (rotateJoint)
            {
                if (rotation != targetPos) //this comparison is intentional
                {
                    rotation = targetPos;

                    if (joint != null)
                    {
                        joint.targetRotation =
                            Quaternion.AngleAxis((rotation - rotationDelta), rotateAxis);
                    }
                    else if (RotateModelTransform != null)
                    {
                        Quaternion curRot =
                            Quaternion.AngleAxis(
                                //(invertSymmetry ? ((part.symmetryCounterparts.Count != 1) ? 1 : -1) : -1)* //obsolete
                                rotation, rotateAxis);
                        RotateModelTransform.localRotation = curRot;
                    }
                }
            }
            else
            {
                if (translation != targetPos) //this comparison is intentional
                {
                    translation = targetPos;

                    if (joint != null)
                    {
                        joint.targetPosition = -translateAxis*(translation - translationDelta);
                    }
                }
            }

            if (freeMoving)
            {
                rotation = GetRealRotation();
            }

            if (jointSpring == 0f || !UseTorque)
                return;

            currentPos = rotateJoint ? GetRealRotation() : GetRealTranslation();

            if (Mathf.Abs(targetPos - currentPos) >= 0.005f && (targetPos - currentPos) >= (targetPos - lastRealPosition))
            {
                //seems like our servo is stuck or not in position due to excess weight.
                //Logger.Log("Servo " + servoName + " seems stuck or does not have enough torque", Logger.Level.Debug);
                isStuck = true;

                //stop the movement? it is broken for presets and commands other than infinity
                if (jointSpring == 0f)
                {
                    //normal servos should just stop any command as they are definitely stuck.
                    Translator.Stop();
                }
                else
                {
                    //springy servos should be treated differently as their current position 
                    //may differ from target position due to spring sag/extension
                    //lastRealPosition = currentPos;
                    //isStuck = false;
                }

                /*
                 * Ideally we need to indicate current position, but it messes with so many things 
                 * For now leave it be.
                 */

                if (rotateJoint)
                {
                    rotation = currentPos;
                }
                else
                {
                    translation = currentPos;
                }

            }
            else
            {
                lastRealPosition = currentPos;
                isStuck = false;
            }
        }
 
        /// <summary>
        /// Adjust joint limits to keep within physical boundaries of the part defined by translateMax and TranslateMin
        /// Called from UpdateJointSettings
        /// </summary>
        protected void EnforceJointLimits()
        {
            float currentPos = rotateJoint ? GetRealRotation () : GetRealTranslation ();

            if (translateJoint && joint!=null && !isMotionLock)
            {
                //alternative approach - remove/increase springiness once currentPos is close to any of the limits
                if ((currentPos - translateMin) <= 0.05f || (translateMax - currentPos) <= 0.05f)
                {
                    var minDelta = Mathf.Max(Mathf.Min(Math.Abs(currentPos - translateMin), Math.Abs(translateMax - currentPos)), 0.0000001f);

                    JointDrive drv = joint.xDrive;
                    drv.maximumForce = UseTorque ? torqueTweak / minDelta : float.PositiveInfinity;
                    drv.positionSpring = jointSpring / minDelta;
                    drv.positionDamper = jointDamping / minDelta;

                    joint.xDrive = drv;

                    drv = joint.yDrive;
                    drv.maximumForce = UseTorque ? torqueTweak / minDelta : float.PositiveInfinity;
                    drv.positionSpring = jointSpring / minDelta;
                    drv.positionDamper = jointDamping / minDelta;
                    joint.yDrive = drv;

                    drv = joint.zDrive;
                    drv.maximumForce = UseTorque ? torqueTweak / minDelta : float.PositiveInfinity;
                    drv.positionSpring = jointSpring / minDelta;
                    drv.positionDamper = jointDamping / minDelta;
                    joint.zDrive = drv;

                    //Logger.Log("Servo " + servoName + " is close to get off limits, increasing spring power to " + jointSpring/minDelta, Logger.Level.Debug);
                }
            }
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
            if (KeyPressed(forwardKey))
            {
                Translator.Move(float.PositiveInfinity, speedTweak * customSpeed);
            }
            else if (KeyPressed(reverseKey))
            {
                Translator.Move(float.NegativeInfinity, speedTweak * customSpeed);
            }
            else if (KeyUnPressed(forwardKey) || KeyUnPressed(reverseKey))
            {
                Translator.Stop();
            }           
        }

        public void OnRescale(ScalingFactor factor)
        {
            if (rotateJoint)
                return;

            // TODO translate limits should be treated here if we ever want to unify
            // translation and rotation in the part configs
            // => enable here when we remove them from the tweakScale configs
            //translateMin *= factor;
            //translateMax *= factor;

            minTweak *= factor.relative.linear;
            maxTweak *= factor.relative.linear;

            // The part center is the origin of the moving mesh
            // so if translation!=0, the fixed mesh moves on rescale.
            // We need to move the part back so the fixed mesh stays at the same place.
            transform.Translate(-translateAxis * translation * (factor.relative.linear-1f) );

            if (HighLogic.LoadedSceneIsEditor)
                translation *= factor.relative.linear;

            // update the window so the new limits are applied
            UpdateMinMaxTweaks();

            //TweakWindow = part.FindActionWindow ();
            TweakIsDirty = true;

            Logger.Log ("OnRescale called, TweakWindow is null? = " + (TweakWindow == null), Logger.Level.Debug);
        }


        public void RefreshTweakUI()
        {
            if (HighLogic.LoadedScene != GameScenes.EDITOR) return;
            if (TweakWindow == null) return;

            UpdateMinMaxTweaks();

            if (part.symmetryCounterparts.Count >= 1)
            {
                foreach (Part counterPart in part.symmetryCounterparts)
                {
                    var module = ((ModuleIRServo)counterPart.Modules ["ModuleIRServo"]);
                    module.rotateMin = rotateMin;
                    module.rotateMax = rotateMax;
                    module.translateMin = translateMin;
                    module.translateMax = translateMax;
                    module.minTweak = minTweak;
                    module.maxTweak = maxTweak;
                }
            }
        }

        protected double GetAvailableElectricCharge()
        {
            if (!UseElectricCharge || !HighLogic.LoadedSceneIsFlight)
            {
                return electricChargeRequired;
            }
            PartResourceDefinition resDef = PartResourceLibrary.Instance.GetDefinition(ELECTRIC_CHARGE_RESOURCE_NAME);

            double amount, maxAmount;
            part.GetConnectedResourceTotals (resDef.id, resDef.resourceFlowMode, out amount, out maxAmount);
            return amount;
        }

        public void Update()
        {
            if (motorSound != null)
            {
                var basePitch = pitchSet;
                var servoBaseSpeed = Translator.GetSpeedUnit();

                if (servoBaseSpeed == 0.0f) servoBaseSpeed = 1;

                var pitchMultiplier = Math.Max(Math.Abs(Interpolator.Velocity/servoBaseSpeed), 0.05f);

                if (pitchMultiplier > 1)
                    pitchMultiplier = (float)Math.Sqrt (pitchMultiplier);

                float speedPitch = basePitch * pitchMultiplier;

                motorSound.Update(soundSet, speedPitch);
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                CheckInputs();
            }
        }

        /// <summary>
        /// This method sends a message every shapeUpdateTimeout FixedUpdate to part and its 
        /// children to update the shape. This is needed for nuFAR to rebuild the voxel shape accordingly.
        /// </summary>
        protected void ProcessShapeUpdates()
        {
            if (shapeUpdateCounter < shapeUpdateTimeout)
            {
                shapeUpdateCounter++;
                return;
            }

            if (Math.Abs(lastPosition - this.Position) >= 0.005)
            {
                part.SendMessage("UpdateShapeWithAnims");
                foreach (var p in part.children)
                {
                    p.SendMessage("UpdateShapeWithAnims");
                }

                lastPosition = this.Position;
            }

            shapeUpdateCounter = 0;
        }

        public void FixedUpdate()
        {
            if (HighLogic.LoadedScene != GameScenes.FLIGHT && HighLogic.LoadedScene != GameScenes.EDITOR)
            {
                return;
            }

            if (HighLogic.LoadedSceneIsEditor && TweakWindow != null && TweakIsDirty)
            {
                RefreshTweakUI();
                TweakWindow.UpdateWindow();
                TweakIsDirty = false;
            }
            
            if (part.State == PartStates.DEAD) 
            {                                  
                return;
            }

            if (isOnRails)
                return;

            if (minTweak > maxTweak)
            {
                maxTweak = minTweak;
            }

            //setup joints if they are not set up already (checked inside).
            SetupJoints ();

            if (HighLogic.LoadedSceneIsFlight)
            {
                //if we don't have enough power to move the whole group, we should not move it at all
                //at least this logic was implemented before
                //do we need it?
                float groupECRequired = GroupElectricChargeRequired * TimeWarp.fixedDeltaTime;

                if (UseElectricCharge && GetAvailableElectricCharge() <= groupECRequired)
                    Translator.Stop();

                if (Interpolator.Active)
                {
                    ServoController.SetWheelAutoStruts(false, FlightGlobals.ActiveVessel);
                    motorSound.Play();
                }
                else
                {
                    motorSound.Stop();
                }

                UpdatePosition();

                /*float currentTorque = (isMotionLock || (!UseTorque)) ? float.PositiveInfinity : (torqueTweak == 0f ? float.PositiveInfinity : torqueTweak);
                float currentSpring = isMotionLock ? float.PositiveInfinity : jointSpring == 0f ? float.PositiveInfinity : jointSpring;
                float currentDamping = isMotionLock ? 0 : jointDamping;


                //Springy joints are broken, need to redo it completely
                UpdateJointSettings(currentTorque, currentSpring, currentDamping);*/

                ConsumeElectricCharge();
            }
            
            if (vessel != null) //means flight mode as vessel is null in editor.
            {
                part.UpdateOrgPosAndRot(vessel.rootPart);
                foreach (Part child in part.FindChildParts<Part>(true))
                {
                    child.UpdateOrgPosAndRot(vessel.rootPart);
                }
            }

            //for FAR voxelization
            ProcessShapeUpdates();
        }

        public void ConsumeElectricCharge()
        {
            if (UseElectricCharge)
            {
                if (Interpolator.Active && !freeMoving)
                {
                    float amountToConsume = electricChargeRequired * TimeWarp.fixedDeltaTime;

                    if (UseTorque)
                        amountToConsume = torqueTweak / torqueMax * amountToConsume;

                    part.RequestResource(ELECTRIC_CHARGE_RESOURCE_NAME, amountToConsume);

                    LastPowerDrawRate = amountToConsume/TimeWarp.fixedDeltaTime;
                }
                else
                {
                    LastPowerDrawRate = 0f;
                }
            }
        }


        public void SetLock(bool isLocked)
        {
            if(!isLocked && failedAttachment)
            {
                BuildAttachments ();
                if (failedAttachment)
                {
                    Logger.Log ("Failed rebuilding attachments, try again", Logger.Level.Debug);
                    return;
                }
                    
            }
            isMotionLock = isLocked;
            Events["MotionLockToggle"].guiName = isMotionLock ? "Disengage Lock" : "Engage Lock";

            Translator.IsMotionLock = isMotionLock;

            if (isMotionLock)
                Translator.Stop();

        }



        /// <summary>
        /// Moves to the next preset. 
        /// Presets and position are assumed to be in internal coordinates.
        /// If servo's axis is inverted acts as MovePrevPreset()
        /// If rotate limits are off and there is no next preset it is supposed 
        /// to go to the first preset + 360 degrees.
        /// </summary>
        public void MoveNextPreset()
        {
            if (PresetPositions == null || PresetPositions.Count == 0) return;

            float nextPosition = Position;

            var availablePositions = invertAxis ? PresetPositions.FindAll (s => s < Position) : PresetPositions.FindAll (s => s > Position);

            if (availablePositions.Count > 0)
                nextPosition = invertAxis ? availablePositions.Max() : availablePositions.Min();
            
            else if (!limitTweakableFlag)
            {
                //part is unrestricted, we can choose first preset + 360
                nextPosition = invertAxis ? (PresetPositions.Max()-360)
                    : (PresetPositions.Min() + 360);
                
            }
            //because Translator expects position in external coordinates
            nextPosition = Translator.ToExternalPos (nextPosition);

            Logger.Log ("[Action] NextPreset, currentPos = " + Position + ", nextPosition=" + nextPosition, Logger.Level.Debug);
            Translator.Move(nextPosition, customSpeed * speedTweak);
        }

        /// <summary>
        /// Moves to the previous preset.
        /// Presets and position are assumed to be in internal coordinates.
        /// Command to be issued is translated to external coordinates.
        /// If servo's axis is inverted acts as MoveNextPreset()
        /// If rotate limits are off and there is no prev preset it is supposed 
        /// to go to the last preset - 360 degrees.
        /// </summary>
        public void MovePrevPreset()
        {
            if (PresetPositions == null || PresetPositions.Count == 0) return;

            float nextPosition = Position;

            var availablePositions = invertAxis ? PresetPositions.FindAll (s => s > Position) : PresetPositions.FindAll (s => s < Position);

            if (availablePositions.Count > 0)
                nextPosition = invertAxis ? availablePositions.Min() : availablePositions.Max();

            else if (!limitTweakableFlag)
            {
                //part is unrestricted, we can choose first preset
                nextPosition = invertAxis ?  (PresetPositions.Min() + 360) 
                    : (PresetPositions.Max()-360);
            }
            //because Translator expects position in external coordinates
            nextPosition = Translator.ToExternalPos (nextPosition);

            Logger.Log ("[Action] PrevPreset, currentPos = " + Position + ", nextPosition=" + nextPosition, Logger.Level.Debug);


            Translator.Move(nextPosition, customSpeed * speedTweak);
        }

        /// <summary>
        /// EditorOnly. Move to the specified direction.
        /// </summary>
        /// <param name="direction">Direction.</param>
        public void EditorMove(float direction)
        {
            float deltaPos = direction * GetAxisInversion();

            if(!freeMoving)
                deltaPos *= Translator.GetSpeedUnit()*Time.deltaTime;
            else
            {
                deltaPos *= 10*Time.deltaTime;
            }

            deltaPos *= speedTweak;

            if (!rotateJoint || limitTweakableFlag)
            {   // enforce limits
                float limitPlus  = maxTweak;
                float limitMinus = minTweak;
                if (Position + deltaPos > limitPlus)
                    deltaPos = limitPlus - Position;
                else if (Position + deltaPos < limitMinus)
                    deltaPos = limitMinus - Position;
            }

            EditorApplyDeltaPos(deltaPos);
        }

        /// <summary>
        /// For Editor use only, applies given deltaPos to servo's current position.
        /// 
        /// The only difference from flight mode is that due to lack of physics there are no joints
        /// And to account for this we need to manually move both FixedMesh and the part in opposite directions.
        /// </summary>
        /// <param name="deltaPos">Delta position.</param>
        public void EditorApplyDeltaPos(float deltaPos)
        {
            if (rotateJoint)
            {
                rotation += deltaPos;
                FixedMeshTransform.Rotate(-rotateAxis, deltaPos, Space.Self);
                transform.Rotate(rotateAxis, deltaPos, Space.Self);
            }
            else
            {
                translation += deltaPos;
                transform.Translate(-translateAxis * deltaPos);
                FixedMeshTransform.Translate(translateAxis * deltaPos);
            }
        }
        /// <summary>
        /// Editor only. Moves servo to the negative direction.
        /// </summary>
        public void EditorMoveLeft()
        {
            EditorMove(-1);
        }
        /// <summary>
        /// Editor Only. Moves servo to the positive direction.
        /// </summary>
        public void EditorMoveRight()
        {
            EditorMove(1);
        }

        /// <summary>
        /// Editor only. Resets servo to 0 rotation/translation
        /// </summary>
        public void EditorMoveCenter()
        {
            if(rotateJoint)
            {
                EditorApplyDeltaPos(defaultPosition-rotation);
            }
            else if (translateJoint)
            {
                EditorApplyDeltaPos(defaultPosition-translation);
            }
        }

    }
}