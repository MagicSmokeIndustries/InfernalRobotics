using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InfernalRobotics.Effects;
using InfernalRobotics.Gui;
using KSP.IO;
using KSPAPIExtensions;
using UnityEngine;

namespace InfernalRobotics.Module
{
    public class MuMechToggle : PartModule
    {

        private const string ELECTRIC_CHARGE_RESOURCE_NAME = "ElectricCharge";
        private const float SPEED = 0.5f;

        private static Material debugMaterial;
        private static int globalCreationOrder;
        private ElectricChargeConstraintData electricChargeConstraintData;
        private ConfigurableJoint joint;

        [KSPField(guiName = "E-State", guiActive = true, guiActiveEditor = true)] 
        public string ElectricStateDisplay = "n.a. Ec/s Power Draw est.";

        [KSPField(isPersistant = true)] public float customSpeed = 1;
        [KSPField(isPersistant = true)] public Vector3 fixedMeshOriginalLocation;

        [KSPField(isPersistant = true)]
        public string forwardKey
        {
            get { return forwardKeyStore; }
            set { forwardKeyStore = value.ToLower(); }
        }

        [KSPField(isPersistant = true)] public bool freeMoving = false;
        [KSPField(isPersistant = true)] public string groupName = "";
        [KSPField(isPersistant = true)] public bool hasModel = false;
        [KSPField(isPersistant = true)] public bool invertAxis = false;
        [KSPField(isPersistant = true)] public bool isMotionLock;
        [KSPField(isPersistant = true)] public bool limitTweakable = false;
        [KSPField(isPersistant = true)] public bool limitTweakableFlag = false;

        [KSPField(isPersistant = true)] public string maxRange = "";
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max Range", guiFormat = "F2", guiUnits = ""), UI_FloatEdit(minValue = -360f, maxValue = 360f, incrementSlide = 0.01f, scene = UI_Scene.All)] 
        public float maxTweak = 360;

        [KSPField(isPersistant = true)] public string minRange = "";
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Min Range", guiFormat = "F2", guiUnits = ""), UI_FloatEdit(minValue = -360f, maxValue = 360f, incrementSlide = 0.01f, scene = UI_Scene.All)] 
        public float minTweak = 0;

        [KSPField(isPersistant = true)] public bool on = false;

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Sound Pitch", guiFormat = "F2", guiUnits = ""),
         UI_FloatEdit(minValue = -10f, maxValue = 10f, incrementSlide = 1f, scene = UI_Scene.All)]
        public float pitchSet = 1f;

        [KSPField(isPersistant = true)]
        public string revRotateKey
        {
            get { return reverseRotateKeyStore; }
            set { reverseRotateKeyStore = value.ToLower(); }
        }

        [KSPField(isPersistant = true)]
        public string reverseKey
        {
            get { return reverseKeyStore; }
            set { reverseKeyStore = value.ToLower(); }
        }

        [KSPField(isPersistant = true)] public bool reversedRotationKey = false;
        [KSPField(isPersistant = true)] public bool reversedRotationOn = false;
        [KSPField(isPersistant = true)] public bool reversedTranslationKey = false;
        [KSPField(isPersistant = true)] public bool reversedTranslationOn = false;
        [KSPField(isPersistant = true)] public string rotateKey = "";
        [KSPField(isPersistant = true)] public bool rotateLimits = false;
        [KSPField(isPersistant = true)] public float rotateMax = 360;
        [KSPField(isPersistant = true)] public float rotateMin = 0;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Rotation:")] public float rotation = 0;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)] public float rotationDelta = 0;
        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false)] public float rotationEuler = 0;
        [KSPField(isPersistant = true)] public string servoName = "";

        [KSPField(isPersistant = true, guiActive = false, guiActiveEditor = false, guiName = "Sound Vol", guiFormat = "F2", guiUnits = ""),
            UI_FloatEdit(minValue = 0.0f, maxValue = 1.0f, incrementSlide = 0.01f, scene = UI_Scene.All)]
        public float soundSet = .5f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Speed", guiFormat = "0.00"), 
         UI_FloatEdit(minValue = 0f, incrementSlide = 0.1f, incrementSmall=10, incrementLarge=100)]
        public float speedTweak = 1;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Fine Speed", guiFormat = "0.00"),
         UI_FloatRange(minValue = -0.1f, maxValue = 0.1f, stepIncrement = 0.01f)]
        public float speedTweakFine = 0f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Accel", guiFormat = "0.00"), 
         UI_FloatEdit(minValue = 0.05f, incrementSlide = 0.1f, incrementSmall=10, incrementLarge=100)]
        public float accelTweak = 100f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Step Increment"), UI_ChooseOption(options = new[] {"0.01", "0.1", "1.0"})] 
        public string stepIncrement = "0.1";

        [KSPField(isPersistant = true)] public bool translateLimits = false;
        [KSPField(isPersistant = true)] public float translateMax = 3;
        [KSPField(isPersistant = true)] public float translateMin = 0;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Translation")] 
        public float translation = 0f;
        [KSPField(isPersistant = true)] public float translationDelta = 0;


        [KSPField(isPersistant = false)] public string bottomNode = "bottom";
        [KSPField(isPersistant = false)] public bool debugColliders = false;
        [KSPField(isPersistant = false)] public float electricChargeRequired = 2.5f;
        [KSPField(isPersistant = false)] public string fixedMesh = string.Empty;
        [KSPField(isPersistant = false)] public float friction = 0.5f;
        [KSPField(isPersistant = false)] public bool invertSymmetry = true;
        [KSPField(isPersistant = false)] public float jointDamping = 0;
        [KSPField(isPersistant = false)] public float jointSpring = 0;
        [KSPField(isPersistant = false)] public float keyRotateSpeed = 0;
        [KSPField(isPersistant = false)] public float keyTranslateSpeed = 0;
        [KSPField(isPersistant = false)]
        public string motorSndPath = "MagicSmokeIndustries/Sounds/infernalRoboticMotor";
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
        [KSPField(isPersistant = false)] public bool rotateLimitsOff = false;
        [KSPField(isPersistant = false)] public bool rotateLimitsRevertKey = false;
        [KSPField(isPersistant = false)] public bool rotateLimitsRevertOn = true;
        [KSPField(isPersistant = false)] public Vector3 rotatePivot = Vector3.zero;
        [KSPField(isPersistant = false)] public string rotateModel = "on";
        [KSPField(isPersistant = false)] public bool showGUI = false;
        [KSPField(isPersistant = false)] public bool toggleBreak = false;
        [KSPField(isPersistant = false)] public bool toggleCollision = false;
        [KSPField(isPersistant = false)] public bool toggleDrag = false;
        [KSPField(isPersistant = false)] public bool toggleModel = false;
        [KSPField(isPersistant = false)] public Vector3 translateAxis = Vector3.forward;
        [KSPField(isPersistant = false)] public bool translateJoint = false;
        [KSPField(isPersistant = false)] public string translateKey = string.Empty;
        [KSPField(isPersistant = false)] public bool translateLimitsOff = false;
        [KSPField(isPersistant = false)] public bool translateLimitsRevertKey = false;
        [KSPField(isPersistant = false)] public bool translateLimitsRevertOn = true;
        [KSPField(isPersistant = false)] public string translateModel = "on";

        private SoundSource motorSound;
        private bool positionGUIEnabled;
        /*private UI_FloatEdit rangeMaxE;
        private UI_FloatEdit rangeMaxF;
        private UI_FloatEdit rangeMinE;
        private UI_FloatEdit rangeMinF;*/
        private string reverseKeyStore;
        private string reverseRotateKeyStore;
        private string forwardKeyStore;

        static MuMechToggle()
        {
            ResetWin = false;
        }

        public MuMechToggle()
        {
            Interpolator = new Interpolator();
            RotationLast = 0;
            GroupElectricChargeRequired = 2.5f;
            OriginalTranslation = 0f;
            OriginalAngle = 0f;
            TweakIsDirty = false;
            UseElectricCharge = true;
            CreationOrder = 0;
            MoveFlags = 0;
            TranslationChanged = 0;
            RotationChanged = 0;
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
        protected int RotationChanged { get; set; }
        protected int TranslationChanged { get; set; }
        protected Transform ModelTransform { get; set; }
        protected Transform OnModelTransform { get; set; }
        protected Transform OffModelTransform { get; set; }
        protected Transform RotateModelTransform { get; set; }
        protected Transform TranslateModelTransform { get; set; }
        protected bool UseElectricCharge { get; set; }
        //protected bool Loaded { get; set; }
        protected static Rect ControlWinPos2 { get; set; }
        protected static bool ResetWin { get; set; }

        public Interpolator Interpolator { get; set; }
        public float RotationLast { get; set; }
        public Transform FixedMeshTransform { get; set; }
        public float GroupElectricChargeRequired { get; set; }
        public float LastPowerDraw { get; set; }
        public int MoveFlags { get; set; }
        public int CreationOrder { get; set; }
        public UIPartActionWindow TweakWindow { get; set; }
        public bool TweakIsDirty { get; set; }
        public float OriginalAngle { get; set; }
        public float OriginalTranslation { get; set; }

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
                    Debug.Log("looking!");
                    strTempAssmbPath = "C:\\Myassemblies\\" + args.Name.Substring(0, args.Name.IndexOf(",")) + ".dll";
                    break;
                }
            }

            //Load the assembly from the specified path.                    
            Assembly myAssembly = Assembly.LoadFrom(strTempAssmbPath);

            //Return the loaded assembly.
            return myAssembly;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Rotate Limits Off", active = false)]
        public void LimitTweakableToggle()
        {
            limitTweakableFlag = !limitTweakableFlag;
            Events["LimitTweakableToggle"].guiName = limitTweakableFlag ? "Rotate Limits On" : "Rotate Limits Off";
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Invert Axis Off")]
        public void InvertAxisOff()
        {
            invertAxis = !invertAxis;
            Events["InvertAxisOn"].active = true;
            Events["InvertAxisOff"].active = false;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Invert Axis On", active = false)]
        public void InvertAxisOn()
        {
            invertAxis = !invertAxis;
            Events["InvertAxisOn"].active = false;
            Events["InvertAxisOff"].active = true;
        }

        //add Move+ and Move- KSPEvents as an alternative to corresponding KSPActions

        [KSPEvent(guiName = "Move +", guiActive = true, guiActiveEditor=false)]
        public void MovePlusEvent()
        {
            if ((MoveFlags & 0x101) == 0) {
                Events ["MovePlusEvent"].guiName = "Stop Move +";
                MoveFlags |= 0x100;
            } else {
                Events ["MovePlusEvent"].guiName = "Move +";
                MoveFlags &= ~0x100;
            }
        }

        [KSPEvent(guiName = "Move -", guiActive = true, guiActiveEditor=false)]
        public void MoveMinusEvent()
        {
            if ((MoveFlags & 0x202) == 0) {
                Events ["MoveMinusEvent"].guiName = "Stop Move -";
                MoveFlags |= 0x200;

            } else {
                Events ["MoveMinusEvent"].guiName = "Move -";
                MoveFlags &= ~0x200;
            }
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "Show Position Editor", active = true)]
        public void ShowMainMenu()
        {
            positionGUIEnabled = true;
        }

        public bool IsSymmMaster()
        {
            return part.symmetryCounterparts.All( cp => ((MuMechToggle) cp.Modules["MuMechToggle"]).CreationOrder >= CreationOrder);
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

        private void OnDestroy()
        {
            PositionLock(false);
        }

        protected void ColliderizeChilds(Transform obj)
        {
            if (obj.name.StartsWith("node_collider")
                || obj.name.StartsWith("fixed_node_collider")
                || obj.name.StartsWith("mobile_node_collider"))
            {
                print("Toggle: converting collider " + obj.name);

                if (!obj.GetComponent<MeshFilter>())
                {
                    print("Collider has no MeshFilter (yet?): skipping Colliderize");
                }
                else
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
            }
            for (int i = 0; i < obj.childCount; i++)
            {
                ColliderizeChilds(obj.GetChild(i));
            }
        }

        public override void OnAwake()
        {
            Debug.Log("[IR OnAwake] Start");

            LoadConfigXml();

            if (!UseElectricCharge || freeMoving)
            {
                Fields["ElectricStateDisplay"].guiActive = false;
                Fields["ElectricStateDisplay"].guiActiveEditor = false;
            }

            FindTransforms();

            if (ModelTransform == null)
                Debug.LogWarning("[IR OnAwake] ModelTransform is null");

            ColliderizeChilds(ModelTransform);

            try
            {
                if (rotateJoint)
                {
                    minTweak = rotateMin;
                    maxTweak = rotateMax;
                    
                    if (limitTweakable)
                    {
                        Events["LimitTweakableToggle"].active = true;
                    }

                    if (freeMoving)
                    {
                        Events["InvertAxisOn"].active = false;
                        Events["InvertAxisOff"].active = false;
                        Fields["minTweak"].guiActive = false;
                        Fields["minTweak"].guiActiveEditor = false;
                        Fields["maxTweak"].guiActive = false;
                        Fields["maxTweak"].guiActiveEditor = false;
                        Fields["speedTweak"].guiActive = false;
                        Fields["speedTweak"].guiActiveEditor = false;
                        Fields["speedTweakFine"].guiActive = false;
                        Fields["speedTweakFine"].guiActiveEditor = false;
                        Events["Activate"].active = false;
                        Events["Deactivate"].active = false;
                        Fields["stepIncrement"].guiActiveEditor = false;
                        Fields["stepIncrement"].guiActive = false;
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
                Debug.LogError(string.Format("MMT.OnAwake exception {0}", ex.Message));
            }
            
            GameScenes scene = HighLogic.LoadedScene;
            if (scene == GameScenes.EDITOR)
            {
                if (rotateJoint)
                    ParseMinMaxTweaks(rotateMin, rotateMax);
                else if (translateJoint)
                    ParseMinMaxTweaks(translateMin, translateMax);

                if (UseElectricCharge)
                {
                    ElectricStateDisplay = string.Format("{0:#0.##} Ec/s est. Power Draw", electricChargeRequired);
                }
            }

            FixedMeshTransform = KSPUtil.FindInPartModel(transform, fixedMesh);

            Debug.Log("[IR OnAwake] End, rotateLimits=" + rotateLimits + ", minTweak=" + minTweak + ", maxTweak=" + maxTweak + ", rotateJoint=" + rotateLimits);
        }

        public Transform FindFixedMesh(Transform meshTransform)
        {
            Transform t = part.transform.FindChild("model").FindChild(fixedMesh);

            return t;
        }


        public override void OnSave(ConfigNode node)
        {
            Debug.Log("[IR OnSave] Start");
            base.OnSave(node);
            if (rotateJoint)
                ParseMinMaxTweaks(rotateMin, rotateMax);
            else if (translateJoint)
                ParseMinMaxTweaks(translateMin, translateMax);
            GameScenes scene = HighLogic.LoadedScene;

            if (scene == GameScenes.EDITOR)
            {
                if (rotateJoint)
                {
                    if (part.name.Contains("IR.Rotatron.OffAxis") && rotationEuler != 0f)
                    {
                        rotation = rotationEuler/0.7070f;
                        rotationEuler = 0f;
                    }
                }
                else
                    rotation = rotationEuler;
            }
            Debug.Log("[IR OnSave] End");
        }

        public void RefreshKeys()
        {
            translateKey = forwardKey;
            revTranslateKey = reverseKey;
            rotateKey = forwardKey;
            revRotateKey = reverseKey;
        }

        public override void OnLoad(ConfigNode config)
        {
            //Loaded = true;
            Debug.Log("[IR OnLoad] Start");

            FindTransforms();

            if (ModelTransform == null)
                Debug.LogWarning("[IR OnLoad] ModelTransform is null");

            ColliderizeChilds(ModelTransform);
            //maybe???
            rotationDelta = RotationLast = rotation;
            translationDelta = translation;

            GameScenes scene = HighLogic.LoadedScene;

            
            //TODO get rid of this hardcoded non-sense

            if (scene == GameScenes.FLIGHT)
            {
                if (part.name.Contains("Gantry"))
                {
                    FixedMeshTransform.Translate((-translateAxis.x*translation*2),
                        (-translateAxis.y*translation*2),
                        (-translateAxis.z*translation*2), Space.Self);
                }
            }

            

            if (scene == GameScenes.EDITOR)
            {
                if (part.name.Contains("Gantry"))
                {
                    FixedMeshTransform.Translate((-translateAxis.x*translation),
                        (-translateAxis.y*translation),
                        (-translateAxis.z*translation), Space.Self);
                }

                if (rotateJoint)
                {
                    if (!part.name.Contains("IR.Rotatron.OffAxis"))
                    {
                        FixedMeshTransform.Rotate(rotateAxis, -rotationEuler);
                    }
                    else
                    {
                        FixedMeshTransform.eulerAngles = (fixedMeshOriginalLocation);
                    }
                }
                else if (translateJoint && !part.name.Contains("Gantry"))
                {
                    FixedMeshTransform.Translate((translateAxis.x*translation),
                        (translateAxis.y*translation),
                        (translateAxis.z*translation), Space.Self);
                }
            }


            translateKey = forwardKey;
            revTranslateKey = reverseKey;
            rotateKey = forwardKey;
            revRotateKey = reverseKey;
            
            if (rotateJoint)
                ParseMinMaxTweaks(rotateMin, rotateMax);
            else if (translateJoint)
                ParseMinMaxTweaks(translateMin, translateMax);
            ParseMinMax();

            Debug.Log("[IR OnLoad] End");
        }

        private void ParseMinMaxTweaks(float movementMinimum, float movementMaximum)
        {
            var rangeMinF = (UI_FloatEdit) Fields["minTweak"].uiControlFlight;
            var rangeMinE = (UI_FloatEdit) Fields["minTweak"].uiControlEditor;
            rangeMinE.minValue = movementMinimum;
            rangeMinE.maxValue = movementMaximum;
            rangeMinE.incrementSlide = float.Parse(stepIncrement);
            rangeMinF.minValue = movementMinimum;
            rangeMinF.maxValue = movementMaximum;
            rangeMinF.incrementSlide = float.Parse(stepIncrement);
            var rangeMaxF = (UI_FloatEdit) Fields["maxTweak"].uiControlFlight;
            var rangeMaxE = (UI_FloatEdit) Fields["maxTweak"].uiControlEditor;
            rangeMaxE.minValue = movementMinimum;
            rangeMaxE.maxValue = movementMaximum;
            rangeMaxE.incrementSlide = float.Parse(stepIncrement);
            rangeMaxF.minValue = movementMinimum;
            rangeMaxF.maxValue = movementMaximum;
            rangeMaxF.incrementSlide = float.Parse(stepIncrement);

            if (rotateJoint)
            {
                Fields["minTweak"].guiName = "Min Rotate";
                Fields["maxTweak"].guiName = "Max Rotate";
            }
            else if (translateJoint)
            {
                Fields["minTweak"].guiName = "Min Translate";
                Fields["maxTweak"].guiName = "Max Translate";
            }
        }

        protected void ParseMinMax()
        {
            // mrblaq - prepare variables for comparison.
            // assigning to temp so I can handle empty setting strings on GUI. Defaulting to +/-200 so items' default motion are uninhibited
            try
            {
                minTweak = float.Parse(minRange);
            }
            catch (FormatException)
            {
                //Debug.Log("Minimum Range Value is not a number");
            }

            try
            {
                maxTweak = float.Parse(maxRange);
            }
            catch (FormatException)
            {
                //Debug.Log("Maximum Range Value is not a number");
            }
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
            //Transform fix = transform.FindChild("model").FindChild(fixedMesh);
            Transform fix = FixedMeshTransform;
            if (rotateJoint)
            {
                /* Commenting out unused code
                 * Vector3 pivot = part.transform.TransformPoint(rotatePivot);
                Vector3 raxis = part.transform.TransformDirection(rotateAxis);

                float sign = 1;
                if (invertSymmetry)
                {
                    //FIXME is this actually desired?
                    sign = ((IsSymmMaster() || (part.symmetryCounterparts.Count != 1)) ? 1 : -1);
                }
                */
                //obj.RotateAround(pivot, raxis, sign * rotation);
                fix.RotateAround(transform.TransformPoint(rotatePivot), transform.TransformDirection(rotateAxis),
                    (invertSymmetry ? ((IsSymmMaster() || (part.symmetryCounterparts.Count != 1)) ? -1 : 1) : -1)*
                    rotation);
            }
            else if (translateJoint)
            {
                //var taxis = part.transform.TransformDirection(translateAxis.normalized);
                //obj.Translate(taxis * -(translation - translateMin), Space.Self);//XXX double check sign!
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

        private void OnEditorAttach()
        {
        }

        // mrblaq return an int to multiply by rotation direction based on GUI "invert" checkbox bool
        public int GetAxisInversion()
        {
            return (invertAxis ? 1 : -1);
        }

        public override void OnStart(StartState state)
        {
            Debug.Log("[IR MMT] OnStart Start");

            BaseField field = Fields["stepIncrement"];

            var optionsEditor = (UI_ChooseOption)field.uiControlEditor;
            var optionsFlight = (UI_ChooseOption)field.uiControlFlight;

            if (translateJoint)
            {
                optionsEditor.options = new[] { "0.01", "0.1", "1.0" };
                optionsFlight.options = new[] { "0.01", "0.1", "1.0" };
            }
            else if (rotateJoint)
            {
                optionsEditor.options = new[] { "0.1", "1", "10" };
                optionsFlight.options = new[] { "0.1", "1", "10" };
            }
            
            //part.stackIcon.SetIcon(DefaultIcons.STRUT);

            limitTweakableFlag = rotateLimits;

            //what if speedTweak or accelTweak is already set in part.cfg?
            speedTweak = rotateJoint ? keyRotateSpeed : keyTranslateSpeed;
            accelTweak = 8f * speedTweak;

            //read starting position
            float position = rotateJoint ? rotation : translation;

            if (!float.IsNaN(position))
                Interpolator.Position = position;
            
            ConfigureInterpolator();


            if (vessel == null)
            {
                Debug.Log(String.Format("[IR MMT] OnStart vessel is null"));
                return;
            }

            if (motorSound==null) motorSound = new SoundSource(part, "motor");

            motorSound.Setup(motorSndPath, true);
            CreationOrder = globalCreationOrder++;

            FindTransforms();

            if (ModelTransform == null)
                Debug.LogWarning("[IR MMT] OnStart ModelTransform is null");

            BuildAttachments();

            UpdateState();

            if (rotateJoint)
            {
                ParseMinMaxTweaks(rotateMin, rotateMax);
                if (limitTweakable)
                {
                    Events["LimitTweakableToggle"].active = true;
                }
            }
            else if (translateJoint)
            {
                ParseMinMaxTweaks(translateMin, translateMax);
                if (limitTweakable)
                {
                    Events["LimitTweakableToggle"].active = false;
                }
            }



            //limitTweakableFlag = rotateLimits;
            //ConfigureInterpolator();

            Debug.Log("[IR MMT] OnStart End, rotateLimits=" + rotateLimits + ", minTweak=" + minTweak + ", maxTweak=" + maxTweak);
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Configure Interpolator", active = true)]
        public void ConfigureInterpolator()
        {
            // write interpolator configuration
            // (this should not change while it is active!!)
            if (Interpolator.Active)
            {
                Debug.Log("IR: configureInterpolator: busy, reconfiguration not possible now!");
                return;
            }

            if (rotateJoint)
            {
                Interpolator.IsModulo = !limitTweakableFlag;
                Interpolator.Position = Interpolator.ReduceModulo(Interpolator.Position);
            } 
            else
                Interpolator.IsModulo = false;

            if (Interpolator.IsModulo)
            {
                Interpolator.MinPosition = -180;
                Interpolator.MaxPosition =  180;
            } 
            else 
            {
                Interpolator.MinPosition = Math.Min(minTweak, maxTweak);
                Interpolator.MaxPosition = Math.Max(minTweak, maxTweak);
            }
            Interpolator.MaxAcceleration = accelTweak;
            Debug.Log("IR: configureInterpolator:" + Interpolator );
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


        protected void UpdateRotation(float rotationSpeed, bool reverse, int mask)
        {
            if (!UseElectricCharge || electricChargeConstraintData.Available)
            {
                rotationSpeed *= speedTweak * customSpeed * (reverse ? -1 : 1);
                rotation += GetAxisInversion() * TimeWarp.fixedDeltaTime * rotationSpeed * electricChargeConstraintData.Ratio;
                RotationChanged |= mask;
                
                if (motorSound != null) motorSound.Play();
            }
        }

        protected void UpdateTranslation(float translationSpeed, bool reverse, int mask)
        {
            if (!UseElectricCharge || electricChargeConstraintData.Available)
            {
                translationSpeed *= speedTweak * customSpeed * (reverse ? -1 : 1);
                translation += GetAxisInversion() * TimeWarp.fixedDeltaTime * translationSpeed *
                               electricChargeConstraintData.Ratio;
                TranslationChanged |= mask;
                
                if (motorSound != null) motorSound.Play();
            }
        }

        protected void CheckRotationLimits()
        {
            if (rotateLimits || limitTweakableFlag)
            {

                if (rotation < minTweak || rotation > maxTweak)
                {
                    rotation = Mathf.Clamp(rotation, minTweak, maxTweak);
                    if (rotateLimitsRevertOn && ((RotationChanged & 1) > 0))
                    {
                        reversedRotationOn = !reversedRotationOn;
                    }
                    if (rotateLimitsRevertKey && ((RotationChanged & 2) > 0))
                    {
                        reversedRotationKey = !reversedRotationKey;
                    }
                    if (rotateLimitsOff)
                    {
                        on = false;
                        UpdateState();
                    }
                }
            }
            else
            {
                if (rotation >= 180)
                {
                    rotation -= 360;
                    rotationDelta -= 360;
                }
                if (rotation < -180)
                {
                    rotation += 360;
                    rotationDelta += 360;
                }
            }
        }

        protected void CheckTranslationLimits()
        {
            if (translateLimits)
            {
                if (translation < minTweak || translation > maxTweak)
                {
                    translation = Mathf.Clamp(translation, minTweak, maxTweak);
                    if (translateLimitsRevertOn && ((TranslationChanged & 1) > 0))
                    {
                        reversedTranslationOn = !reversedTranslationOn;
                    }
                    if (translateLimitsRevertKey && ((TranslationChanged & 2) > 0))
                    {
                        reversedTranslationKey = !reversedTranslationKey;
                    }
                    if (translateLimitsOff)
                    {
                        on = false;
                        UpdateState();
                    }
                }
            }
        }

        //used with Interpolator only
        protected void UpdatePosition()
        {
            float pos = Interpolator.GetPosition();
            if (invertAxis)
            {
                pos = Interpolator.MinPosition + Interpolator.MaxPosition - pos;
            }
            if (rotateJoint)
            {
                if (rotation != pos) 
                {
                    rotation = pos;
                    RotationChanged |= 4;
                } 
                else
                    RotationChanged = 0;
            }
            else
            {
                if (translation != pos) 
                {
                    translation = pos;
                    TranslationChanged |= 4;
                } 
                else
                    TranslationChanged = 0;
            }
        }

        protected bool KeyPressed(string key)
        {
            return (key != "" && vessel == FlightGlobals.ActiveVessel
                    && InputLockManager.IsUnlocked(ControlTypes.LINEAR)
                    && Input.GetKey(key));
        }

        protected float HomeSpeed(float offset, float maxSpeed)
        {
            float seekSpeed = Math.Abs(offset)/TimeWarp.deltaTime;
            if (seekSpeed > maxSpeed)
            {
                seekSpeed = maxSpeed;
            }
            return -seekSpeed*Mathf.Sign(offset)*GetAxisInversion();
        }

        //old version
        /*
        protected void CheckInputs()
        {
            if (part.isConnected && KeyPressed(onKey))
            {
                on = !on;
                UpdateState();
            }

            if (on && (onRotateSpeed != 0))
            {
                UpdateRotation(+onRotateSpeed, reversedRotationOn, 1);
            }
            if (on && (onTranslateSpeed != 0))
            {
                UpdateTranslation(+onTranslateSpeed, reversedTranslationOn, 1);
            }

            if ((MoveFlags & 0x101) != 0 || KeyPressed(rotateKey))
            {
                UpdateRotation(+keyRotateSpeed, reversedRotationKey, 2);
            }
            if ((MoveFlags & 0x202) != 0 || KeyPressed(revRotateKey))
            {
                UpdateRotation(-keyRotateSpeed, reversedRotationKey, 2);
            }
            //FIXME Hmm, these moveFlag checks clash with rotation. Is rotation and translation in the same part not intended?
            if ((MoveFlags & 0x101) != 0 || KeyPressed(translateKey))
            {
                UpdateTranslation(+keyTranslateSpeed, reversedTranslationKey, 2);
            }
            if ((MoveFlags & 0x202) != 0 || KeyPressed(revTranslateKey))
            {
                UpdateTranslation(-keyTranslateSpeed, reversedTranslationKey, 2);
            }

            if (((MoveFlags & 0x404) != 0) && (RotationChanged == 0) && (TranslationChanged == 0))
            {
                float totalSpeed;
                totalSpeed = HomeSpeed(rotation, keyRotateSpeed);
                UpdateRotation(totalSpeed, false, 2);
                totalSpeed = HomeSpeed(translation, keyTranslateSpeed);
                UpdateTranslation(totalSpeed, false, 2);
            }

            if (MoveFlags == 0 && !on)
            {
                if (motorSound != null) motorSound.Stop();
            }
        }
        */
        protected void CheckInputs()
        {
            if (part.isConnected && KeyPressed(onKey))
            {
                on = !on;
                UpdateState();
            }

            if      ((MoveFlags & 0x101) != 0)         // move forward
                Interpolator.SetCommand (float.PositiveInfinity, speedTweak);
            else if ((MoveFlags & 0x202) != 0)         // move back
                Interpolator.SetCommand (float.NegativeInfinity, speedTweak);
            else if ((MoveFlags & 0x404) != 0)         // position to zero
                Interpolator.SetCommand (0f, speedTweak);
            else if (MoveFlags == 0)                   // stop
                Interpolator.SetCommand (0f, 0f);
        }

        protected void DoRotation()
        {
            
            if ((RotationChanged != 0) && (rotateJoint || RotateModelTransform != null))
            {
                if (rotateJoint && joint != null)
                {
                    joint.targetRotation =
                        Quaternion.AngleAxis(
                            (invertSymmetry ? ((IsSymmMaster() || (part.symmetryCounterparts.Count != 1)) ? 1 : -1) : 1)*
                            (rotation - rotationDelta), rotateAxis);
                    RotationLast = rotation;
                }
                else if (transform != null)
                {
                    Quaternion curRot =
                        Quaternion.AngleAxis(
                            (invertSymmetry ? ((IsSymmMaster() || (part.symmetryCounterparts.Count != 1)) ? 1 : -1) : 1)*
                            rotation, rotateAxis);
                    transform.FindChild("model").FindChild(rotateModel).localRotation = curRot;
                }
                electricChargeConstraintData.RotationDone = true;
            }
        }

        protected void DoTranslation()
        {
            if ((TranslationChanged != 0) && (translateJoint || TranslateModelTransform != null) && joint != null)
            {
                if (translateJoint)
                {
                    joint.targetPosition = -translateAxis*(translation - translationDelta);
                }
                else
                {
                    joint.targetPosition = OrigTranslation - translateAxis.normalized*(translation - translationDelta);
                }
                electricChargeConstraintData.TranslationDone = true;
            }
        }

        public void Resized()
        {
            UIPartActionWindow[] actionWindows = FindObjectsOfType<UIPartActionWindow>();
            if (actionWindows.Length > 0)
            {
                foreach (UIPartActionWindow actionWindow in actionWindows)
                {
                    if (actionWindow.part == part)
                    {
                        TweakWindow = actionWindow;
                        TweakIsDirty = true;
                    }
                }
            }
            else
            {
                TweakWindow = null;
            }
        }


        public void RefreshTweakUI()
        {
            if (HighLogic.LoadedScene != GameScenes.EDITOR) return;
            if (TweakWindow == null) return;

            if (translateJoint)
            {
                var rangeMinF = (UI_FloatEdit) Fields["minTweak"].uiControlEditor;
                rangeMinF.minValue = translateMin;
                rangeMinF.maxValue = translateMax;
                rangeMinF.incrementSlide = float.Parse(stepIncrement);
                minTweak = translateMin;
                var rangeMaxF = (UI_FloatEdit) Fields["maxTweak"].uiControlEditor;
                rangeMaxF.minValue = translateMin;
                rangeMaxF.maxValue = translateMax;
                rangeMaxF.incrementSlide = float.Parse(stepIncrement);
                maxTweak = translateMax;
                ElectricStateDisplay = string.Format("{0:#0.##} Ec/s est. Power Draw", electricChargeRequired);
                //this.updateGroupECRequirement(this.groupName);
            }
            else if (rotateJoint)
            {
                var rangeMinF = (UI_FloatEdit) Fields["minTweak"].uiControlEditor;
                rangeMinF.minValue = rotateMin;
                rangeMinF.maxValue = rotateMax;
                rangeMinF.incrementSlide = float.Parse(stepIncrement);
                minTweak = rotateMin;
                var rangeMaxF = (UI_FloatEdit) Fields["maxTweak"].uiControlEditor;
                rangeMaxF.minValue = rotateMin;
                rangeMaxF.maxValue = rotateMax;
                rangeMaxF.incrementSlide = float.Parse(stepIncrement);
                maxTweak = rotateMax;
                ElectricStateDisplay = string.Format("{0:#0.##} Ec/s est. Power Draw", electricChargeRequired);
            }

            if (part.symmetryCounterparts.Count > 1)
            {
                foreach (Part counterPart in part.symmetryCounterparts)
                {
                    ((MuMechToggle) counterPart.Modules["MuMechToggle"]).rotateMin = rotateMin;
                    ((MuMechToggle) counterPart.Modules["MuMechToggle"]).rotateMax = rotateMax;
                    ((MuMechToggle) counterPart.Modules["MuMechToggle"]).stepIncrement = stepIncrement;
                    ((MuMechToggle) counterPart.Modules["MuMechToggle"]).minTweak = rotateMin;
                    ((MuMechToggle) counterPart.Modules["MuMechToggle"]).maxTweak = maxTweak;
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
            if (motorSound!=null) motorSound.Update(soundSet, pitchSet);
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

            if (isMotionLock || part.State == PartStates.DEAD)
            {
                return;
            }

            if (SetupJoints())
            {
                RotationChanged = 4;
                TranslationChanged = 4;
            }

            electricChargeConstraintData = new ElectricChargeConstraintData(GetAvailableElectricCharge(),
                electricChargeRequired*TimeWarp.fixedDeltaTime, GroupElectricChargeRequired*TimeWarp.fixedDeltaTime);

            if (minTweak > maxTweak)
            {
                maxTweak = minTweak;
            }

            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {

                CheckInputs();

                if (UseElectricCharge && !electricChargeConstraintData.Available)
                    Interpolator.SetCommand(0f, 0f);


                Interpolator.Update(TimeWarp.fixedDeltaTime);
                UpdatePosition();

                if (Interpolator.Active && (Interpolator.CmdVelocity != 0))
                    motorSound.Play();
                else
                    motorSound.Stop();
            }

            //CheckRotationLimits();
            //CheckTranslationLimits();

            DoRotation();
            DoTranslation();

            if (UseElectricCharge)
            {
                if (electricChargeConstraintData.RotationDone || electricChargeConstraintData.TranslationDone)
                {
                    part.RequestResource(ELECTRIC_CHARGE_RESOURCE_NAME, electricChargeConstraintData.ToConsume);
                    float displayConsume = electricChargeConstraintData.ToConsume/TimeWarp.fixedDeltaTime;
                    if (electricChargeConstraintData.Available)
                    {
                        bool lowPower = Mathf.Abs(electricChargeRequired - displayConsume) >
                                        Mathf.Abs(electricChargeRequired*.001f);
                        ElectricStateDisplay = string.Format("{2}{0:#0.##}/{1:#0.##} Ec/s", displayConsume,
                            electricChargeRequired, lowPower ? "low power! - " : "active - ");
                        LastPowerDraw = displayConsume;
                    }
                    else
                    {
                        ElectricStateDisplay = "not enough power!";
                    }
                    LastPowerDraw = displayConsume;
                }
                else
                {
                    ElectricStateDisplay = string.Format("idle - {0:#0.##} Ec/s max.", electricChargeRequired);
                    LastPowerDraw = 0f;
                }
            }

            RotationChanged = 0;
            TranslationChanged = 0;

            if (vessel != null)
            {
                part.UpdateOrgPosAndRot(vessel.rootPart);
                foreach (Part child in part.FindChildParts<Part>(true))
                {
                    child.UpdateOrgPosAndRot(vessel.rootPart);
                }
            }
        }


        public override void OnInactive()
        {
            on = false;
            UpdateState();
        }

        public void SetLock(bool locked)
        {
            isMotionLock = locked;
            Events["Activate"].active = !isMotionLock;
            Events["Deactivate"].active = isMotionLock;
        }

        [KSPEvent(guiActive = true, guiName = "Engage Lock")]
        public void Activate()
        {
            SetLock(true);
        }

        [KSPEvent(guiActive = true, guiName = "Disengage Lock", active = false)]
        public void Deactivate()
        {
            SetLock(false);
        }

        [KSPAction("Engage Lock")]
        public void LockToggle(KSPActionParam param)
        {
            SetLock(!isMotionLock);
        }

        [KSPAction("Move +")]
        public void MovePlusAction(KSPActionParam param)
        {
            switch (param.type)
            {
                case KSPActionType.Activate:
                    MoveFlags |= 0x100;
                    break;
                case KSPActionType.Deactivate:
                    MoveFlags &= ~0x100;
                    break;
            }
        }

        [KSPAction("Move -")]
        public void MoveMinusAction(KSPActionParam param)
        {
            switch (param.type)
            {
                case KSPActionType.Activate:
                    MoveFlags |= 0x200;
                    break;
                case KSPActionType.Deactivate:
                    MoveFlags &= ~0x200;
                    break;
            }
        }

        [KSPAction("Move Center")]
        public void MoveCenterAction(KSPActionParam param)
        {
            switch (param.type)
            {
                case KSPActionType.Activate:
                    MoveFlags |= 0x400;
                    break;
                case KSPActionType.Deactivate:
                    MoveFlags &= ~0x400;
                    break;
            }
        }

        public void LoadConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<MuMechToggle>();
            config.load();
            UseElectricCharge = config.GetValue<bool>("useEC");
        }

        public void SaveConfigXml()
        {
            PluginConfiguration config = PluginConfiguration.CreateForType<ControlsGUI>();
            config.SetValue("useEC", UseElectricCharge);
            config.save();
        }

        private void PositionWindow(int windowID)
        {
            GUILayout.BeginVertical();
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label(servoName, GUILayout.ExpandWidth(true));

                float angle;
                Vector3 tempAxis;
                transform.rotation.ToAngleAxis(out angle, out tempAxis);

                GUILayout.BeginVertical();
                if (rotateJoint)
                {
                    GUILayout.Label("Rotation " + rotation);
                }
                else if (translateJoint)
                {
                    GUILayout.Label("Translation " + translation);
                }
                GUILayout.EndVertical();

                if (GUILayout.RepeatButton("←←", GUILayout.Width(40)))
                {
                    MoveLeft();
                }
                if (GUILayout.Button("←", GUILayout.Width(21)))
                {
                    MoveLeft();
                }
                if (GUILayout.Button("→", GUILayout.Width(21)))
                {
                    MoveRight();
                }

                if (GUILayout.RepeatButton("→→", GUILayout.Width(40)))
                {
                    MoveRight();
                }
                translationDelta = translation;
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
                GUILayout.EndHorizontal();
            }


            if (GUILayout.Button("Close"))
            {
                positionGUIEnabled = false;
            }
            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        public void MoveRight()
        {
            if (rotateJoint)
            {
                if ((rotationEuler != rotateMin && rotationEuler > minTweak) ||
                    rotationEuler != rotateMax && rotationEuler < maxTweak)
                {
                    if (part.name.Contains("IR.Rotatron.OffAxis"))
                    {
                        rotationEuler = rotationEuler - (1*GetAxisInversion());
                        rotation = rotationEuler/0.7070f;
                    }
                    else
                    {
                        rotationEuler = rotationEuler - (1*GetAxisInversion());
                        rotation = Mathf.Clamp(rotationEuler, minTweak, maxTweak);
                    }
                }
                if (rotateLimits || limitTweakableFlag)
                {
                    if (!part.name.Contains("IR.Rotatron.OffAxis"))
                    {
                        if ((rotationEuler > rotateMin && rotationEuler > minTweak) &&
                            (rotationEuler < rotateMax && rotationEuler < maxTweak) || (rotationEuler == 0))
                        {
                            FixedMeshTransform.Rotate(rotateAxis*GetAxisInversion(), Space.Self);
                            transform.Rotate(-rotateAxis*GetAxisInversion(), Space.Self);
                        }
                    }
                    else
                    {
                        transform.Rotate(-rotateAxis*GetAxisInversion(), Space.Self);
                        FixedMeshTransform.Rotate(rotateAxis*GetAxisInversion(), Space.Self);
                    }
                    if (rotationEuler < minTweak || rotationEuler > maxTweak)
                    {
                        rotationEuler = Mathf.Clamp(rotationEuler, minTweak, maxTweak);
                    }
                }
                else
                {
                    if (!part.name.Contains("IR.Rotatron.OffAxis"))
                    {
                        FixedMeshTransform.Rotate(rotateAxis*GetAxisInversion(), Space.Self);
                        transform.Rotate(-rotateAxis*GetAxisInversion(), Space.Self);
                    }
                    else
                    {
                        transform.Rotate(-rotateAxis*GetAxisInversion(), Space.Self);
                        FixedMeshTransform.Rotate(rotateAxis*GetAxisInversion(), Space.Self);
                    }
                }
            }

            if (translateJoint)
            {
                TranslatePositive();
            }
        }

        public void MoveLeft()
        {
            if (rotateJoint)
            {
                if ((rotationEuler != rotateMax && rotationEuler < maxTweak) ||
                    (rotationEuler != rotateMin && rotationEuler > minTweak))
                {
                    if (part.name.Contains("IR.Rotatron.OffAxis"))
                    {
                        rotationEuler = rotationEuler + (1*GetAxisInversion());
                        rotation = rotationEuler/0.7070f;
                    }
                    else
                    {
                        rotationEuler = rotationEuler + (1*GetAxisInversion());
                        rotation = Mathf.Clamp(rotationEuler, minTweak, maxTweak);
                    }
                }
                if (rotateLimits || limitTweakableFlag)
                {
                    if (!part.name.Contains("IR.Rotatron.OffAxis"))
                    {
                        if ((rotationEuler < rotateMax && rotationEuler < maxTweak) &&
                            (rotationEuler > rotateMin && rotationEuler > minTweak) || (rotationEuler == 0))
                        {
                            FixedMeshTransform.Rotate(-rotateAxis*GetAxisInversion(), Space.Self);
                            transform.Rotate(rotateAxis*GetAxisInversion(), Space.Self);
                        }
                    }
                    else
                    {
                        transform.Rotate(rotateAxis*GetAxisInversion(), Space.Self);
                        FixedMeshTransform.Rotate(-rotateAxis*GetAxisInversion(), Space.Self);
                    }
                    if (rotationEuler < minTweak || rotationEuler > maxTweak)
                    {
                        rotationEuler = Mathf.Clamp(rotationEuler, minTweak, maxTweak);
                    }
                }
                else
                {
                    if (!part.name.Contains("IR.Rotatron.OffAxis"))
                    {
                        FixedMeshTransform.Rotate(-rotateAxis*GetAxisInversion(), Space.Self);
                        transform.Rotate(rotateAxis*GetAxisInversion(), Space.Self);
                    }
                    else
                    {
                        transform.Rotate(rotateAxis*GetAxisInversion(), Space.Self);
                        FixedMeshTransform.Rotate(-rotateAxis*GetAxisInversion(), Space.Self);
                    }
                }
            }

            if (translateJoint)
            {
                TranslateNegative();
            }
        }
        //resets servo to 0 rotation/translation
        //very early version do not use for now
        public void MoveCenter()
        {
            //need this method to reset servo to default position as per part.cfg
        }

        private void TranslateNegative()
        {
            float isGantry;

            if (part.name.Contains("Gantry"))
                isGantry = -1f;
            else
                isGantry = 1f;
            if ((translation < translateMax && translation < maxTweak) &&
                (translation > translateMin && translation > minTweak))
            {
                transform.Translate((-translateAxis.x*isGantry*SPEED*Time.deltaTime*GetAxisInversion()),
                    (-translateAxis.y*isGantry*SPEED*Time.deltaTime*GetAxisInversion()),
                    (-translateAxis.z*isGantry*SPEED*Time.deltaTime*GetAxisInversion()), Space.Self);
                //this.transform.Find("model/" + findFixedMesh()).Translate((translateAxis.x * isGantry * speed * Time.deltaTime * getAxisInversion()),
                //                                                    (translateAxis.y * isGantry * speed * Time.deltaTime * getAxisInversion()),
                //                                                    (translateAxis.z * isGantry * speed * Time.deltaTime * getAxisInversion()), Space.Self);
                FixedMeshTransform.Translate((translateAxis.x*isGantry*SPEED*Time.deltaTime*GetAxisInversion()),
                    (translateAxis.y*isGantry*SPEED*Time.deltaTime*GetAxisInversion()),
                    (translateAxis.z*isGantry*SPEED*Time.deltaTime*GetAxisInversion()), Space.Self);
            }
            if ((translation != translateMax && translation < maxTweak) ||
                (translation != translateMin && translation > minTweak))
            {
                translation = translation + SPEED*Time.deltaTime*GetAxisInversion();
            }

            if (translation < minTweak || translation > maxTweak)
                translation = Mathf.Clamp(translation, minTweak, maxTweak);
        }

        private void TranslatePositive()
        {
            float isGantry;

            if (part.name.Contains("Gantry"))
                isGantry = -1f;
            else
                isGantry = 1f;

            if ((translation > translateMin && translation > minTweak) &&
                (translation < translateMax && translation < maxTweak))
            {
                transform.Translate((translateAxis.x*isGantry*SPEED*Time.deltaTime*GetAxisInversion()),
                    (translateAxis.y*isGantry*SPEED*Time.deltaTime*GetAxisInversion()),
                    (translateAxis.z*isGantry*SPEED*Time.deltaTime*GetAxisInversion()), Space.Self);
                //this.transform.Find("model/" + findFixedMesh()).Translate((-translateAxis.x * isGantry * speed * Time.deltaTime * getAxisInversion()),
                //                                                    (-translateAxis.y * isGantry * speed * Time.deltaTime * getAxisInversion()),
                //                                                    (-translateAxis.z * isGantry * speed * Time.deltaTime * getAxisInversion()), Space.Self);
                FixedMeshTransform.Translate((-translateAxis.x*isGantry*SPEED*Time.deltaTime*GetAxisInversion()),
                    (-translateAxis.y*isGantry*SPEED*Time.deltaTime*GetAxisInversion()),
                    (-translateAxis.z*isGantry*SPEED*Time.deltaTime*GetAxisInversion()), Space.Self);
            }
            if ((translation != translateMin && translation > minTweak) ||
                translation != translateMax && translation < maxTweak)
            {
                translation = translation - SPEED*Time.deltaTime*GetAxisInversion();
            }

            if (translation < minTweak || translation > maxTweak)
                translation = Mathf.Clamp(translation, minTweak, maxTweak);
        }

        private void OnGUI()
        {
            if (InputLockManager.IsLocked(ControlTypes.LINEAR))
                return;
            if (ControlWinPos2.x == 0 && ControlWinPos2.y == 0)
            {
                //controlWinPos = new Rect(Screen.width - 510, 70, 10, 10);
                ControlWinPos2 = new Rect(260, 66, 10, 10);
            }
            if (ResetWin)
            {
                ControlWinPos2 = new Rect(ControlWinPos2.x, ControlWinPos2.y,
                    10, 10);
                ResetWin = false;
            }
            GUI.skin = DefaultSkinProvider.DefaultSkin;
            GameScenes scene = HighLogic.LoadedScene;

            //Call the DragAndDrop GUI Setup stuff
            if (scene == GameScenes.EDITOR)
            {
                //var height = GUILayout.Height(Screen.height / 2f);
                if (positionGUIEnabled)
                    ControlWinPos2 = GUILayout.Window(960, ControlWinPos2,
                        PositionWindow,
                        "Position Editor",
                        GUILayout.Width(300),
                        GUILayout.Height(80));

                //PositionLock(positionGUIEnabled && controlWinPos2.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)));
            }
        }

        internal void PositionLock(Boolean apply)
        {
            //only do this lock in the editor - no point elsewhere
            if (HighLogic.LoadedSceneIsEditor && apply)
            {
                //only add a new lock if there isnt already one there
                if (InputLockManager.GetControlLock("PositionEditor") != ControlTypes.EDITOR_LOCK)
                {
#if DEBUG
                    Debug.Log(String.Format("[IR GUI] AddingLock-{0}", "PositionEditor"));
#endif
                    InputLockManager.SetControlLock(ControlTypes.EDITOR_LOCK, "PositionEditor");
                }
            }
                //Otherwise make sure the lock is removed
            else
            {
                //Only try and remove it if there was one there in the first place
                if (InputLockManager.GetControlLock("PositionEditor") == ControlTypes.EDITOR_LOCK)
                {
#if DEBUG
                    Debug.Log(String.Format("[IR GUI] Removing-{0}", "PositionEditor"));
#endif
                    InputLockManager.RemoveControlLock("PositionEditor");
                }
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
                RotationDone = false;
                TranslationDone = false;
            }

            public float Ratio { get; set; }
            public float ToConsume { get; set; }
            public bool Available { get; set; }
            public bool RotationDone { get; set; }
            public bool TranslationDone { get; set; }
            public bool Enough { get; set; }
        }
    }
}