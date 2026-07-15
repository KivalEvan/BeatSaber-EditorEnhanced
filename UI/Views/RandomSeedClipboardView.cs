using System;
using System.Collections.Generic;
using BeatmapEditor3D.Views;
using EditorEnhanced.Managers;
using EditorEnhanced.UI.Extensions;
using HMUI;
using TMPro;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

internal sealed class RandomSeedClipboardView : IInitializable, IDisposable
{
   private readonly RandomSeedClipboardManager _rscm;

   private readonly List<GameObject> _texts = [];
   private readonly UIBuilder _uiBuilder;
   private readonly EditorViewLocator _viewLocator;
   private EventBoxView _eventBoxView;

   public RandomSeedClipboardView(
      RandomSeedClipboardManager rscm,
      EditorViewLocator viewLocator,
      UIBuilder uiBuilder)
   {
      _rscm = rscm;
      _viewLocator = viewLocator;
      _uiBuilder = uiBuilder;
   }

   public void Dispose()
   {
      foreach (var gameObject in _texts) Object.Destroy(gameObject);
      _texts.Clear();
   }

   public void Initialize()
   {
      if (!_viewLocator.TryGetEventBoxView(out _eventBoxView)
          || !_viewLocator.TryGetEventBoxToolbarInsertion(out var toolbar, out var siblingIndex))
         return;

      var verticalTag = _uiBuilder.CreateVerticalLayout();
      var horizontalTag = _uiBuilder.CreateHorizontalLayout();
      var textTag = _uiBuilder.CreateText();
      var checkboxTag = _uiBuilder.CreateCheckbox().SetFontSize(10f);
      var buttonTag = _uiBuilder.CreateButton().SetFontSize(10f);

      var vt = verticalTag
         .SetChildControlWidth(true)
         .SetSpacing(2f)
         .Create(toolbar);
      vt.transform.SetSiblingIndex(siblingIndex);

      var ht = horizontalTag.Create(vt.transform);
      textTag
         .SetText("Seeds")
         .SetFontWeight(FontWeight.Bold)
         .SetTextAlignment(TextAlignmentOptions.Center)
         .Create(ht.transform);
      ht = horizontalTag.Create(vt.transform);
      var text = textTag
         .SetText(_rscm.Seed.ToString())
         .SetFontSize(10f)
         .SetFontWeight(FontWeight.Regular)
         .SetTextAlignment(TextAlignmentOptions.Center)
         .Create(ht.transform);
      _texts.Add(text);

      ht = horizontalTag.Create(vt.transform);
      checkboxTag
         .SetSize(20)
         .SetBool(_rscm.RandomOnPaste)
         .SetText("Paste New")
         .SetOnValueChange(ToggleNewSeed)
         .Create(ht.transform);

      ht = horizontalTag.Create(vt.transform);
      checkboxTag
         .SetSize(20)
         .SetBool(_rscm.UseClipboard)
         .SetText("Use Copy")
         .SetOnValueChange(ToggleClipboard)
         .Create(ht.transform);

      text = textTag
         .SetText(_rscm.Seed.ToString())
         .SetFontSize(14f)
         .SetTextAlignment(TextAlignmentOptions.Left)
         .Create(_eventBoxView._indexFilterView._randomSeedValidator.transform.parent);
      _texts.Add(text);
      text.GetComponent<RectTransform>()
         .anchoredPosition = new Vector2(-55f, -30f);
      buttonTag
         .SetText("C")
         .SetOnClick(CopySeed)
         .Create(_eventBoxView._indexFilterView._newSeedButton.transform.parent)
         .GetComponent<RectTransform>()
         .anchoredPosition = new Vector2(-32f, -30f);
      buttonTag
         .SetText("P")
         .SetOnClick(PasteSeed)
         .Create(_eventBoxView._indexFilterView._newSeedButton.transform.parent)
         .GetComponent<RectTransform>()
         .anchoredPosition = new Vector2(0f, -30f);
   }

   private void ToggleNewSeed(bool value)
   {
      _rscm.RandomOnPaste = value;
   }

   private void ToggleClipboard(bool value)
   {
      _rscm.UseClipboard = value;
   }

   private void CopySeed()
   {
      _rscm.Seed = _eventBoxView._eventBox.indexFilter.seed;
      _texts.ForEach(t => t.GetComponent<CurvedTextMeshPro>().text = _rscm.Seed.ToString());
   }

   private void PasteSeed()
   {
      _eventBoxView._indexFilterView._randomSeedValidator.SetValue(_rscm.Seed);
   }
}
