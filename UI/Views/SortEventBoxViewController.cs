using System;
using BeatmapEditor3D;
using BeatmapEditor3D.Views;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

internal class SortEventBoxViewController : IInitializable, IDisposable
{
   private readonly EditBeatmapViewController _ebvc;
   private readonly SignalBus _signalBus;
   private readonly UIBuilder _uiBuilder;

   private EventBoxesView _ebv;

   public SortEventBoxViewController(
      SignalBus signalBus,
      EditBeatmapViewController ebvc,
      UIBuilder uiBuilder)
   {
      _signalBus = signalBus;
      _ebvc = ebvc;
      _uiBuilder = uiBuilder;
   }

   public void Dispose()
   {
   }

   public void Initialize()
   {
      _ebv = _ebvc._editBeatmapRightPanelView._panels[2].elements[0].GetComponent<EventBoxesView>();
      var target =
         _ebv._eventBoxButtonsScrollView.transform.parent.parent.Find("ControlButtons/RemoveButtonsWrapper");
      if (target == null) return;

      // target.parent.parent.gameObject.AddComponent<VerticalLayoutGroup>();
      // var csf = target.parent.parent.gameObject.AddComponent<ContentSizeFitter>();
      // csf.verticalFit  = ContentSizeFitter.FitMode.MinSize;

      // target.parent.gameObject.AddComponent<VerticalLayoutGroup>();
      // var csf = target.parent.gameObject.AddComponent<ContentSizeFitter>();

      _ebv._eventBoxButtonsScrollView.transform.parent.localPosition = new Vector3(-20f, -85f, 0f);

      var instance = Object.Instantiate(target.gameObject, target.parent);
      instance.name = "SortButtonsWrapper";

      instance.transform.localPosition = new Vector3(40f, -80f, 0f);
      var behev = instance.GetComponent<BeatmapEditorHoverExpandView>();
      for (var i = behev._content.childCount - 1; i >= 0; i--) Object.Destroy(behev._content.GetChild(i).gameObject);

      var btnTag = _uiBuilder
         .Button.Instantiate()
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