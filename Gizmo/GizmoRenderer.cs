using System;
using System.Collections.Generic;
using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo.Components;
using EditorEnhanced.UI;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using Zenject;
using EventBoxGroupType = BeatSaber.TrackDefinitions.DataModels.EventBoxGroupType;
using Object = UnityEngine.Object;

namespace EditorEnhanced.Gizmo;

public sealed class GizmoRenderer : IInitializable, IDisposable
{
   private readonly List<GizmoInstance> _activeGizmos = [];
   private readonly PluginConfig _config;
   private readonly DiContainer _container;
   private readonly GizmoAssets _gizmoAssets;
   private readonly Dictionary<int, GizmoHighlightController> _highlighterMap = [];
   private readonly UIBuilder _uiBuilder;
   private readonly EditorViewLocator _viewLocator;

   private GizmoDragInputSystem _dragInputSystem;
   private GizmoInfo _gizmoInfo;

   public GizmoRenderer(
      GizmoAssets gizmoAssets,
      PluginConfig config,
      DiContainer container,
      UIBuilder uiBuilder,
      EditorViewLocator viewLocator)
   {
      _gizmoAssets = gizmoAssets;
      _config = config;
      _container = container;
      _uiBuilder = uiBuilder;
      _viewLocator = viewLocator;
   }

   public void Dispose()
   {
      Clear();
      if (_gizmoInfo != null) Object.Destroy(_gizmoInfo.gameObject);
      if (_dragInputSystem != null) Object.Destroy(_dragInputSystem.gameObject);
   }

   public void Initialize()
   {
      var dragInputObject = new GameObject("GizmoDragInputSystem");
      dragInputObject.SetActive(false);
      _dragInputSystem = dragInputObject.AddComponent<GizmoDragInputSystem>();

      var infoObject = _uiBuilder
         .CreateStackLayout()
         .SetName("GizmoInfo")
         .SetPreferredWidth(0)
         .SetPreferredHeight(0)
         .SetAnchorMin(new Vector2(0f, 1f))
         .SetAnchorMax(new Vector2(0f, 1f))
         .Create(_viewLocator.GetEditorRoot());
      infoObject.SetActive(false);
      _gizmoInfo = _container.InstantiateComponent<GizmoInfo>(infoObject);
      _uiBuilder
         .CreateText()
         .SetColor(Color.white)
         .SetTextAlignment(TextAlignmentOptions.TopLeft)
         .SetFontSize(12f)
         .SetCharacterSpacing(16f)
         .Create(infoObject.transform);
   }

   public void Render(GizmoPlan plan)
   {
      Clear();
      foreach (var batch in plan.Batches) RenderBatch(plan.Root, batch);

      _gizmoInfo.gameObject.SetActive(_config.Gizmo.ShowInfo);
      _dragInputSystem.gameObject.SetActive(true);
   }

   public void Clear()
   {
      foreach (var gizmo in _activeGizmos) _gizmoAssets.Release(gizmo);
      _activeGizmos.Clear();
      _highlighterMap.Clear();

      if (_gizmoInfo != null)
      {
         _gizmoInfo.gameObject.SetActive(false);
         _gizmoInfo.Clear();
      }

      if (_dragInputSystem != null) _dragInputSystem.gameObject.SetActive(false);
   }

   private void RenderBatch(Transform root, GizmoRenderBatch batch)
   {
      var transforms = batch.Transforms;
      var onlyUnique = HasSingleAxisBoxIndex(transforms);
      _highlighterMap.Clear();

      foreach (var data in transforms)
      {
         var colorIndex = onlyUnique && !data.Distributed
            ? ColorAssignment.WhiteIndex
            : _config.Gizmo.MulticolorId
               ? ColorAssignment.GetColorIndexEventBox(
                  data.AxisBoxIndex * _config.Gizmo.ColorIdStep,
                  data.ChunkIndex * _config.Gizmo.ColorGradientStep,
                  data.Distributed)
               : ColorAssignment.WhiteIndex;

         if (!_highlighterMap.TryGetValue(data.GlobalBoxIndex, out var sharedHighlightController))
         {
            var laneGizmo = _gizmoAssets.GetOrCreate(GizmoType.Lane, colorIndex);
            laneGizmo.Transform.SetParent(root, false);
            laneGizmo.Swappable.EventBoxEditorDataContext = data.EventBoxContext;
            laneGizmo.LaneScrollable.EventBoxEditorDataContext = data.EventBoxContext;

            var highlightController = laneGizmo.HighlightController;
            highlightController.Init();
            highlightController.Add(laneGizmo.Highlight);
            _highlighterMap.Add(data.GlobalBoxIndex, highlightController);
            sharedHighlightController = highlightController;

            laneGizmo.GameObject.SetActive(_config.Gizmo.ShowLane);
            _activeGizmos.Add(laneGizmo);
         }

         if (data.Transform == null) continue;
         _gizmoInfo.AddLightTransform(data);
         if (!_config.Gizmo.ShowBase) continue;

         var baseGizmo = _gizmoAssets.GetOrCreate(
            _config.Gizmo.DistributeShape && data.Distributed ? GizmoType.Sphere : GizmoType.Cube,
            colorIndex);
         var baseHighlightController = baseGizmo.HighlightController;
         baseHighlightController.SharedWith(sharedHighlightController);
         baseHighlightController.Add(baseGizmo.Highlight);
         SetConstraints(baseGizmo, data.Transform, batch.Subsystem);
         baseGizmo.ScaleController.SetDraggable(null);

         if (_config.Gizmo.ShowModifier)
         {
            var modifierGizmo = GetModifierGizmo(batch.GroupType, batch.Axis);
            if (modifierGizmo != null)
            {
               modifierGizmo.Transform.SetParent(baseGizmo.Transform.GetChild(0), false);
               var modifierHighlightController = modifierGizmo.HighlightController;
               modifierHighlightController.SharedWith(sharedHighlightController);
               modifierHighlightController.Add(modifierGizmo.Highlight);

               var draggable = modifierGizmo.Draggable;
               draggable.EventBoxEditorDataContext = data.EventBoxContext;
               draggable.LightGroupSubsystemContext = batch.Subsystem;
               draggable.Axis = batch.Axis;
               draggable.TargetTransform = data.Transform;
               baseGizmo.ScaleController.SetDraggable(draggable);

               modifierGizmo.GameObject.SetActive(true);
               _activeGizmos.Add(modifierGizmo);
            }
         }

         baseGizmo.GameObject.SetActive(true);
         _activeGizmos.Add(baseGizmo);
      }

      var selection = _gizmoAssets.GetOrCreate(GizmoType.Selection, ColorAssignment.WhiteIndex);
      selection.GameObject.SetActive(_config.Gizmo.ShowLane);
      _activeGizmos.Add(selection);
   }

   private GizmoInstance GetModifierGizmo(EventBoxGroupType groupType, LightAxis axis)
   {
      var axisColor = axis switch
      {
         LightAxis.X => ColorAssignment.RedIndex,
         LightAxis.Y => ColorAssignment.GreenIndex,
         LightAxis.Z => ColorAssignment.BlueIndex,
         _ => ColorAssignment.WhiteIndex
      };
      return groupType switch
      {
         EventBoxGroupType.Rotation => _gizmoAssets.GetOrCreate(GizmoType.Rotation, axisColor),
         EventBoxGroupType.Translation => _gizmoAssets.GetOrCreate(GizmoType.Translation, axisColor),
         _ => null
      };
   }

   private static void SetConstraints(GizmoInstance gizmo, Transform target, LightGroupSubsystem subsystem)
   {
      gizmo.PositionConstraint.SetSources([new ConstraintSource { sourceTransform = target, weight = 1f }]);
      gizmo.RotationConstraint.SetSources(
         [
            new ConstraintSource
            {
               sourceTransform = subsystem is LightTranslationGroup ? target.parent : target, weight = 1f
            }
         ]);
   }

   private static bool HasSingleAxisBoxIndex(LightTransformData[] transforms)
   {
      if (transforms.Length == 0) return false;

      var axisBoxIndex = transforms[0].AxisBoxIndex;
      for (var i = 1; i < transforms.Length; i++)
         if (transforms[i].AxisBoxIndex != axisBoxIndex)
            return false;

      return true;
   }
}
