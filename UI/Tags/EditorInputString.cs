using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using EditorEnhanced.UI.Interfaces;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace EditorEnhanced.UI.Tags;

public class EditorInputStringBuilder : IEditorBuilder<EditorInputStringTag>
{
   private readonly EditBeatmapViewController _ebvc;

   public EditorInputStringBuilder(EditBeatmapViewController ebvc)
   {
      _ebvc = ebvc;
   }

   public EditorInputStringTag Instantiate()
   {
      return new EditorInputStringTag(_ebvc);
   }
}

public class EditorInputStringTag : EditorInputTag<string>
{
   public EditorInputStringTag(EditBeatmapViewController ebvc)
   {
      _ebvc = ebvc;
   }

   public override string Name { get; set; } = "EEEditorInputString";

   [CanBeNull] public string Value { get; set; }
   public bool? TrimSpaces { get; set; }
   public bool? AllowEmpty { get; set; }
   public List<Action<string>> OnValueChange { get; set; } = [];

   public GameObject Create(Transform parent)
   {
      var go = base.Create(parent);
      go.name = Name;

      var inputField = go.GetComponent<TMP_InputField>();
      var validator = go.AddComponent<StringInputFieldValidator>();
      validator._inputField = inputField;
      if (Value != null) validator.SetValueWithoutNotify(Value, false);
      if (TrimSpaces != null) validator._trimSpaces = (bool)TrimSpaces;
      if (AllowEmpty != null) validator._allowEmptyInput = (bool)AllowEmpty;
      OnValueChange.ForEach(ovc => { validator.onInputValidated += ovc; });

      go.SetActive(true);
      return go;
   }

   public EditorInputStringTag SetValue(string value)
   {
      Value = value;
      return this;
   }

   public EditorInputStringTag SetTrimSpaces(bool value)
   {
      TrimSpaces = value;
      return this;
   }

   public EditorInputStringTag SetAllowEmpty(bool value)
   {
      AllowEmpty = value;
      return this;
   }

   public EditorInputStringTag ResetOnValueChange()
   {
      OnValueChange.Clear();
      return this;
   }

   public EditorInputStringTag AddOnValueChange(Action<string> fn)
   {
      OnValueChange.Add(fn);
      return this;
   }

   public EditorInputStringTag SetOnValueChange(Action<string> fn)
   {
      return ResetOnValueChange()
         .AddOnValueChange(fn);
   }
}