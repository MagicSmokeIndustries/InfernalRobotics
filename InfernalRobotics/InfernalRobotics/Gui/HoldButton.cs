using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace InfernalRobotics.Gui
{
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public delegate void callOnDown();
        public delegate void callOnUp();
        public delegate void callEachUpdate();

        public callOnUp callbackOnUp;
        public callOnDown callbackOnDown;
        public callEachUpdate updateHandler;
        public bool isPressed = false;
        
        public void OnPointerDown(PointerEventData eventData)
        {
            callbackOnDown();
            isPressed = true;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            callbackOnUp();
            isPressed = false;
        }

        public void Update()
        {
            if(updateHandler!=null && isPressed)
            {
                updateHandler();
            }
        }
    }
}
