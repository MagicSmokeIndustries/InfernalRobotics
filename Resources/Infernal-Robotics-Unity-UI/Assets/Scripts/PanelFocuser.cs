using UnityEngine;
using UnityEngine.EventSystems;

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
