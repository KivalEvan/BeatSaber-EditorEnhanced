using System;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using EditorEnhanced.Configuration;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Gizmo.Components;

public abstract class GizmoDraggable : MonoBehaviour, IGizmoInput
{
   public LightGroupSubsystem LightGroupSubsystemContext;
   public LightAxis Axis;
   public Transform TargetTransform;
   public bool Mirror;
   [Inject] protected readonly BeatmapState _beatmapState = null!;
   [Inject] protected readonly PluginConfig _config = null!;
   [Inject] protected readonly SignalBus _signalBus = null!;
   protected Camera Camera;
   public EventBoxEditorData EventBoxEditorDataContext;

   private void Awake()
   {
      Camera = Camera.main;
   }

   protected virtual void OnEnable()
   {
      AdjustSize();
      Mirror = LightGroupSubsystemContext switch
      {
         LightRotationGroup lrg => Axis switch
         {
            LightAxis.X => lrg.mirrorX,
            LightAxis.Y => lrg.mirrorY,
            LightAxis.Z => lrg.mirrorZ,
            _ => throw new ArgumentOutOfRangeException()
         },
         LightTranslationGroup ltg => Axis switch
         {
            LightAxis.X => ltg.mirrorX,
            LightAxis.Y => ltg.mirrorY,
            LightAxis.Z => ltg.mirrorZ,
            _ => throw new ArgumentOutOfRangeException()
         },
         _ => Mirror
      };
      transform.localRotation = Axis switch
      {
         LightAxis.X => Mirror ? Quaternion.Euler(0, 270, 180) : Quaternion.Euler(0, 90, 180),
         LightAxis.Y => Mirror ? Quaternion.Euler(90, 0, 0) : Quaternion.Euler(270, 0, 0),
         LightAxis.Z => Mirror ? Quaternion.Euler(180, 0, 90) : Quaternion.Euler(0, 0, 90),
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
}