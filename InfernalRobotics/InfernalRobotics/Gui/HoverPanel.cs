using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace InfernalRobotics.Gui
{
    public class HoverPanel : MonoBehaviour
    {
        internal Transform anchor;
        internal Vector3 offset;

        internal Vector3 lastAnchorPosition;
        
        public void Start()
        {
            lastAnchorPosition = anchor.position;
        }

        public void Update()
        {
            if(anchor.position != lastAnchorPosition)
            {
                lastAnchorPosition = anchor.position;
                this.transform.position = lastAnchorPosition + offset;
            }
            
        }
    }
}
