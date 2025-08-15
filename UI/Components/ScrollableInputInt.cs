using System.Collections.Generic;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Types;
using EditorEnhanced.Misc;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Zenject;

namespace EditorEnhanced.UI.Components;

public class ScrollableInputInt : MonoBehaviour, IScrollHandler
{
   public float multiplier = 1.0f;

   [Inject] private readonly BeatmapState _beatmapState;
   private IntInputFieldValidator _validator;

   public Dictionary<PrecisionType, int> PrecisionDelta = CustomPrecisions.NoPrecisionInt;

   public void Awake()
   {
      _validator = GetComponent<IntInputFieldValidator>();
   }

   public void OnScroll(PointerEventData eventData)
   {
      if (!Keyboard.current.altKey.isPressed) return;
      _validator.SetValue(
         _validator.value
         + (int)(Mathf.Sign(eventData.scrollDelta.y) * PrecisionDelta[_beatmapState.scrollPrecision] * multiplier));
   }
}