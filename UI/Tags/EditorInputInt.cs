using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using EditorEnhanced.UI.Components;
using EditorEnhanced.UI.Interfaces;
using TMPro;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.UI.Tags;

public class EditorInputIntBuilder : IEditorBuilder<EditorInputIntTag>
{
   private readonly EditBeatmapViewController _ebvc;
   private readonly DiContainer _container;

   public EditorInputIntBuilder(EditBeatmapViewController ebvc, DiContainer container)
   {
      _ebvc = ebvc;
      _container = container;
   }

   public EditorInputIntTag Instantiate()
   {
      return new EditorInputIntTag(_ebvc, _container);
   }
}

public class EditorInputIntTag : EditorInputTag<int>
{
   private readonly DiContainer _container;
   
   public EditorInputIntTag(EditBeatmapViewController ebvc, DiContainer container)
   {
      _ebvc = ebvc;
      _container = container;
   }

   public override string Name { get; set; } = "EEEditorInputInt";

   public int? Value { get; set; }
   public float? MinValue { get; set; }
   public float? MaxValue { get; set; }
   public FloatInputFieldValidator.ValidatorType? ValidatorType { get; set; }
   public List<Action<int>> OnValueChange { get; set; } = [];

   public override GameObject Create(Transform parent)
   {
      var go = base.Create(parent);

      var inputField = go.GetComponent<TMP_InputField>();
      var validator = go.AddComponent<IntInputFieldValidator>();
      validator._inputField = inputField;
      if (MinValue != null) validator._min = (int)MinValue;
      if (MaxValue != null) validator._max = (int)MaxValue;
      if (ValidatorType != null) validator._validatorType = (IntInputFieldValidator.ValidatorType)ValidatorType;
      if (Value != null) validator.SetValueWithoutNotify((int)Value, false);
      OnValueChange.ForEach(ovc => { validator.onInputValidated += ovc; });

      _container.InstantiateComponent<ScrollableInputInt>(go);
      
      go.SetActive(true);
      return go;
   }

   public EditorInputIntTag SetValue(int value)
   {
      Value = value;
      return this;
   }

   public EditorInputIntTag SetMinValue(float value)
   {
      MinValue = value;
      return this;
   }

   public EditorInputIntTag SetMaxValue(float value)
   {
      MaxValue = value;
      return this;
   }

   public EditorInputIntTag SetValidatorType(FloatInputFieldValidator.ValidatorType value)
   {
      ValidatorType = value;
      return this;
   }

   public EditorInputIntTag ResetOnValueChange()
   {
      OnValueChange.Clear();
      return this;
   }

   public EditorInputIntTag AddOnValueChange(Action<int> fn)
   {
      OnValueChange.Add(fn);
      return this;
   }

   public EditorInputIntTag SetOnValueChange(Action<int> fn)
   {
      return ResetOnValueChange()
         .AddOnValueChange(fn);
   }
}