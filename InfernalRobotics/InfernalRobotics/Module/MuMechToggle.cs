using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InfernalRobotics.Command;
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

        private static Material debugMaterial;
        private static int globalCreationOrder;
        private ElectricChargeConstraintData electricChargeConstraintData;
        private ConfigurableJoint joint;

        [KSPField(isPersistant = true)] public float customSpeed = 1;
        [KSPField(isPersistant = true)] public Vector3 fixedMeshOriginalLocation;

        [KSPField(isPersistant = true)]
        public string forwardKey
        {
            get { return forwardKeyStore; }
            set
            {
                forwardKeyStore = value.ToLower();
                rotateKey = translateKey = forwardKey;
            }
        }

        [KSPField(isPersistant = true)] public bool freeMoving = false;
        [KSPField(isPersistant = true)] public string groupName = "";
        [KSPField(isPersistant = true)] public bool hasModel = false;
        [KSPField(isPersistant = true)] public bool invertAxis = false;
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
        public string revRotateKey
        {
            get { return reverseRotateKeyStore; }
            set { reverseRotateKeyStore = value.ToLower(); }
        }

        [KSPField(isPersistant = true)]
        public string reverseKey
        {
            get { return reverseKeyStore; }
            set
            {
                reverseKeyStore = value.ToLower();
                revRotateKey = revTranslateKey = reverseKey;
            }
        }

        [KSPField(isPersistant = true)] public string rotateKey = "";
        [KSPField(isPersistant = true)] public bool rotateLimits = false;
        [KSPField(isPersistant = true)] public float rotateMax = 360;
        [KSPField(isPersistant = true)] public float rotateMin = 0;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Rotation:")] public float rotation = 0;
        [KSPField(isPersistant = false, guiActive = false, guiActiveEditor = false)] public float rotationDelta = 0;
        [KSPField(isPersistant = true)] public string servoName = "";

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Speed", guiFormat = "0.00"), 
         UI_FloatEdit(minValue = 0f, incrementSlide = 0.05f, incrementSmall=0.5f, incrementLarge=1f)]
        public float speedTweak = 1f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Accel", guiFormat = "0.00"), 
         UI_FloatEdit(minValue = 0.05f, incrementSlide = 0.05f, incrementSmall=0.5f, incrementLarge=1f)]
        public float accelTweak = 4f;

        [KSPField(isPersistant = true)] public float translateMax = 3;
        [KSPField(isPersistant = true)] public float translateMin = 0;
        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Translation")] 
        public float translation = 0f;
        [KSPField(isPersistant = true)] public float translationDelta = 0;

        [KSPField(isPersistant = true)]
        public string presetPositionsSerialized = "";

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
        [KSPField(isPersistant = false)] public string translateModel = "on";

        private SoundSource motorSound;
        private string reverseKeyStore;
        private string reverseRotateKeyStore;
        private string forwardKeyStore;

        public MuMechToggle()
        {
            Interpolator = new Interpolator();
            Translator = new Translator();
            GroupElectricChargeRequired = 2.5f;
            OriginalTranslation = 0f;
            OriginalAngle = 0f;
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
        public float OriginalAngle { get; set; }
        public float OriginalTranslation { get; set; }

        public List<float> PresetPositions { get; set; }

        public float Position { get { return rotateJoint ? rotation : translation; } }

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

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Rotate Limits are Off", active = false)]
        public void LimitTweakableToggle()
        {
            if (!rotateJoint)
                return;
            limitTweakableFlag = !limitTweakableFlag;
            Events["LimitTweakableToggle"].guiName = limitTweakableFlag ? "Rotate Limits are On" : "Rotate Limits are Off";
            if (!limitTweakableFlag)
            {
                //we need to convert part's minTweak and maxTweak to [-180,180] range to get consistent behaviour with Interpolator
                Fields["minTweak"].guiActive = false;
                Fields["minTweak"].guiActiveEditor = false;
                Fields["maxTweak"].guiActive = false;
                Fields["maxTweak"].guiActiveEditor = false;

                minTweak = -180f;
                maxTweak = 180f;
            }
            else
            {
                //revert back to full range as in part.cfg
                if (!freeMoving)
                {
                    Fields["minTweak"].guiActive = true;
                    Fields["minTweak"].guiActiveEditor = true;
                    Fields["maxTweak"].guiActive = true;
                    Fields["maxTweak"].guiActiveEditor = true;
                }
                minTweak = rotateMin;
                maxTweak = rotateMax;
            }
            SetupMinMaxTweaks ();
            TweakIsDirty = true;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Invert Axis is Off")]
        public void InvertAxisToggle()
        {
            invertAxis = !invertAxis;
            Translator.IsAxisInverted = invertAxis;
            Events["InvertAxisToggle"].guiName = invertAxis ? "Invert Axis is On" : "Invert Axis is Off";
            TweakIsDirty = true;
        }

        public bool IsSymmMaster()
        {
            return part.symmetryCounterparts.All( cp => ((MuMechToggle) cp.Modules["MuMechToggle"]).CreationOrder >= CreationOrder);
        }

        public float GetStepIncrement()
        {
            return rotateJoint ? 1f : 0.05f;
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
            Logger.Log("[OnAwake] Start", Logger.Level.Debug);

            LoadConfigXml();

            FindTransforms();

            if (ModelTransform == null)
                Logger.Log("[OnAwake] ModelTransform is null", Logger.Level.Warning);

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
            
            GameScenes scene = HighLogic.LoadedScene;
            if (scene == GameScenes.EDITOR)
            {
                SetupMinMaxTweaks();
            }

            ParsePresetPositions();

            FixedMeshTransform = KSPUtil.FindInPartModel(transform, fixedMesh);

            Logger.Log(string.Format("[OnAwake] End, rotateLimits={0}, minTweak={1}, maxTweak={2}, rotateJoint={0}", rotateLimits, minTweak, maxTweak), Logger.Level.Debug);
        }

        public Transform FindFixedMesh(Transform meshTransform)
        {
            Transform t = part.transform.FindChild("model").FindChild(fixedMesh);

            return t;
        }


        public override void OnSave(ConfigNode node)
        {
            Logger.Log("[OnSave] Start", Logger.Level.Debug);
            base.OnSave(node);
            SetupMinMaxTweaks();

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
                        FixedMeshTransform.Rotate(rotateAxis, -rotation);
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
            
            SetupMinMaxTweaks();

            ParsePresetPositions();

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
            UpdateMinMaxTweaks();
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

        private void OnEditorAttach()
        {
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
            limitTweakableFlag = rotateLimits;
            if (!float.IsNaN(Position))
                Interpolator.Position = Position;

            Translator.Init(Interpolator, invertAxis, isMotionLock, this);

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
            SetupMinMaxTweaks();
            if (limitTweakable)
            {
                Events["LimitTweakableToggle"].active = rotateJoint;
            }

            ParsePresetPositions();

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
            Logger.Log("configureInterpolator:" + Interpolator, Logger.Level.Debug);
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
            if (part.isConnected && KeyPressed(onKey))
            {
                on = !on;
                UpdateState();
            }

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
            else if (transform != null)
            {
                Quaternion curRot =
                    Quaternion.AngleAxis(
                        (invertSymmetry ? ((IsSymmMaster() || (part.symmetryCounterparts.Count != 1)) ? 1 : -1) : 1)*
                        rotation, rotateAxis);
                transform.FindChild("model").FindChild(rotateModel).localRotation = curRot;
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

        public void Resized(float factor)
        {
            if (rotateJoint)
                return;

            // TODO translate limits should be treated here
            // => enable here when we remove them from the tweakScale configs (BREAKING CHANGE)
            //translateMin *= factor;
            //translateMax *= factor;

            translation  *= factor;
            minTweak *= factor;
            maxTweak *= factor;

            // TweakScale considers the origin of the moving mesh as the part center
            // so if translation!=0, the fixed mesh moves.
            // Not sure what we'd have to do to repair that.

            // update the window so the new limits are applied
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

            UpdateMinMaxTweaks();

            if (part.symmetryCounterparts.Count > 1)
            {
                foreach (Part counterPart in part.symmetryCounterparts)
                {
                    ((MuMechToggle) counterPart.Modules["MuMechToggle"]).rotateMin = rotateMin;
                    ((MuMechToggle) counterPart.Modules["MuMechToggle"]).rotateMax = rotateMax;
                    ((MuMechToggle) counterPart.Modules["MuMechToggle"]).minTweak = minTweak;
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
            if (motorSound != null)
            {
                var basePitch = pitchSet;
                var servoBaseSpeed = Translator.GetSpeedUnit();

                if (servoBaseSpeed == 0.0f) servoBaseSpeed = 1;

                float speedPitch = basePitch * Math.Max(Math.Abs(Interpolator.Velocity/servoBaseSpeed), 0.05f);

                motorSound.Update(soundSet, speedPitch);
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                CheckInputs();
            }
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

            if (part.State == PartStates.DEAD) // not sure what this means
            {                                  // probably: the part is destroyed but the object still exists?
                return;
            }

            SetupJoints();

            if (HighLogic.LoadedSceneIsFlight)
            {
                electricChargeConstraintData = new ElectricChargeConstraintData(GetAvailableElectricCharge(),
                    electricChargeRequired*TimeWarp.fixedDeltaTime, GroupElectricChargeRequired*TimeWarp.fixedDeltaTime);

                //moved to Update due to Unity's way to handle KeyPresses
                //CheckInputs();

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

        public void MoveNextPreset()
        {
            float currentPosition = Interpolator.Position;
            float nextPosition = currentPosition;

            var availablePositions = PresetPositions.FindAll (s => s > currentPosition);

            if (availablePositions.Count > 0)
                nextPosition = availablePositions.Min();
            else if (!limitTweakableFlag)
            {
                //part is unrestricted, we can choose first preset
                nextPosition = PresetPositions.Min() + 360;
            }
            
            Logger.Log ("[Action] NextPreset, currentPos = " + currentPosition + ", nextPosition=" + nextPosition, Logger.Level.Debug);

            Translator.Move(nextPosition, customSpeed * speedTweak);
        }

        public void MovePrevPreset()
        {
            float currentPosition = Interpolator.Position;
            float nextPosition = currentPosition;

            var availablePositions = PresetPositions.FindAll (s => s < currentPosition);

            if (availablePositions.Count > 0)
                nextPosition = availablePositions.Max();
            else if (!limitTweakableFlag)
            {
                //part is unrestricted, we can choose last preset
                nextPosition = PresetPositions.Max()-360;
            }

            Logger.Log ("[Action] PrevPreset, currentPos = " + currentPosition + ", nextPosition=" + nextPosition, Logger.Level.Debug);

            Translator.Move(nextPosition, customSpeed * speedTweak);
        }

        [KSPAction("Move To Next Preset")]
        public void MoveNextPresetAction(KSPActionParam param)
        {
            switch (param.type)
            {
                case KSPActionType.Activate:
                    MoveNextPreset ();
                    break;
                case KSPActionType.Deactivate:
                    Translator.Stop();
                    break;
            }
        }

        [KSPAction("Move To Previous Preset")]
        public void MovePrevPresetAction(KSPActionParam param)
        {
            switch (param.type)
            {
                case KSPActionType.Activate:
                    MovePrevPreset ();
                    break;
                case KSPActionType.Deactivate:
                    Translator.Stop();
                    break;
            }
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
                    Translator.Move(0f, customSpeed * speedTweak);
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
                FixedMeshTransform.Rotate(-rotateAxis*deltaPos, Space.Self);
                transform.Rotate(rotateAxis*deltaPos, Space.Self);
            }
            else
            {
                translation += deltaPos;
                float gantryCorrection = part.name.Contains("Gantry") ? -1f : 1f;
                transform.Translate(-translateAxis * gantryCorrection*deltaPos);
                FixedMeshTransform.Translate(translateAxis * gantryCorrection*deltaPos);
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
        //very early version do not use for now
        public void MoveCenter()
        {
            //no ideas yet on how to do it
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