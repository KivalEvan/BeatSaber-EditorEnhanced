using System.Collections.Generic;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Types;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Zenject;

namespace EditorEnhanced.UI.Components;

public class ScrollableInputFloat : MonoBehaviour, IScrollHandler
{
    [Inject] private readonly BeatmapState _beatmapState;
    private FloatInputFieldValidator _validator;

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

    public void Awake()
    {
        _validator = GetComponent<FloatInputFieldValidator>();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!Keyboard.current.altKey.isPressed) return;
        _validator.SetValue(_validator.value +
                            Mathf.Sign(eventData.scrollDelta.y) * PrecisionDelta[_beatmapState.scrollPrecision]);
    }
}