using InfernalRobotics.Command;
using InfernalRobotics.Control;
using InfernalRobotics.Control.Servo;
using InfernalRobotics.Module;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace InfernalRobotics.Gui.IRBuildAid
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class IRMouseGrabber : MonoBehaviour
    {
        IServo currentGrabbedServo;
        public bool isGrabbing = false;
        Vector3 startingMousePos = Vector3.zero;
        float startingServoPos = 0;

        public void Update()
        {
            if (!HighLogic.LoadedSceneIsEditor)
                return;
            
            if(Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.LeftControl)) //Returns true during the frame the user pressed the given mouse button. 
                                            //It will not return true until the user has released the mouse button and pressed it again.
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
                        currentGrabbedServo = servos[0];
                        isGrabbing = true;
                        startingMousePos = Input.mousePosition;
                        startingServoPos = currentGrabbedServo.Mechanism.Position;
                    }

                }
            }

            if(Input.GetMouseButtonUp(0)) //Returns true during the frame the user releases the given mouse button.
            {
                currentGrabbedServo = null;
                isGrabbing = false;
            }

            if(isGrabbing || currentGrabbedServo != null)
            {
                var deltaPos = (Input.mousePosition.y - startingMousePos.y) / (Screen.height * 0.25f);

                var newPos = startingServoPos + deltaPos * (currentGrabbedServo.Mechanism.MaxPositionLimit - currentGrabbedServo.Mechanism.MinPositionLimit);

                newPos = Mathf.Clamp (newPos, currentGrabbedServo.Mechanism.MinPositionLimit, currentGrabbedServo.Mechanism.MaxPositionLimit);

                currentGrabbedServo.Motor.MoveTo (newPos);
            }

        }

    }
}

