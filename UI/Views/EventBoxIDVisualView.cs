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
using EditorEnhanced.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

internal class EventBoxIDVisualView : IInitializable, IDisposable
{
   private readonly List<GameObject> _instantiatedErrorText = new();

   private readonly List<Image> _instantiatedImages = new();
   private readonly SignalBus _signalBus;
   private readonly UIBuilder _uiBuilder;
   private readonly EditorViewLocator _viewLocator;

   private EventBoxesView _ebv;
   private Transform _errorTextTargetTransform;
   private GameObject _imageContainer;
   private TMP_Text _maxDurationText;
   private float? _minimumDuration;
   private int _selectedIdCount;
   private Button _setDurationButton;

   private EditorTextTag _textTag;

   public EventBoxIDVisualView(
      SignalBus signalBus,
      EditorViewLocator viewLocator,
      UIBuilder uiBuilder)
   {
      _signalBus = signalBus;
      _viewLocator = viewLocator;
      _uiBuilder = uiBuilder;
   }

   public void Dispose()
   {
      _signalBus.TryUnsubscribe<BeatmapEditingModeSwitchedSignal>(HandleEditingEventBoxGroupChangedWithSignal);
      _signalBus.TryUnsubscribe<EventBoxesUpdatedSignal>(HandleEditingEventBoxGroupChanged);
      _signalBus.TryUnsubscribe<EventBoxModifiedSignal>(HandleEditingEventBoxGroupChanged);
      _signalBus.TryUnsubscribe<EventBoxSelectedSignal>(HandleEditingEventBoxGroupChanged);
   }

   public void Initialize()
   {
      if (!_viewLocator.TryGetEventBoxesView(out _ebv)
         || !_viewLocator.TryGetEventBoxView(out var target))
         return;
      if (!_viewLocator.TryFindRect(target.transform, "GroupInfoView", out var modification)) return;
      modification.GetComponent<LayoutElement>().preferredHeight = -1;
      var le = modification.GetChild(0).gameObject.AddComponent<LayoutElement>();
      le.ignoreLayout = true;

      _textTag = _uiBuilder.CreateText().SetColor(new Color(0.75f, 0f, 0f)).SetFontSize(16f);
      _errorTextTargetTransform = modification;

      var vlg = modification.gameObject.AddComponent<VerticalLayoutGroup>();
      vlg.padding = new RectOffset(16, 16, 16, 16);
      vlg.childForceExpandHeight = true;
      vlg.childForceExpandWidth = true;
      vlg.spacing = 4f;

      var horizontalTag = _uiBuilder
         .CreateHorizontalLayout()
         .SetChildAlignment(TextAnchor.LowerCenter)
         .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
         .SetVerticalFit(ContentSizeFitter.FitMode.Unconstrained);

      _imageContainer = horizontalTag.Create(modification.transform);
      horizontalTag.SetChildAlignment(TextAnchor.MiddleLeft);
      var durationContainer = horizontalTag.Create(modification.transform);
      _maxDurationText = _uiBuilder
         .CreateText()
         .SetText("Max Duration: ∞")
         .SetFontSize(16f)
         .SetTextAlignment(TextAlignmentOptions.Left)
         .Create(durationContainer.transform)
         .GetComponent<TMP_Text>();
      _setDurationButton = _uiBuilder
         .CreateButton()
         .SetText("Set")
         .SetFontSize(16f)
         .SetSize(new Vector2(56f, 32f))
         .SetOnClick(SetMinimumDuration)
         .Create(durationContainer.transform)
         .GetComponent<Button>();
      _signalBus.Subscribe<BeatmapEditingModeSwitchedSignal>(HandleEditingEventBoxGroupChangedWithSignal);
      _signalBus.Subscribe<EventBoxesUpdatedSignal>(HandleEditingEventBoxGroupChanged);
      _signalBus.Subscribe<EventBoxModifiedSignal>(HandleEditingEventBoxGroupChanged);
      _signalBus.Subscribe<EventBoxSelectedSignal>(HandleEditingEventBoxGroupChanged);
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

      var durationById = EventBoxDurationHelpers.GetNextEventBoxGroupDurationById(
         _ebv._beatmapEventBoxGroupsDataModel,
         _ebv._eventBoxGroupsState.eventBoxGroupContext,
         box);
      var selectedIds = IndexFilterHelpers
         .GetIndexFilterRange(box.indexFilter, groupSize)
         .Select(item => item.index)
         .Where(id => id >= 0 && id < groupSize)
         .Distinct()
         .ToArray();
      _selectedIdCount = selectedIds.Length;
      _minimumDuration = durationById.Count == 0 ? null : durationById.Values.Min();
      _setDurationButton.interactable = _minimumDuration.HasValue;
      _maxDurationText.text = GetDurationText(selectedIds.Length, durationById);

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
               _instantiatedImages[element].color =
                  b == box ? Color.green : currentBoxPassed ? Color.gray : Color.white;
            else if (b == box) _instantiatedImages[element].color = Color.red;
         }
      }
   }

   private string GetDurationText(int selectedIdCount, IReadOnlyDictionary<int, float> durationById)
   {
      if (durationById.Count == 0) return "Max Duration: ∞";

      var minimum = durationById.Values.Min();
      if (selectedIdCount <= 1) return $"Max Duration: {minimum:0.###} beats";

      var maximum = durationById.Count < selectedIdCount ? (float?)null : durationById.Values.Max();
      if (maximum.HasValue && Mathf.Approximately(minimum, maximum.Value))
         return $"Max Duration: {minimum:0.###} beats";

      var maximumText = maximum.HasValue ? $"{maximum.Value:0.###}" : "∞";
      return $"Max Duration: {minimum:0.###}–{maximumText} beats";
   }

   private void SetMinimumDuration()
   {
      if (!_minimumDuration.HasValue || _ebv._eventBoxView._eventBox == null) return;

      var eventBox = _ebv._eventBoxView._eventBox;
      var availableDuration = Mathf.Max(0f, _minimumDuration.Value - 0.001f);
      var limitRatio = eventBox.indexFilter.limitAlsoAffectType == IndexFilter.IndexFilterLimitAlsoAffectType.Duration
         && eventBox.indexFilter.limit > 0f
            ? eventBox.indexFilter.limit
            : 1f;

      float distribution;
      if (eventBox.beatDistributionParamType == BeatmapEventDataBox.DistributionParamType.Step)
      {
         var baseEvents = _ebv
            ._beatmapEventBoxGroupsDataModel
            .GetBaseEventsListByEventBoxId(eventBox.id)
            .ToArray();
         var latestEventBeat = baseEvents.Length == 0 ? 0f : baseEvents.Max(baseEvent => baseEvent.beat);
         var effectiveIdCount = _selectedIdCount * limitRatio;
         distribution = effectiveIdCount <= 0f
            ? 0f
            : Mathf.Max(0f, availableDuration - latestEventBeat) / effectiveIdCount;
      }
      else
         distribution = availableDuration * limitRatio;

      _ebv._eventBoxView._beatDistributionInput.SetValue(distribution);
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