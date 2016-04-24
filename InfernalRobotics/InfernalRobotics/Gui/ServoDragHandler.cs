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
        public override float GetDraggedItemHeight()
        {
            return draggedItem.GetComponent<HorizontalLayoutGroup>().preferredHeight;
        }

        public override void OnBeginDrag(PointerEventData eventData) 
        {
            draggedItem = this.transform.parent.gameObject;

            base.OnBeginDrag(eventData);
        }

        public override void OnDrag(PointerEventData eventData)
        {
            var rt = draggedItem.transform as RectTransform;

            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mainCanvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out localPointerPosition))
            {
                rt.localPosition = localPointerPosition - startingPosition;
            }

            //we don't want to change siblings while we are still animating
            if (animationHelper.isHeightActive)
                return;

            if(eventData.pointerEnter != null && eventData.pointerEnter.GetComponent<ServoDropHandler>() != null)
            {
                dropZone = eventData.pointerEnter.transform;
                placeholder.transform.SetParent(dropZone,false);
            }

            var currentSiblingIndex = placeholder.transform.GetSiblingIndex();
            var newSiblingIndex = dropZone.childCount - 1;

            for (int i = 0; i < dropZone.childCount; i++)
            {
                var child = dropZone.GetChild(i);
                if (localPointerPosition.y > child.position.y)
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
        protected override void OnEndDragAnimateEnd()
        {
            var servoDropHandler = dropZone.GetComponent<ServoDropHandler>();
            if (servoDropHandler != null)
            {
                servoDropHandler.onServoDrop(this);
            }

            base.OnEndDragAnimateEnd();
        }
    }

}
