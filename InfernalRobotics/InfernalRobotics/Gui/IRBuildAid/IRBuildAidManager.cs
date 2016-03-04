using InfernalRobotics.Command;
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

        public static IRBuildAidManager Instance 
        {
            get { return instance;}
        }

        public Dictionary<IServo, LinePrimitive> lines;

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
                currentRange.currentPosition = s.Mechanism.Position;
                currentRange.defaultPosition = s.Mechanism.DefaultPosition;
            }
            else
            {
                var currentRange = (BasicInterval)lines [s];
                currentRange.length = (s.Mechanism.MaxPositionLimit - s.Mechanism.MinPositionLimit);

                currentRange.lineVector = currentRange.transform.forward * currentRange.length;
                currentRange.offset = s.Mechanism.MinPositionLimit;
                currentRange.currentPosition = s.Mechanism.Position;
                currentRange.defaultPosition = s.Mechanism.DefaultPosition;
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

                var c = new Color (1, 1, 0, 0.5f);
                aid.UpdateColor (c);
                aid.UpdateWidth (0.05f);
                aid.arcAngle = (s.Mechanism.MaxPositionLimit - s.Mechanism.MinPositionLimit);
                aid.offsetAngle = s.Mechanism.MinPositionLimit;
                aid.currentPosition = s.Mechanism.Position;
                aid.defaultPosition = s.Mechanism.DefaultPosition;

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
                aid.currentPosition = s.Mechanism.Position;
                aid.defaultPosition = s.Mechanism.DefaultPosition;

                var c = new Color (1, 1, 0, 0.5f);
                aid.UpdateColor (c);
                aid.UpdateWidth (0.05f);

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
                aid.enabled = !aid.enabled;
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
                        {
                            if (lines.ContainsKey(servos [0]))
                            {
                                ToggleServoRange (servos [0]);
                            }
                            else
                            {
                                DrawServoRange (servos [0]);
                            }    
                        }

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

