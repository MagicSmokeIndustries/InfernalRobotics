using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System;

namespace InfernalRobotics.Gui
{

    public class ServoDropHandler : MonoBehaviour, IDropHandler
    {
        public void OnDrop(PointerEventData eventData)
        {
            var dropedObject = eventData.pointerDrag;

            //here the group ordering logic for persistence will go in IR

            Debug.Log("Servo OnDrop: " + dropedObject.name);
        }
    }

}