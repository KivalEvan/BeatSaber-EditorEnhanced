using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BeatmapEditor3D.DataModels;
using BeatSaber.TrackDefinitions.DataModels;
using HMUI;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.Gizmo;

public class GizmoInfo : MonoBehaviour
{
   [Inject] private readonly EventBoxGroupsState _ebgs = null!;
   private List<LightTransformData> data = [];
   private CurvedTextMeshPro tmp;

   private void Awake()
   {
      tmp = GetComponentInChildren<CurvedTextMeshPro>();
   }

   private void OnEnable()
   {
      data = data
         .OrderBy(a => a.Index)
         .ThenBy(a => a.GlobalBoxIndex)
         .ToList();
      InvokeRepeating(nameof(UpdateInfo), .1f, .1f);
   }

   private void OnDisable()
   {
      CancelInvoke(nameof(UpdateInfo));
      tmp.SetText("");
   }

   private void UpdateInfo()
   {
      var sb = new StringBuilder();
      for (var i = 0; i < data.Count; i++)
      {
         switch (_ebgs.eventBoxGroupContext.type)
         {
            case EventBoxGroupType.Rotation:
               sb.AppendLine($"[{data[i].GlobalBoxIndex}::{data[i].Index}] {data[i].Transform.eulerAngles}");
               break;
            case EventBoxGroupType.Translation:
               sb.AppendLine($"[{data[i].GlobalBoxIndex}::{data[i].Index}] {data[i].Transform.position}");
               break;
            case EventBoxGroupType.Color:
            case EventBoxGroupType.FloatFx:
            default:
               break;
         }
      }

      tmp.SetText(sb.ToString());
   }

   public void Clear()
   {
      data.Clear();
   }

   public void AddLightTransform(LightTransformData lightTransformData)
   {
      data.Add(lightTransformData);
   }
}