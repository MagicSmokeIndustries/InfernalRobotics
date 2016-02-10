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

        public void DrawServoRange (IServo s)
        {
            if(lines.ContainsKey(s))
            {
                lines [s].enabled = true;
                return;
            }

            if (s.RawServo.rotateJoint)
            {
                var obj = new GameObject ("Servo IRBuildAid object");
                //obj.layer = s.RawServo.gameObject.layer;
                obj.layer = 1;
                obj.transform.position = s.RawServo.part.transform.position;
                obj.transform.parent = s.RawServo.part.transform;
                //var v = Vector3.Cross (s.RawServo.transform.TransformDirection(s.RawServo.rotateAxis), s.RawServo.transform.up);
                obj.transform.rotation = Quaternion.LookRotation(s.RawServo.transform.TransformDirection(s.RawServo.rotateAxis), s.RawServo.transform.up);

                /*var meshedRange = obj.AddComponent<MeshedRangeIndicator> ();
                meshedRange.angleRange = (s.Mechanism.MaxPositionLimit - s.Mechanism.MinPositionLimit);
                meshedRange.angleStart = s.Mechanism.MinPositionLimit;
                meshedRange.dist_min = 0.5f;
                meshedRange.dist_max = 2f;
                meshedRange.layer = obj.layer;
                meshedRange.meshColor = new Color (0f, 1f, 1f, 0.25f); //transparent yellow
                meshedRange.normalVector = Vector3.Cross (s.RawServo.rotateAxis, s.RawServo.part.transform.forward);
                if (meshedRange.normalVector == Vector3.zero)
                    meshedRange.normalVector = Vector3.up;
                */

                var aid = obj.AddComponent<CircularInterval> ();
                var c = new Color (1, 1, 0, 0.5f);
                aid.transform.parent = obj.transform;
                aid.transform.rotation = obj.transform.rotation;
                aid.width = 0.05f;
                aid.UpdateColor (c);
                aid.UpdateWidth (0.05f);
                aid.arcAngle = (s.Mechanism.MaxPositionLimit - s.Mechanism.MinPositionLimit);
                aid.offsetAngle = s.Mechanism.MinPositionLimit;

                aid.enabled = true;

                lines.Add (s, aid);
            }
            else
            {
                var obj = new GameObject ("Servo IRBuildAid object");
                obj.layer = gameObject.layer;
                var aid = obj.AddComponent<BasicInterval> ();

                aid.transform.position = s.RawServo.part.transform.position;

                aid.length = (s.Mechanism.MaxPositionLimit - s.Mechanism.MinPositionLimit);

                //must change it 
                aid.lineVector = s.RawServo.part.transform.forward;

                var servoTranslateAxis = s.RawServo.translateAxis;
                var v1 = Vector3.Cross (servoTranslateAxis, s.RawServo.part.transform.forward);

                aid.mainStartPoint = s.RawServo.part.transform.right;

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
                            DrawServoRange (servos [0]);
                        }

                    }
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

