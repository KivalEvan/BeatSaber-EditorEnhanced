using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using BeatmapEditor3D.Commands;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Types;
using EditorEnhanced.Gizmo;
using EditorEnhanced.Gizmo.Components;
using EditorEnhanced.Gizmo.Drawers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.MotionPath;

internal sealed class MotionPathEventGizmoController : IInitializable, ITickable, IDisposable
{
   private const int InputLayer = 2;
   private const int InputLayerMask = 1 << InputLayer;
     private const int MaximumPointerHits = 512;
    private const float MinimumRayDenominator = 0.0001f;

    private readonly BeatmapState _beatmapState;
    private readonly MotionPathEventCommitter _committer;
   private readonly DiContainer _diContainer;
   private readonly RaycastHit[] _pointerHits = new RaycastHit[MaximumPointerHits];
   private readonly List<EventGizmo> _rotationGizmos = [];
   private readonly List<EventGizmo> _translationGizmos = [];

   private EventGizmo _active;
    private Camera _camera;
    private int _configuredRotationCount;
    private int _configuredTranslationCount;
    private float _currentBeat;
   private EventGizmo _hovered;
   private InputAction _pointerPositionAction;
   private Transform _root;
    private float _rotationDelta;
    private Vector3 _rotationRadial;
    private Vector3 _startRotationRadial;
   private float _startAxisParameter;
   private bool _missingPrefabWarningLogged;
   private bool _valueChanged;

    public MotionPathEventGizmoController(
       BeatmapState beatmapState,
       MotionPathEventCommitter committer,
       DiContainer diContainer)
    {
       _beatmapState = beatmapState;
       _committer = committer;
      _diContainer = diContainer;
   }

   public void Initialize()
   {
      _root = new GameObject("MotionPathEventGizmos").transform;
      _pointerPositionAction = new InputAction(
         binding: "<Mouse>/position",
         type: InputActionType.Value,
         expectedControlType: "Vector2");
      _pointerPositionAction.Enable();
      MotionPathEventGizmoInputGate.Register(this);
   }

   public void Dispose()
   {
      MotionPathEventGizmoInputGate.Unregister(this);
      Clear();
      if (_pointerPositionAction != null)
      {
         _pointerPositionAction.Disable();
         _pointerPositionAction.Dispose();
      }
      if (_root != null) Object.Destroy(_root.gameObject);
      _rotationGizmos.Clear();
      _translationGizmos.Clear();
      _root = null;
   }

     public void SetCurrentBeat(float currentBeat)
     {
        _currentBeat = currentBeat;
     }

     public void BeginMarkers()
     {
        _configuredRotationCount = 0;
        _configuredTranslationCount = 0;
     }

    public void ConfigureMarker(
      MotionPathEventPoint point,
      float markerSize,
      Material material,
      bool interactionEnabled)
    {
       if (_active != null || !interactionEnabled || point.Sources == null) return;
       MotionPathEventSource preferredSource = null;
       foreach (var source in point.Sources)
          if (source.IsInvertible
              && (preferredSource == null || source.SourceSortOrder < preferredSource.SourceSortOrder))
             preferredSource = source;

       if (preferredSource != null)
          ConfigureSource(point.Beat, point.Position, preferredSource, markerSize, material);
    }

    private void ConfigureSource(
       float beat,
       Vector3 position,
       MotionPathEventSource source,
       float markerSize,
       Material material)
    {
       var isRotation = source.ValueType == MotionPathEventValueType.Rotation;
       var prefab = isRotation ? RotationGizmo.SObject : TranslationGizmo.SObject;
      if (prefab == null)
      {
         if (!_missingPrefabWarningLogged)
         {
            Plugin.Log.Error("The bundled motion-path gizmo prefabs are not initialized.");
            _missingPrefabWarningLogged = true;
         }
         return;
      }
      var gizmos = isRotation
         ? _rotationGizmos
         : _translationGizmos;
      var configuredIndex = isRotation
         ? _configuredRotationCount++
         : _configuredTranslationCount++;
      while (gizmos.Count <= configuredIndex)
         gizmos.Add(
            new EventGizmo(
                _root,
                material,
                _diContainer,
                prefab,
                source.ValueType));
       var gizmo = gizmos[configuredIndex];
       gizmo.Configure(beat, position, source, markerSize, GetCamera());
   }

   public void EndMarkers()
   {
      if (_active != null) return;
      ReleaseFrom(_rotationGizmos, _configuredRotationCount);
      ReleaseFrom(_translationGizmos, _configuredTranslationCount);
      if (_hovered != null && !_hovered.IsActive)
      {
         _hovered = null;
      }
   }

   public void Clear()
   {
      CancelDrag();
      SetHovered(null);
      foreach (var gizmo in _rotationGizmos) gizmo.Release();
      foreach (var gizmo in _translationGizmos) gizmo.Release();
      _configuredRotationCount = 0;
      _configuredTranslationCount = 0;
   }

   private static void ReleaseFrom(IReadOnlyList<EventGizmo> gizmos, int firstUnusedIndex)
   {
      for (var index = firstUnusedIndex; index < gizmos.Count; index++) gizmos[index].Release();
   }

    public void Tick()
    {
       if (_active == null && _configuredRotationCount == 0 && _configuredTranslationCount == 0) return;
       if (_active != null)
      {
          if (IsPointerOverUi())
          {
             CancelDrag();
             SetHovered(null);
             return;
         }
         UpdateDrag();
         return;
      }

      if (IsPointerOverUi() || !TryGetPointerHit(out var gizmo, out _))
         SetHovered(null);
      else
         SetHovered(gizmo);
   }

    internal bool TryBeginPointerGesture()
    {
       if (_active != null
           || (_configuredRotationCount == 0 && _configuredTranslationCount == 0)
           || IsPointerOverUi()
           || !TryGetPointerHit(out var gizmo, out var hit))
          return false;
      var camera = GetCamera();
      if (camera == null) return false;

      _active = gizmo;
      _active.SetState(EventGizmoState.Active);
      _rotationDelta = 0f;
      _valueChanged = false;
      var ray = camera.ScreenPointToRay(_pointerPositionAction.ReadValue<Vector2>());
      if (_active.Source.ValueType == MotionPathEventValueType.Translation)
      {
         _startAxisParameter = GetAxisParameter(ray, _active.OriginalPosition, _active.AxisWorld);
      }
      else
      {
          _rotationRadial = GetRotationRadial(ray, hit.point, _active.OriginalPosition, _active.AxisWorld, camera);
          _startRotationRadial = _rotationRadial;
          _active.SetRotationIndicator(_rotationRadial);
      }
      return true;
   }

   internal void EndPointerGesture()
   {
      if (_active == null) return;
      if (IsPointerOverUi())
      {
         CancelDrag();
         return;
      }
      var active = _active;
      var value = active.PreviewValue;
      var shouldCommit = _valueChanged;
      FinishDrag();
      if (!shouldCommit) return;

       if (Mathf.Approximately(value, active.Source.AuthoredValue)) return;
      if (!_committer.TryCommit(active.Source, value))
         Plugin.Log.Warn("Motion-path event changed or was deleted before the edit could be committed.");
   }

   internal void CancelPointerGesture() => CancelDrag();

   private void UpdateDrag()
   {
      var camera = GetCamera();
      if (camera == null) return;
      var ray = camera.ScreenPointToRay(_pointerPositionAction.ReadValue<Vector2>());
      if (_active.Source.ValueType == MotionPathEventValueType.Translation)
      {
         var axisParameter = GetAxisParameter(ray, _active.OriginalPosition, _active.AxisWorld);
         var worldDelta = axisParameter - _startAxisParameter;
         var parentAxis = _active.Source.ParentWorldMatrix.MultiplyVector(_active.Source.GetLocalAxis());
         var localDelta = parentAxis.sqrMagnitude <= Mathf.Epsilon
            ? 0f
            : Vector3.Dot(_active.AxisWorld * worldDelta, parentAxis) / parentAxis.sqrMagnitude;
         var axisIndex = (int)_active.Source.Axis;
          var localCoordinate = _active.Source.SourceLocalPosition[axisIndex] + localDelta;
          if (!TryInvertTranslation(_active.Source, localCoordinate, out var value)) return;

          value = SnapTranslationValue(_active.Source.AuthoredValue, value);
          var runtimeValue = value * _active.Source.FlipSign;
         var previewCoordinate = LightTranslationEventHandler.ComputeTranslation(
            runtimeValue,
            _active.Source.TranslationLimits,
            _active.Source.RuntimeDistribution,
            _active.Source.DistributionLimits,
            _active.Source.Mirrored);
         var previewLocalDelta = previewCoordinate - _active.Source.SourceLocalPosition[axisIndex];
         _active.SetTranslationPreview(parentAxis * previewLocalDelta, value);
         _valueChanged = !Mathf.Approximately(value, _active.Source.AuthoredValue);
         return;
      }

      if (!TryGetPlanePoint(ray, _active.OriginalPosition, _active.AxisWorld, out var planePoint)) return;
      var radial = Vector3.ProjectOnPlane(planePoint - _active.OriginalPosition, _active.AxisWorld);
      if (radial.sqrMagnitude <= Mathf.Epsilon) return;
      radial.Normalize();
      _rotationDelta += Vector3.SignedAngle(_rotationRadial, radial, _active.AxisWorld);
      _rotationRadial = radial;
       var mirrorSign = _active.Source.Mirrored ? -1f : 1f;
       var parentHandedness = _active.Source.ParentWorldMatrix.determinant < 0f ? -1f : 1f;
       var worldToAuthored = parentHandedness * mirrorSign * _active.Source.FlipSign;
       if (!IsFinite(worldToAuthored) || Mathf.Approximately(worldToAuthored, 0f)) return;
       var authoredDelta = SnapRotationDelta(_rotationDelta / worldToAuthored);
       var rotationValue = NormalizeValue(Mathf.Repeat(_active.Source.AuthoredValue + authoredDelta, 360f));
       var worldAngle = authoredDelta * worldToAuthored;
       if (!IsFinite(worldAngle)) return;
       var snappedRadial = Quaternion.AngleAxis(worldAngle, _active.AxisWorld) * _startRotationRadial;
       _active.SetRotationPreview(snappedRadial, rotationValue, worldAngle);
       _valueChanged = !Mathf.Approximately(rotationValue, _active.Source.AuthoredValue);
   }

   private void CancelDrag()
   {
      if (_active == null) return;
      FinishDrag();
   }

   private void FinishDrag()
   {
      var active = _active;
      _active = null;
      _rotationDelta = 0f;
      _valueChanged = false;
      active.ResetPreview();
      active.SetState(active == _hovered ? EventGizmoState.Hovered : EventGizmoState.Normal);
   }

   private void SetHovered(EventGizmo gizmo)
   {
      if (_hovered == gizmo) return;
      if (_hovered != null && _hovered != _active) _hovered.SetState(EventGizmoState.Normal);
      _hovered = gizmo;
      if (_hovered != null && _hovered != _active) _hovered.SetState(EventGizmoState.Hovered);
   }

   private bool TryGetPointerHit(out EventGizmo gizmo, out RaycastHit hit)
   {
      gizmo = null;
      hit = default;
      var camera = GetCamera();
      if (camera == null || _pointerPositionAction == null) return false;
      var ray = camera.ScreenPointToRay(_pointerPositionAction.ReadValue<Vector2>());
      var hitCount = Physics.RaycastNonAlloc(
         ray,
         _pointerHits,
         Mathf.Infinity,
         InputLayerMask,
         QueryTriggerInteraction.Ignore);
        for (var hitIndex = 0; hitIndex < hitCount; hitIndex++)
       {
          var candidateHit = _pointerHits[hitIndex];
          var candidate = candidateHit.collider.GetComponent<MotionPathEventGizmoHitProxy>()?.Gizmo;
          if (candidate == null || !candidate.IsActive) continue;
          if (gizmo == null || IsPreferredPointerHit(candidate, candidateHit, gizmo, hit))
          {
             gizmo = candidate;
             hit = candidateHit;
          }
       }
       return gizmo != null;
    }

    private bool IsPreferredPointerHit(
       EventGizmo candidate,
       RaycastHit candidateHit,
       EventGizmo current,
       RaycastHit currentHit)
    {
       var candidateBeatDistance = Mathf.Abs(candidate.Beat - _currentBeat);
       var currentBeatDistance = Mathf.Abs(current.Beat - _currentBeat);
        if (candidateBeatDistance != currentBeatDistance) return candidateBeatDistance < currentBeatDistance;
        if (candidate.Beat != current.Beat) return candidate.Beat < current.Beat;
        if (candidate.Source.SourceSortOrder != current.Source.SourceSortOrder)
           return candidate.Source.SourceSortOrder < current.Source.SourceSortOrder;
        return candidateHit.distance < currentHit.distance;
    }

   private Camera GetCamera()
   {
      if (_camera == null || !_camera.isActiveAndEnabled) _camera = Camera.main;
      return _camera;
   }

   private static bool IsPointerOverUi() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

   private static float GetAxisParameter(Ray ray, Vector3 point, Vector3 axis)
   {
      var directionDot = Vector3.Dot(axis, ray.direction);
      var pointDelta = point - ray.origin;
      var pointAxisDot = Vector3.Dot(axis, pointDelta);
      var pointRayDot = Vector3.Dot(ray.direction, pointDelta);
      var denominator = 1f - directionDot * directionDot;
      if (Mathf.Abs(denominator) <= MinimumRayDenominator)
         return pointAxisDot;
      return (directionDot * pointRayDot - pointAxisDot) / denominator;
   }

   private static Vector3 GetRotationRadial(
      Ray ray,
      Vector3 hitPoint,
      Vector3 center,
      Vector3 axis,
      Camera camera)
   {
      var radial = Vector3.ProjectOnPlane(hitPoint - center, axis);
      if (radial.sqrMagnitude <= Mathf.Epsilon
          && TryGetPlanePoint(ray, center, axis, out var planePoint))
         radial = Vector3.ProjectOnPlane(planePoint - center, axis);
      if (radial.sqrMagnitude <= Mathf.Epsilon)
         radial = Vector3.ProjectOnPlane(camera.transform.right, axis);
      if (radial.sqrMagnitude <= Mathf.Epsilon)
         radial = Vector3.ProjectOnPlane(camera.transform.up, axis);
      return radial.normalized;
   }

   private static bool TryGetPlanePoint(Ray ray, Vector3 center, Vector3 normal, out Vector3 point)
   {
      var plane = new Plane(normal, center);
      if (plane.Raycast(ray, out var distance))
      {
         point = ray.GetPoint(distance);
         return true;
      }
      point = center;
      return false;
   }

    private static bool TryInvertTranslation(
      MotionPathEventSource source,
      float localCoordinate,
      out float value)
   {
      var limits = source.TranslationLimits;
      var limitRange = limits.y - limits.x;
      if (Mathf.Approximately(limitRange, 0f))
      {
         value = source.AuthoredValue;
         return false;
      }

      var distribution = source.Mirrored ? -source.RuntimeDistribution : source.RuntimeDistribution;
      var distributionPosition = Mathf.LerpUnclamped(
         source.DistributionLimits.x,
         source.DistributionLimits.y,
         (distribution + 1f) * 0.5f);
      var effectiveTranslation = 2f * ((localCoordinate - distributionPosition - limits.x) / limitRange) - 1f;
      var runtimeTranslation = source.Mirrored ? -effectiveTranslation : effectiveTranslation;
      value = runtimeTranslation / source.FlipSign;
       return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private float SnapRotationDelta(float delta)
    {
       if (!TryGetPrecision(ModifyHoveredLightRotationDeltaRotationCommand._precisions, out var precision))
          return delta;
       var snapped = Mathf.Round(delta / precision) * precision;
       return IsFinite(snapped) ? snapped : delta;
    }

    private float SnapTranslationValue(float authoredValue, float value)
    {
       if (!TryGetPrecision(ModifyHoveredLightTranslationDeltaTranslationCommand._precisions, out var precision))
          return NormalizeValue(value);
       var snapped = authoredValue + Mathf.Round((value - authoredValue) * precision) / precision;
       return NormalizeValue(IsFinite(snapped) ? snapped : value);
    }

    private bool TryGetPrecision(IReadOnlyDictionary<PrecisionType, float> precisions, out float precision)
    {
       return precisions.TryGetValue(_beatmapState.scrollPrecision, out precision)
              && IsFinite(precision)
              && !Mathf.Approximately(precision, 0f);
    }

    private static float NormalizeValue(float value)
    {
       if (!IsFinite(value)) return value;
       var normalized = Mathf.Round(value * 1_000f) / 1_000f;
       return IsFinite(normalized) ? normalized : value;
    }

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

   internal enum EventGizmoState
   {
      Normal,
      Hovered,
      Active
   }

   internal sealed class EventGizmo
   {
      private static readonly Color ActiveColor = new(0.3f, 1f, 1f, 1f);
      private static readonly Color HoverColor = new(1f, 0.95f, 0.35f, 1f);

      private readonly Vector3 _baseModelScale;
      private readonly CapsuleCollider _capsuleCollider;
      private readonly LineRenderer _indicator;
      private readonly GizmoHighlight _modelHighlight;
      private readonly GizmoMaterial _modelMaterial;
      private readonly Transform _modelTransform;
      private readonly SphereCollider _sphereCollider;
      private readonly Transform _transform;
      private Color _axisColor;
      private float _handleSize;
      private Quaternion _modelBaseRotation;
      private Vector3 _modelForward;

      public EventGizmo(
         Transform parent,
         Material material,
         DiContainer diContainer,
         GameObject prefab,
         MotionPathEventValueType valueType)
      {
         var gameObject = new GameObject($"EditableMotionPath{valueType}Event");
         gameObject.layer = InputLayer;
         _transform = gameObject.transform;
         _transform.SetParent(parent, false);
         var model = diContainer.InstantiatePrefab(prefab);
         model.name = $"Bundled{valueType}Handle";
         _modelTransform = model.transform;
         _baseModelScale = _modelTransform.localScale;
         _modelTransform.SetParent(_transform, false);
         SetLayerRecursively(model.transform, InputLayer);
         foreach (var collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
         foreach (var behaviour in model.GetComponentsInChildren<MonoBehaviour>(true))
            if (behaviour is IGizmoInput)
               behaviour.enabled = false;
         _modelHighlight = model.GetComponentInChildren<GizmoHighlight>(true);
         _modelMaterial = model.GetComponent<GizmoMaterial>();
         var proxy = gameObject.AddComponent<MotionPathEventGizmoHitProxy>();
         proxy.Gizmo = this;
         _sphereCollider = gameObject.AddComponent<SphereCollider>();
         _capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
         _indicator = CreateLine(gameObject, "ValueIndicator", material);
         Release();
      }

       public Vector3 AxisWorld { get; private set; }
       public float Beat { get; private set; }
       public bool IsActive => _transform.gameObject.activeSelf;
      public Vector3 OriginalPosition { get; private set; }
      public float PreviewValue { get; private set; }
      public MotionPathEventSource Source { get; private set; }

       public void Configure(
          float beat,
          Vector3 position,
          MotionPathEventSource source,
         float markerSize,
         Camera camera)
       {
          Beat = beat;
          Source = source;
         OriginalPosition = position;
         PreviewValue = source.AuthoredValue;
         var parentAxis = source.ParentWorldMatrix.MultiplyVector(source.GetLocalAxis());
         AxisWorld = parentAxis.sqrMagnitude <= Mathf.Epsilon ? source.GetLocalAxis() : parentAxis.normalized;
         _axisColor = source.Axis switch
         {
            LightAxis.X => new Color(1f, 0.25f, 0.2f, 1f),
            LightAxis.Y => new Color(0.25f, 1f, 0.35f, 1f),
            _ => new Color(0.25f, 0.55f, 1f, 1f)
         };
         _handleSize = Mathf.Max(markerSize, 0.025f);
         _transform.position = position;
         _transform.rotation = Quaternion.identity;
         _transform.localScale = Vector3.one;
         _modelForward = GetModelForward(source);
         _modelBaseRotation = GetModelRotation(source, _modelForward, camera);
         _modelTransform.localPosition = Vector3.zero;
         _modelTransform.localScale = _baseModelScale * (_handleSize * 3f);
         _modelTransform.gameObject.SetActive(true);
         _transform.gameObject.SetActive(true);
         _modelTransform.rotation = _modelBaseRotation;
         _indicator.gameObject.SetActive(false);
         if (source.ValueType == MotionPathEventValueType.Rotation)
            ConfigureRotationCollider();
         else
            ConfigureTranslationCollider();
         _modelTransform.rotation = _modelBaseRotation;
         _modelHighlight?.RemoveOutline();
         SetState(EventGizmoState.Normal);
      }

      public void Release()
      {
         Source = null;
         _transform.gameObject.SetActive(false);
      }

      public void SetState(EventGizmoState state)
      {
         var color = state switch
         {
            EventGizmoState.Active => ActiveColor,
            EventGizmoState.Hovered => HoverColor,
            _ => _axisColor
         };
         _modelMaterial?.SetColor(color);
         _indicator.startColor = color;
         _indicator.endColor = color;
      }

      public void SetTranslationPreview(Vector3 worldDelta, float value)
      {
         _transform.position = OriginalPosition + worldDelta;
         PreviewValue = value;
      }

      public void SetRotationPreview(Vector3 radial, float value, float worldAngle)
      {
         SetRotationIndicator(radial);
         _modelTransform.rotation = Quaternion.AngleAxis(worldAngle, AxisWorld) * _modelBaseRotation;
         PreviewValue = value;
      }

      public void SetRotationIndicator(Vector3 radial)
      {
         _indicator.gameObject.SetActive(true);
         _indicator.positionCount = 2;
         _indicator.SetPosition(0, OriginalPosition);
         _indicator.SetPosition(1, OriginalPosition + radial * (_handleSize * 3f));
      }

      public void ResetPreview()
      {
         _transform.position = OriginalPosition;
         _modelTransform.rotation = _modelBaseRotation;
         PreviewValue = Source.AuthoredValue;
         _indicator.gameObject.SetActive(false);
      }

      private void ConfigureRotationCollider()
      {
         _sphereCollider.enabled = true;
         _sphereCollider.radius = _handleSize * 3.75f;
         _capsuleCollider.enabled = false;
      }

      private void ConfigureTranslationCollider()
      {
         var length = _handleSize * 5f;
         _sphereCollider.enabled = false;
         _capsuleCollider.enabled = true;
         _transform.rotation = Quaternion.FromToRotation(Vector3.up, _modelForward);
         _capsuleCollider.direction = 1;
         _capsuleCollider.radius = _handleSize * 0.75f;
         _capsuleCollider.height = length;
         _capsuleCollider.center = Vector3.up * (_handleSize * 1.1f);
      }

      private Vector3 GetModelForward(MotionPathEventSource source)
      {
         var sign = source.Mirrored ? -1f : 1f;
         if (source.ValueType == MotionPathEventValueType.Rotation)
         {
            sign *= source.FlipSign;
            if (source.ParentWorldMatrix.determinant < 0f) sign *= -1f;
         }
         return AxisWorld * sign;
      }

      private static Quaternion GetModelRotation(
         MotionPathEventSource source,
         Vector3 forward,
         Camera camera)
      {
         var localReference = source.Axis == LightAxis.Y ? Vector3.forward : Vector3.up;
         var up = Vector3.ProjectOnPlane(
            source.ParentWorldMatrix.MultiplyVector(localReference),
            forward);
         if (up.sqrMagnitude <= Mathf.Epsilon && camera != null)
            up = Vector3.ProjectOnPlane(camera.transform.up, forward);
         if (up.sqrMagnitude <= Mathf.Epsilon)
            up = Vector3.ProjectOnPlane(Vector3.up, forward);
         if (up.sqrMagnitude <= Mathf.Epsilon)
            up = Vector3.ProjectOnPlane(Vector3.right, forward);
         return Quaternion.LookRotation(forward, up.normalized);
      }

      private static LineRenderer CreateLine(GameObject parent, string name, Material material)
      {
         var lineObject = new GameObject(name) { layer = InputLayer };
         lineObject.transform.SetParent(parent.transform, false);
         var line = lineObject.AddComponent<LineRenderer>();
         line.useWorldSpace = true;
         line.sharedMaterial = material;
         line.numCapVertices = 4;
         line.numCornerVertices = 4;
         return line;
      }

      private static void SetLayerRecursively(Transform root, int layer)
      {
         root.gameObject.layer = layer;
         for (var childIndex = 0; childIndex < root.childCount; childIndex++)
            SetLayerRecursively(root.GetChild(childIndex), layer);
      }
   }
}

internal sealed class MotionPathEventGizmoHitProxy : MonoBehaviour
{
   public MotionPathEventGizmoController.EventGizmo Gizmo;
}
