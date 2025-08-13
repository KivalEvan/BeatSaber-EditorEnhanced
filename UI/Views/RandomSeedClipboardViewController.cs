using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using BeatmapEditor3D.Types;
using BeatmapEditor3D.Views;
using EditorEnhanced.Managers;
using EditorEnhanced.UI.Extensions;
using HMUI;
using TMPro;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

public class RandomSeedClipboardViewController : IInitializable, IDisposable
{
   private readonly EditBeatmapNavigationViewController _ebnvc;
   private readonly EditBeatmapViewController _ebvc;
   private readonly RandomSeedClipboardManager _rscm;

   private readonly List<GameObject> _texts = [];
   private readonly UIBuilder _uiBuilder;
   private EventBoxesView _ebv;

   public RandomSeedClipboardViewController(
      EditBeatmapViewController ebvc,
      EditBeatmapNavigationViewController ebnvc,
      RandomSeedClipboardManager rscm,
      UIBuilder uiBuilder)
   {
      _ebvc = ebvc;
      _ebnvc = ebnvc;
      _rscm = rscm;
      _uiBuilder = uiBuilder;
   }

   public void Dispose()
   {
      foreach (var gameObject in _texts) Object.Destroy(gameObject);
      _texts.Clear();
   }

   public void Initialize()
   {
      _ebv = _ebvc
         ._editBeatmapRightPanelView._panels.First(p => p.panelType == BeatmapPanelType.EventBox)
         .elements[0]
         .GetComponent<EventBoxesView>();
      var targetNav = _ebnvc._eventBoxGroupsToolbarView;

      var verticalTag = _uiBuilder.LayoutVertical.Instantiate();
      var horizontalTag = _uiBuilder.LayoutHorizontal.Instantiate();
      var textTag = _uiBuilder.Text.Instantiate();
      var checkboxTag = _uiBuilder.Checkbox.Instantiate().SetFontSize(10f);
      var buttonTag = _uiBuilder.Button.Instantiate().SetFontSize(10f);

      var vt = verticalTag
         .SetChildControlWidth(true)
         .SetSpacing(2f)
         .Create(targetNav.transform);
      vt.transform.SetSiblingIndex(
         _ebnvc._eventBoxGroupsToolbarView._extensionToggle.transform.parent.GetSiblingIndex());

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
         .Create(_ebv._eventBoxView._indexFilterView._randomSeedValidator.transform.parent);
      _texts.Add(text);
      text.GetComponent<RectTransform>()
         .anchoredPosition = new Vector2(-55f, -30f);
      buttonTag
         .SetText("C")
         .SetOnClick(CopySeed)
         .Create(_ebv._eventBoxView._indexFilterView._newSeedButton.transform.parent)
         .GetComponent<RectTransform>()
         .anchoredPosition = new Vector2(-32f, -30f);
      buttonTag
         .SetText("P")
         .SetOnClick(PasteSeed)
         .Create(_ebv._eventBoxView._indexFilterView._newSeedButton.transform.parent)
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
      _rscm.Seed = _ebv._eventBoxView._eventBox.indexFilter.seed;
      _texts.ForEach(t => t.GetComponent<CurvedTextMeshPro>().text = _rscm.Seed.ToString());
   }

   private void PasteSeed()
   {
      _ebv._eventBoxView._indexFilterView._randomSeedValidator.SetValue(_rscm.Seed);
   }
}