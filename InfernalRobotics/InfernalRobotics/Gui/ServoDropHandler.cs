using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System;
using InfernalRobotics.Control;

namespace InfernalRobotics.Gui
{
    public class ServoDropHandler : MonoBehaviour, IDropHandler
    {
        public void OnDrop(PointerEventData eventData)
        {
            var dropedObject = eventData.pointerDrag;
            Debug.Log("Servo OnDrop: " + dropedObject.name);

            var dragHandler = dropedObject.GetComponent<ServoDragHandler>();
            if(dragHandler == null)
            {
                Logger.Log("ServoDropHandler: something odd was dropped here", Logger.Level.Debug);
                return;
            }

            var servoLine = dragHandler.draggedItem;

            
            //here the group ordering logic for persistence will go in IR

            //WindowManager should have the link to Servo object given the draggedItem object from the ServoDragHandler

            
        }
    }

}