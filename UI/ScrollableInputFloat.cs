using System.Collections.Generic;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Types;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace EditorEnhanced.UI;

public class ScrollableInputFloat : MonoBehaviour, IScrollHandler
{
    public FloatInputFieldValidator fieldValidator;
    public BeatmapState BeatmapState;

    public Dictionary<PrecisionType, float> PrecisionDelta = new()
    {
        {
            PrecisionType.Ultra, 0.01f
        },
        {
            PrecisionType.High, 0.1f
        },
        {
            PrecisionType.Standard, 0.5f
        },
        {
            PrecisionType.Low, 1f
        }
    };

    public void OnScroll(PointerEventData eventData)
    {
        if (!Keyboard.current.altKey.isPressed) return;
        fieldValidator.SetValue(fieldValidator.value + Mathf.Sign(eventData.scrollDelta.y) *
            PrecisionDelta[BeatmapState.scrollPrecision]);
    }
}