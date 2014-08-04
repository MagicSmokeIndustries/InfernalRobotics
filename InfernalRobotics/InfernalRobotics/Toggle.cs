using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using System.Linq;
using KSPAPIExtensions;
using System.Reflection;

namespace MuMech
{
    //18.3
    public class MuMechToggle : PartModule
    {
        AppDomain currentDomain = AppDomain.CurrentDomain;
        

    private Assembly MyResolveEventHandler(object sender, ResolveEventArgs args)
    {
        //This handler is called only when the common language runtime tries to bind to the assembly and fails.

        //Retrieve the list of referenced assemblies in an array of AssemblyName.
        Assembly MyAssembly, objExecutingAssembly;
        string strTempAssmbPath = "";

        objExecutingAssembly = Assembly.GetExecutingAssembly();
        AssemblyName[] arrReferencedAssmbNames = objExecutingAssembly.GetReferencedAssemblies();

        //Loop through the array of referenced assembly names.
        foreach(AssemblyName strAssmbName in arrReferencedAssmbNames)
        {
            //Check for the assembly names that have raised the "AssemblyResolve" event.
            if(strAssmbName.FullName.Substring(0, strAssmbName.FullName.IndexOf(",")) == args.Name.Substring(0, args.Name.IndexOf(",")))
            {
                //Build the path of the assembly from where it has to be loaded.        
                Debug.Log("looking!");
                strTempAssmbPath = "C:\\Myassemblies\\" + args.Name.Substring(0,args.Name.IndexOf(","))+".dll";
                break;
            }

        }

        //Load the assembly from the specified path.                    
        MyAssembly = Assembly.LoadFrom(strTempAssmbPath);                   

        //Return the loaded assembly.
        return MyAssembly;          
    }

        [KSPField(isPersistant = false)]
        public bool toggle_drag = false;
        [KSPField(isPersistant = false)]
        public bool toggle_break = false;
        [KSPField(isPersistant = false)]
        public bool toggle_model = false;
        [KSPField(isPersistant = false)]
        public bool toggle_collision = false;
        [KSPField(isPersistant = false)]
        public float on_angularDrag = 2.0F;
        [KSPField(isPersistant = false)]
        public float on_maximum_drag = 0.2F;
        [KSPField(isPersistant = false)]
        public float on_minimum_drag = 0.2F;
        [KSPField(isPersistant = false)]
        public float on_crashTolerance = 9.0F;
        [KSPField(isPersistant = false)]
        public float on_breakingForce = 22.0F;
        [KSPField(isPersistant = false)]
        public float on_breakingTorque = 22.0F;
        [KSPField(isPersistant = false)]
        public float off_angularDrag = 2.0F;
        [KSPField(isPersistant = false)]
        public float off_maximum_drag = 0.2F;
        [KSPField(isPersistant = false)]
        public float off_minimum_drag = 0.2F;
        [KSPField(isPersistant = false)]
        public float off_crashTolerance = 9.0F;
        [KSPField(isPersistant = false)]
        public float off_breakingForce = 22.0F;
        [KSPField(isPersistant = false)]
        public float off_breakingTorque = 22.0F;
        [KSPField(isPersistant = false)]
        public string on_model = "on";
        [KSPField(isPersistant = false)]
        public string off_model = "off";

        [KSPField(isPersistant = true)]
        public string servoName = "";
        [KSPField(isPersistant = true)]
        public string groupName = "";
        [KSPField(isPersistant = true)]
        public string forwardKey = "";
        [KSPField(isPersistant = true)]
        public string reverseKey = "";

        [KSPField(isPersistant = false)]
        public string onKey = "p";
        [KSPField(isPersistant = false)]
        public bool onActivate = true;
        [KSPField(isPersistant = true)]
        public bool on = false;

        [KSPField(isPersistant = true)]
        public bool isMotionLock;
        [KSPField(isPersistant = true)]
        public float customSpeed = 1;

        [KSPField(isPersistant = false)]
        public string rotate_model = "on";
        [KSPField(isPersistant = false)]
        public Vector3 rotateAxis = Vector3.forward;
        [KSPField(isPersistant = false)]
        public Vector3 rotatePivot = Vector3.zero;
        [KSPField(isPersistant = false)]
        public float onRotateSpeed = 0;
        [KSPField(isPersistant = false)]
        public float keyRotateSpeed = 0;
        [KSPField(isPersistant = true)]
        public string rotateKey = "";
        [KSPField(isPersistant = true)]
        public string revRotateKey = "";
        [KSPField(isPersistant = false)]
        public bool rotateJoint = false;
        [KSPField(isPersistant = true)]
        public bool rotateLimits = false;
        [KSPField(isPersistant = true)]
        public float rotateMin = 0;
        [KSPField(isPersistant = true)]
        public float rotateMax = 360;
        [KSPField(isPersistant = false)]
        public bool rotateLimitsRevertOn = true;
        [KSPField(isPersistant = false)]
        public bool rotateLimitsRevertKey = false;
        [KSPField(isPersistant = false)]
        public bool rotateLimitsOff = false;
        public float rotationLast = 0;
        [KSPField(isPersistant = true)]
        public bool reversedRotationOn = false;
        [KSPField(isPersistant = true)]
        public bool reversedRotationKey = false;
        [KSPField(isPersistant = true)]
        public float rotationDelta = 0;
        [KSPField(isPersistant = true)]
        public float rotation = 0;

        [KSPField(isPersistant = false)]
        public string bottomNode = "bottom";
        [KSPField(isPersistant = false)]
        public string fixedMesh = "";
        [KSPField(isPersistant = false)]
        public float jointSpring = 0;
        [KSPField(isPersistant = false)]
        public float jointDamping = 0;
        [KSPField(isPersistant = false)]
        public bool invertSymmetry = true;
        [KSPField(isPersistant = false)]
        public float friction = 0.5F;

        [KSPField(isPersistant = false)]
        public string translate_model = "on";
        [KSPField(isPersistant = false)]
        public Vector3 translateAxis = Vector3.forward;
        [KSPField(isPersistant = false)]
        public float onTranslateSpeed = 0;
        [KSPField(isPersistant = false)]
        public float keyTranslateSpeed = 0;
        [KSPField(isPersistant = false)]
        public string translateKey = "";
        [KSPField(isPersistant = false)]
        public string revTranslateKey = "";
        [KSPField(isPersistant = false)]
        public bool translateJoint = false;
        [KSPField(isPersistant = true)]
        public bool translateLimits = false;
        [KSPField(isPersistant = true)]
        public float translateMin = 0;
        [KSPField(isPersistant = true)]
        public float translateMax = 3;
        [KSPField(isPersistant = false)]
        public bool translateLimitsRevertOn = true;
        [KSPField(isPersistant = false)]
        public bool translateLimitsRevertKey = false;
        [KSPField(isPersistant = false)]
        public bool translateLimitsOff = false;
        [KSPField(isPersistant = true)]
        public bool reversedTranslationOn = false;
        [KSPField(isPersistant = true)]
        public bool reversedTranslationKey = false;
        [KSPField(isPersistant = true)]
        public float translationDelta = 0;
        [KSPField(isPersistant = true)]
        public float translation = 0;
        [KSPField(isPersistant = false)]
        public bool showGUI = false;
        [KSPField(isPersistant = true)]
        public bool freeMoving = false;

        [KSPField(isPersistant = true)]
        public string minRange = "";
        [KSPField(isPersistant = true)]
        public string maxRange = "";

        [KSPField(isPersistant = false)]
        public bool debugColliders = false;

        [KSPField(isPersistant = false)]
        public string motorSndPath = "";
        public FXGroup fxSndMotor;
        public bool isPlaying = false;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Min Range", guiFormat = "F2", guiUnits = ""),
        UI_FloatEdit(minValue = -360f, maxValue = 360f, incrementSlide = 0.01f, scene = UI_Scene.All)]
        public float minTweak = 0;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Max Range", guiFormat = "F2", guiUnits = ""),
        UI_FloatEdit(minValue = -360f, maxValue = 360f, incrementSlide = 0.01f, scene = UI_Scene.All)]
        public float maxTweak = 360;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Step Increment"),
        UI_ChooseOption(options = new string[] { "0.01", "0.1", "1.0" })]
        public string stepIncrement = "0.1";

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Coarse Speed"), UI_FloatRange(minValue = .1f, maxValue = 5f, stepIncrement = 0.1f)]
        public float speedTweak = 1;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "Fine Speed"), UI_FloatRange(minValue = -0.1f, maxValue = 0.1f, stepIncrement = 0.01f)]
        public float speedTweakFine = 0;

        [KSPField(isPersistant = true)]
        public bool limitTweakable = false;
        [KSPField(isPersistant = true)]
        public bool limitTweakableFlag = false;
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Rotate Limits Off", active = false)]
        public void limitTweakableToggle()
        {
            limitTweakableFlag = !limitTweakableFlag;
            if (limitTweakableFlag)
            {
                this.Events["limitTweakableToggle"].guiName = "Rotate Limits On";
            }
            else
            {

                this.Events["limitTweakableToggle"].guiName = "Rotate Limits Off";
            }
        }

        [KSPField(isPersistant = true)]
        public bool invertAxis = false;
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Invert Axis Off")]
        public void InvertAxisOff()
        {
            invertAxis = !invertAxis;
            this.Events["InvertAxisOn"].active = true;
            this.Events["InvertAxisOff"].active = false;
        }

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Invert Axis On", active = false)]
        public void InvertAxisOn()
        {
            invertAxis = !invertAxis;
            this.Events["InvertAxisOn"].active = false;
            this.Events["InvertAxisOff"].active = true;
        }

        protected Vector3 origTranslation;
        protected bool gotOrig = false;

        protected List<Transform> mobileColliders = new List<Transform>();
        protected int rotationChanged = 0;
        protected int translationChanged = 0;

        static Material debug_material;

        protected Transform model_transform;
        protected Transform on_model_transform;
        protected Transform off_model_transform;
        protected Transform rotate_model_transform;
        protected Transform translate_model_transform;

        protected bool loaded;

        public int moveFlags = 0;

        private static int s_creationOrder = 0;
        public int creationOrder = 0;

        public bool isSymmMaster()
        {
            for (int i = 0; i < part.symmetryCounterparts.Count; i++)
            {
                if (((MuMechToggle)part.symmetryCounterparts[i].Modules["MuMechToggle"]).creationOrder < creationOrder)
                {
                    return false;
                }
            }
            return true;
        }

        //credit for sound support goes to the creators of the Kerbal Attachment
        //System
        public static bool createFXSound(Part part, FXGroup group, string sndPath,
                                         bool loop, float maxDistance)
        {
            maxDistance = 10f;
            if (sndPath == "")
            {
                group.audio = null;
                return false;
            }
            Debug.Log("Loading sounds : " + sndPath);
            if (!GameDatabase.Instance.ExistsAudioClip(sndPath))
            {
                Debug.Log("Sound not found in the game database!");
                //ScreenMessages.PostScreenMessage("Sound file : " + sndPath + " as not been found, please check your Infernal Robotics installation!", 10, ScreenMessageStyle.UPPER_CENTER);
                group.audio = null;
                return false;
            }
            group.audio = part.gameObject.AddComponent<AudioSource>();
            group.audio.volume = GameSettings.SHIP_VOLUME;
            group.audio.rolloffMode = AudioRolloffMode.Logarithmic;
            group.audio.dopplerLevel = 0f;
            group.audio.panLevel = 1f;
            group.audio.maxDistance = maxDistance;
            group.audio.loop = loop;
            group.audio.playOnAwake = false;
            group.audio.clip = GameDatabase.Instance.GetAudioClip(sndPath);
            Debug.Log("Sound successfully loaded.");
            return true;
        }

        private void playAudio()
        {
            if (!isPlaying && fxSndMotor.audio)
            {
                fxSndMotor.audio.Play();
                isPlaying = true;
            }
        }

        private T configValue<T>(string name, T defaultValue)
        {
            try
            {
                return (T)Convert.ChangeType(name, typeof(T));
            }
            catch (InvalidCastException)
            {
                print("Failed to convert string value \"" + name + "\" to type " + typeof(T).Name);
                return defaultValue;
            }
        }



        public void updateState()
        {
            if (on)
            {
                if (toggle_model)
                {
                    on_model_transform.renderer.enabled = true;
                    off_model_transform.renderer.enabled = false;
                }
                if (toggle_drag)
                {
                    part.angularDrag = on_angularDrag;
                    part.minimum_drag = on_minimum_drag;
                    part.maximum_drag = on_maximum_drag;
                }
                if (toggle_break)
                {
                    part.crashTolerance = on_crashTolerance;
                    part.breakingForce = on_breakingForce;
                    part.breakingTorque = on_breakingTorque;
                }
            }
            else
            {
                if (toggle_model)
                {
                    on_model_transform.renderer.enabled = false;
                    off_model_transform.renderer.enabled = true;
                }
                if (toggle_drag)
                {
                    part.angularDrag = off_angularDrag;
                    part.minimum_drag = off_minimum_drag;
                    part.maximum_drag = off_maximum_drag;
                }
                if (toggle_break)
                {
                    part.crashTolerance = off_crashTolerance;
                    part.breakingForce = off_breakingForce;
                    part.breakingTorque = off_breakingTorque;
                }
            }
            if (toggle_collision)
            {
                part.collider.enabled = on;
                part.collisionEnhancer.enabled = on;
                part.terrainCollider.enabled = on;
            }
        }

        protected void colliderizeChilds(Transform obj)
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
                    Mesh sharedMesh = UnityEngine.Object.Instantiate(obj.GetComponent<MeshFilter>().mesh) as Mesh;
                    UnityEngine.Object.Destroy(obj.GetComponent<MeshFilter>());
                    UnityEngine.Object.Destroy(obj.GetComponent<MeshRenderer>());
                    MeshCollider meshCollider = obj.gameObject.AddComponent<MeshCollider>();
                    meshCollider.sharedMesh = sharedMesh;
                    meshCollider.convex = true;
                    obj.parent = part.transform;

                    if (obj.name.StartsWith("mobile_node_collider"))
                    {
                        mobileColliders.Add(obj);
                    }
                }
            }
            for (int i = 0; i < obj.childCount; i++)
            {
                colliderizeChilds(obj.GetChild(i));
            }
        }

        public override void OnAwake()
        {
            FindTransforms();
            colliderizeChilds(model_transform);
            if (rotateJoint)
            {
                minTweak = rotateMin;
                maxTweak = rotateMax;
                if (limitTweakable)
                {
                    this.Events["limitTweakableToggle"].active = true;
                }

                if (freeMoving)
                {
                    this.Events["InvertAxisOn"].active = false;
                    this.Events["InvertAxisOff"].active = false;
                    this.Fields["minTweak"].guiActive = false;
                    this.Fields["minTweak"].guiActiveEditor = false;
                    this.Fields["maxTweak"].guiActive = false;
                    this.Fields["maxTweak"].guiActiveEditor = false;
                    this.Fields["speedTweak"].guiActive = false;
                    this.Fields["speedTweak"].guiActiveEditor = false;
                    this.Fields["speedTweakFine"].guiActive = false;
                    this.Fields["speedTweakFine"].guiActiveEditor = false;
                    this.Events["Activate"].active = false;
                    this.Events["Deactivate"].active = false;
                    this.Fields["stepIncrement"].guiActiveEditor = false;
                    this.Fields["stepIncrement"].guiActive = false;
                }

            }
            else if (translateJoint)
            {
                minTweak = translateMin;
                maxTweak = translateMax;
                this.Events["limitTweakableToggle"].active = false;
                this.Events["limitTweakableToggle"].active = false;
            }
            var scene = HighLogic.LoadedScene;
            if (scene == GameScenes.EDITOR || scene == GameScenes.SPH)
            {
                if (rotateJoint)
                    parseMinMaxTweaks(rotateMin, rotateMax);
                else if (translateJoint)
                    parseMinMaxTweaks(translateMin, translateMax);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            if (rotateJoint)
                parseMinMaxTweaks(rotateMin, rotateMax);
            else if (translateJoint)
                parseMinMaxTweaks(translateMin, translateMax);
        }

        public void refreshKeys()
        {
            translateKey = forwardKey;
            revTranslateKey = reverseKey;
            rotateKey = forwardKey;
            revRotateKey = reverseKey;
        }

        public override void OnLoad(ConfigNode config)
        {
           
            loaded = true;
            FindTransforms();
            colliderizeChilds(model_transform);
            //maybe???
            rotationDelta = rotationLast = rotation;
            translationDelta = translation;
            translateKey = forwardKey;
            revTranslateKey = reverseKey;
            rotateKey = forwardKey;
            revRotateKey = reverseKey;

            if (rotateJoint)
                parseMinMaxTweaks(rotateMin, rotateMax);
            else if (translateJoint)
                parseMinMaxTweaks(translateMin, translateMax);
            parseMinMax();
        }

        private void parseMinMaxTweaks(float movementMinimum, float movementMaximum)
        {
            UI_FloatEdit rangeMinF = (UI_FloatEdit)this.Fields["minTweak"].uiControlFlight;
            UI_FloatEdit rangeMinE = (UI_FloatEdit)this.Fields["minTweak"].uiControlEditor;
            rangeMinE.minValue = movementMinimum;
            rangeMinE.maxValue = movementMaximum;
            rangeMinE.incrementSlide = float.Parse(stepIncrement);
            rangeMinF.minValue = movementMinimum;
            rangeMinF.maxValue = movementMaximum;
            rangeMinF.incrementSlide = float.Parse(stepIncrement);
            UI_FloatEdit rangeMaxF = (UI_FloatEdit)this.Fields["maxTweak"].uiControlFlight;
            UI_FloatEdit rangeMaxE = (UI_FloatEdit)this.Fields["maxTweak"].uiControlEditor;
            rangeMaxE.minValue = movementMinimum;
            rangeMaxE.maxValue = movementMaximum;
            rangeMaxE.incrementSlide = float.Parse(stepIncrement);
            rangeMaxF.minValue = movementMinimum;
            rangeMaxF.maxValue = movementMaximum;
            rangeMaxF.incrementSlide = float.Parse(stepIncrement);

            if (rotateJoint)
            {
                this.Fields["minTweak"].guiName = "Min Rotate";
                this.Fields["maxTweak"].guiName = "Max Rotate";
            }
            else if (translateJoint)
            {
                this.Fields["minTweak"].guiName = "Min Translate";
                this.Fields["maxTweak"].guiName = "Max Translate";
            }
        }

        protected void parseMinMax()
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

        protected void DebugCollider(MeshCollider collider)
        {
            if (debug_material == null)
            {
                debug_material = new Material(Shader.Find("Self-Illumin/Specular"));
                debug_material.color = Color.red;
            }
            MeshFilter mf = collider.gameObject.GetComponent<MeshFilter>();
            if (mf == null)
            {
                mf = collider.gameObject.AddComponent<MeshFilter>();
            }
            mf.sharedMesh = collider.sharedMesh;
            MeshRenderer mr = collider.gameObject.GetComponent<MeshRenderer>();
            if (mr == null)
            {
                mr = collider.gameObject.AddComponent<MeshRenderer>();
            }
            mr.sharedMaterial = debug_material;
        }

        protected void AttachToParent(Transform obj)
        {
            Transform fix = transform.FindChild("model").FindChild(fixedMesh);
            if (rotateJoint)
            {
                var pivot = part.transform.TransformPoint(rotatePivot);
                var raxis = part.transform.TransformDirection(rotateAxis);

                float sign = 1;
                if (invertSymmetry)
                {
                    //FIXME is this actually desired?
                    sign = ((isSymmMaster() || (part.symmetryCounterparts.Count != 1)) ? 1 : -1);
                }
                //obj.RotateAround(pivot, raxis, sign * rotation);
                fix.RotateAround(transform.TransformPoint(rotatePivot), transform.TransformDirection(rotateAxis), (invertSymmetry ? ((isSymmMaster() || (part.symmetryCounterparts.Count != 1)) ? -1 : 1) : -1) * rotation);
            }
            else if (translateJoint)
            {
                //var taxis = part.transform.TransformDirection(translateAxis.normalized);
                //obj.Translate(taxis * -(translation - translateMin), Space.Self);//XXX double check sign!
                fix.Translate(transform.TransformDirection(translateAxis.normalized) * translation, Space.World);
            }
            fix.parent = part.parent.transform;
        }

        protected void reparentFriction(Transform obj)
        {
            for (int i = 0; i < obj.childCount; i++)
            {
                var child = obj.GetChild(i);
                MeshCollider tmp = child.GetComponent<MeshCollider>();
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
            if ((mobileColliders.Count > 0) && (rotate_model_transform != null))
            {
                foreach (Transform c in mobileColliders)
                {
                    c.parent = rotate_model_transform;
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
                    Transform fix = model_transform.FindChild(fixedMesh);
                    if ((fix != null) && (part.parent != null))
                    {
                        AttachToParent(fix);
                    }
                }
            }
            else
            {
                foreach (Transform t in model_transform)
                {
                    if (t.name != fixedMesh)
                    {
                        AttachToParent(t);
                    }
                }
                if (translateJoint)
                    translateAxis *= -1;
            }
            reparentFriction(part.transform);
        }

        protected void FindTransforms()
        {
            model_transform = part.transform.FindChild("model");
            on_model_transform = model_transform.FindChild(on_model);
            off_model_transform = model_transform.FindChild(off_model);
            rotate_model_transform = model_transform.FindChild(rotate_model);
            translate_model_transform = model_transform.FindChild(translate_model);
        }

        public void ParseCData()
        {
            Debug.Log(String.Format("[IR] not 'loaded': checking cData"));
            string customPartData = part.customPartData;
            if (customPartData != null && customPartData != "")
            {
                Debug.Log(String.Format("[IR] old cData found"));
                var settings = (Dictionary<string, object>)KSP.IO.IOUtils.DeserializeFromBinary(Convert.FromBase64String(customPartData.Replace("*", "=").Replace("|", "/")));
                servoName = (string)settings["name"];
                groupName = (string)settings["group"];
                forwardKey = (string)settings["key"];
                reverseKey = (string)settings["revkey"];

                rotation = (float)settings["rot"];
                translation = (float)settings["trans"];
                invertAxis = (bool)settings["invertAxis"];
                minRange = (string)settings["minRange"];
                maxRange = (string)settings["maxRange"];

                parseMinMax();
                part.customPartData = "";
            }
        }

        private void onEditorAttach()
        {

        }

        // mrblaq return an int to multiply by rotation direction based on GUI "invert" checkbox bool
        protected int getAxisInversion()
        {
            return (invertAxis ? 1 : -1);
        }

        public override void OnStart(PartModule.StartState state)
        {
            BaseField field = Fields["stepIncrement"];
            UI_ChooseOption optionsEditor = (UI_ChooseOption)field.uiControlEditor;
            UI_ChooseOption optionsFlight = (UI_ChooseOption)field.uiControlFlight;

            if (translateJoint)
            {
                optionsEditor.options = new string[] { "0.01", "0.1", "1.0" };
                optionsFlight.options = new string[] { "0.01", "0.1", "1.0" };
            }
            else if (rotateJoint)
            {
                optionsEditor.options = new string[] { "0.1", "1", "10" };
                optionsFlight.options = new string[] { "0.1", "1", "10" };
            }

            part.stackIcon.SetIcon(DefaultIcons.STRUT);
            if (vessel == null)
            {
                return;
            }
            if (!loaded)
            {
                loaded = true;
                ParseCData();
                on = false;
            }
            createFXSound(part, fxSndMotor, motorSndPath, true, 10f);
            creationOrder = s_creationOrder++;
            FindTransforms();
            BuildAttachments();
            updateState();
            if (rotateJoint)
            {
                parseMinMaxTweaks(rotateMin, rotateMax);
                if (limitTweakable)
                {
                    this.Events["limitTweakableToggle"].active = true;
                }
            }
            else if (translateJoint)
            {
                parseMinMaxTweaks(translateMin, translateMax);
                if (limitTweakable)
                {
                    this.Events["limitTweakableToggle"].active = false;
                }
            }
        }



        private ConfigurableJoint joint;
        public bool setupJoints()
        {
            if (!gotOrig)
            {
                print("setupJoints - !gotOrig");
                if (rotate_model_transform != null)
                {
                    //sr 4/27
                    //origRotation = rotate_model_transform.localRotation;
                }
                else if (translate_model_transform != null)
                {
                    //sr 4/27
                    //origTranslation = translate_model_transform.localPosition;
                }
                if (translateJoint)
                {
                    //sr 4/27
                    //origTranslation = part.transform.localPosition;
                }

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
                        joint.anchor = joint.rigidbody.transform.InverseTransformPoint(joint.connectedBody.transform.position);
                        joint.connectedAnchor = Vector3.zero;

                        // Set correct axis
                        joint.axis = joint.rigidbody.transform.InverseTransformDirection(joint.connectedBody.transform.right);
                        joint.secondaryAxis = joint.rigidbody.transform.InverseTransformDirection(joint.connectedBody.transform.up);


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
                        JointDrive resetDrv = new JointDrive();
                        resetDrv.mode = JointDriveMode.PositionAndVelocity;
                        resetDrv.positionSpring = 0;
                        resetDrv.positionDamper = 0;
                        resetDrv.maximumForce = 0;

                        part.attachJoint.Joint.angularXDrive = resetDrv;
                        part.attachJoint.Joint.angularYZDrive = resetDrv;
                        part.attachJoint.Joint.xDrive = resetDrv;
                        part.attachJoint.Joint.yDrive = resetDrv;
                        part.attachJoint.Joint.zDrive = resetDrv;

                        gotOrig = true;
                        return true;
                    }
                }
                else
                {
                    gotOrig = true;
                    return true;
                }
            }
            return false;
        }

        public override void OnActive()
        {
            if (onActivate)
            {
                on = true;
                updateState();
            }
        }
        /*
            protected override void onJointDisable()
            {
                rotationDelta = rotationLast = rotation;
                translationDelta = translation;
                gotOrig = false;
            }
        */
        protected void updateRotation(float speed, bool reverse, int mask)
        {
            speed *= (speedTweak + speedTweakFine) * customSpeed * (reverse ? -1 : 1);
            rotation += getAxisInversion() * TimeWarp.fixedDeltaTime * speed;
            rotationChanged |= mask;
            playAudio();
        }

        protected void updateTranslation(float speed, bool reverse, int mask)
        {
            speed *= (speedTweak + speedTweakFine) * customSpeed * (reverse ? -1 : 1);
            translation += getAxisInversion() * TimeWarp.fixedDeltaTime * speed;
            translationChanged |= mask;
            playAudio();
        }

        protected bool keyPressed(string key)
        {
            return (key != "" && vessel == FlightGlobals.ActiveVessel
                    && InputLockManager.IsUnlocked(ControlTypes.LINEAR)
                    && Input.GetKey(key));
        }

        protected float HomeSpeed(float offset, float maxSpeed)
        {
            float speed = Math.Abs(offset) / TimeWarp.deltaTime;
            if (speed > maxSpeed)
            {
                speed = maxSpeed;
            }
            return -speed * Mathf.Sign(offset) * getAxisInversion();
        }


        protected void checkInputs()
        {
            if (part.isConnected && keyPressed(onKey))
            {
                on = !on;
                updateState();
            }

            if (on && (onRotateSpeed != 0))
            {
                updateRotation(+onRotateSpeed, reversedRotationOn, 1);
            }
            if (on && (onTranslateSpeed != 0))
            {
                updateTranslation(+onTranslateSpeed, reversedTranslationOn, 1);
            }

            if ((moveFlags & 0x101) != 0 || keyPressed(rotateKey))
            {
                updateRotation(+keyRotateSpeed, reversedRotationKey, 2);
            }
            if ((moveFlags & 0x202) != 0 || keyPressed(revRotateKey))
            {
                updateRotation(-keyRotateSpeed, reversedRotationKey, 2);
            }
            //FIXME Hmm, these moveFlag checks clash with rotation. Is rotation and translation in the same part not intended?
            if ((moveFlags & 0x101) != 0 || keyPressed(translateKey))
            {
                updateTranslation(+keyTranslateSpeed, reversedTranslationKey, 2);
            }
            if ((moveFlags & 0x202) != 0 || keyPressed(revTranslateKey))
            {
                updateTranslation(-keyTranslateSpeed, reversedTranslationKey, 2);
            }

            if (((moveFlags & 0x404) != 0) && (rotationChanged == 0) && (translationChanged == 0))
            {
                float speed;
                speed = HomeSpeed(rotation, keyRotateSpeed);
                updateRotation(speed, false, 2);
                speed = HomeSpeed(translation, keyTranslateSpeed);
                updateTranslation(speed, false, 2);
            }

            if (moveFlags == 0 && !on && fxSndMotor.audio != null)
            {
                fxSndMotor.audio.Stop();
                isPlaying = false;
            }
        }

        protected void checkRotationLimits()
        {
            if (rotateLimits || limitTweakableFlag)
            {

                if (rotation < minTweak || rotation > maxTweak)
                {
                    rotation = Mathf.Clamp(rotation, minTweak, maxTweak);
                    if (rotateLimitsRevertOn && ((rotationChanged & 1) > 0))
                    {
                        reversedRotationOn = !reversedRotationOn;
                    }
                    if (rotateLimitsRevertKey && ((rotationChanged & 2) > 0))
                    {
                        reversedRotationKey = !reversedRotationKey;
                    }
                    if (rotateLimitsOff)
                    {
                        on = false;
                        updateState();
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

        protected void checkTranslationLimits()
        {
            if (translateLimits)
            {
                if (translation < minTweak || translation > maxTweak)
                {
                    translation = Mathf.Clamp(translation, minTweak, maxTweak);
                    if (translateLimitsRevertOn && ((translationChanged & 1) > 0))
                    {
                        reversedTranslationOn = !reversedTranslationOn;
                    }
                    if (translateLimitsRevertKey && ((translationChanged & 2) > 0))
                    {
                        reversedTranslationKey = !reversedTranslationKey;
                    }
                    if (translateLimitsOff)
                    {
                        on = false;
                        updateState();
                    }
                }
            }
        }

        protected void doRotation()
        {
            if ((rotationChanged != 0) && (rotateJoint || rotate_model_transform != null))
            {
                if (rotateJoint)
                {
                    joint.targetRotation = Quaternion.AngleAxis((invertSymmetry ? ((isSymmMaster() || (part.symmetryCounterparts.Count != 1)) ? 1 : -1) : 1) * (rotation - rotationDelta), rotateAxis);
                    rotationLast = rotation;
                }
                else
                {
                    Quaternion curRot = Quaternion.AngleAxis((invertSymmetry ? ((isSymmMaster() || (part.symmetryCounterparts.Count != 1)) ? 1 : -1) : 1) * rotation, rotateAxis);
                    transform.FindChild("model").FindChild(rotate_model).localRotation = curRot;
                }
            }
        }

        protected void doTranslation()
        {
            if ((translationChanged != 0) && (translateJoint || translate_model_transform != null))
            {
                if (translateJoint)
                {
                    joint.targetPosition = -translateAxis * (translation - translationDelta);
                }
                else
                {
                    joint.targetPosition = origTranslation - translateAxis.normalized * (translation - translationDelta);
                }
            }
        }

        protected bool actionUIUpdate;
        public UIPartActionWindow tweakWindow;
        public void resized()
        {
            UIPartActionWindow[] actionWindows = MonoBehaviour.FindObjectsOfType<UIPartActionWindow>();
            if (actionWindows.Length > 0)
            {
                foreach (UIPartActionWindow actionWindow in actionWindows)
                {
                    if (actionWindow.part == this.part)
                    {
                        this.tweakWindow = actionWindow;
                        tweakIsDirty = true;
                    }
                }
            }
            else
            {
                this.tweakWindow = null;
            }
        }


        public bool tweakIsDirty = false;
        public void refreshTweakUI()
        {
            if (HighLogic.LoadedScene == GameScenes.EDITOR || HighLogic.LoadedScene == GameScenes.SPH)
            {
                if (this.tweakWindow != null)
                {
                    if (translateJoint)
                    {
                        UI_FloatEdit rangeMinF = (UI_FloatEdit)this.Fields["minTweak"].uiControlEditor;
                        rangeMinF.minValue = this.translateMin;
                        rangeMinF.maxValue = this.translateMax;
                        rangeMinF.incrementSlide = float.Parse(stepIncrement); ;
                        minTweak = this.translateMin;
                        UI_FloatEdit rangeMaxF = (UI_FloatEdit)this.Fields["maxTweak"].uiControlEditor;
                        rangeMaxF.minValue = this.translateMin;
                        rangeMaxF.maxValue = this.translateMax;
                        rangeMaxF.incrementSlide = float.Parse(stepIncrement); ;
                        maxTweak = this.translateMax;
                    }
                    else if (rotateJoint)
                    {
                        UI_FloatEdit rangeMinF = (UI_FloatEdit)this.Fields["minTweak"].uiControlEditor;
                        rangeMinF.minValue = this.rotateMin;
                        rangeMinF.maxValue = this.rotateMax;
                        rangeMinF.incrementSlide = float.Parse(stepIncrement); ;
                        minTweak = this.rotateMin;
                        UI_FloatEdit rangeMaxF = (UI_FloatEdit)this.Fields["maxTweak"].uiControlEditor;
                        rangeMaxF.minValue = this.rotateMin;
                        rangeMaxF.maxValue = this.rotateMax;
                        rangeMaxF.incrementSlide = float.Parse(stepIncrement); ;
                        maxTweak = this.rotateMax;
                    }

                    if (part.symmetryCounterparts.Count > 1)
                    {
                        for (int i = 0; i < part.symmetryCounterparts.Count; i++)
                        {
                            ((MuMechToggle)part.symmetryCounterparts[i].Modules["MuMechToggle"]).rotateMin = this.rotateMin;
                            ((MuMechToggle)part.symmetryCounterparts[i].Modules["MuMechToggle"]).rotateMax = this.rotateMax;
                            ((MuMechToggle)part.symmetryCounterparts[i].Modules["MuMechToggle"]).stepIncrement = this.stepIncrement;
                            ((MuMechToggle)part.symmetryCounterparts[i].Modules["MuMechToggle"]).minTweak = this.rotateMin;
                            ((MuMechToggle)part.symmetryCounterparts[i].Modules["MuMechToggle"]).maxTweak = this.maxTweak;
                        }
                    }
                }
            }
        }

        UI_FloatEdit rangeMinF;
        UI_FloatEdit rangeMinE;
        UI_FloatEdit rangeMaxF;
        UI_FloatEdit rangeMaxE;
        public void FixedUpdate()
        {
            rangeMinF = (UI_FloatEdit)this.Fields["minTweak"].uiControlFlight;
            rangeMinE = (UI_FloatEdit)this.Fields["minTweak"].uiControlEditor;
            rangeMinE.incrementSlide = float.Parse(stepIncrement);
            rangeMinF.incrementSlide = float.Parse(stepIncrement);
            rangeMaxF = (UI_FloatEdit)this.Fields["maxTweak"].uiControlFlight;
            rangeMaxE = (UI_FloatEdit)this.Fields["maxTweak"].uiControlEditor;
            rangeMaxE.incrementSlide = float.Parse(stepIncrement);
            rangeMaxF.incrementSlide = float.Parse(stepIncrement);

            if (HighLogic.LoadedScene == GameScenes.SPH || HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                if (this.tweakWindow != null && tweakIsDirty)
                {
                    refreshTweakUI();
                    this.tweakWindow.UpdateWindow();
                    tweakIsDirty = false;
                }
            }
            if (HighLogic.LoadedScene != GameScenes.FLIGHT)
                return;
            if (isMotionLock || part.State == PartStates.DEAD)
            {
                return;
            }

            if (setupJoints())
            {
                rotationChanged = 4;
                translationChanged = 4;
            }

            checkInputs();
            checkRotationLimits();
            checkTranslationLimits();

            doRotation();
            doTranslation();

            rotationChanged = 0;
            translationChanged = 0;

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
            updateState();
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
                    moveFlags |= 0x100;
                    break;
                case KSPActionType.Deactivate:
                    moveFlags &= ~0x100;
                    break;
            }
        }

        [KSPAction("Move -")]
        public void MoveMinusAction(KSPActionParam param)
        {
            switch (param.type)
            {
                case KSPActionType.Activate:
                    moveFlags |= 0x200;
                    break;
                case KSPActionType.Deactivate:
                    moveFlags &= ~0x200;
                    break;
            }
        }

        [KSPAction("Move Center")]
        public void MoveCenterAction(KSPActionParam param)
        {
            switch (param.type)
            {
                case KSPActionType.Activate:
                    moveFlags |= 0x400;
                    break;
                case KSPActionType.Deactivate:
                    moveFlags &= ~0x400;
                    break;
            }
        }
    }
}
