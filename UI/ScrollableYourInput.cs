using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.Types;
using BeatmapEditor3D.Views;
using EditorEnhanced.UI.Components;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI;

public class ScrollableYourInput : IInitializable
{
   private static readonly Dictionary<PrecisionType, float> NoPrecisionFloat = new()
   {
      {
         PrecisionType.Ultra, 1f
      },
      {
         PrecisionType.High, 1f
      },
      {
         PrecisionType.Standard, 1f
      },
      {
         PrecisionType.Low, 1f
      }
   };

   private static readonly Dictionary<PrecisionType, int> NoPrecisionInt = new()
   {
      {
         PrecisionType.Ultra, 1
      },
      {
         PrecisionType.High, 1
      },
      {
         PrecisionType.Standard, 1
      },
      {
         PrecisionType.Low, 1
      }
   };

   private readonly DiContainer _container;
   private readonly EditBeatmapViewController _ebvc;

   public ScrollableYourInput(
      EditBeatmapViewController ebvc,
      DiContainer container)
   {
      _ebvc = ebvc;
      _container = container;
   }

   public void Initialize()
   {
      ApplyToNoteDataView();
      ApplyToBombDataView();
      ApplyToObstacleDataView();
      ApplyToArcDataView();
      ApplyToChainDataView();

      ApplyToBasicEventDataView();
      ApplyToEventBoxGroupDataView();
      ApplyToLightColorDataView();
      ApplyToLightRotationDataView();
      ApplyToLightTranslationDataView();
      ApplyToFloatFxDataView();

      ApplyToEventBoxView();
   }

   private void ApplyToBaseBeatmapObjectView(BaseBeatmapObjectView bbov)
   {
      ApplyScrollableFloatInput(bbov._beatInputFieldValidator, null);
      bbov._beatInputFieldValidator._validatorType = FloatInputFieldValidator.ValidatorType.Max;
      bbov._beatInputFieldValidator._max = 0;

      ApplyScrollableIntInput(bbov._columnInputFieldValidator, NoPrecisionInt);
      bbov._columnInputFieldValidator._validatorType = IntInputFieldValidator.ValidatorType.Clamp;
      bbov._columnInputFieldValidator._min = 0;
      bbov._columnInputFieldValidator._max = 3;

      ApplyScrollableIntInput(bbov._rowInputFieldValidator, NoPrecisionInt);
      bbov._rowInputFieldValidator._validatorType = IntInputFieldValidator.ValidatorType.Clamp;
      bbov._rowInputFieldValidator._min = 0;
      bbov._rowInputFieldValidator._max = 2;
   }

   private void ApplyToNoteDataView()
   {
      var ndv = _ebvc._editBeatmapRightPanelView._editObjectView._noteDataView;
      ApplyToBaseBeatmapObjectView(ndv);
   }

   private void ApplyToBombDataView()
   {
      var bdv = _ebvc._editBeatmapRightPanelView._editObjectView._bombDataView;
      ApplyToBaseBeatmapObjectView(bdv);
   }

   private void ApplyToObstacleDataView()
   {
      var odv = _ebvc._editBeatmapRightPanelView._editObjectView._obstacleDataView;
      ApplyToBaseBeatmapObjectView(odv);

      ApplyScrollableFloatInput(odv._durationInputField, null);

      ApplyScrollableIntInput(odv._widthInputField, NoPrecisionInt);
      odv._widthInputField._validatorType = IntInputFieldValidator.ValidatorType.Clamp;
      odv._widthInputField._min = 1;
      odv._widthInputField._max = 4;

      ApplyScrollableIntInput(odv._heightInputField, NoPrecisionInt);
      odv._heightInputField._validatorType = IntInputFieldValidator.ValidatorType.Clamp;
      odv._heightInputField._min = 1;
      odv._heightInputField._max = 5;
   }

   private void ApplyToArcDataView()
   {
      var adv = _ebvc._editBeatmapRightPanelView._editObjectView._arcDataView;
      ApplyToBaseBeatmapObjectView(adv);

      ApplyScrollableFloatInput(adv._tailBeatInputField, null);

      ApplyScrollableIntInput(adv._tailColumnInputField, NoPrecisionInt);
      adv._tailColumnInputField._validatorType = IntInputFieldValidator.ValidatorType.Clamp;
      adv._tailColumnInputField._min = 0;
      adv._tailColumnInputField._max = 3;

      ApplyScrollableIntInput(adv._tailRowInputField, NoPrecisionInt);
      adv._tailRowInputField._validatorType = IntInputFieldValidator.ValidatorType.Clamp;
      adv._tailRowInputField._min = 0;
      adv._tailRowInputField._max = 2;

      ApplyScrollableFloatInput(adv._controlPointInputField, null);
      ApplyScrollableFloatInput(adv._tailControlPointInputField, null);
   }

   private void ApplyToChainDataView()
   {
      var cdv = _ebvc._editBeatmapRightPanelView._editObjectView._chainDataView;
      ApplyToBaseBeatmapObjectView(cdv);

      ApplyScrollableFloatInput(cdv._tailBeatInputField, null);
      cdv._tailBeatInputField._validatorType = FloatInputFieldValidator.ValidatorType.Max;
      cdv._tailBeatInputField._max = 0;

      ApplyScrollableIntInput(cdv._tailColumnInputField, NoPrecisionInt);
      cdv._tailColumnInputField._validatorType = IntInputFieldValidator.ValidatorType.Clamp;
      cdv._tailColumnInputField._min = 0;
      cdv._tailColumnInputField._max = 3;

      ApplyScrollableIntInput(cdv._tailRowInputField, NoPrecisionInt);
      cdv._tailRowInputField._validatorType = IntInputFieldValidator.ValidatorType.Clamp;
      cdv._tailRowInputField._min = 0;
      cdv._tailRowInputField._max = 2;

      ApplyScrollableFloatInput(cdv._squishInputField, null);
      cdv._squishInputField._validatorType = FloatInputFieldValidator.ValidatorType.Max;
      cdv._squishInputField._max = Mathf.Epsilon;

      ApplyScrollableIntInput(cdv._slicesInputField, NoPrecisionInt);
      cdv._slicesInputField._validatorType = IntInputFieldValidator.ValidatorType.Max;
      cdv._slicesInputField._max = 1;
   }

   private void ApplyToBasicEventDataView()
   {
      var bedv = _ebvc._editBeatmapRightPanelView._editObjectView._basicEventDataView;
      ApplyScrollableFloatInput(bedv._beatInputFieldValidator, null);
      bedv._beatInputFieldValidator._validatorType = FloatInputFieldValidator.ValidatorType.Max;
      bedv._beatInputFieldValidator._max = 0;

      ApplyScrollableIntInput(bedv._intValueInput, NoPrecisionInt);
      bedv._intValueInput._validatorType = IntInputFieldValidator.ValidatorType.Max;
      bedv._intValueInput._max = 0;

      ApplyScrollableFloatInput(bedv._floatValueInput, LightColorEventHelper._precisions, 0.1f);
      bedv._floatValueInput._validatorType = FloatInputFieldValidator.ValidatorType.Max;
      bedv._floatValueInput._max = 0;
   }

   private void ApplyToEventBoxGroupDataView()
   {
      var ebgdv = _ebvc._editBeatmapRightPanelView._editObjectView._eventBoxGroupDataView;
      ApplyScrollableFloatInput(ebgdv._beatInputFieldValidator, null);
      ebgdv._beatInputFieldValidator._validatorType = FloatInputFieldValidator.ValidatorType.Max;
      ebgdv._beatInputFieldValidator._max = 0;
   }

   private void ApplyToLightColorDataView()
   {
      var lcdv = _ebvc._editBeatmapRightPanelView._editObjectView._lightColorDataView;
      ApplyScrollableFloatInput(lcdv._beatInputFieldValidator, null);
      lcdv._beatInputFieldValidator._validatorType = FloatInputFieldValidator.ValidatorType.Max;
      lcdv._beatInputFieldValidator._max = 0;

      ApplyScrollableFloatInput(lcdv._valueInput, LightColorEventHelper._precisions);
      lcdv._valueInput._validatorType = FloatInputFieldValidator.ValidatorType.Max;
      lcdv._valueInput._max = 0;

      ApplyScrollableFloatInput(lcdv._strobeBrightnessInput, LightColorEventHelper._precisions);
      lcdv._strobeBrightnessInput._validatorType = FloatInputFieldValidator.ValidatorType.Max;
      lcdv._strobeBrightnessInput._max = 0;

      ApplyScrollableIntInput(lcdv._strobeFrequencyInput, NoPrecisionInt);
      lcdv._strobeFrequencyInput._validatorType = IntInputFieldValidator.ValidatorType.Max;
      lcdv._strobeFrequencyInput._max = 0;
   }

   private void ApplyToLightRotationDataView()
   {
      var lrdv = _ebvc._editBeatmapRightPanelView._editObjectView._lightRotationDataView;
      ApplyScrollableFloatInput(lrdv._beatInputFieldValidator, null);
      lrdv._beatInputFieldValidator._validatorType = FloatInputFieldValidator.ValidatorType.Max;
      lrdv._beatInputFieldValidator._max = 0;

      ApplyScrollableFloatInput(lrdv._valueInput, ModifyHoveredLightRotationDeltaRotationCommand._precisions);

      ApplyScrollableIntInput(lrdv._loopsInput, NoPrecisionInt);
      lrdv._loopsInput._validatorType = IntInputFieldValidator.ValidatorType.Max;
      lrdv._loopsInput._max = 0;
   }

   private void ApplyToLightTranslationDataView()
   {
      var ltdv = _ebvc._editBeatmapRightPanelView._editObjectView._lightTranslationDataView;
      ApplyScrollableFloatInput(ltdv._beatInputFieldValidator, null);
      ltdv._beatInputFieldValidator._validatorType = FloatInputFieldValidator.ValidatorType.Max;
      ltdv._beatInputFieldValidator._max = 0;

      ApplyScrollableFloatInput(ltdv._valueInput, ModifyHoveredLightTranslationDeltaTranslationCommand._precisions);
   }

   private void ApplyToFloatFxDataView()
   {
      var ffdv = _ebvc._editBeatmapRightPanelView._editObjectView._floatFxDataView;
      ApplyScrollableFloatInput(ffdv._beatInputFieldValidator, null);
      ffdv._beatInputFieldValidator._validatorType = FloatInputFieldValidator.ValidatorType.Max;
      ffdv._beatInputFieldValidator._max = 0;

      ApplyScrollableFloatInput(ffdv._valueInput, ModifyHoveredFloatFxDeltaValueCommand._precisions);
   }

   private void ApplyToEventBoxView()
   {
      var ebv = _ebvc
         ._editBeatmapRightPanelView._panels.First(p => p.panelType == BeatmapPanelType.EventBox)
         .elements[0]
         .GetComponent<EventBoxesView>()
         ._eventBoxView;

      ApplyScrollableFloatInput(ebv._beatDistributionInput, null);
      ebv._beatDistributionInput._validatorType = FloatInputFieldValidator.ValidatorType.Max;

      ApplyScrollableIntInput(ebv._indexFilterView._groupingValidator, NoPrecisionInt);

      // temporary fix
      Object.Destroy(
         ebv
            ._indexFilterView._param0Input
            .GetComponents<IntInputFieldValidator>()
            .First(comp => comp != ebv._indexFilterView._param0Input));
      ApplyScrollableIntInput(ebv._indexFilterView._param0Input, NoPrecisionInt);
      ebv._indexFilterView._param0Input._validatorType = IntInputFieldValidator.ValidatorType.Max;
      ebv._indexFilterView._param0Input._max = 1;

      // temporary fix
      Object.Destroy(
         ebv
            ._indexFilterView._param1Input.GetComponents<IntInputFieldValidator>()
            .First(comp => comp != ebv._indexFilterView._param1Input));
      ApplyScrollableIntInput(ebv._indexFilterView._param1Input, NoPrecisionInt);
      ebv._indexFilterView._param1Input._validatorType = IntInputFieldValidator.ValidatorType.Max;
      ApplyScrollableIntInput(ebv._indexFilterView._randomSeedValidator, NoPrecisionInt);

      ApplyScrollableIntInput(ebv._indexFilterView._limitValidator, null);
      ebv._indexFilterView._limitValidator._validatorType = IntInputFieldValidator.ValidatorType.Clamp;
      ebv._indexFilterView._limitValidator._min = 0;
      ebv._indexFilterView._limitValidator._max = 100;

      ebv._brightnessDistributionView._brightnessDistributionParamInput._validatorType =
         FloatInputFieldValidator.ValidatorType.None;
      ApplyScrollableFloatInput(
         ebv._brightnessDistributionView._brightnessDistributionParamInput,
         LightColorEventHelper._precisions);

      ebv._brightnessDistributionView._brightnessDistributionParamInput._validatorType =
         FloatInputFieldValidator.ValidatorType.None;
      ApplyScrollableFloatInput(
         ebv._rotationDistributionView._rotationDistributionParamInput,
         ModifyHoveredLightRotationDeltaRotationCommand._precisions);

      ebv._brightnessDistributionView._brightnessDistributionParamInput._validatorType =
         FloatInputFieldValidator.ValidatorType.None;
      ApplyScrollableFloatInput(
         ebv._gapDistributionView._translationDistributionParamInput,
         ModifyHoveredLightTranslationDeltaTranslationCommand._precisions);

      ebv._brightnessDistributionView._brightnessDistributionParamInput._validatorType =
         FloatInputFieldValidator.ValidatorType.None;
      ApplyScrollableFloatInput(
         ebv._fxDistributionView._fxDistributionParamInput,
         ModifyHoveredFloatFxDeltaValueCommand._precisions);
   }

   public void ApplyScrollableIntInput(
      IntInputFieldValidator component,
      Dictionary<PrecisionType, int> precision,
      float multiplier = 1.0f)
   {
      Object.Destroy(component.gameObject.GetComponent<IntInputFieldValidatorChangeOnScroll>());
      var scrollable = _container.InstantiateComponent<ScrollableInputInt>(component.gameObject);
      if (precision != null) scrollable.PrecisionDelta = precision;
      scrollable.multiplier = multiplier;
   }

   public void ApplyScrollableFloatInput(
      FloatInputFieldValidator component,
      Dictionary<PrecisionType, float> precision,
      float multiplier = 1.0f)
   {
      Object.Destroy(component.gameObject.GetComponent<FloatInputFieldValidatorChangeOnScroll>());
      var scrollable = _container.InstantiateComponent<ScrollableInputFloat>(component.gameObject);
      if (precision != null) scrollable.PrecisionDelta = precision;
      scrollable.multiplier = multiplier;
   }
}