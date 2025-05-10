using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace EditorEnhanced.UI;

public class ScrollableInput : MonoBehaviour, IScrollHandler
{
    public Action<float> OnScrollAction;

    public void OnScroll(PointerEventData eventData)
    {
        OnScrollAction?.Invoke(Mathf.Sign(eventData.scrollDelta.y));
    }
}