using System.Collections.Generic;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.Types;
using BeatmapEditor3D.Views;
using EditorEnhanced.Misc;
using EditorEnhanced.UI.Components;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI;

public sealed class ScrollableYourInput : IInitializable
{
   private readonly DiContainer _container;
   private readonly EditorViewLocator _viewLocator;

   public ScrollableYourInput(EditorViewLocator viewLocator, DiContainer container)
   {
      _viewLocator = viewLocator;
      _container = container;
   }

   public void Initialize()
   {
      if (!_viewLocator.TryGetEditObjectView(out var views)) return;

      ConfigureBaseObject(views._noteDataView);
      ConfigureBaseObject(views._bombDataView);
      ConfigureObstacle(views._obstacleDataView);
      ConfigureArc(views._arcDataView);
      ConfigureChain(views._chainDataView);
      ConfigureBasicEvent(views._basicEventDataView);
      ConfigureEventBoxGroup(views._eventBoxGroupDataView);
      ConfigureLightColor(views._lightColorDataView);
      ConfigureLightRotation(views._lightRotationDataView);
      ConfigureLightTranslation(views._lightTranslationDataView);
      ConfigureFloatFx(views._floatFxDataView);

      if (_viewLocator.TryGetEventBoxView(out var eventBoxView)) ConfigureEventBox(eventBoxView);
   }

   private void ConfigureBaseObject(BaseBeatmapObjectView view)
   {
      Configure(
         new FloatInputRegistration(
            view._beatInputFieldValidator,
            CustomPrecisions.TimePrecisionFloat,
            validatorType: FloatInputFieldValidator.ValidatorType.Max,
            max: 0f));
      Configure(
         new IntInputRegistration(
            view._columnInputFieldValidator,
            CustomPrecisions.NoPrecisionInt,
            IntInputFieldValidator.ValidatorType.Clamp,
            0,
            3),
         new IntInputRegistration(
            view._rowInputFieldValidator,
            CustomPrecisions.NoPrecisionInt,
            IntInputFieldValidator.ValidatorType.Clamp,
            0,
            2));
   }

   private void ConfigureObstacle(ObstacleDataView view)
   {
      ConfigureBaseObject(view);
      Configure(new FloatInputRegistration(view._durationInputField, CustomPrecisions.TimePrecisionFloat));
      Configure(
         new IntInputRegistration(
            view._widthInputField,
            CustomPrecisions.NoPrecisionInt,
            IntInputFieldValidator.ValidatorType.Clamp,
            1,
            4),
         new IntInputRegistration(
            view._heightInputField,
            CustomPrecisions.NoPrecisionInt,
            IntInputFieldValidator.ValidatorType.Clamp,
            1,
            5));
   }

   private void ConfigureArc(ArcDataView view)
   {
      ConfigureBaseObject(view);
      Configure(
         new FloatInputRegistration(view._tailBeatInputField, CustomPrecisions.TimePrecisionFloat),
         new FloatInputRegistration(view._controlPointInputField, CustomPrecisions.PercentPrecisionFloat, 0.01f),
         new FloatInputRegistration(
            view._tailControlPointInputField,
            CustomPrecisions.PercentPrecisionFloat,
            0.01f));
      Configure(
         new IntInputRegistration(
            view._tailColumnInputField,
            CustomPrecisions.NoPrecisionInt,
            IntInputFieldValidator.ValidatorType.Clamp,
            0,
            3),
         new IntInputRegistration(
            view._tailRowInputField,
            CustomPrecisions.NoPrecisionInt,
            IntInputFieldValidator.ValidatorType.Clamp,
            0,
            2));
   }

   private void ConfigureChain(ChainDataView view)
   {
      ConfigureBaseObject(view);
      Configure(
         new FloatInputRegistration(
            view._tailBeatInputField,
            CustomPrecisions.TimePrecisionFloat,
            validatorType: FloatInputFieldValidator.ValidatorType.Max,
            max: 0f),
         new FloatInputRegistration(
            view._squishInputField,
            CustomPrecisions.PercentPrecisionFloat,
            0.01f,
            FloatInputFieldValidator.ValidatorType.Max,
            max: Mathf.Epsilon));
      Configure(
         new IntInputRegistration(
            view._tailColumnInputField,
            CustomPrecisions.NoPrecisionInt,
            IntInputFieldValidator.ValidatorType.Clamp,
            0,
            3),
         new IntInputRegistration(
            view._tailRowInputField,
            CustomPrecisions.NoPrecisionInt,
            IntInputFieldValidator.ValidatorType.Clamp,
            0,
            2),
         new IntInputRegistration(
            view._slicesInputField,
            CustomPrecisions.NoPrecisionInt,
            IntInputFieldValidator.ValidatorType.Max,
            max: 1));
   }

   private void ConfigureBasicEvent(BasicEventDataView view)
   {
      Configure(
         new FloatInputRegistration(
            view._beatInputFieldValidator,
            CustomPrecisions.TimePrecisionFloat,
            validatorType: FloatInputFieldValidator.ValidatorType.Max,
            max: 0f),
         new FloatInputRegistration(
            view._floatValueInput,
            LightColorEventHelper._precisions,
            0.1f,
            FloatInputFieldValidator.ValidatorType.Max,
            max: 0f));
      Configure(
         new IntInputRegistration(
            view._intValueInput,
            CustomPrecisions.NoPrecisionInt,
            IntInputFieldValidator.ValidatorType.Max,
            max: 0));
   }

   private void ConfigureEventBoxGroup(EventBoxGroupDataView view)
   {
      Configure(
         new FloatInputRegistration(
            view._beatInputFieldValidator,
            CustomPrecisions.TimePrecisionFloat,
            validatorType: FloatInputFieldValidator.ValidatorType.Max,
            max: 0f));
   }

   private void ConfigureLightColor(LightColorDataView view)
   {
      Configure(
         new FloatInputRegistration(
            view._beatInputFieldValidator,
            CustomPrecisions.TimePrecisionFloat,
            validatorType: FloatInputFieldValidator.ValidatorType.Max,
            max: 0f),
         new FloatInputRegistration(
            view._valueInput,
            LightColorEventHelper._precisions,
            validatorType: FloatInputFieldValidator.ValidatorType.Max,
            max: 0f),
         new FloatInputRegistration(
            view._strobeBrightnessInput,
            LightColorEventHelper._precisions,
            validatorType: FloatInputFieldValidator.ValidatorType.Max,
            max: 0f));
      Configure(
         new IntInputRegistration(
            view._strobeFrequencyInput,
            CustomPrecisions.NoPrecisionInt,
            IntInputFieldValidator.ValidatorType.Max,
            max: 0));
   }

   private void ConfigureLightRotation(LightRotationDataView view)
   {
      Configure(
         new FloatInputRegistration(
            view._beatInputFieldValidator,
            CustomPrecisions.TimePrecisionFloat,
            validatorType: FloatInputFieldValidator.ValidatorType.Max,
            max: 0f),
         new FloatInputRegistration(view._valueInput, ModifyHoveredLightRotationDeltaRotationCommand._precisions));
      Configure(
         new IntInputRegistration(
            view._loopsInput,
            CustomPrecisions.NoPrecisionInt,
            IntInputFieldValidator.ValidatorType.Max,
            max: 0));
   }

   private void ConfigureLightTranslation(LightTranslationDataView view)
   {
      Configure(
         new FloatInputRegistration(
            view._beatInputFieldValidator,
            CustomPrecisions.TimePrecisionFloat,
            validatorType: FloatInputFieldValidator.ValidatorType.Max,
            max: 0f),
         new FloatInputRegistration(
            view._valueInput,
            ModifyHoveredLightTranslationDeltaTranslationCommand._precisions));
   }

   private void ConfigureFloatFx(FloatFxDataView view)
   {
      Configure(
         new FloatInputRegistration(
            view._beatInputFieldValidator,
            CustomPrecisions.TimePrecisionFloat,
            validatorType: FloatInputFieldValidator.ValidatorType.Max,
            max: 0f),
         new FloatInputRegistration(view._valueInput, ModifyHoveredFloatFxDeltaValueCommand._precisions));
   }

   private void ConfigureEventBox(EventBoxView view)
   {
      Configure(
         new FloatInputRegistration(
            view._beatDistributionInput,
            CustomPrecisions.TimePrecisionFloat,
            validatorType: FloatInputFieldValidator.ValidatorType.Max),
         new FloatInputRegistration(
            view._brightnessDistributionView._brightnessDistributionParamInput,
            LightColorEventHelper._precisions,
            validatorType: FloatInputFieldValidator.ValidatorType.None),
         new FloatInputRegistration(
            view._rotationDistributionView._rotationDistributionParamInput,
            ModifyHoveredLightRotationDeltaRotationCommand._precisions,
            validatorType: FloatInputFieldValidator.ValidatorType.None),
         new FloatInputRegistration(
            view._gapDistributionView._translationDistributionParamInput,
            ModifyHoveredLightTranslationDeltaTranslationCommand._precisions,
            validatorType: FloatInputFieldValidator.ValidatorType.None),
         new FloatInputRegistration(
            view._fxDistributionView._fxDistributionParamInput,
            ModifyHoveredFloatFxDeltaValueCommand._precisions,
            validatorType: FloatInputFieldValidator.ValidatorType.None));
      Configure(
         new IntInputRegistration(view._indexFilterView._groupingValidator, CustomPrecisions.NoPrecisionInt),
         new IntInputRegistration(
            view._indexFilterView._param0Input,
            CustomPrecisions.NoPrecisionInt,
            IntInputFieldValidator.ValidatorType.Max,
            max: 1),
         new IntInputRegistration(
            view._indexFilterView._param1Input,
            CustomPrecisions.NoPrecisionInt,
            IntInputFieldValidator.ValidatorType.Max),
         new IntInputRegistration(view._indexFilterView._randomSeedValidator, CustomPrecisions.NoPrecisionInt),
         new IntInputRegistration(
            view._indexFilterView._limitValidator,
            CustomPrecisions.PercentPrecisionInt,
            IntInputFieldValidator.ValidatorType.Clamp,
            0,
            100));
   }

   private void Configure(params IntInputRegistration[] registrations)
   {
      foreach (var registration in registrations)
      {
         var component = registration.Component;
         Object.Destroy(component.gameObject.GetComponent<IntInputFieldValidatorChangeOnScroll>());
         var scrollable = _container.InstantiateComponent<ScrollableInputInt>(component.gameObject);
         scrollable.PrecisionDelta = registration.Precision;
         scrollable.multiplier = registration.Multiplier;

         if (registration.ValidatorType.HasValue) component._validatorType = registration.ValidatorType.Value;
         if (registration.Min.HasValue) component._min = registration.Min.Value;
         if (registration.Max.HasValue) component._max = registration.Max.Value;
      }
   }

   private void Configure(params FloatInputRegistration[] registrations)
   {
      foreach (var registration in registrations)
      {
         var component = registration.Component;
         Object.Destroy(component.gameObject.GetComponent<FloatInputFieldValidatorChangeOnScroll>());
         var scrollable = _container.InstantiateComponent<ScrollableInputFloat>(component.gameObject);
         scrollable.PrecisionDelta = registration.Precision;
         scrollable.multiplier = registration.Multiplier;

         if (registration.ValidatorType.HasValue) component._validatorType = registration.ValidatorType.Value;
         if (registration.Min.HasValue) component._min = registration.Min.Value;
         if (registration.Max.HasValue) component._max = registration.Max.Value;
      }
   }

   private readonly struct IntInputRegistration
   {
      public IntInputRegistration(
         IntInputFieldValidator component,
         Dictionary<PrecisionType, int> precision,
         IntInputFieldValidator.ValidatorType? validatorType = null,
         int? min = null,
         int? max = null,
         float multiplier = 1f)
      {
         Component = component;
         Precision = precision;
         ValidatorType = validatorType;
         Min = min;
         Max = max;
         Multiplier = multiplier;
      }

      public IntInputFieldValidator Component { get; }
      public Dictionary<PrecisionType, int> Precision { get; }
      public IntInputFieldValidator.ValidatorType? ValidatorType { get; }
      public int? Min { get; }
      public int? Max { get; }
      public float Multiplier { get; }
   }

   private readonly struct FloatInputRegistration
   {
      public FloatInputRegistration(
         FloatInputFieldValidator component,
         Dictionary<PrecisionType, float> precision,
         float multiplier = 1f,
         FloatInputFieldValidator.ValidatorType? validatorType = null,
         float? min = null,
         float? max = null)
      {
         Component = component;
         Precision = precision;
         Multiplier = multiplier;
         ValidatorType = validatorType;
         Min = min;
         Max = max;
      }

      public FloatInputFieldValidator Component { get; }
      public Dictionary<PrecisionType, float> Precision { get; }
      public float Multiplier { get; }
      public FloatInputFieldValidator.ValidatorType? ValidatorType { get; }
      public float? Min { get; }
      public float? Max { get; }
   }
}
