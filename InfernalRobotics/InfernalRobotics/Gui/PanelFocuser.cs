using UnityEngine;
using UnityEngine.EventSystems;

namespace InfernalRobotics.Gui
{

    public class PanelFocuser : MonoBehaviour, IPointerDownHandler
    {

        private RectTransform panel;

        void Awake()
        {
            panel = GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData data)
        {
            panel.SetAsLastSibling();
        }
    }
}