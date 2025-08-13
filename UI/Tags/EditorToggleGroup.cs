using System;
using BeatmapEditor3D;
using EditorEnhanced.UI.Interfaces;
using Tweening;
using UnityEngine;

namespace EditorEnhanced.UI.Tags;

public class EditorToggleGroupBuilder : IEditorBuilder<EditorToggleGroupTag>
{
   private readonly EditBeatmapViewController _ebvc;
   private readonly TimeTweeningManager _twm;

   public EditorToggleGroupBuilder(EditBeatmapViewController ebvc, TimeTweeningManager twm)
   {
      _ebvc = ebvc;
      _twm = twm;
   }

   public EditorToggleGroupTag Instantiate()
   {
      return new EditorToggleGroupTag(_ebvc, _twm);
   }
}

public class EditorToggleGroupTag : IEditorTag
{
   public EditorToggleGroupTag(EditBeatmapViewController ebvc, TimeTweeningManager twm)
   {
   }

   public string Name { get; set; } = "EditorToggleGroup";

   public GameObject Create(Transform parent)
   {
      throw new NotImplementedException();
   }
}