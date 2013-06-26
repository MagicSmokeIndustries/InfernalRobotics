using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class MuMechToggle : MuMechPart
{
    public bool toggle_drag = false;
    public bool toggle_break = false;
    public bool toggle_model = false;
    public bool toggle_collision = false;
    public float on_angularDrag = 2.0F;
    public float on_maximum_drag = 0.2F;
    public float on_minimum_drag = 0.2F;
    public float on_crashTolerance = 9.0F;
    public float on_breakingForce = 22.0F;
    public float on_breakingTorque = 22.0F;
    public float off_angularDrag = 2.0F;
    public float off_maximum_drag = 0.2F;
    public float off_minimum_drag = 0.2F;
    public float off_crashTolerance = 9.0F;
    public float off_breakingForce = 22.0F;
    public float off_breakingTorque = 22.0F;
    public string on_model = "on";
    public string off_model = "off";
    public string rotate_model = "on";
    public Vector3 rotateAxis = Vector3.forward;
    public Vector3 rotatePivot = Vector3.zero;
    public float onRotateSpeed = 0;
    public string onKey = "p";
    public float keyRotateSpeed = 0;
    public string rotateKey = "9";
    public string revRotateKey = "0";
    public bool rotateJoint = false;
    public bool rotateLimits = false;
    public float rotateMin = 0;
    public float rotateMax = 300;
    public bool rotateLimitsRevertOn = true;
    public bool rotateLimitsRevertKey = false;
    public bool rotateLimitsOff = false;
    public float jointSpring = 0;
    public float jointDamping = 0;
    public bool onActivate = true;
    public bool invertSymmetry = true;
    public string fixedMesh = "";
    public float friction = 0.5F;

    public string translate_model = "on";
    public Vector3 translateAxis = Vector3.forward;
    public float onTranslateSpeed = 0;
    public float keyTranslateSpeed = 0;
    public string translateKey = "9";
    public string revTranslateKey = "0";
    public bool translateJoint = false;
    public bool translateLimits = false;
    public float translateMin = 0;
    public float translateMax = 300;
    public bool translateLimitsRevertOn = true;
    public bool translateLimitsRevertKey = false;
    public bool translateLimitsOff = false;

    public bool debugColliders = false;

    protected bool on = false;
    protected Quaternion origRotation;
    protected float rotation = 0;
    protected float rotationDelta = 0;
    protected float rotationLast = 0;
    protected bool reversedRotationOn = false;
    protected bool reversedRotationKey = false;
    protected Vector3 origTranslation;
    protected float translation = 0;
    protected float translationDelta = 0;
    protected bool reversedTranslationOn = false;
    protected bool reversedTranslationKey = false;
    protected bool gotOrig = false;
    protected List<Transform> mobileColliders = new List<Transform>();
    protected int rotationChanged = 0;
    protected int translationChanged = 0;

    public int moveFlags = 0;
    public bool isRotationLock; //motion lock
    public override void onFlightStateSave(Dictionary<string, KSPParseable> partDataCollection)
    {
        partDataCollection.Add("on", new KSPParseable(on, KSPParseable.Type.BOOL));
        partDataCollection.Add("reversedRotationOn", new KSPParseable(reversedRotationOn, KSPParseable.Type.BOOL));
        partDataCollection.Add("reversedRotationKey", new KSPParseable(reversedRotationKey, KSPParseable.Type.BOOL));
        partDataCollection.Add("reversedTranslationOn", new KSPParseable(reversedTranslationOn, KSPParseable.Type.BOOL));
        partDataCollection.Add("reversedTranslationKey", new KSPParseable(reversedTranslationKey, KSPParseable.Type.BOOL));
        partDataCollection.Add("rot", new KSPParseable(rotation, KSPParseable.Type.FLOAT));
        partDataCollection.Add("trans", new KSPParseable(translation, KSPParseable.Type.FLOAT));
        partDataCollection.Add("rotD", new KSPParseable(rotationDelta, KSPParseable.Type.FLOAT));
        partDataCollection.Add("transD", new KSPParseable(translationDelta, KSPParseable.Type.FLOAT));

        base.onFlightStateSave(partDataCollection);
    }

    public override void onFlightStateLoad(Dictionary<string, KSPParseable> parsedData)
    {
        if (parsedData.ContainsKey("on")) on = parsedData["on"].value_bool;
        if (parsedData.ContainsKey("reversedRotationOn")) reversedRotationOn = parsedData["reversedRotationOn"].value_bool;
        if (parsedData.ContainsKey("reversedRotationKey")) reversedRotationKey = parsedData["reversedRotationKey"].value_bool;
        if (parsedData.ContainsKey("reversedTranslationOn")) reversedTranslationOn = parsedData["reversedTranslationOn"].value_bool;
        if (parsedData.ContainsKey("reversedTranslationKey")) reversedTranslationKey = parsedData["reversedTranslationKey"].value_bool;
        if (parsedData.ContainsKey("rot")) rotation = parsedData["rot"].value_float;
        if (parsedData.ContainsKey("trans")) translation = parsedData["trans"].value_float;
        if (parsedData.ContainsKey("rotD")) rotationDelta = parsedData["rotD"].value_float;
        if (parsedData.ContainsKey("transD")) translationDelta = parsedData["transD"].value_float;
        updateState();

        rotationDelta = rotationLast = rotation;
        translationDelta = translation;

        base.onFlightStateLoad(parsedData);
    }

    public void updateState()
    {
        if (on)
        {
            if (toggle_model)
            {
                transform.FindChild("model").FindChild(on_model).renderer.enabled = true;
                transform.FindChild("model").FindChild(off_model).renderer.enabled = false;
            }
            if (toggle_drag)
            {
                angularDrag = on_angularDrag;
                minimum_drag = on_minimum_drag;
                maximum_drag = on_maximum_drag;
            }
            if (toggle_break)
            {
                crashTolerance = on_crashTolerance;
                breakingForce = on_breakingForce;
                breakingTorque = on_breakingTorque;
            }
        }
        else
        {
            if (toggle_model)
            {
                transform.FindChild("model").FindChild(on_model).renderer.enabled = false;
                transform.FindChild("model").FindChild(off_model).renderer.enabled = true;
            }
            if (toggle_drag)
            {
                angularDrag = off_angularDrag;
                minimum_drag = off_minimum_drag;
                maximum_drag = off_maximum_drag;
            }
            if (toggle_break)
            {
                crashTolerance = off_crashTolerance;
                breakingForce = off_breakingForce;
                breakingTorque = off_breakingTorque;
            }
        }
        if (toggle_collision)
        {
            collider.enabled = on;
            collisionEnhancer.enabled = on;
            terrainCollider.enabled = on;
        }
    }

    protected void colliderizeChilds(Transform obj)
    {
        //if (obj.name.StartsWith("node_collider") || obj.name.StartsWith("fixed_node_collider") || obj.name.StartsWith("mobile_node_collider"))
        //{
        //    print("Toggle: converting collider " + obj.name);
        //    Mesh sharedMesh = UnityEngine.Object.Instantiate(obj.GetComponent<MeshFilter>().mesh) as Mesh;
        //    UnityEngine.Object.Destroy(obj.GetComponent<MeshFilter>());
        //    UnityEngine.Object.Destroy(obj.GetComponent<MeshRenderer>());
        //    MeshCollider meshCollider = obj.gameObject.AddComponent<MeshCollider>();
        //    meshCollider.sharedMesh = sharedMesh;
        //    meshCollider.convex = true;
        //    obj.parent = transform;
        //    if (obj.name.StartsWith("mobile_node_collider"))
        //    {
        //        mobileColliders.Add(obj);
        //    }
        //}
        //for (int i = 0; i < obj.childCount; i++)
        //{
        //    colliderizeChilds(obj.GetChild(i));
        //}
        
        

        if (obj.name.StartsWith("node_collider") || obj.name.StartsWith("fixed_node_collider") || obj.name.StartsWith("mobile_node_collider"))
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
                obj.parent = transform;

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

    protected override void onPartAwake()
    {
        colliderizeChilds(transform.FindChild("model"));
        base.onPartAwake();
    }

    protected override void onPartLoad()
    {
        colliderizeChilds(transform.FindChild("model"));
        base.onPartLoad();
    }

    protected void reparentFriction(Transform obj)
    {
        Material debug = new Material(Shader.Find("Self-Illumin/Specular"));
        debug.color = Color.red;
        Transform rotMod = transform.FindChild("model").FindChild(rotate_model);
        for (int i = 0; i < obj.childCount; i++)
        {
            MeshCollider tmp = obj.GetChild(i).GetComponent<MeshCollider>();
            if (tmp != null)
            {
                tmp.material.dynamicFriction = tmp.material.staticFriction = friction;
                tmp.material.frictionCombine = PhysicMaterialCombine.Maximum;
                if (debugColliders)
                {
                    MeshFilter mf = tmp.gameObject.GetComponent<MeshFilter>();
                    if (mf == null)
                    {
                        mf = tmp.gameObject.AddComponent<MeshFilter>();
                    }
                    mf.sharedMesh = tmp.sharedMesh;
                    MeshRenderer mr = tmp.gameObject.GetComponent<MeshRenderer>();
                    if (mr == null)
                    {
                        mr = tmp.gameObject.AddComponent<MeshRenderer>();
                    }
                    mr.sharedMaterial = debug;
                }
            }
            if (obj.GetChild(i).name.StartsWith("fixed_node_collider") && (parent != null))
            {
                print("Toggle: reparenting collider " + obj.GetChild(i).name);
                obj.GetChild(i).RotateAround(transform.TransformPoint(rotatePivot), transform.TransformDirection(-rotateAxis), (invertSymmetry ? ((isSymmMaster() || (symmetryCounterparts.Count != 1)) ? -1 : 1) : -1) * rotation);
                obj.GetChild(i).Translate(transform.TransformDirection(translateAxis.normalized) * -translation, Space.World);
                obj.GetChild(i).parent = parent.transform;
            }
        }
        if ((mobileColliders.Count > 0) && (rotMod != null))
        {
            foreach (Transform c in mobileColliders)
            {
                c.parent = rotMod;
            }
        }
    }

    protected override void onPartStart()
    {
        base.onPartStart();
        stackIcon.SetIcon(DefaultIcons.STRUT);
        if (vessel == null)
        {
            return;
        }
        if (fixedMesh != "")
        {
            Transform fix = transform.FindChild("model").FindChild(fixedMesh);
            if ((fix != null) && (parent != null))
            {
                fix.RotateAround(transform.TransformPoint(rotatePivot), transform.TransformDirection(rotateAxis.normalized), (invertSymmetry ? ((isSymmMaster() || (symmetryCounterparts.Count != 1)) ? 1 : -1) : 1) * rotation);
                fix.Translate(transform.TransformDirection(translateAxis.normalized) * -translation, Space.World);
                fix.parent = parent.transform;
            }
        }
        reparentFriction(transform);
        on = true;
        updateState();
    }

    protected override void onPartAttach(Part parent)
    {
        on = false;
        updateState();
    }

    protected override void onPartDetach()
    {
        on = true;
        updateState();
    }

    protected override void onEditorUpdate()
    {
        base.onEditorUpdate();
    }

    protected bool setupJoints()
    {
        if (!gotOrig)
        {
            print("setupJoints - !gotOrig");
            if ((rotate_model != "") && (transform.FindChild("model").FindChild(rotate_model) != null))
            {
                origRotation = transform.FindChild("model").FindChild(rotate_model).localRotation;
            }
            else if ((translate_model != "") && (transform.FindChild("model").FindChild(translate_model) != null))
            {
                origTranslation = transform.FindChild("model").FindChild(translate_model).localPosition;
            }
            if (translateJoint)
            {
                origTranslation = transform.localPosition;
            }
            if (rotateJoint || translateJoint)
            {
                if (attachJoint != null)
                {
                    GameObject.Destroy(attachJoint);
                    ConfigurableJoint newJoint = gameObject.AddComponent<ConfigurableJoint>();
                    newJoint.breakForce = breakingForce;
                    newJoint.breakTorque = breakingTorque;
                    newJoint.axis = rotateJoint ? rotateAxis : translateAxis;
                    newJoint.secondaryAxis = (newJoint.axis == Vector3.up) ? Vector3.forward : Vector3.up;
                    SoftJointLimit spring = new SoftJointLimit();
                    spring.limit = 0;
                    spring.damper = jointDamping;
                    spring.spring = jointSpring;
                    if (translateJoint)
                    {
                        newJoint.xMotion = ConfigurableJointMotion.Free;
                        newJoint.yMotion = ConfigurableJointMotion.Free;
                        newJoint.zMotion = ConfigurableJointMotion.Free;
                        //newJoint.linearLimit = spring;
                        JointDrive drv = new JointDrive();
                        drv.mode = JointDriveMode.PositionAndVelocity;
                        drv.positionSpring = 1e20F;
                        drv.positionDamper = 0;
                        drv.maximumForce = 1e20F;
                        newJoint.xDrive = newJoint.yDrive = newJoint.zDrive = drv;
                    }
                    else
                    {
                        newJoint.xMotion = ConfigurableJointMotion.Locked;
                        newJoint.yMotion = ConfigurableJointMotion.Locked;
                        newJoint.zMotion = ConfigurableJointMotion.Locked;
                    }
                    if (rotateJoint)
                    {
                        newJoint.angularXMotion = ConfigurableJointMotion.Limited;
                        newJoint.lowAngularXLimit = newJoint.highAngularXLimit = spring;
                    }
                    else
                    {
                        newJoint.angularXMotion = ConfigurableJointMotion.Locked;
                    }
                    newJoint.angularYMotion = ConfigurableJointMotion.Locked;
                    newJoint.angularZMotion = ConfigurableJointMotion.Locked;
                    //newJoint.anchor = rotateJoint ? rotatePivot : origTranslation;
                    newJoint.anchor = rotateJoint ? rotatePivot : Vector3.zero;

                    newJoint.projectionMode = JointProjectionMode.PositionAndRotation;
                    newJoint.projectionDistance = 0;
                    newJoint.projectionAngle = 0;

                    newJoint.connectedBody = parent.Rigidbody;
                    attachJoint = newJoint;
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

    protected override void onFlightStart()
    {
        setupJoints();
        on = false;
        updateState();
    }

    protected override void onPartUpdate()
    {
        if (connected && Input.GetKeyDown(onKey) && (vessel == FlightGlobals.ActiveVessel) && InputLockManager.IsUnlocked(ControlTypes.LINEAR))
        {
            on = !on;
            updateState();
        }
    }

    protected override bool onPartActivate()
    {
        if (onActivate)
        {
            on = true;
            updateState();
        }
        return true;
    }

    protected override void onJointDisable()
    {
        rotationDelta = rotationLast = rotation;
        translationDelta = translation;
        gotOrig = false;
    }

    //public void rotate(float amount)
    //{
    //    rotation += amount;
    //    rotationChanged = 8;
    //}

    //public void translate(float amount)
    //{
    //    translation += amount;
    //    translationChanged = 8;
    //}

    protected override void onPartFixedUpdate()
    {
        if (!isRotationLock) //sr this part only!
        {
            if (state == PartStates.DEAD)
            {
                return;
            }

            if (setupJoints())
            {
                rotationChanged = 4;
                translationChanged = 4;
            }

            if (on && (onRotateSpeed != 0))
            {
                rotation += TimeWarp.fixedDeltaTime * onRotateSpeed * (reversedRotationOn ? -1 : 1);
                rotationChanged |= 1;
            }

            if (((keyRotateSpeed != 0) && Input.GetKey(rotateKey) && (vessel == FlightGlobals.ActiveVessel) && InputLockManager.IsUnlocked(ControlTypes.LINEAR)) || ((moveFlags & 0x101) != 0))
            {
                rotation += TimeWarp.fixedDeltaTime * keyRotateSpeed * (reversedRotationKey ? -1 : 1);
                rotationChanged |= 2;
            }
            if (((keyRotateSpeed != 0) && Input.GetKey(revRotateKey) && (vessel == FlightGlobals.ActiveVessel) && InputLockManager.IsUnlocked(ControlTypes.LINEAR)) || ((moveFlags & 0x202) != 0))
            {
                rotation -= TimeWarp.fixedDeltaTime * keyRotateSpeed * (reversedRotationKey ? -1 : 1);
                rotationChanged |= 2;
            }

            if (on && (onTranslateSpeed != 0))
            {
                translation += TimeWarp.fixedDeltaTime * onTranslateSpeed * (reversedTranslationOn ? -1 : 1);
                translationChanged |= 1;
            }
            if (((keyTranslateSpeed != 0) && Input.GetKey(translateKey) && (vessel == FlightGlobals.ActiveVessel) && InputLockManager.IsUnlocked(ControlTypes.LINEAR)) || ((moveFlags & 0x101) != 0))
            {
                translation += TimeWarp.fixedDeltaTime * keyTranslateSpeed * (reversedTranslationKey ? -1 : 1);
                translationChanged |= 2;
            }
            if (((keyTranslateSpeed != 0) && Input.GetKey(revTranslateKey) && (vessel == FlightGlobals.ActiveVessel) && InputLockManager.IsUnlocked(ControlTypes.LINEAR)) || ((moveFlags & 0x202) != 0))
            {
                translation -= TimeWarp.fixedDeltaTime * keyTranslateSpeed * (reversedTranslationKey ? -1 : 1);
                translationChanged |= 2;
            }

            if (((moveFlags & 0x404) != 0) && (rotationChanged == 0) && (translationChanged == 0))
            {
                rotation -= Mathf.Sign(rotation) * Mathf.Min(Mathf.Abs(keyRotateSpeed * TimeWarp.deltaTime), Mathf.Abs(rotation));
                translation -= Mathf.Sign(translation) * Mathf.Min(Mathf.Abs(keyTranslateSpeed * TimeWarp.deltaTime), Mathf.Abs(translation));
                rotationChanged |= 2;
                translationChanged |= 2;
            }

            if (rotateLimits)
            {
                if (rotation < rotateMin)
                {
                    rotation = rotateMin;
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
                if (rotation > rotateMax)
                {
                    rotation = rotateMax;
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
            if (Math.Abs(rotation - rotationDelta) > 120)
            {
                rotationDelta = rotationLast;
                attachJoint.connectedBody = null;
                attachJoint.connectedBody = parent.Rigidbody;
            }

            if (translateLimits)
            {
                if (translation < translateMin)
                {
                    translation = translateMin;
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
                if (translation > translateMax)
                {
                    translation = translateMax;
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

            if ((rotationChanged != 0) && (rotateJoint || (transform.FindChild("model").FindChild(rotate_model) != null)))
            {
                if (rotateJoint)
                {
                    SoftJointLimit tmp = ((ConfigurableJoint)attachJoint).lowAngularXLimit;
                    tmp.limit = (invertSymmetry ? ((isSymmMaster() || (symmetryCounterparts.Count != 1)) ? 1 : -1) : 1) * (rotation - rotationDelta);
                    ((ConfigurableJoint)attachJoint).lowAngularXLimit = ((ConfigurableJoint)attachJoint).highAngularXLimit = tmp;
                    rotationLast = rotation;
                }
                else
                {
                    Quaternion curRot = Quaternion.AngleAxis((invertSymmetry ? ((isSymmMaster() || (symmetryCounterparts.Count != 1)) ? 1 : -1) : 1) * rotation, rotateAxis);
                    transform.FindChild("model").FindChild(rotate_model).localRotation = curRot;
                }
            }

            if ((translationChanged != 0) && (translateJoint || (transform.FindChild("model").FindChild(translate_model) != null)))
            {
                if (translateJoint)
                {
                    ((ConfigurableJoint)attachJoint).targetPosition = -Vector3.right * (translation - translationDelta);
                }
                else
                {
                    transform.FindChild("model").FindChild(translate_model).localPosition = origTranslation + translateAxis.normalized * (translation - translationDelta);
                }
            }

            rotationChanged = 0;
            translationChanged = 0;

            if (vessel != null)
            {
                UpdateOrgPosAndRot(vessel.rootPart);
                foreach (Part child in FindChildParts<Part>(true))
                {
                    child.UpdateOrgPosAndRot(vessel.rootPart);
                }
            }
        }
    }

    protected override void onPartDeactivate()
    {
        on = false;
        updateState();
    }
}
