using System.Collections.Generic;
using System.Linq;
using System.Text;
using HMUI;
using UnityEngine;

namespace EditorEnhanced.Gizmo;

public class GizmoInfo : MonoBehaviour
{
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
   }

   private void UpdateInfo()
   {
      var sb = new StringBuilder();
      for (var i = 0; i < data.Count; i++)
         sb.AppendLine($"[{data[i].GlobalBoxIndex}::{data[i].Index}] {data[i].Transform.position}");

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