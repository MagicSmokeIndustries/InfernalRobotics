using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using InfernalRobotics.Command;

namespace InfernalRobotics.Gui
{
    /// <summary>
    /// Handles the IR logic of group drop
    /// </summary>
    public class GroupDropHandler : MonoBehaviour, IDropHandler
    {
        public void OnDrop(PointerEventData eventData)
        {
            var droppedObject = eventData.pointerDrag;
            var dragHandler = droppedObject.GetComponent<GroupDragHandler>();

            if(dragHandler == null)
            {
                Logger.Log("[GroupDropHandler] No GroupDragHandler on dropped object", Logger.Level.Debug);
                return;
            }

            onGroupDrop(dragHandler);

            Debug.Log("Group OnDrop: " + droppedObject.name);
        }

        public void onGroupDrop(GroupDragHandler dragHandler)
        {
            //here the group ordering logic for persistence will go in IR
            var groupUIControls = dragHandler.draggedItem;
            int insertAt = dragHandler.placeholder.transform.GetSiblingIndex();

            foreach (var pair in WindowManager._servoGroupUIControls)
            {
                if (pair.Value == groupUIControls)
                {
                    var g = pair.Key;

                    ServoController.Instance.ServoGroups.Remove(g);
                    ServoController.Instance.ServoGroups.Insert(insertAt, g);

                    break;
                }
            }
        }
    }

}