using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using InfernalRobotics.Control;
using System;


namespace InfernalRobotics.Gui
{
    public class ServoHighlighter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {

        public IServo servo;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (servo != null)
                servo.Highlight = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (servo != null)
                servo.Highlight = false;
        }

        public void OnDestroy()
        {
            if (servo != null)
                servo.Highlight = false;
        }
    }
}
