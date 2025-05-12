using System;
using System.Collections.Generic;
using System.Globalization;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Types;
using Zenject;

namespace EditorEnhanced.UI;

public class ScrollableYourInput : IInitializable
{
    private static readonly Dictionary<PrecisionType, float> PrecisionFloat = new()
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

    private static readonly Dictionary<PrecisionType, int> PrecisionInt = new()
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
        var ebv = _ebvc._eventBoxesView._eventBoxView;

        ApplyScrollableFloatInput(ebv._beatDistributionInput,
            val => ebv._beatDistributionInput.ValidateInput(
                (ebv._beatDistributionInput.value + val * PrecisionFloat[_bs.scrollPrecision])
                .ToString(CultureInfo.InvariantCulture)));
        ebv._beatDistributionInput._validatorType = FloatInputFieldValidator.ValidatorType.Max;

        ApplyScrollableIntInput(ebv._indexFilterView._groupingValidator);
        ApplyScrollableIntInput(ebv._indexFilterView._param0Input);
        ebv._indexFilterView._param0Input._validatorType = IntInputFieldValidator.ValidatorType.Max;
        ebv._indexFilterView._param0Input._max = 1;
        ApplyScrollableIntInput(ebv._indexFilterView._param1Input);
        ebv._indexFilterView._param1Input._validatorType = IntInputFieldValidator.ValidatorType.Max;
        ApplyScrollableIntInput(ebv._indexFilterView._randomSeedValidator);

        ApplyScrollableIntInput(ebv._indexFilterView._limitValidator,
            val => ebv._indexFilterView._limitValidator.ValidateInput(
                (ebv._indexFilterView._limitValidator.value + val * PrecisionInt[_bs.scrollPrecision])
                .ToString(CultureInfo.InvariantCulture)));
        ebv._indexFilterView._limitValidator._validatorType = IntInputFieldValidator.ValidatorType.Clamp;
        ebv._indexFilterView._limitValidator._min = 0;
        ebv._indexFilterView._limitValidator._max = 100;

        ebv._brightnessDistributionView._brightnessDistributionParamInput._validatorType =
            FloatInputFieldValidator.ValidatorType.None;
        ApplyScrollableFloatInput(ebv._brightnessDistributionView._brightnessDistributionParamInput,
            val => ebv._brightnessDistributionView._brightnessDistributionParamInput.ValidateInput(
                (ebv._brightnessDistributionView._brightnessDistributionParamInput.value +
                 val * LightColorEventHelper._precisions[_bs.scrollPrecision]).ToString(CultureInfo.InvariantCulture)));

        ebv._brightnessDistributionView._brightnessDistributionParamInput._validatorType =
            FloatInputFieldValidator.ValidatorType.None;
        ApplyScrollableFloatInput(ebv._rotationDistributionView._rotationDistributionParamInput,
            val => ebv._rotationDistributionView._rotationDistributionParamInput.ValidateInput(
                (ebv._rotationDistributionView._rotationDistributionParamInput.value +
                 val * ModifyHoveredLightRotationDeltaRotationCommand._precisions[_bs.scrollPrecision])
                .ToString(CultureInfo.InvariantCulture)));

        ebv._brightnessDistributionView._brightnessDistributionParamInput._validatorType =
            FloatInputFieldValidator.ValidatorType.None;
        ApplyScrollableFloatInput(ebv._gapDistributionView._translationDistributionParamInput,
            val => ebv._gapDistributionView._translationDistributionParamInput.ValidateInput(
                (ebv._gapDistributionView._translationDistributionParamInput.value +
                 val * ModifyHoveredLightTranslationDeltaTranslationCommand._precisions[_bs.scrollPrecision])
                .ToString(CultureInfo.InvariantCulture)));

        ebv._brightnessDistributionView._brightnessDistributionParamInput._validatorType =
            FloatInputFieldValidator.ValidatorType.None;
        ApplyScrollableFloatInput(ebv._fxDistributionView._fxDistributionParamInput,
            val => ebv._fxDistributionView._fxDistributionParamInput.ValidateInput(
                (ebv._fxDistributionView._fxDistributionParamInput.value +
                 val * ModifyHoveredFloatFxDeltaValueCommand._precisions[_bs.scrollPrecision])
                .ToString(CultureInfo.InvariantCulture)));
    }

    private static void ApplyScrollableIntInput(IntInputFieldValidator component, Action<float> action = null)
    {
        action ??= val => component.ValidateInput(
            (component.value + val).ToString(CultureInfo.InvariantCulture));
        component.gameObject.AddComponent<ScrollableInput>().OnScrollAction = action;
    }

    private static void ApplyScrollableFloatInput(FloatInputFieldValidator component, Action<float> action = null)
    {
        action ??= val => component.ValidateInput(
            (component.value + val).ToString(CultureInfo.InvariantCulture));
        component.gameObject.AddComponent<ScrollableInput>().OnScrollAction = action;
    }
}