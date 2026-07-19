using System;
using System.Collections.Generic;
using System.Linq;
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
   private readonly List<GameObject> _activeGizmos = [];
   private readonly PluginConfig _config;
   private readonly DiContainer _container;
   private readonly GizmoAssets _gizmoAssets;
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
      var onlyUnique = transforms.Select(data => data.AxisBoxIndex).ToHashSet().Count == 1;
      var highlighterMap = new Dictionary<int, GizmoHighlightController>();

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

         if (!highlighterMap.ContainsKey(data.GlobalBoxIndex))
         {
            var laneGizmo = _gizmoAssets.GetOrCreate(GizmoType.Lane, colorIndex);
            laneGizmo.transform.SetParent(root, false);
            laneGizmo.GetComponent<GizmoSwappable>().EventBoxEditorDataContext = data.EventBoxContext;
            laneGizmo.GetComponent<GizmoLaneScrollable>().EventBoxEditorDataContext = data.EventBoxContext;

            var highlightController = laneGizmo.GetComponent<GizmoHighlightController>();
            highlightController.Init();
            highlightController.Add(laneGizmo);
            highlighterMap.Add(data.GlobalBoxIndex, highlightController);

            laneGizmo.SetActive(_config.Gizmo.ShowLane);
            _activeGizmos.Add(laneGizmo);
         }

         if (data.Transform == null) continue;
         _gizmoInfo.AddLightTransform(data);
         if (!_config.Gizmo.ShowBase) continue;

         var baseGizmo = _gizmoAssets.GetOrCreate(
            _config.Gizmo.DistributeShape && data.Distributed ? GizmoType.Sphere : GizmoType.Cube,
            colorIndex);
         var baseHighlightController = baseGizmo.GetComponentInChildren<GizmoHighlightController>();
         baseHighlightController.SharedWith(highlighterMap[data.GlobalBoxIndex]);
         baseHighlightController.Add(baseGizmo);
         SetConstraints(baseGizmo, data.Transform, batch.Subsystem);

         if (_config.Gizmo.ShowModifier)
         {
            var modifierGizmo = GetModifierGizmo(batch.GroupType, batch.Axis);
            if (modifierGizmo != null)
            {
               modifierGizmo.transform.SetParent(baseGizmo.transform.GetChild(0), false);
               var modifierHighlightController = modifierGizmo.GetComponent<GizmoHighlightController>();
               modifierHighlightController.SharedWith(highlighterMap[data.GlobalBoxIndex]);
               modifierHighlightController.Add(modifierGizmo);

               var draggable = modifierGizmo.GetComponent<GizmoDraggable>();
               draggable.EventBoxEditorDataContext = data.EventBoxContext;
               draggable.LightGroupSubsystemContext = batch.Subsystem;
               draggable.Axis = batch.Axis;
               draggable.TargetTransform = data.Transform;

               modifierGizmo.SetActive(true);
               _activeGizmos.Add(modifierGizmo);
            }
         }

         baseGizmo.SetActive(true);
         _activeGizmos.Add(baseGizmo);
      }

      var selection = _gizmoAssets.GetOrCreate(GizmoType.Selection, ColorAssignment.WhiteIndex);
      selection.SetActive(_config.Gizmo.ShowLane);
      _activeGizmos.Add(selection);
   }

   private GameObject GetModifierGizmo(EventBoxGroupType groupType, LightAxis axis)
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

   private static void SetConstraints(GameObject gizmo, Transform target, LightGroupSubsystem subsystem)
   {
      gizmo
         .GetComponentInChildren<PositionConstraint>()
         .SetSources([new ConstraintSource { sourceTransform = target, weight = 1f }]);
      gizmo
         .GetComponentInChildren<RotationConstraint>()
         .SetSources(
         [
            new ConstraintSource
            {
               sourceTransform = subsystem is LightTranslationGroup ? target.parent : target, weight = 1f
            }
         ]);
   }
}
