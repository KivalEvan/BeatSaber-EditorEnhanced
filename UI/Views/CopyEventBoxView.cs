using BeatmapEditor3D;
using BeatmapEditor3D.Views;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

internal class CopyEventBoxView : IInitializable
{
   private readonly EditorViewLocator _viewLocator;
   private readonly SignalBus _signalBus;
   private readonly UIBuilder _uiBuilder;

   private bool _copyEvent;
   private EventBoxView _eventBoxView;
   private bool _increment;
   private bool _randomSeed;
   private bool _addValue;
   private float _value;

   public CopyEventBoxView(
      SignalBus signalBus,
      EditorViewLocator viewLocator,
      UIBuilder uiBuilder)
   {
      _signalBus = signalBus;
      _viewLocator = viewLocator;
      _uiBuilder = uiBuilder;
   }

   public void Initialize()
   {
      if (!_viewLocator.TryGetEventBoxView(out _eventBoxView)) return;
      var target = _eventBoxView;
      if (!_viewLocator.TryFind(target.transform, "EventBoxInfo", out var replacement)
          || !_viewLocator.TryFind(target.transform, "GroupInfoView/Background4px", out var background))
         return;
      replacement.gameObject.SetActive(false);

      var stackTag = _uiBuilder.CreateStackLayout()
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize);
      var verticalTag = _uiBuilder.CreateVerticalLayout()
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetPadding(new RectOffset(4, 4, 4, 4));
      var horizontalTag = _uiBuilder.CreateHorizontalLayout()
         .SetChildAlignment(TextAnchor.LowerCenter)
         .SetChildControlWidth(true)
         .SetSpacing(8)
         .SetPadding(new RectOffset(4, 4, 2, 2));
      var btnTag = _uiBuilder.CreateButton()
         .SetFontSize(16);
      var checkboxTag = _uiBuilder.CreateCheckbox()
         .SetSize(28)
         .SetFontSize(16);
      var inputFloatTag = _uiBuilder.CreateFloatInput()
         .SetHorizontalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
         .SetValidatorType(FloatInputFieldValidator.ValidatorType.None)
         .SetPreferredWidth(80);

      var container = stackTag.Create(target.transform);
      container.transform.SetAsFirstSibling();
      Object.Instantiate(
         background,
         container.transform,
         false);
      container = verticalTag.Create(container.transform);
      var layout = horizontalTag.Create(container.transform);

      replacement.GetChild(1).SetParent(layout.transform);
      btnTag
         .SetText("Copy")
         .SetOnClick(CopyEventBox)
         .Create(layout.transform);
      btnTag
         .SetText("Paste")
         .SetOnClick(PasteEventBox)
         .Create(layout.transform);
      btnTag
         .SetText("Duplicate")
         .SetOnClick(DuplicateEventBox)
         .Create(layout.transform);
      layout = horizontalTag.Create(container.transform);
      checkboxTag
         .SetText("Event")
         .SetBool(_copyEvent)
         .SetOnValueChange(val => _copyEvent = val)
         .Create(layout.transform);
      checkboxTag
         .SetText("RSeed")
         .SetBool(_randomSeed)
         .SetOnValueChange(val => _randomSeed = val)
         .Create(layout.transform);
      checkboxTag
         .SetText("ID++")
         .SetBool(_increment)
         .SetOnValueChange(val => _increment = val)
         .Create(layout.transform);
      checkboxTag
         .SetText("+Val")
         .SetBool(_addValue)
         .SetOnValueChange(val => _addValue = val)
         .Create(layout.transform);
      inputFloatTag
         .SetValue(_value)
         .SetOnValueChange(val => _value = val)
         .Create(layout.transform);
   }

   private void CopyEventBox()
   {
      _signalBus.Fire(new CopyEventBoxSignal(_eventBoxView._eventBox));
   }

   private void PasteEventBox()
   {
      _signalBus.Fire(
         new PasteEventBoxSignal(
            _eventBoxView._eventBox,
            _copyEvent,
            _randomSeed,
            _increment,
            _addValue ? _value : 0f));
   }

   private void DuplicateEventBox()
   {
      _signalBus.Fire(
         new DuplicateEventBoxSignal(
            _eventBoxView._eventBox.id,
            _copyEvent,
            _randomSeed,
            _increment,
            _addValue ? _value : 0f));
   }
}
