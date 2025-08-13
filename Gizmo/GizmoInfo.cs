using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

namespace EditorEnhanced.Gizmo;

public class GizmoInfo : MonoBehaviour
{
    private List<LightTransformData> lightTransformDataArray;
    private TextMeshPro textMeshPro;
    
    private void OnEnable()
    {
        InvokeRepeating(nameof(UpdateInfo), 1f, 1f);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(UpdateInfo));
    }

    private void UpdateInfo()
    {
        var sb = new StringBuilder();
        for (var i = 0; i < lightTransformDataArray.Count; i++)
        {
            sb.AppendLine($"{lightTransformDataArray[i].Transform.position}");
        }
        textMeshPro.SetText(sb.ToString());
    }

    public void Clear()
    {
        lightTransformDataArray.Clear();
    }

    public void AddLightTransform(LightTransformData lightTransformData)
    {
        lightTransformDataArray.Append(lightTransformData);
    }
}