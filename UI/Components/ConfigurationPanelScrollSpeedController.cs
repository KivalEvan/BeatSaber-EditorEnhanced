using HMUI;
using HarmonyLib;
using UnityEngine;

namespace EditorEnhanced.UI.Components;

internal sealed class ConfigurationPanelScrollSpeedController : MonoBehaviour
{
   private const float SpeedMultiplier = 1.5f;
   private static readonly System.Reflection.FieldInfo ScrollSpeedField =
      AccessTools.Field(typeof(ScrollView), "_joystickScrollSpeed");

   private float _originalSpeed;
   private ScrollView _scrollView;
   private bool _speedApplied;

   public void Configure(ScrollView scrollView)
   {
      _scrollView = scrollView;
      if (isActiveAndEnabled) ApplySpeed();
   }

   private void OnEnable()
   {
      ApplySpeed();
   }

   private void OnDisable()
   {
      if (!_speedApplied || _scrollView == null) return;

      ScrollSpeedField?.SetValue(_scrollView, _originalSpeed);
      _speedApplied = false;
   }

   private void ApplySpeed()
   {
      if (_speedApplied || _scrollView == null) return;

      if (ScrollSpeedField?.GetValue(_scrollView) is not float currentSpeed) return;

      _originalSpeed = currentSpeed;
      ScrollSpeedField.SetValue(_scrollView, _originalSpeed * SpeedMultiplier);
      _speedApplied = true;
   }
}
