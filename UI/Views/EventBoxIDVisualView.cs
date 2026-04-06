using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapDataLoaderVersion4;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.LevelEditor;
using BeatmapEditor3D.SerializedData;
using BeatmapEditor3D.Types;
using BeatmapEditor3D.Views;
using EditorEnhanced.Gizmo.Commands;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

internal class EventBoxIDVisualView : IInitializable, IDisposable
{
   private readonly EditBeatmapViewController _ebvc;
   private readonly SignalBus _signalBus;
   private readonly UIBuilder _uiBuilder;

   private EventBoxesView _ebv;

   private EditorTextTag _textTag;
   private readonly List<GameObject> _instantiatedErrorText = new();
   private Transform _errorTextTargetTransform;

   private List<Image> _instantiatedImages = new();
   private GameObject _imageContainer;

   public EventBoxIDVisualView(
      SignalBus signalBus,
      EditBeatmapViewController ebvc,
      UIBuilder uiBuilder)
   {
      _signalBus = signalBus;
      _ebvc = ebvc;
      _uiBuilder = uiBuilder;
   }

   public void Initialize()
   {
      _ebv = _ebvc._editBeatmapRightPanelView._panels[2].elements[0].GetComponent<EventBoxesView>();
      var target = _ebv._eventBoxView;
      var modification = target.transform.Find("GroupInfoView") as RectTransform;
      modification.GetComponent<LayoutElement>().preferredHeight = -1;
      var le = modification.GetChild(0).gameObject.AddComponent<LayoutElement>();
      le.ignoreLayout = true;

      _textTag = _uiBuilder.Text.Instantiate().SetColor(new Color(0.75f, 0f, 0f)).SetFontSize(16f);
      _errorTextTargetTransform = modification;

      var vlg = modification.gameObject.AddComponent<VerticalLayoutGroup>();
      vlg.padding = new RectOffset(16, 16, 16, 16);
      vlg.childForceExpandHeight = true;
      vlg.childForceExpandWidth = true;
      vlg.spacing = 4f;

      var horizontalTag = _uiBuilder
         .LayoutHorizontal.Instantiate()
         .SetChildAlignment(TextAnchor.LowerCenter)
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.Unconstrained);

      _imageContainer = horizontalTag.Create(modification.transform);
      _signalBus.Subscribe<BeatmapEditingModeSwitchedSignal>(HandleEditingEventBoxGroupChangedWithSignal);
      _signalBus.Subscribe<EventBoxesUpdatedSignal>(HandleEditingEventBoxGroupChanged);
      _signalBus.Subscribe<EventBoxModifiedSignal>(HandleEditingEventBoxGroupChanged);
      _signalBus.Subscribe<EventBoxSelectedSignal>(HandleEditingEventBoxGroupChanged);
   }

   public void Dispose()
   {
      _signalBus.TryUnsubscribe<BeatmapEditingModeSwitchedSignal>(HandleEditingEventBoxGroupChangedWithSignal);
      _signalBus.TryUnsubscribe<EventBoxesUpdatedSignal>(HandleEditingEventBoxGroupChanged);
      _signalBus.TryUnsubscribe<EventBoxModifiedSignal>(HandleEditingEventBoxGroupChanged);
      _signalBus.TryUnsubscribe<EventBoxSelectedSignal>(HandleEditingEventBoxGroupChanged);
   }

   private void HandleEditingEventBoxGroupChangedWithSignal(BeatmapEditingModeSwitchedSignal signal)
   {
      if (signal.mode == BeatmapEditingMode.EventBoxes) HandleEditingEventBoxGroupChanged();
   }

   private void HandleEditingEventBoxGroupChanged()
   {
      if (_ebv._eventBoxGroupsState.eventBoxGroupContext == null || _ebv._eventBoxView._eventBox == null) return;

      foreach (var t in _instantiatedErrorText) Object.Destroy(t);
      _instantiatedErrorText.Clear();

      var groupSize = _ebv._beatmapEventBoxGroupsDataModel._groupIdToGroupSize.GetValueOrDefault(
         _ebv._eventBoxGroupsState.eventBoxGroupContext.groupId,
         0);

      var boxes = _ebv._eventBoxes;
      var box = _ebv._eventBoxView._eventBox;

      int i;
      for (i = 0; i < groupSize; i++)
      {
         var image = i >= _instantiatedImages.Count ? CreateImage() : _instantiatedImages[i];
         image.color = new Color(0.1f, 0.1f, 0.1f);
         image.gameObject.SetActive(true);
      }

      for (; i < _instantiatedImages.Count; i++) _instantiatedImages[i].gameObject.SetActive(false);

      HashSet<int> affectedId = new();
      var currentBoxPassed = false;
      foreach (var (b, x) in boxes.Select((b, x) => (b, x)).Where(b => GetAxis(b.b) == GetAxis(box)))
      {
         var ifh = IndexFilterConverter.Convert(LightshowSaver.ConvertIndexFilter(b.indexFilter), groupSize);
         if (ifh == null)
         {
            if (_instantiatedErrorText.Count > 10) continue;
            var t = _textTag.SetText($"[{x + 1}] Filter is invalid").Create(_errorTextTargetTransform);
            _instantiatedErrorText.Add(t);
            continue;
         }

         if (b == box) currentBoxPassed = true;
         foreach (var (element, _, _) in ifh)
         {
            if (0 > element || element >= groupSize)
            {
               if (_instantiatedErrorText.Count > 10) continue;
               var t = _textTag
                  .SetText($"[{x + 1}] Filter returned OOB ID {element}")
                  .Create(_errorTextTargetTransform);
               _instantiatedErrorText.Add(t);
               continue;
            }

            if (affectedId.Add(element))
            {
               _instantiatedImages[element].color =
                  b == box ? Color.green : currentBoxPassed ? Color.gray : Color.white;
            }
            else if (b == box) _instantiatedImages[element].color = Color.red;
         }
      }
   }

   private LightAxis GetAxis(EventBoxEditorData data)
   {
      return data switch
      {
         LightRotationEventBoxEditorData rot => rot.axis,
         LightTranslationEventBoxEditorData tr => tr.axis,
         _ => LightAxis.X
      };
   }

   private Image CreateImage()
   {
      var go = new GameObject("Image");
      go.transform.SetParent(_imageContainer.transform);
      var image = go.AddComponent<Image>();
      image.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
      image.raycastTarget = false;

      _instantiatedImages.Add(image);
      return image;
   }
}