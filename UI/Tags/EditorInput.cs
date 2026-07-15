using BeatmapEditor3D;
using EditorEnhanced.UI.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Tags;

public abstract class EditorInputTag<T> : IEditorTag, IUILayoutElement
{
   private readonly GameObject _prefabInput;

   protected EditorInputTag(GameObject prefabInput)
   {
      _prefabInput = prefabInput;
   }

   public abstract string Name { get; set; }

   public virtual GameObject Create(Transform parent)
   {
      var go = Object.Instantiate(_prefabInput, parent, false);
      go.name = Name;
      go.SetActive(false);

      Object.Destroy(go.GetComponent<FloatInputFieldValidator>());
      Object.Destroy(go.GetComponent<FloatInputFieldValidatorChangeOnScroll>());

      var le = go.AddComponent<LayoutElement>();
      le.flexibleWidth = FlexibleWidth ?? le.flexibleWidth;
      le.flexibleHeight = FlexibleHeight ?? le.flexibleHeight;
      le.preferredWidth = PreferredWidth ?? le.preferredWidth;
      le.preferredHeight = PreferredHeight ?? le.preferredHeight;

      var csf = go.AddComponent<ContentSizeFitter>();
      csf.horizontalFit = HorizontalFit ?? ContentSizeFitter.FitMode.PreferredSize;
      csf.verticalFit = VerticalFit ?? ContentSizeFitter.FitMode.PreferredSize;

      return go;
   }

   public ContentSizeFitter.FitMode? VerticalFit { get; set; }
   public ContentSizeFitter.FitMode? HorizontalFit { get; set; }
   public float? FlexibleWidth { get; set; }
   public float? FlexibleHeight { get; set; }
   public float? PreferredWidth { get; set; }
   public float? PreferredHeight { get; set; }
}