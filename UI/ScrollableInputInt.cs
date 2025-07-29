using System;
using System.Collections.Generic;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Types;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace EditorEnhanced.UI;

public class ScrollableInputInt : MonoBehaviour, IScrollHandler
{
    public IntInputFieldValidator fieldValidator;
    public BeatmapState BeatmapState;

    public Dictionary<PrecisionType, int> PrecisionDelta = new()
    {
        {
            PrecisionType.Ultra, 1
        },
        {
            PrecisionType.High, 2
        },
        {
            PrecisionType.Standard, 5
        },
        {
            PrecisionType.Low, 10
        }
    };

    public void OnScroll(PointerEventData eventData)
    {
        if (!Keyboard.current.altKey.isPressed) return;
        fieldValidator.SetValue(fieldValidator.value + (int)(Mathf.Sign(eventData.scrollDelta.y) *
                                                             PrecisionDelta[BeatmapState.scrollPrecision]));
    }
}