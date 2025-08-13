using System.Collections.Generic;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Types;
using BeatmapEditor3D.Views;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Zenject;

namespace EditorEnhanced.UI.Components;

public class ScrollableInputInt : MonoBehaviour, IScrollHandler
{
    public float multiplier = 1.0f;
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

    [Inject] private readonly BeatmapState _beatmapState;
    private IntInputFieldValidator _validator;

    public void Awake()
    {
        _validator = GetComponent<IntInputFieldValidator>();
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (!Keyboard.current.altKey.isPressed) return;
        _validator.SetValue(_validator.value + (int)(Mathf.Sign(eventData.scrollDelta.y) *
                                                     PrecisionDelta[_beatmapState.scrollPrecision]* multiplier));
    }
}