using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace InfernalRobotics.Gui
{
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public delegate void callOnDown();
        public delegate void callOnUp();

        public callOnUp callbackOnUp;
        public callOnDown callbackOnDown;

        public void OnPointerDown(PointerEventData eventData)
        {
            callbackOnDown();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            callbackOnUp();
        }


    }
}
