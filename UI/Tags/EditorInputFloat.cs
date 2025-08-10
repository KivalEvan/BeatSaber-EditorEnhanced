using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using EditorEnhanced.UI.Interfaces;
using TMPro;
using UnityEngine;

namespace EditorEnhanced.UI.Tags;

public class EditorInputFloatBuilder : IEditorBuilder<EditorInputFloatTag>
{
    private readonly EditBeatmapViewController _ebvc;

    public EditorInputFloatBuilder(EditBeatmapViewController ebvc)
    {
        _ebvc = ebvc;
    }

    public EditorInputFloatTag Instantiate()
    {
        return new EditorInputFloatTag(_ebvc);
    }
}

public class EditorInputFloatTag : EditorInputTag<float>
{
    public EditorInputFloatTag(EditBeatmapViewController ebvc)
    {
        _ebvc = ebvc;
    }

    public override string Name { get; set; } = "EEEditorInputFloat";

    public float? Value { get; set; }
    public float? MinValue { get; set; }
    public float? MaxValue { get; set; }
    public FloatInputFieldValidator.ValidatorType? ValidatorType { get; set; }
    public List<Action<float>> OnValueChange { get; set; } = [];

    public GameObject Create(Transform parent)
    {
        var go = base.Create(parent);

        var inputField = go.GetComponent<TMP_InputField>();
        var validator = go.AddComponent<FloatInputFieldValidator>();
        validator._inputField = inputField;
        if (MinValue != null) validator._min = (int)MinValue;
        if (MaxValue != null) validator._max = (int)MaxValue;
        if (ValidatorType != null)
            validator._validatorType = (FloatInputFieldValidator.ValidatorType)ValidatorType;
        if (Value != null) validator.SetValueWithoutNotify((float)Value, false);
        OnValueChange.ForEach(ovc => { validator.onInputValidated += ovc; });

        go.SetActive(true);
        return go;
    }

    public EditorInputFloatTag SetValue(float value)
    {
        Value = value;
        return this;
    }

    public EditorInputFloatTag SetMinValue(float value)
    {
        MinValue = value;
        return this;
    }

    public EditorInputFloatTag SetMaxValue(float value)
    {
        MaxValue = value;
        return this;
    }

    public EditorInputFloatTag SetValidatorType(FloatInputFieldValidator.ValidatorType value)
    {
        ValidatorType = value;
        return this;
    }

    public EditorInputFloatTag ResetOnValueChange()
    {
        OnValueChange.Clear();
        return this;
    }

    public EditorInputFloatTag AddOnValueChange(Action<float> fn)
    {
        OnValueChange.Add(fn);
        return this;
    }

    public EditorInputFloatTag SetOnValueChange(Action<float> fn)
    {
        return ResetOnValueChange()
            .AddOnValueChange(fn);
    }
}