using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EditorEnhanced.UI;

public class ScrollableInput : MonoBehaviour, IScrollHandler
{
    public Action<float> OnScrollAction;

    public void OnScroll(PointerEventData eventData)
    {
        if (!Keyboard.current.altKey.isPressed) return;
        OnScrollAction?.Invoke(Mathf.Sign(eventData.scrollDelta.y));
    }
}