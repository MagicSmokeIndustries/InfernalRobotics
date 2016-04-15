using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using KSP.UI.Screens;
using KSP.UI;

namespace InfernalRobotics.Gui
{

    public class PanelDragger : MonoBehaviour, IPointerDownHandler, IDragHandler
    {

        private Vector2 pointerOffset;
        private RectTransform canvasRectTransform;
        private RectTransform panelRectTransform;

        void Awake()
        {
            Canvas canvas = UIMasterController.Instance.appCanvas;
            if (canvas != null)
            {
                canvasRectTransform = canvas.transform as RectTransform;
                panelRectTransform = transform.parent as RectTransform;
            }
        }

        public void OnPointerDown(PointerEventData data)
        {
            panelRectTransform.SetAsLastSibling();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(panelRectTransform, data.position, data.pressEventCamera, out pointerOffset);
        }

        public void OnDrag(PointerEventData data)
        {
            if (panelRectTransform == null)
                return;

            Vector2 pointerPostion = ClampToWindow(data);

            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRectTransform, pointerPostion, data.pressEventCamera, out localPointerPosition
            ))
            {
                panelRectTransform.localPosition = localPointerPosition - pointerOffset;
            }
        }

        Vector2 ClampToWindow(PointerEventData data)
        {
            Vector2 rawPointerPosition = data.position;

            float clampedX = Mathf.Clamp(rawPointerPosition.x, 0, Screen.width);
            float clampedY = Mathf.Clamp(rawPointerPosition.y, 0, Screen.height);

            Vector2 newPointerPosition = new Vector2(clampedX, clampedY);
            return newPointerPosition;
        }
    }
}