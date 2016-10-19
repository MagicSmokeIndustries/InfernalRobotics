using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System;
using InfernalRobotics.Command;

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
                Logger.Log("[ServoDropHandler]: dropped object missing ServoDragHandler", Logger.Level.Debug);
                return;
            }
        }

        public void onServoDrop(ServoDragHandler dragHandler)
        {
            
            var servoUIControls = dragHandler.draggedItem;
            int insertAt = dragHandler.placeholder.transform.GetSiblingIndex();

            foreach (var pair in WindowManager._servoUIControls)
            {
                if (pair.Value == servoUIControls)
                {
                    var s = pair.Key;
                    var oldGroupIndex = ServoController.Instance.ServoGroups.FindIndex(g => g.Servos.Contains(s));

                    if (oldGroupIndex < 0)
                    {
                        //error
                        return;
                    }

                    var newGroupIndex = dragHandler.dropZone.parent.GetSiblingIndex();
                    ServoController.MoveServo(ServoController.Instance.ServoGroups[oldGroupIndex], ServoController.Instance.ServoGroups[newGroupIndex], s);
                    WindowManager.guiRebuildPending = true;
                    break;
                }
            }
        }

    }

}