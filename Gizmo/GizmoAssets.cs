using System;
using System.Collections.Generic;
using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo.Commands;
using EditorEnhanced.Gizmo.Drawers;
using EditorEnhanced.Utils;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.Gizmo;

public enum GizmoType
{
   Cube,
   Rotation,
   Translation,
   Sphere,
   Lane,
   Selection
}

public class GizmoAssets : IInitializable, IDisposable
{
   public const float MinSize = 0.1f;
   public const float MaxSize = 10f;

   public static readonly Material DefaultMaterial = FetchMaterial("Assets/Shaders/Gizmo.mat");
   public static readonly Material OutlineMaterial = FetchMaterial("Assets/Shaders/Outline.mat");

   private readonly PluginConfig _config;
   private readonly DiContainer _diContainer;

   private readonly Stack<GizmoInstance>[] _availableObjects = new Stack<GizmoInstance>[6];
   private readonly List<GizmoInstance>[] _gizmoObjects = new List<GizmoInstance>[6];
   private readonly GameObject[] _gizmoPrefab = new GameObject[6];
   private readonly HashSet<GizmoInstance> _leasedObjects = [];
   private readonly SignalBus _signalBus;

   public GizmoAssets(DiContainer diContainer, SignalBus signalBus, PluginConfig config)
   {
      _diContainer = diContainer;
      _signalBus = signalBus;
      _config = config;
   }

   public void Dispose()
   {
      foreach (var gizmos in _gizmoObjects)
      {
         if (gizmos == null) continue;
         gizmos.ForEach(gizmo => Object.Destroy(gizmo.GameObject));
         gizmos.Clear();
      }

      foreach (var available in _availableObjects) available?.Clear();
      _leasedObjects.Clear();

      _signalBus.TryUnsubscribe<GizmoColliderConfigChangedSignal>(HandleColliderConfigChanged);
   }

   public void Initialize()
   {
      _gizmoPrefab[(int)GizmoType.Cube] = CubeGizmo.SObject = CubeGizmo.Create();
      _gizmoPrefab[(int)GizmoType.Rotation] = RotationGizmo.SObject = RotationGizmo.Create();
      _gizmoPrefab[(int)GizmoType.Translation] = TranslationGizmo.SObject = TranslationGizmo.Create();
      _gizmoPrefab[(int)GizmoType.Sphere] = SphereGizmo.SObject = SphereGizmo.Create();
      _gizmoPrefab[(int)GizmoType.Lane] = LaneGizmo.SObject = LaneGizmo.Create();
      _gizmoPrefab[(int)GizmoType.Selection] = SelectionGizmo.SObject = SelectionGizmo.Create();

      for (var i = 0; i < _gizmoObjects.Length; i++)
      {
         _availableObjects[i] = new Stack<GizmoInstance>();
         _gizmoObjects[i] = [];
      }

      _signalBus.Subscribe<GizmoColliderConfigChangedSignal>(HandleColliderConfigChanged);
   }

   private void HandleColliderConfigChanged()
   {
      HandleRaycastGizmoUpdate();
      HandleRaycastLaneUpdate();
   }

   private void HandleRaycastGizmoUpdate()
   {
      Action<GizmoInstance> action = _config.Gizmo.RaycastGizmo ? EnableCollider : DisableCollider;
      _gizmoObjects[(int)GizmoType.Cube].ForEach(action);
      _gizmoObjects[(int)GizmoType.Sphere].ForEach(action);
      _gizmoObjects[(int)GizmoType.Rotation].ForEach(action);
      _gizmoObjects[(int)GizmoType.Translation].ForEach(action);
   }

   private void HandleRaycastLaneUpdate()
   {
      Action<GizmoInstance> action = _config.Gizmo.RaycastLane ? EnableCollider : DisableCollider;
      _gizmoObjects[(int)GizmoType.Lane].ForEach(action);
      _gizmoObjects[(int)GizmoType.Selection].ForEach(action);
   }

   private static void EnableCollider(GizmoInstance gizmo)
   {
      gizmo.GameObject.layer = 22;
   }

   private static void DisableCollider(GizmoInstance gizmo)
   {
      gizmo.GameObject.layer = 0;
   }

   private static Color GetColor(int index)
   {
      return index is < 0 or >= ColorAssignment.HueRange ? Color.white : ColorAssignment.GetColorFromIndex(index);
   }

   private static Material FetchMaterial(string path)
   {
      var bundle = AssetLoader.LoadFromResource(nameof(EditorEnhanced) + ".model");
      return bundle.LoadAsset<Material>(path);
   }

   internal GizmoInstance GetOrCreate(GizmoType gizmoType, int colorIdx)
   {
      var objects = _gizmoObjects[(int)gizmoType];
      var available = _availableObjects[(int)gizmoType];

      GizmoInstance gizmo;
      if (!available.TryPop(out gizmo))
      {
         var prefab = _gizmoPrefab[(int)gizmoType];
         gizmo = new GizmoInstance(gizmoType, _diContainer.InstantiatePrefab(prefab));
         objects.Add(gizmo);
      }

      _leasedObjects.Add(gizmo);

      if (gizmo.Material == null) return gizmo;

      Action<GizmoInstance> colliderAction;
      if (gizmoType is GizmoType.Lane or GizmoType.Selection)
         colliderAction = _config.Gizmo.RaycastLane ? EnableCollider : DisableCollider;
      else
         colliderAction = _config.Gizmo.RaycastGizmo ? EnableCollider : DisableCollider;

      colliderAction(gizmo);

      var color = GetColor(colorIdx);
      gizmo.Material.SetColor(color);

      // var lineRenderController = go.GetComponent<LineRenderController>();
      // if (lineRenderController != null)
      // {
      //     lineRenderController.SetMaterial(mat);
      //     lineRenderController.SetTransforms([]);
      //     lineRenderController.enabled = false;
      // }

      return gizmo;
   }

   internal void Release(GizmoInstance gizmo)
   {
      if (gizmo == null) return;

      if (!_leasedObjects.Remove(gizmo)) return;

      gizmo.GameObject.SetActive(false);
      foreach (var poolable in gizmo.Poolables) poolable.ResetForPool();
      foreach (var constraint in gizmo.PositionConstraints) constraint.SetSources([]);
      foreach (var constraint in gizmo.RotationConstraints) constraint.SetSources([]);
      gizmo.Transform.SetParent(null, false);
      gizmo.Transform.localPosition = Vector3.zero;
      gizmo.Transform.localRotation = Quaternion.identity;
      _availableObjects[(int)gizmo.Type].Push(gizmo);
   }
}
