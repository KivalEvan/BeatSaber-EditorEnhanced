using System;
using System.Collections.Generic;
using System.Linq;
using EditorEnhanced.Configuration;
using EditorEnhanced.Gizmo.Commands;
using EditorEnhanced.Gizmo.Components;
using EditorEnhanced.Gizmo.Drawers;
using EditorEnhanced.Utils;
using UnityEngine;
using UnityEngine.Animations;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.Gizmo;

internal enum GizmoType
{
   Cube,
   Rotation,
   Translation,
   Sphere,
   Lane,
   Selection
}

internal class GizmoAssets : IInitializable, IDisposable
{
   public const float MinSize = 0.1f;
   public const float MaxSize = 10f;

   public static readonly Material DefaultMaterial = FetchMaterial("Assets/Shaders/Gizmo.mat");
   public static readonly Material OutlineMaterial = FetchMaterial("Assets/Shaders/Outline.mat");

   private readonly PluginConfig _config;
   private readonly DiContainer _diContainer;

   private readonly List<GameObject>[] _gizmoObjects = new List<GameObject>[6];
   private readonly GameObject[] _gizmoPrefab = new GameObject[6];
   private readonly HashSet<GameObject> _leasedObjects = [];
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
         gizmos.ForEach(Object.Destroy);
         gizmos.Clear();
      }

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

      for (var i = 0; i < _gizmoObjects.Length; i++) _gizmoObjects[i] = [];

      _signalBus.Subscribe<GizmoColliderConfigChangedSignal>(HandleColliderConfigChanged);
   }

   private void HandleColliderConfigChanged()
   {
      HandleRaycastGizmoUpdate();
      HandleRaycastLaneUpdate();
   }

   private void HandleRaycastGizmoUpdate()
   {
      Action<GameObject> action = _config.Gizmo.RaycastGizmo ? EnableCollider : DisableCollider;
      _gizmoObjects[(int)GizmoType.Cube].ForEach(action);
      _gizmoObjects[(int)GizmoType.Sphere].ForEach(action);
      _gizmoObjects[(int)GizmoType.Rotation].ForEach(action);
      _gizmoObjects[(int)GizmoType.Translation].ForEach(action);
   }

   private void HandleRaycastLaneUpdate()
   {
      Action<GameObject> action = _config.Gizmo.RaycastLane ? EnableCollider : DisableCollider;
      _gizmoObjects[(int)GizmoType.Lane].ForEach(action);
      _gizmoObjects[(int)GizmoType.Selection].ForEach(action);
   }

   private static void EnableCollider(GameObject gizmo)
   {
      gizmo.layer = 22;
   }

   private static void DisableCollider(GameObject gizmo)
   {
      gizmo.layer = 0;
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

   public GameObject GetOrCreate(GizmoType gizmoType, int colorIdx)
   {
      var objects = _gizmoObjects[(int)gizmoType];

      var go = objects.FirstOrDefault(obj => !_leasedObjects.Contains(obj));
      if (go == null)
      {
         var prefab = _gizmoPrefab[(int)gizmoType];
         go = _diContainer.InstantiatePrefab(prefab);
         objects.Add(go);
      }

      _leasedObjects.Add(go);

      var gizmoMat = go.GetComponent<GizmoMaterial>();
      if (gizmoMat == null) return go;

      Action<GameObject> colliderAction;
      if (gizmoType is GizmoType.Lane or GizmoType.Selection)
         colliderAction = _config.Gizmo.RaycastLane ? EnableCollider : DisableCollider;
      else
         colliderAction = _config.Gizmo.RaycastGizmo ? EnableCollider : DisableCollider;

      colliderAction(go);

      var color = GetColor(colorIdx);
      gizmoMat.SetColor(color);

      // var lineRenderController = go.GetComponent<LineRenderController>();
      // if (lineRenderController != null)
      // {
      //     lineRenderController.SetMaterial(mat);
      //     lineRenderController.SetTransforms([]);
      //     lineRenderController.enabled = false;
      // }

      return go;
   }

   public void Release(GameObject gizmo)
   {
      if (gizmo == null) return;

      _leasedObjects.Remove(gizmo);
      gizmo.SetActive(false);
      foreach (var poolable in gizmo.GetComponentsInChildren<IGizmoPoolable>(true)) poolable.ResetForPool();
      foreach (var constraint in gizmo.GetComponentsInChildren<PositionConstraint>(true)) constraint.SetSources([]);
      foreach (var constraint in gizmo.GetComponentsInChildren<RotationConstraint>(true)) constraint.SetSources([]);
      gizmo.transform.SetParent(null, false);
      gizmo.transform.localPosition = Vector3.zero;
      gizmo.transform.localRotation = Quaternion.identity;
   }
}