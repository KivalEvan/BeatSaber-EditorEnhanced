using BeatmapEditor3D;
using EditorEnhanced.UI.Interfaces;
using HMUI;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;

namespace EditorEnhanced.UI.Tags;

public class EditorTextBuilder : IEditorBuilder<EditorTextTag>
{
   private readonly EditBeatmapViewController _ebvc;

   public EditorTextBuilder(EditBeatmapViewController ebvc)
   {
      _ebvc = ebvc;
   }

   public EditorTextTag Instantiate()
   {
      return new EditorTextTag(_ebvc);
   }
}

public class EditorTextTag : IEditorTag, IUIText, IUIRect
{
   private readonly EditBeatmapViewController _ebvc;

   public EditorTextTag(EditBeatmapViewController ebvc)
   {
      _ebvc = ebvc;
   }

   private GameObject PrefabText => _ebvc._activeSelectionView._arcsCountText.gameObject;

   public string Name { get; set; } = "EEEditorText";

   public GameObject Create(Transform parent)
   {
      var go = new GameObject(Name)
      {
         layer = 5
      };
      go.SetActive(false);
      go.transform.SetParent(parent, false);

      var ctmp = go.AddComponent<CurvedTextMeshPro>();
      var prefabCtmp = PrefabText.GetComponent<CurvedTextMeshPro>();
      ctmp.font = prefabCtmp.font;
      ctmp.fontSharedMaterial = prefabCtmp.fontSharedMaterial;
      ctmp.color = Color ?? prefabCtmp.color;
      ctmp.fontSize = FontSize ?? 12f;
      ctmp.fontSizeMin = 18f;
      ctmp.fontSizeMax = 72f;
      ctmp.text = Text ?? "Default Text";
      ctmp.fontWeight = FontWeight ?? ctmp.fontWeight;
      ctmp.alignment = TextAlignment ?? ctmp.alignment;

      ctmp.rectTransform.anchorMin = AnchorMin ?? new Vector2(0.5f, 0.5f);
      ctmp.rectTransform.anchorMax = AnchorMax ?? new Vector2(0.5f, 0.5f);
      ctmp.rectTransform.offsetMin = OffsetMin ?? ctmp.rectTransform.offsetMin;
      ctmp.rectTransform.offsetMax = OffsetMax ?? ctmp.rectTransform.offsetMax;

      go.SetActive(true);
      return go;
   }

   public Vector2? AnchorMin { get; set; }
   public Vector2? AnchorMax { get; set; }
   public Vector2? OffsetMin { get; set; }
   public Vector2? OffsetMax { get; set; }
   public Vector2? SizeDelta { get; set; }
   public Rect? Rect { get; set; }

   [CanBeNull] public string Text { get; set; }
   public Color? Color { get; set; }
   public TextAlignmentOptions? TextAlignment { get; set; }
   public bool? RichText { get; set; }
   public float? FontSize { get; set; }
   public FontWeight? FontWeight { get; set; }
}