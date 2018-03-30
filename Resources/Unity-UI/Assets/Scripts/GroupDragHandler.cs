using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

namespace InfernalRobotics.Gui
{
    
    public class GroupDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Canvas mainCanvas;
        public Sprite background;

        protected GameObject draggedItem;
        protected Transform dropZone;
        protected Vector2 dragHandleOffset;
        protected Image draggedItemBG;
        protected int startingSiblingIndex = 0;

        protected GameObject placeholder;
        protected const float PLACEHOLDER_MIN_HEIGHT = 10f;

        protected UIAnimationHelper animationHelper;

        protected float startingHeight;

        protected void SetPlaceholderHeight(float newHeight)
        {
            placeholder.GetComponent<LayoutElement>().preferredHeight = newHeight;
        }

        protected void SetDraggedItemPosition(Vector2 newPosition)
        {
            var t = draggedItem.transform as RectTransform;
            t.position = newPosition;
        }

        public virtual float GetDraggedItemHeight()
        {
            return draggedItem.GetComponent<VerticalLayoutGroup>().preferredHeight;
        }

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            if(draggedItem == null)
                draggedItem = this.transform.parent.parent.gameObject; //need to get the whole line as dragged item
            dropZone = draggedItem.transform.parent;
            startingSiblingIndex = draggedItem.transform.GetSiblingIndex();
            dragHandleOffset = this.transform.position - draggedItem.transform.position;
            
            placeholder = new GameObject();
            placeholder.transform.SetParent(draggedItem.transform.parent);
            placeholder.transform.SetSiblingIndex(startingSiblingIndex);
            var rt = placeholder.AddComponent<RectTransform>();
            rt.pivot = Vector2.zero;

            var le = placeholder.AddComponent<LayoutElement>();
            le.preferredHeight = startingHeight = GetDraggedItemHeight();
            //le.flexibleWidth = 1;

            animationHelper = draggedItem.AddComponent<UIAnimationHelper>();
            animationHelper.SetHeight = SetPlaceholderHeight;
            animationHelper.SetPosition = SetDraggedItemPosition;

            animationHelper.AnimateHeight(le.preferredHeight, PLACEHOLDER_MIN_HEIGHT, 0.1f);

            var cg = draggedItem.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;

            draggedItemBG = draggedItem.AddComponent<Image>();
            draggedItemBG.sprite = background;
            draggedItemBG.type = Image.Type.Sliced;
            draggedItemBG.color = Color.white;
            draggedItemBG.fillCenter = true;

            draggedItem.transform.SetParent(mainCanvas.transform);

            Debug.Log("OnBeginDrag: draggedItem.name = " + draggedItem.name + ", dropZone.name" + dropZone.name);
        }

        public virtual void OnDrag(PointerEventData eventData)
        {
            draggedItem.transform.position = eventData.position - dragHandleOffset;

            //we don't want to change siblings while we are still animating
            if (animationHelper.isHeightActive)
                return;

            var currentSiblingIndex = placeholder.transform.GetSiblingIndex();
            var newSiblingIndex = dropZone.childCount-1;

            for (int i=0; i< dropZone.childCount; i++)
            {
                var child = dropZone.GetChild(i);
                if(eventData.position.y > child.position.y)
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

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            if (animationHelper.isHeightActive)
            {
                animationHelper.StopHeight();
            }
            RectTransform t = draggedItem.transform as RectTransform;
            RectTransform p = placeholder.transform as RectTransform;

            Vector2 newPosition = new Vector2(p.position.x, p.position.y - startingHeight + PLACEHOLDER_MIN_HEIGHT);

            if(p.sizeDelta.y > PLACEHOLDER_MIN_HEIGHT)
                newPosition = p.position;

            animationHelper.AnimatePosition(t.position, newPosition, 0.07f);
            animationHelper.AnimateHeight(placeholder.GetComponent<LayoutElement>().preferredHeight, startingHeight, 0.1f, OnEndDragAnimateEnd);
            
            Debug.Log("OnEndDrag");
        }

        protected void OnEndDragAnimateEnd()
        {
            var cg = draggedItem.GetComponent<CanvasGroup>();
            if (cg!= null)
            {
                cg.blocksRaycasts = true;
                Destroy(cg);
            }
                
            draggedItem.transform.SetParent(dropZone);
            draggedItem.transform.SetSiblingIndex(placeholder.transform.GetSiblingIndex());
            draggedItem = null;
            
            Destroy(placeholder);
            Destroy(animationHelper);
            Destroy(draggedItemBG);
        }
    }

}
