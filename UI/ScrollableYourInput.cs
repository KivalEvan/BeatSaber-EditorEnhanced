using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Types;
using BeatmapEditor3D.Views;
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

    private readonly BeatmapState _bs;
    private readonly EditBeatmapViewController _ebvc;

    public ScrollableYourInput(EditBeatmapViewController ebvc,
        BeatmapState bs)
    {
        _ebvc = ebvc;
        _bs = bs;
    }

    public void Initialize()
    {
        var ebv = _ebvc._editBeatmapRightPanelView._panels.First(p => p.panelType == BeatmapPanelType.EventBox)
            .elements[0].GetComponent<EventBoxesView>()._eventBoxView;

        ApplyScrollableFloatInput(ebv._beatDistributionInput, null);
        ebv._beatDistributionInput._validatorType = FloatInputFieldValidator.ValidatorType.Max;

        ApplyScrollableIntInput(ebv._indexFilterView._groupingValidator, NoPrecisionInt);
        ApplyScrollableIntInput(ebv._indexFilterView._param0Input, NoPrecisionInt);
        ebv._indexFilterView._param0Input._validatorType = IntInputFieldValidator.ValidatorType.Max;
        ebv._indexFilterView._param0Input._max = 1;

        ApplyScrollableIntInput(ebv._indexFilterView._param1Input, NoPrecisionInt);
        ebv._indexFilterView._param1Input._validatorType = IntInputFieldValidator.ValidatorType.Max;
        ApplyScrollableIntInput(ebv._indexFilterView._randomSeedValidator, NoPrecisionInt);

        ApplyScrollableIntInput(ebv._indexFilterView._limitValidator, null);
        ebv._indexFilterView._limitValidator._validatorType = IntInputFieldValidator.ValidatorType.Clamp;
        ebv._indexFilterView._limitValidator._min = 0;
        ebv._indexFilterView._limitValidator._max = 100;

        ebv._brightnessDistributionView._brightnessDistributionParamInput._validatorType =
            FloatInputFieldValidator.ValidatorType.None;
        ApplyScrollableFloatInput(ebv._brightnessDistributionView._brightnessDistributionParamInput,
            LightColorEventHelper._precisions);

        ebv._brightnessDistributionView._brightnessDistributionParamInput._validatorType =
            FloatInputFieldValidator.ValidatorType.None;
        ApplyScrollableFloatInput(ebv._rotationDistributionView._rotationDistributionParamInput,
            ModifyHoveredLightRotationDeltaRotationCommand._precisions);

        ebv._brightnessDistributionView._brightnessDistributionParamInput._validatorType =
            FloatInputFieldValidator.ValidatorType.None;
        ApplyScrollableFloatInput(ebv._gapDistributionView._translationDistributionParamInput,
            ModifyHoveredLightTranslationDeltaTranslationCommand._precisions);

        ebv._brightnessDistributionView._brightnessDistributionParamInput._validatorType =
            FloatInputFieldValidator.ValidatorType.None;
        ApplyScrollableFloatInput(ebv._fxDistributionView._fxDistributionParamInput,
            ModifyHoveredFloatFxDeltaValueCommand._precisions);
    }

    public void ApplyScrollableIntInput(IntInputFieldValidator component, Dictionary<PrecisionType, int> precision)
    {
        Object.Destroy(component.gameObject.GetComponent<IntInputFieldValidatorChangeOnScroll>());
        var scrollable = component.gameObject.AddComponent<ScrollableInputInt>();
        scrollable.fieldValidator = component;
        scrollable.BeatmapState = _bs;
        if (precision != null) scrollable.PrecisionDelta = precision;
    }

    public void ApplyScrollableFloatInput(FloatInputFieldValidator component,
        Dictionary<PrecisionType, float> precision)
    {
        Object.Destroy(component.gameObject.GetComponent<FloatInputFieldValidatorChangeOnScroll>());
        var scrollable = component.gameObject.AddComponent<ScrollableInputFloat>();
        scrollable.fieldValidator = component;
        scrollable.BeatmapState = _bs;
        if (precision != null) scrollable.PrecisionDelta = precision;
    }
}