using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using KSP.UI;

namespace InfernalRobotics.Gui
{
    public class BasicTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public string tooltipText;
        public const float TOOLTIP_DELAY = 0.5f;
        public const float TOOLTIP_DISPLAY_TIME = 2.5f;
        public const float TOOLTIP_FADE_TIME = 0.1f;
        public Vector2 tooltipOffset = new Vector2(-15, 15);

        private GameObject tooltipPanel;
        private CanvasGroupFader tooltipPanelFader;
        private float tooltipTime = 0f;

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!UIAssetsLoader.allPrefabsReady)
                return;

            if(!tooltipPanel)
            {
                tooltipPanel = GameObject.Instantiate(UIAssetsLoader.basicTooltipPrefab);
                tooltipPanel.transform.SetParent(UIMasterController.Instance.appCanvas.transform, false);
                tooltipPanel.GetComponent<CanvasGroup>().alpha = 0f;
                tooltipPanel.GetComponent<CanvasGroup>().blocksRaycasts = false;

                tooltipPanelFader = tooltipPanel.AddComponent<CanvasGroupFader>();
            }
            
            var panelRectTransform = tooltipPanel.transform as RectTransform;
            
            tooltipPanel.GetChild("Text").GetComponent<Text>().text = tooltipText;
            tooltipTime = 0f;
            
            Vector2 localPointerPosition;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(tooltipPanel.transform.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPointerPosition))
            {
                panelRectTransform.localPosition = localPointerPosition - tooltipOffset;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            DestroyToolTip();
            tooltipTime = 0f;
        }

        public void DestroyToolTip()
        {
            if(tooltipPanel)
            {
                tooltipPanel.DestroyGameObjectImmediate();
                tooltipPanel = null;
                tooltipPanelFader = null;
            }
        }

        public void OnDestroy()
        {
            DestroyToolTip();
        }

        public void Update()
        {
            if(tooltipPanel)
            {
                if (!tooltipPanelFader.IsFading)
                {
                    //update position on mouse move and handle the timers
                    if (tooltipTime >= TOOLTIP_DELAY && tooltipTime < (TOOLTIP_DELAY + TOOLTIP_DISPLAY_TIME))
                    {
                        //tooltipPanel.GetComponent<CanvasGroup>().alpha = 1f;
                        tooltipPanelFader.FadeTo(1f, TOOLTIP_FADE_TIME);
                    }

                    if (tooltipTime >= (TOOLTIP_DELAY + TOOLTIP_DISPLAY_TIME))
                    {
                        //tooltipPanel.GetComponent<CanvasGroup>().alpha = 0f;
                        tooltipPanelFader.FadeTo(0f, TOOLTIP_FADE_TIME, DestroyToolTip);
                    }
                }

                var panelRectTransform = tooltipPanel.transform as RectTransform;

                Vector2 localPointerPosition;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(tooltipPanel.transform.parent as RectTransform, Input.mousePosition, UIMasterController.Instance.uiCamera, out localPointerPosition))
                {
                    panelRectTransform.localPosition = localPointerPosition - tooltipOffset;
                }

                tooltipTime += Time.deltaTime;
            }
        }
    }
}
