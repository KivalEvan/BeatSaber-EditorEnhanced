using BeatmapEditor3D;
using BeatmapEditor3D.Views;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

internal class SortEventBoxView : IInitializable
{
   private readonly EditorViewLocator _viewLocator;
   private readonly SignalBus _signalBus;
   private readonly UIBuilder _uiBuilder;

   private EventBoxesView _ebv;

   public SortEventBoxView(
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
      if (!_viewLocator.TryGetEventBoxesView(out _ebv)) return;
      var controls = _ebv._eventBoxButtonsScrollView == null
         ? null
         : _ebv._eventBoxButtonsScrollView.transform.parent?.parent;
      if (!_viewLocator.TryFind(controls, "ControlButtons/RemoveButtonsWrapper", out var target)) return;

      // target.parent.parent.gameObject.AddComponent<VerticalLayoutGroup>();
      // var csf = target.parent.parent.gameObject.AddComponent<ContentSizeFitter>();
      // csf.verticalFit  = ContentSizeFitter.FitMode.MinSize;

      // target.parent.gameObject.AddComponent<VerticalLayoutGroup>();
      // var csf = target.parent.gameObject.AddComponent<ContentSizeFitter>();

      var rect = (RectTransform)_ebv._eventBoxButtonsScrollView.transform.parent;
      rect.sizeDelta = new Vector2(40f, -130f);
      rect.localPosition = new Vector3(-20f, -65f, 0f);

      var instance = Object.Instantiate(target.gameObject, target.parent);
      instance.name = "SortButtonsWrapper";

      instance.transform.localPosition = new Vector3(40f, -80f, 0f);
      var behev = instance.GetComponent<BeatmapEditorHoverExpandView>();
      for (var i = behev._content.childCount - 1; i >= 0; i--) Object.Destroy(behev._content.GetChild(i).gameObject);

      var btnTag = _uiBuilder.CreateButton()
         .SetSize(new Vector2(40f, 40f))
         .SetPadding(new RectOffset(0, 0, 0, 0))
         .SetChildForceExpandWidth(true)
         .SetChildForceExpandHeight(true)
         .SetFontSize(12f);

      btnTag
         .SetText("Sort\nAxis")
         .SetOnClick(SortAxisHandler)
         .Create(behev._content);
      btnTag
         .SetText("Sort\nID")
         .SetOnClick(SortIdHandler)
         .Create(behev._content);
   }

   private void SortAxisHandler()
   {
      _signalBus.Fire(new SortAxisEventBoxGroupSignal());
   }

   private void SortIdHandler()
   {
      _signalBus.Fire(new SortIdEventBoxGroupSignal());
   }
}
