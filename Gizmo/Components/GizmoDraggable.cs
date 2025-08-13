using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

namespace EditorEnhanced.Gizmo.Components;

public abstract class GizmoDraggable : MonoBehaviour, IGizmoInput
{
   public LightGroupSubsystem LightGroupSubsystemContext;
   public LightAxis Axis;
   public Transform TargetTransform;
   public bool Mirror;
   [Inject] protected readonly BeatmapState _beatmapState;
   [Inject] protected readonly PluginConfig _config;
   [Inject] protected readonly SignalBus _signalBus;
   protected Camera Camera;
   public EventBoxEditorData EventBoxEditorDataContext;
   protected Vector3 InitialScreenPosition;

   private void Awake()
   {
      Camera = Camera.main;
   }

   protected virtual void OnEnable()
   {
      AdjustSize();
      transform.localRotation = Axis switch
      {
         LightAxis.X => Mirror ? Quaternion.Euler(0, 270, 0) : Quaternion.Euler(0, 90, 0),
         LightAxis.Y => Mirror ? Quaternion.Euler(90, 0, 0) : Quaternion.Euler(270, 0, 0),
         LightAxis.Z => Mirror ? Quaternion.Euler(180, 0, 0) : Quaternion.Euler(0, 0, 0),
         _ => Quaternion.identity
      };
   }

   public bool IsDragging { get; set; }


   public void OnPointerEnter()
   {
   }

   public void OnPointerExit()
   {
   }

   public abstract void OnDrag();
   public abstract void OnMouseClick();
   public abstract void OnMouseRelease();

   protected abstract float GetSize();

   protected void AdjustSize()
   {
      transform.localScale = Vector3.one;
      transform.localScale = new Vector3(
         Mathf.Abs(GetSize() * _config.Gizmo.GlobalScale / transform.lossyScale.x),
         Mathf.Abs(GetSize() * _config.Gizmo.GlobalScale / transform.lossyScale.y),
         Mathf.Abs(GetSize() * _config.Gizmo.GlobalScale / transform.lossyScale.z));
   }

   protected Vector3 GetScreenPosition()
   {
      return new Vector3(
         Mouse.current.position.x.value,
         Mouse.current.position.y.value,
         Camera.WorldToScreenPoint(transform.parent.position).z);
   }
}