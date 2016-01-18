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
    class ModuleIRHydraulicPiston : ModuleIRServo
    {
        //HydraulicPiston as a separate Module, but a child to ModuleIRServo, with several methods overriden.
        // HydraulicPiston is translation only.
        //  Main points: 
        //  * switch joint to SpringJoint? via override in SetupJoints and BuildAttachments and 
        //  * keep targetPosition to 0 all the time
        //  * have a spring to keep the joint in one position, spring power is set in cfg(set in UI for tests)
        //  * have a high damping power, set in part.cfg
        //  * servo's position to be regulated via maxTorque, if the part is not under load, torque equal to spring power should extend the part fully
        //  * Override the UpdatePosition method, add a PID loop to increase/decrease torque to maintain given target position
        //  * consider ditching Interpolator for this part???.

        private SpringJoint sJoint;

        private float currentForce = 0f;
        private PID positionPID;

        public new void InitModule()
        {
            FindTransforms();

            if (ModelTransform == null)
                Logger.Log("[OnLoad] ModelTransform is null", Logger.Level.Warning);

            if (HighLogic.LoadedSceneIsEditor)
            {
                //apply saved rotation/translation in reverse to a fixed mesh.
                //if (rotateJoint)
                //{
                //    FixedMeshTransform.Rotate(rotateAxis, -rotation);
                //}
                //else
                //{
                //    FixedMeshTransform.Translate(translateAxis * translation);
                //}
            }

            ParsePresetPositions();

            UpdateMinMaxTweaks();
        }

        public override void OnLoad(ConfigNode config)
        {
            Logger.Log("[OnLoad] Start", Logger.Level.Debug);

            //save persistent rotation/translation data, because the joint will be initialized at current position.
            rotationDelta = rotation;
            translationDelta = translation;

            InitModule();

            Logger.Log("[OnLoad] End", Logger.Level.Debug);
        }

        /// <summary>
        /// Core function, messes with Transforms.
        /// Some problems with Joints arise because this is called before the Joints are created.
        /// Fixed mesh gets pushed in space to the corresponding rotation/translation and only after that joint is created.
        /// Meaning that Limits on joints are realy hard to set in terms of maxRotation and maxTranslation
        /// 
        /// Ultimately called from OnStart (usually meanining start of Flight mode). 
        /// 
        /// Basically rotates/translates the fixed mesh int the opposite direction of saved rotation/translation.
        /// </summary>
        /// <param name="obj">Transform</param>
        protected new void AttachToParent(Transform obj)
        {
            //Transform fix = FixedMeshTransform;
            Transform fix = obj;
            //we don't need to translate the FixedMesh
            //fix.Translate(transform.TransformDirection(translateAxis.normalized) * translation, Space.World);

            fix.parent = part.parent.transform;
        }

        public new bool SetupJoints()
        {
            if (!JointSetupDone)
            {
                if (rotateJoint || translateJoint)
                {
                    if (part.attachJoint != null)
                    {
                        // Catch reversed joint
                        // Maybe there is a best way to do it?
                        if (transform.position != part.attachJoint.Joint.connectedBody.transform.position)
                        {
                            sJoint = part.attachJoint.Joint.connectedBody.gameObject.AddComponent<SpringJoint>();
                            sJoint.connectedBody = part.attachJoint.Joint.rigidbody;

                        }
                        else
                        {
                            sJoint = part.attachJoint.Joint.rigidbody.gameObject.AddComponent<SpringJoint>();
                            sJoint.connectedBody = part.attachJoint.Joint.connectedBody;

                        }

                        sJoint.breakForce = 1e15f;
                        sJoint.breakTorque = 1e15f;
                        // And to default joint
                        part.attachJoint.Joint.breakForce = 1e15f;
                        part.attachJoint.Joint.breakTorque = 1e15f;
                        part.attachJoint.SetBreakingForces(1e15f, 1e15f);

                        // Set anchor position
                        sJoint.anchor =
                            joint.rigidbody.transform.InverseTransformPoint(joint.connectedBody.transform.position);
                        sJoint.connectedAnchor = Vector3.zero;

                        // Set correct axis
                        sJoint.axis =
                            joint.rigidbody.transform.InverseTransformDirection(joint.connectedBody.transform.right);  //x axis

                        sJoint.spring = jointSpring;
                        sJoint.damper = jointDamping;
                        sJoint.maxDistance = translateMax;
                        sJoint.minDistance = translateMin;
                        
                        /*
                        
                            damper	The damper force used to dampen the spring force.
                            maxDistance	The maximum distance between the bodies relative to their initial distance.
                            minDistance	The minimum distance between the bodies relative to their initial distance.
                            spring	The spring force used to keep the two objects together.
                            tolerance	The maximum allowed error between the current spring length and the length defined by minDistance and maxDistance.
                        */

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

                        JointSetupDone = true;
                        return true;
                    }
                    return false;
                }

                JointSetupDone = true;
                return true;
            }
            return false;
        }

        public new void UpdateJointSettings(float torque, float springPower, float dampingPower)
        {
            if (sJoint == null)
                return;

            sJoint.spring = springPower;
            sJoint.damper = dampingPower;
            sJoint.maxDistance = translateMax;
            sJoint.minDistance = translateMin;

            //currentForce = torque;
            
        }

        /// <summary>
        /// Called every FixedUpdate, reads next target position and updates the rotation/translation correspondingly.
        /// Marked for overhaul, use Motor instead.
        /// </summary>
        protected new void UpdatePosition()
        {
            Interpolator.Update(TimeWarp.fixedDeltaTime);

            float targetPos = Interpolator.GetPosition();
            float currentPos = GetRealTranslation();

            // Idea is to use sJoint.connectedBody.ApplyForce() to move the attached parts while keeping sJoint.targetPosition at 0 translation.
            // Ideally we need to use PID to control piston position

            //torqueTweak should be our max possible force maybe?

            if(currentPos <= targetPos)
            {
                //we need to increase force

            }
            
        }

    }
}
