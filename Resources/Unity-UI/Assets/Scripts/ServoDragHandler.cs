using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace InfernalRobotics.Gui
{
    /// <summary>
    /// Will only handle the visual aspect of the drag and drop. Actual IR logic should be put somewhere in OnDrop
    /// </summary>
    public class ServoDragHandler: GroupDragHandler
    {
        private GameObject lastPointerOver;

        public override float GetDraggedItemHeight()
        {
            return draggedItem.GetComponent<HorizontalLayoutGroup>().preferredHeight;
        }

        public override void OnBeginDrag(PointerEventData eventData) 
        {
            draggedItem = this.transform.parent.gameObject;

            base.OnBeginDrag(eventData);

            lastPointerOver = eventData.pointerEnter;
        }

        public override void OnDrag(PointerEventData eventData)
        {
            draggedItem.transform.position = eventData.position - dragHandleOffset;

            //we don't want to change siblings while we are still animating
            if (animationHelper.isHeightActive)
                return;

            if(eventData.pointerEnter.GetComponent<ServoDropHandler>() != null)
            {
                dropZone = eventData.pointerEnter.transform;
                placeholder.transform.SetParent(dropZone);
            }

            var currentSiblingIndex = placeholder.transform.GetSiblingIndex();
            var newSiblingIndex = dropZone.childCount - 1;

            for (int i = 0; i < dropZone.childCount; i++)
            {
                var child = dropZone.GetChild(i);
                if (eventData.position.y > child.position.y)
                {
                    newSiblingIndex = i;

                    if (currentSiblingIndex < newSiblingIndex)
                        newSiblingIndex--;

                    break;
                }
            }

            if (newSiblingIndex != placeholder.transform.GetSiblingIndex())
            {
                placeholder.transform.SetSiblingIndex(newSiblingIndex);
                animationHelper.AnimateHeight(PLACEHOLDER_MIN_HEIGHT, startingHeight, 0.1f);
            }
        }
    }

}
