using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InfernalRobotics.Command;
using InfernalRobotics.Control.Servo;
using InfernalRobotics.Effects;
using InfernalRobotics.Gui;
using KSP.IO;
using KSPAPIExtensions;
using UnityEngine;
using TweakScale;

namespace InfernalRobotics.Module
{
    public class MuMechToggle : PartModule, IRescalable
    {
        //these 3 are for sending messages to inform nuFAR of shape changes to the craft.
        private const int shapeUpdateTimeout = 60; //it will send message every xx FixedUpdates
        private int shapeUpdateCounter = 0;
        private float lastPosition = 0f;

        private const string ELECTRIC_CHARGE_RESOURCE_NAME = "ElectricCharge";

        private static Material debugMaterial;
        private static int globalCreationOrder;
        private ElectricChargeConstraintData electricChargeConstraintData;
        private ConfigurableJoint joint;

        [KSPField(isPersistant = true)] public float customSpeed = 1;

        [KSPField(isPersistant = true)]
        public string forwardKey;
        
        [KSPField(isPersistant = true)] public bool freeMoving = false;
        [KSPField(isPersistant = true)] public string groupName = "";

        [KSPField(isPersistant = true)]
        public bool invertAxis;

        [KSPField(isPersistant = true)] public bool isMotionLock;
        [KSPField(isPersistant = true)] public bool limitTweakable = false;
        [KSPField(isPersistant = true)] public bool limitTweakableFlag = false;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max", guiFormat = "F2", guiUnits = ""), UI_FloatEdit(minValue = -360f, maxValue = 360f, incrementSlide = 0.01f, scene = UI_Scene.All)] 
        public float maxTweak = 360;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Min", guiFormat = "F2", guiUnits = ""), UI_FloatEdit(minValue = -360f, maxValue = 360f, incrementSlide = 0.01f, scene = UI_Scene.All)] 
        public float minTweak = 0;

        [KSPField(isPersistant = true)] public bool on = false;

        [KSPField(isPersistant = true)]
        public float pitchSet = 1f;

        [KSPField(isPersistant = true)]
        public float soundSet = .5f;

        [KSPField(isPersistant = true)]
        public string revRotateKey;
        
        [KSPField(isPersistant = true)]
        public string reverseKey;

        [KSPField(isPersistant = true)] public string rotateKey = "";
        [KSPField(isPersistant = true)] public bool rotateLimits = false;
        [KSPField(isPersistant = true)] public float rotateMax = 360;
        [KSPField(isPersistant = true)] public float rotateMin = 0;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Rotation")] public float rotation = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)] public float rotationDelta = 0;
        [KSPField(isPersistant = true)] public string servoName = "";

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Speed", guiFormat = "0.00"), 
         UI_FloatEdit(minValue = 0f, incrementSlide = 0.05f, incrementSmall=0.5f, incrementLarge=1f)]
        public float speedTweak = 1f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Acceleration", guiFormat = "0.00"), 
         UI_FloatEdit(minValue = 0.05f, incrementSlide = 0.05f, incrementSmall=0.5f, incrementLarge=1f)]
        public float accelTweak = 4f;

        [KSPField(isPersistant = true)] public float translateMax = 3;
        [KSPField(isPersistant = true)] public float translateMin = 0;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Translation")] public float translation = 0f;
        [KSPField(isPersistant = true)] public float translationDelta = 0;
        [KSPField(isPersistant = true)] public string presetPositionsSerialized = "";
        [KSPField(isPersistant = true)]
        public float defaultPosition = 0;
        [KSPField(isPersistant = false)] public string bottomNode = "bottom";
        [KSPField(isPersistant = false)] public bool debugColliders = false;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = true, guiName = "Electric Charge required", guiUnits = "EC/s")] public float electricChargeRequired = 2.5f;
        [KSPField(isPersistant = false)] public string fixedMesh = string.Empty;
        [KSPField(isPersistant = false)] public float friction = 0.5f;
        [KSPField(isPersistant = false)] public bool invertSymmetry = true;
        [KSPField(isPersistant = false)] public float jointDamping = 0;
        [KSPField(isPersistant = false)] public float jointSpring = 0;
        [KSPField(isPersistant = false)] public float keyRotateSpeed = 0;
        [KSPField(isPersistant = false)] public float keyTranslateSpeed = 0;
        [KSPField(isPersistant = false)] public string motorSndPath = "MagicSmokeIndustries/Sounds/infernalRoboticMotor";
        [KSPField(isPersistant = false)] public float offAngularDrag = 2.0f;
        [KSPField(isPersistant = false)] public float offBreakingForce = 22.0f;
        [KSPField(isPersistant = false)] public float offBreakingTorque = 22.0f;
        [KSPField(isPersistant = false)] public float offCrashTolerance = 9.0f;
        [KSPField(isPersistant = false)] public float offMaximumDrag = 0.2f;
        [KSPField(isPersistant = false)] public float offMinimumDrag = 0.2f;
        [KSPField(isPersistant = false)] public string offModel = "off";
        [KSPField(isPersistant = false)] public bool onActivate = true;
        [KSPField(isPersistant = false)] public string onKey = string.Empty;
        [KSPField(isPersistant = false)] public float onRotateSpeed = 0;
        [KSPField(isPersistant = false)] public float onTranslateSpeed = 0;
        [KSPField(isPersistant = false)] public float onAngularDrag = 2.0f;
        [KSPField(isPersistant = false)] public float onBreakingForce = 22.0f;
        [KSPField(isPersistant = false)] public float onBreakingTorque = 22.0f;
        [KSPField(isPersistant = false)] public float onCrashTolerance = 9.0f;
        [KSPField(isPersistant = false)] public float onMaximumDrag = 0.2f;
        [KSPField(isPersistant = false)] public float onMinimumDrag = 0.2f;
        [KSPField(isPersistant = false)] public string onModel = "on";
        [KSPField(isPersistant = false)] public Part origRootPart;
        [KSPField(isPersistant = false)] public string revTranslateKey = string.Empty;
        [KSPField(isPersistant = false)] public Vector3 rotateAxis = Vector3.forward;
        [KSPField(isPersistant = false)] public bool rotateJoint = false; 
        [KSPField(isPersistant = false)] public Vector3 rotatePivot = Vector3.zero;
        [KSPField(isPersistant = false)] public string rotateModel = "on";
        [KSPField(isPersistant = false)] public bool toggleBreak = false;
        [KSPField(isPersistant = false)] public bool toggleCollision = false;
        [KSPField(isPersistant = false)] public bool toggleDrag = false;
        [KSPField(isPersistant = false)] public bool toggleModel = false;
        [KSPField(isPersistant = false)] public Vector3 translateAxis = Vector3.forward;
        [KSPField(isPersistant = false)] public bool translateJoint = false;
        [KSPField(isPersistant = false)] public string translateKey = string.Empty;
        [KSPField(isPersistant = false)] public string translateModel = "on";

        private SoundSource motorSound;
        
        public MuMechToggle()
        {
            Interpolator = new Interpolator();
            Translator = new Translator();
            GroupElectricChargeRequired = 2.5f;
            TweakIsDirty = false;
            UseElectricCharge = true;
            CreationOrder = 0;
            MoveFlags = 0;
            MobileColliders = new List<Transform>();
            GotOrig = false;
            forwardKey = "";
            reverseKey = "";
            revRotateKey = "";

            //motorSound = new SoundSource(this.part, "motor");
        }

        protected Vector3 OrigTranslation { get; set; }
        protected bool GotOrig { get; set; }
        protected List<Transform> MobileColliders { get; set; }
        protected Transform ModelTransform { get; set; }
        protected Transform OnModelTransform { get; set; }
        protected Transform OffModelTransform { get; set; }
        protected Transform RotateModelTransform { get; set; }
        protected Transform TranslateModelTransform { get; set; }
        protected bool UseElectricCharge { get; set; }
        
        //Interpolator represents a controller, assuring smooth movements
        public Interpolator Interpolator { get; set; }

        //Translator represents an interface to interact with the servo
        public Translator Translator { get; set; }
        public Transform FixedMeshTransform { get; set; }
        public float GroupElectricChargeRequired { get; set; }
        public float LastPowerDraw { get; set; }
        public int MoveFlags { get; set; }
        public int CreationOrder { get; set; }
        public UIPartActionWindow TweakWindow { get; set; }
        public bool TweakIsDirty { get; set; }

        public List<float> PresetPositions { get; set; }

        public float Position { get { return rotateJoint ? rotation : translation; } }
        public float MinPosition {get { return Interpolator.Initialised ? Interpolator.MinPosition : minTweak;}}
        public float MaxPosition {get { return Interpolator.Initialised ? Interpolator.MaxPosition : maxTweak;}}

        private Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
        {
            //This handler is called only when the common language runtime tries to bind to the assembly and fails.

            //Retrieve the list of referenced assemblies in an array of AssemblyName.
            string strTempAssmbPath = "";
;
            Assembly objExecutingAssembly = Assembly.GetExecutingAssembly();
            AssemblyName[] arrReferencedAssmbNames = objExecutingAssembly.GetReferencedAssemblies();

            //Loop through the array of referenced assembly names.
            foreach (AssemblyName strAssmbName in arrReferencedAssmbNames)
            {
                //Check for the assembly names that have raised the "AssemblyResolve" event.
                if (strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) ==
                    args.Name.Substring(0, args.Name.IndexOf(",")))
                {
                    //Build the path of the assembly from where it has to be loaded.        
                    Logger.Log("looking!");
                    strTempAssmbPath = "C:\\Myassemblies\\" + args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";
                    break;
                }
            }

            //Load the assembly from the specified path.                    
            Assembly myAssembly = Assembly.LoadFrom(strTempAssmbPath);

            //Return the loaded assembly.
            return myAssembly;
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

        public bool IsSymmMaster()
        {
            return part.symmetryCounterparts.All( cp => ((MuMechToggle) cp.Modules["MuMechToggle"]).CreationOrder >= CreationOrder);
        }

        public float GetStepIncrement()
        {
            return rotateJoint ? 1f : 0.01f;
        }

        public void UpdateState()
        {
            if (on)
            {
                if (toggleModel)
                {
                    OnModelTransform.renderer.enabled = true;
                    OffModelTransform.renderer.enabled = false;
                }
                if (toggleDrag)
                {
                    part.angularDrag = onAngularDrag;
                    part.minimum_drag = onMinimumDrag;
                    part.maximum_drag = onMaximumDrag;
                }
                if (toggleBreak)
                {
                    part.crashTolerance = onCrashTolerance;
                    part.breakingForce = onBreakingForce;
                    part.breakingTorque = onBreakingTorque;
                }
            }
            else
            {
                if (toggleModel)
                {
                    OnModelTransform.renderer.enabled = false;
                    OffModelTransform.renderer.enabled = true;
                }
                if (toggleDrag)
                {
                    part.angularDrag = offAngularDrag;
                    part.minimum_drag = offMinimumDrag;
                    part.maximum_drag = offMaximumDrag;
                }
                if (toggleBreak)
                {
                    part.crashTolerance = offCrashTolerance;
                    part.breakingForce = offBreakingForce;
                    part.breakingTorque = offBreakingTorque;
                }
            }
            if (toggleCollision)
            {
                part.collider.enabled = on;
                part.collisionEnhancer.enabled = on;
                part.terrainCollider.enabled = on;
            }
        }

        protected void ColliderizeChilds(Transform obj)
        {
            if (obj.name.StartsWith("node_collider")
                || obj.name.StartsWith("fixed_node_collider")
                || obj.name.StartsWith("mobile_node_collider"))
            {
                print("Toggle: converting collider " + obj.name);

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
                    print("Collider has no MeshFilter (yet?): skipping Colliderize");
                }
            }
            for (int i = 0; i < obj.childCount; i++)
            {
                ColliderizeChilds(obj.GetChild(i));
            }
        }

        public override void OnAwake()
        {
            Logger.Log("[OnAwake] Start", Logger.Level.Debug);

            LoadConfigXml();

            FindTransforms();

            if (ModelTransform == null)
                Logger.Log("[OnAwake] ModelTransform is null", Logger.Level.Warning);

            ColliderizeChilds(ModelTransform);

            limitTweakableFlag = limitTweakableFlag | rotateLimits;

            try
            {
                Events["InvertAxisToggle"].guiName = invertAxis ? "Un-invert Axis" : "Invert Axis";
                Events["MotionLockToggle"].guiName = isMotionLock ? "Disengage Lock" : "Engage Lock";

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
                        Fields["speedTweak"].guiActive = false;
                        Fields["speedTweak"].guiActiveEditor = false;
                        Fields["accelTweak"].guiActive = false;
                        Fields["accelTweak"].guiActiveEditor = false;
                        Fields["rotation"].guiActive = false;
                        Fields["rotation"].guiActiveEditor = false;
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

                if (motorSound==null) motorSound = new SoundSource(part, "motor");
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("MMT.OnAwake exception {0}", ex.Message), Logger.Level.Fatal);
            }
            
            SetupMinMaxTweaks();
            ParsePresetPositions();

            FixedMeshTransform = KSPUtil.FindInPartModel(transform, fixedMesh);

            Logger.Log(string.Format("[OnAwake] End, rotateLimits={0}, minTweak={1}, maxTweak={2}, rotateJoint={0}", rotateLimits, minTweak, maxTweak), Logger.Level.Debug);
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

        public override void OnLoad(ConfigNode config)
        {
            Logger.Log("[OnLoad] Start", Logger.Level.Debug);

            FindTransforms();

            if (ModelTransform == null)
                Logger.Log("[OnLoad] ModelTransform is null", Logger.Level.Warning);

            ColliderizeChilds(ModelTransform);
            //maybe???
            rotationDelta = rotation;
            translationDelta = translation;

            GameScenes scene = HighLogic.LoadedScene;

            if (scene == GameScenes.EDITOR)
            {
                if (rotateJoint)
                {
                    FixedMeshTransform.Rotate(rotateAxis, -rotation);
                }
                else
                {
                    FixedMeshTransform.Translate(translateAxis * translation);
                }
            }


            translateKey = forwardKey;
            revTranslateKey = reverseKey;
            rotateKey = forwardKey;
            revRotateKey = reverseKey;

            ParsePresetPositions();

            UpdateMinMaxTweaks ();

            Logger.Log("[OnLoad] End", Logger.Level.Debug);
        }

        private void UpdateMinMaxTweaks()
        {
            var isEditor = (HighLogic.LoadedSceneIsEditor);

            var rangeMinF = isEditor? (UI_FloatEdit) Fields["minTweak"].uiControlEditor :(UI_FloatEdit) Fields["minTweak"].uiControlFlight;
            var rangeMaxF = isEditor? (UI_FloatEdit) Fields["maxTweak"].uiControlEditor :(UI_FloatEdit) Fields["maxTweak"].uiControlFlight;

            rangeMinF.minValue = rotateJoint ? rotateMin : translateMin;
            rangeMinF.maxValue = rotateJoint ? rotateMax : translateMax;
            rangeMaxF.minValue = rotateJoint ? rotateMin : translateMin;
            rangeMaxF.maxValue = rotateJoint ? rotateMax : translateMax;

            Logger.Log (string.Format ("UpdateTweaks: rotateJoint = {0}, rotateMin={1}, rotateMax={2}, translateMin={3}, translateMax={4}",
                rotateJoint, rotateMin, rotateMax, translateMin, translateMax), Logger.Level.Debug);
        }


        private void SetupMinMaxTweaks()
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

        protected void DebugCollider(MeshCollider toDebug)
        {
            if (debugMaterial == null)
            {
                debugMaterial = new Material(Shader.Find("Self-Illumin/Specular"))
                {
                    color = Color.red
                };
            }
            MeshFilter mf = toDebug.gameObject.GetComponent<MeshFilter>()
                            ?? toDebug.gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = toDebug.sharedMesh;
            MeshRenderer mr = toDebug.gameObject.GetComponent<MeshRenderer>()
                              ?? toDebug.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = debugMaterial;
        }

        protected void AttachToParent(Transform obj)
        {
            Transform fix = FixedMeshTransform;
            if (rotateJoint)
            {
                fix.RotateAround(transform.TransformPoint(rotatePivot), transform.TransformDirection(rotateAxis),
                    (invertSymmetry ? ((IsSymmMaster() || (part.symmetryCounterparts.Count != 1)) ? -1 : 1) : -1)*
                    rotation);
            }
            else if (translateJoint)
            {
                fix.Translate(transform.TransformDirection(translateAxis.normalized)*translation, Space.World);
            }
            fix.parent = part.parent.transform;
        }

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
                    if (debugColliders)
                    {
                        DebugCollider(tmp);
                    }
                }
                if (child.name.StartsWith("fixed_node_collider") && (part.parent != null))
                {
                    print("Toggle: reparenting collider " + child.name);
                    AttachToParent(child);
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

        public void BuildAttachments()
        {
            if (part.findAttachNodeByPart(part.parent).id.Contains(bottomNode)
                || part.attachMode == AttachModes.SRF_ATTACH)
            {
                if (fixedMesh != "")
                {
                    //Transform fix = model_transform.FindChild(fixedMesh);
                    Transform fix = FixedMeshTransform;
                    if ((fix != null) && (part.parent != null))
                    {
                        AttachToParent(fix);
                    }
                }
            }
            else
            {
                foreach (Transform t in ModelTransform)
                {
                    if (t.name != fixedMesh)
                    {
                        AttachToParent(t);
                    }
                }
                if (translateJoint)
                    translateAxis *= -1;
            }
            ReparentFriction(part.transform);
        }

        protected void FindTransforms()
        {
            ModelTransform = part.transform.FindChild("model");
            OnModelTransform = ModelTransform.FindChild(onModel);
            OffModelTransform = ModelTransform.FindChild(offModel);
            RotateModelTransform = ModelTransform.FindChild(rotateModel);
            TranslateModelTransform = ModelTransform.FindChild(translateModel);
        }
            
        // mrblaq return an int to multiply by rotation direction based on GUI "invert" checkbox bool
        public int GetAxisInversion()
        {
            return (invertAxis ? -1 : 1);
        }

        public override void OnStart(StartState state)
        {
            Logger.Log("[MMT] OnStart Start", Logger.Level.Debug);

            //part.stackIcon.SetIcon(DefaultIcons.STRUT);
            limitTweakableFlag = limitTweakableFlag | rotateLimits;

            if (!float.IsNaN(Position))
                Interpolator.Position = Position;

            Translator.Init(isMotionLock, new Servo(this), Interpolator);

            ConfigureInterpolator();

            if (vessel == null)
            {
                Logger.Log(string.Format("[MMT] OnStart vessel is null"));
                return;
            }

            if (motorSound==null) motorSound = new SoundSource(part, "motor");

            motorSound.Setup(motorSndPath, true);
            CreationOrder = globalCreationOrder++;

            FindTransforms();

            if (ModelTransform == null)
                Logger.Log("[MMT] OnStart ModelTransform is null", Logger.Level.Warning);

            BuildAttachments();

            UpdateState();
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

        public void ConfigureInterpolator()
        {
            // write interpolator configuration
            // (this should not change while it is active!!)
            if (Interpolator.Active)
                return;

            Interpolator.IsModulo = rotateJoint && !limitTweakableFlag;
            if (Interpolator.IsModulo)
            {
                Interpolator.Position = Interpolator.ReduceModulo(Interpolator.Position);
                Interpolator.MinPosition = -180;
                Interpolator.MaxPosition =  180;
            } 
            else
            {
                float min = Math.Min(minTweak, maxTweak);
                float max = Math.Max(minTweak, maxTweak);
                Interpolator.MinPosition = Math.Min(min, Interpolator.Position);
                Interpolator.MaxPosition = Math.Max(max, Interpolator.Position);
            }
            Interpolator.MaxAcceleration = accelTweak * Translator.GetSpeedUnit();
            Interpolator.Initialised = true;

            //Logger.Log("configureInterpolator:" + Interpolator, Logger.Level.Debug);
        }


        public bool SetupJoints()
        {
            if (!GotOrig)
            {
                // remove for less spam in editor
                //print("setupJoints - !gotOrig");
                if (rotateJoint || translateJoint)
                {
                    if (part.attachJoint != null)
                    {
                        // Catch reversed joint
                        // Maybe there is a best way to do it?
                        if (transform.position != part.attachJoint.Joint.connectedBody.transform.position)
                        {
                            joint = part.attachJoint.Joint.connectedBody.gameObject.AddComponent<ConfigurableJoint>();
                            joint.connectedBody = part.attachJoint.Joint.rigidbody;
                        }
                        else
                        {
                            joint = part.attachJoint.Joint.rigidbody.gameObject.AddComponent<ConfigurableJoint>();
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

                        // Set anchor position
                        joint.anchor =
                            joint.rigidbody.transform.InverseTransformPoint(joint.connectedBody.transform.position);
                        joint.connectedAnchor = Vector3.zero;

                        // Set correct axis
                        joint.axis =
                            joint.rigidbody.transform.InverseTransformDirection(joint.connectedBody.transform.right);
                        joint.secondaryAxis =
                            joint.rigidbody.transform.InverseTransformDirection(joint.connectedBody.transform.up);


                        if (translateJoint)
                        {
                            joint.xMotion = ConfigurableJointMotion.Free;
                            joint.yMotion = ConfigurableJointMotion.Free;
                            joint.zMotion = ConfigurableJointMotion.Free;
                        }

                        if (rotateJoint)
                        {
                            //Docking washer is broken currently?
                            joint.rotationDriveMode = RotationDriveMode.XYAndZ;
                            joint.angularXMotion = ConfigurableJointMotion.Free;
                            joint.angularYMotion = ConfigurableJointMotion.Free;
                            joint.angularZMotion = ConfigurableJointMotion.Free;

                            // Docking washer test
                            if (jointSpring > 0)
                            {
                                if (rotateAxis == Vector3.right || rotateAxis == Vector3.left)
                                {
                                    JointDrive drv = joint.angularXDrive;
                                    drv.positionSpring = jointSpring;
                                    joint.angularXDrive = drv;

                                    joint.angularYMotion = ConfigurableJointMotion.Locked;
                                    joint.angularZMotion = ConfigurableJointMotion.Locked;
                                }
                                else
                                {
                                    JointDrive drv = joint.angularYZDrive;
                                    drv.positionSpring = jointSpring;
                                    joint.angularYZDrive = drv;

                                    joint.angularXMotion = ConfigurableJointMotion.Locked;
                                    joint.angularZMotion = ConfigurableJointMotion.Locked;
                                }
                            }
                        }

                        // Reset default joint drives
                        var resetDrv = new JointDrive
                        {
                            mode = JointDriveMode.PositionAndVelocity,
                            positionSpring = 0,
                            positionDamper = 0,
                            maximumForce = 0
                        };

                        part.attachJoint.Joint.angularXDrive = resetDrv;
                        part.attachJoint.Joint.angularYZDrive = resetDrv;
                        part.attachJoint.Joint.xDrive = resetDrv;
                        part.attachJoint.Joint.yDrive = resetDrv;
                        part.attachJoint.Joint.zDrive = resetDrv;

                        GotOrig = true;
                        return true;
                    }
                    return false;
                }

                GotOrig = true;
                return true;
            }
            return false;
        }

        public override void OnActive()
        {
            if (onActivate)
            {
                on = true;
                UpdateState();
            }
        }

        protected void UpdatePosition()
        {
            float pos = Interpolator.GetPosition();
            if (rotateJoint)
            {
                if (rotation != pos) 
                {
                    rotation = pos;
                    DoRotation();
                } 
            }
            else
            {
                if (translation != pos) 
                {
                    translation = pos;
                    DoTranslation();
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
            if (KeyPressed(rotateKey) || KeyPressed(translateKey))
            {
                Translator.Move(float.PositiveInfinity, speedTweak * customSpeed);
            }
            else if (KeyPressed(revRotateKey) || KeyPressed(revTranslateKey))
            {
                Translator.Move(float.NegativeInfinity, speedTweak * customSpeed);
            }
            else if (KeyUnPressed(rotateKey) || KeyUnPressed(translateKey) || KeyUnPressed(revRotateKey) || KeyUnPressed(revTranslateKey))
            {
                Translator.Stop();
            }           
        }

        protected void DoRotation()
        {
            if (joint != null)
            {
                joint.targetRotation =
                    Quaternion.AngleAxis(
                        (invertSymmetry ? ((IsSymmMaster() || (part.symmetryCounterparts.Count != 1)) ? 1 : -1) : 1)*
                        (rotation - rotationDelta), rotateAxis);
            }
            else if (RotateModelTransform != null)
            {
                Quaternion curRot =
                    Quaternion.AngleAxis(
                        (invertSymmetry ? ((IsSymmMaster() || (part.symmetryCounterparts.Count != 1)) ? 1 : -1) : 1)*
                        rotation, rotateAxis);
                RotateModelTransform.localRotation = curRot;
                //transform.FindChild("model").FindChild(rotateModel).localRotation = curRot;
            }
        }

        protected void DoTranslation()
        {
            if (joint != null)
            {
                if (translateJoint)
                {
                    joint.targetPosition = -translateAxis*(translation - translationDelta);
                }
                else
                {
                    joint.targetPosition = OrigTranslation - translateAxis.normalized*(translation - translationDelta);
                }
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

            TweakWindow = part.FindActionWindow ();
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
                    var module = ((MuMechToggle)counterPart.Modules ["MuMechToggle"]);
                    module.rotateMin = rotateMin;
                    module.rotateMax = rotateMax;
                    module.translateMin = translateMin;
                    module.translateMax = translateMax;
                    module.minTweak = minTweak;
                    module.maxTweak = maxTweak;
                }
            }
        }

        private double GetAvailableElectricCharge()
        {
            if (!UseElectricCharge || !HighLogic.LoadedSceneIsFlight)
            {
                return electricChargeRequired;
            }
            PartResourceDefinition resDef = PartResourceLibrary.Instance.GetDefinition(ELECTRIC_CHARGE_RESOURCE_NAME);
            var resources = new List<PartResource>();
            part.GetConnectedResources(resDef.id, resDef.resourceFlowMode, resources);
            return resources.Count <= 0 ? 0f : resources.Select(r => r.amount).Sum();
        }

        void Update()
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
        private void ProcessShapeUpdates()
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

            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                if (TweakWindow != null && TweakIsDirty)
                {
                    RefreshTweakUI();
                    TweakWindow.UpdateWindow();
                    TweakIsDirty = false;
                }
            }

            if (part.State == PartStates.DEAD) 
            {                                  
                return;
            }

            SetupJoints();

            if (HighLogic.LoadedSceneIsFlight)
            {
                electricChargeConstraintData = new ElectricChargeConstraintData(GetAvailableElectricCharge(),
                    electricChargeRequired*TimeWarp.fixedDeltaTime, GroupElectricChargeRequired*TimeWarp.fixedDeltaTime);

                if (UseElectricCharge && !electricChargeConstraintData.Available)
                    Translator.Stop();

                Interpolator.Update(TimeWarp.fixedDeltaTime);
                UpdatePosition();

                if (Interpolator.Active)
                {
                    motorSound.Play();
                    electricChargeConstraintData.MovementDone = true;
                }
                else
                    motorSound.Stop();
            }

            if (minTweak > maxTweak)
            {
                maxTweak = minTweak;
            }

            if (HighLogic.LoadedSceneIsFlight)
                HandleElectricCharge();

            if (vessel != null)
            {
                part.UpdateOrgPosAndRot(vessel.rootPart);
                foreach (Part child in part.FindChildParts<Part>(true))
                {
                    child.UpdateOrgPosAndRot(vessel.rootPart);
                }
            }

            ProcessShapeUpdates();
        }

        public void HandleElectricCharge()
        {
            if (UseElectricCharge)
            {
                if (electricChargeConstraintData.MovementDone)
                {
                    part.RequestResource(ELECTRIC_CHARGE_RESOURCE_NAME, electricChargeConstraintData.ToConsume);
                    float displayConsume = electricChargeConstraintData.ToConsume/TimeWarp.fixedDeltaTime;
                    if (electricChargeConstraintData.Available)
                    {
                        LastPowerDraw = displayConsume;
                    }
                    LastPowerDraw = displayConsume;
                }
                else
                {
                    LastPowerDraw = 0f;
                }
            }
        }

        public override void OnInactive()
        {
            on = false;
            UpdateState();
        }

        public void SetLock(bool isLocked)
        {
            isMotionLock = isLocked;
            Events["MotionLockToggle"].guiName = isMotionLock ? "Disengage Lock" : "Engage Lock";

            Translator.IsMotionLock = isMotionLock;
            if (isMotionLock)
                Translator.Stop();
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
                Translator.Move(Translator.ToExternalPos(0f), customSpeed * speedTweak);
                    break;
                case KSPActionType.Deactivate:
                    Translator.Stop ();
                    break;
            }
        }

        public void LoadConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<MuMechToggle>();
            config.load();
            UseElectricCharge = config.GetValue<bool>("useEC");
            if (!rotateAxis.IsZero())
                rotateAxis.Normalize();
            if (!translateAxis.IsZero())
                translateAxis.Normalize();
        }

        public void SaveConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<ControlsGUI>();
            config.SetValue("useEC", UseElectricCharge);
            config.save();
        }

        public void Move(float direction)
        {
            float deltaPos = direction * GetAxisInversion();

            deltaPos *= Translator.GetSpeedUnit()*Time.deltaTime;

            if (!rotateJoint || limitTweakableFlag)
            {   // enforce limits
                float limitPlus  = maxTweak;
                float limitMinus = minTweak;
                if (Position + deltaPos > limitPlus)
                    deltaPos = limitPlus - Position;
                else if (Position + deltaPos < limitMinus)
                    deltaPos = limitMinus - Position;
            }

            ApplyDeltaPos(deltaPos);
        }

        public void ApplyDeltaPos(float deltaPos)
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

        public void MoveLeft()
        {
            Move(-1);
        }
        public void MoveRight()
        {
            Move(1);
        }

        //resets servo to 0 rotation/translation
        public void MoveCenter()
        {
            if(rotateJoint)
            {
                ApplyDeltaPos(defaultPosition-rotation);
            }
            else if (translateJoint)
            {
                ApplyDeltaPos(defaultPosition-translation);
            }
        }

        protected class ElectricChargeConstraintData
        {
            public ElectricChargeConstraintData(double availableCharge, float requiredCharge, float groupRequiredCharge)
            {
                Available = availableCharge > 0.01d;
                Enough = Available && (availableCharge >= groupRequiredCharge*0.1);
                float groupRatio = availableCharge >= groupRequiredCharge
                    ? 1f
                    : (float) availableCharge/groupRequiredCharge;
                Ratio = Enough ? groupRatio : 0f;
                ToConsume = requiredCharge*groupRatio;
                MovementDone = false;
            }

            public float Ratio { get; set; }
            public float ToConsume { get; set; }
            public bool Available { get; set; }
            public bool MovementDone { get; set; }
            public bool Enough { get; set; }
        }
    }
}