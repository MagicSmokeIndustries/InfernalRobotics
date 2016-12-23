﻿using InfernalRobotics.Command;
using InfernalRobotics.Control;
using InfernalRobotics.Control.Servo;
using InfernalRobotics.Utility;
using InfernalRobotics.Module;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace InfernalRobotics.Gui.IRBuildAid
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class IRBuildAidManager : MonoBehaviour
    {
        private static IRBuildAidManager instance;

        public static Color endPoint1Color = new Color(1f, 1f, 0, 0.5f);
        public static Color endPoint2Color = new Color(1f, 1f, 0, 0.5f);
        public static Color mainLineColor1 = new Color(0.88f, 0.7f, 0.188f, 0.7f);
        public static Color mainLineColor2 = new Color(0.7f, 0.5f, 0, 0.5f);
        public static Color presetPositionsColor = new Color(1f, 1f, 1f, 0.5f);

        public static Color currentPositionColor = new Color(0f, 1f, 0f, 0.5f);
        public static Color currentPositionLockedColor = new Color(1f, 0f, 0f, 0.5f);

        public static IRBuildAidManager Instance 
        {
            get { return instance;}
        }

        public static Dictionary<IServo, LinePrimitive> lines;

        private static bool hiddenStatus = false;

        public static bool isHidden {
            get
            {
                return hiddenStatus;
            }
            set
            {
                if (lines == null || lines.Count == 0)
                    return;

                foreach(var pair in lines)
                {
                    pair.Value.enabled = value;
                }
            }
        }

        public static void Reset()
        {
            if (lines == null || lines.Count == 0)
                return;

            /* remove lines */
            foreach (var pair in lines)
            {
                Destroy(pair.Value.gameObject);
            }
            lines.Clear();
        }

        public IRBuildAidManager ()
        {
            lines = new Dictionary<IServo, LinePrimitive> ();
            instance = this;
        }

        public void UpdateServoRange(IServo s)
        {
            if(!lines.ContainsKey(s))
                return;
            
            if (s.RawServo.rotateJoint) 
            {
                var currentRange = (CircularInterval)lines [s];

                currentRange.arcAngle = (s.Mechanism.MaxPositionLimit - s.Mechanism.MinPositionLimit);
                currentRange.offsetAngle = s.Mechanism.MinPositionLimit;
                currentRange.currentPosition = s.RawServo.Translator.ToInternalPos(s.Mechanism.Position);
                currentRange.defaultPosition = s.RawServo.Translator.ToInternalPos(s.Mechanism.DefaultPosition);

                if (s.RawServo.PresetPositions != null)
                    currentRange.SetPresetPositions(s.RawServo.PresetPositions);

                if (s.Motor.IsAxisInverted)
                {
                    currentRange.SetMainLineColors(mainLineColor2, mainLineColor1);
                }
                else
                {
                    currentRange.SetMainLineColors(mainLineColor1, mainLineColor2);
                }

                currentRange.currentPositionColor = s.RawServo.isMotionLock ? currentPositionLockedColor : currentPositionColor;
                currentRange.endPoint1Color = endPoint1Color;
                currentRange.endPoint2Color = endPoint2Color;
            }
            else
            {
                var currentRange = (BasicInterval)lines [s];
                currentRange.length = (s.Mechanism.MaxPositionLimit - s.Mechanism.MinPositionLimit);

                currentRange.lineVector = currentRange.transform.forward * currentRange.length;
                currentRange.offset = s.Mechanism.MinPositionLimit;
                currentRange.currentPosition = s.RawServo.Translator.ToInternalPos(s.Mechanism.Position);
                currentRange.defaultPosition = s.RawServo.Translator.ToInternalPos(s.Mechanism.DefaultPosition);

                if (s.RawServo.PresetPositions != null)
                    currentRange.SetPresetPositions(s.RawServo.PresetPositions);

                if (s.Motor.IsAxisInverted)
                {
                    currentRange.SetMainLineColors(mainLineColor2, mainLineColor1);
                }
                else
                {
                    currentRange.SetMainLineColors(mainLineColor1, mainLineColor2);
                }

                currentRange.currentPositionColor = s.RawServo.isMotionLock ? currentPositionLockedColor : currentPositionColor;
                currentRange.endPoint1Color = endPoint1Color;
                currentRange.endPoint2Color = endPoint2Color;
            }

            
        }

        public void DrawServoRange (IServo s)
        {
            if(lines.ContainsKey(s))
            {
                UpdateServoRange (s);
                lines [s].enabled = true;
                return;
            }

            if (s.RawServo.rotateJoint)
            {
                var obj = new GameObject ("Servo IRBuildAid object");
                //obj.layer = s.RawServo.gameObject.layer;
                obj.layer = 1;
                obj.transform.position = s.RawServo.FixedMeshTransform.position;
                obj.transform.parent = s.RawServo.FixedMeshTransform;

                obj.transform.rotation = Quaternion.LookRotation(s.RawServo.FixedMeshTransform.TransformDirection(-s.RawServo.rotateAxis), 
                                                                 s.RawServo.FixedMeshTransform.TransformDirection(s.RawServo.zeroUp));
                
                var aid = obj.AddComponent<CircularInterval> ();
                //CircularInterval uses Local Space
                aid.transform.parent = obj.transform;
                aid.transform.rotation = obj.transform.rotation;
                aid.width = 0.05f;

                aid.UpdateColor (mainLineColor1);

                if (s.Motor.IsAxisInverted)
                {
                    aid.SetMainLineColors(mainLineColor2, mainLineColor1);
                }
                else
                {
                    aid.SetMainLineColors(mainLineColor1, mainLineColor2);
                }

                aid.UpdateWidth (0.05f);
                aid.arcAngle = (s.Mechanism.MaxPositionLimit - s.Mechanism.MinPositionLimit);
                aid.offsetAngle = s.Mechanism.MinPositionLimit;
                aid.currentPosition = s.RawServo.Translator.ToInternalPos(s.Mechanism.Position);
                aid.defaultPosition = s.RawServo.Translator.ToInternalPos(s.Mechanism.DefaultPosition);

                aid.presetPositionsColor = presetPositionsColor;
                if (s.RawServo.PresetPositions != null)
                    aid.SetPresetPositions(s.RawServo.PresetPositions);

                aid.currentPositionColor = s.RawServo.isMotionLock ? currentPositionLockedColor : currentPositionColor;
                aid.endPoint1Color = endPoint1Color;
                aid.endPoint2Color = endPoint2Color;

                aid.enabled = true;

                lines.Add (s, aid);
            }
            else
            {
                var obj = new GameObject ("Servo IRBuildAid object");
                //obj.layer = s.RawServo.gameObject.layer;
                obj.layer = 1;
                obj.transform.position = s.RawServo.FixedMeshTransform.position;
                obj.transform.parent = s.RawServo.FixedMeshTransform;
                obj.transform.rotation = Quaternion.LookRotation(s.RawServo.FixedMeshTransform.TransformDirection(-s.RawServo.translateAxis), 
                                                                 s.RawServo.FixedMeshTransform.TransformDirection(s.RawServo.zeroUp));

                var aid = obj.AddComponent<BasicInterval> ();
                //BasicInterval uses worldSpace
                aid.transform.parent = obj.transform;
                aid.transform.rotation = obj.transform.rotation;
                aid.transform.position = obj.transform.position + obj.transform.right * 0.5f;
                aid.width = 0.05f;
                aid.length = (s.Mechanism.MaxPositionLimit - s.Mechanism.MinPositionLimit);

                aid.lineVector = aid.transform.forward * aid.length;

                aid.offset = s.Mechanism.MinPositionLimit;
                aid.currentPosition = s.RawServo.Translator.ToInternalPos(s.Mechanism.Position);
                aid.defaultPosition = s.RawServo.Translator.ToInternalPos(s.Mechanism.DefaultPosition);

                aid.UpdateColor(mainLineColor1);
                if (s.Motor.IsAxisInverted)
                {
                    aid.SetMainLineColors(mainLineColor2, mainLineColor1);
                }
                else
                {
                    aid.SetMainLineColors(mainLineColor1, mainLineColor2);
                }
                aid.UpdateWidth (0.05f);

                aid.presetPositionsColor = presetPositionsColor;

                if (s.RawServo.PresetPositions != null)
                    aid.SetPresetPositions(s.RawServo.PresetPositions);

                aid.currentPositionColor = s.RawServo.isMotionLock ? currentPositionLockedColor : currentPositionColor;
                aid.endPoint1Color = endPoint1Color;
                aid.endPoint2Color = endPoint2Color;

                lines.Add (s, aid);
            }
        }

        public void ToggleServoRange(IServo s)
        {
            if (lines == null || lines.Count == 0)
                return;

            LinePrimitive aid;
            if (lines.TryGetValue (s, out aid))
            {
                //aid.enabled = !aid.enabled;
                lines.Remove(s);
                aid.gameObject.DestroyGameObjectImmediate();
            }
            else
            {
                //Draw the lines 
                DrawServoRange (s);
            }
        }

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsEditor)
                return;

            if(ServoController.Instance != null)
            {
                if(Input.GetMouseButtonDown(2) && Input.GetKey(KeyCode.LeftShift)) 
                {
                    RaycastHit hit;
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                    if (Physics.Raycast (ray, out hit)) 
                    {
                        GameObject hitObject = hit.transform.gameObject;
                        if (hitObject == null)
                            return;

                        Part part = hitObject.GetComponentInParent<Part>();
                        if (part == null)
                            return;

                        var servos = part.ToServos ();

                        if (servos.Count > 0)
                            DrawServoRange (servos [0]);    
                    }
                }

                foreach (var pair in lines) 
                {
                    UpdateServoRange (pair.Key);
                }
            }
        }

        public void OnDestroy()
        {
            /* remove lines */
            foreach (var pair in lines)
            {
                Destroy (pair.Value.gameObject);
            }

        }
    }
}

