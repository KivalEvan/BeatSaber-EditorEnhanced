using UnityEngine;

namespace EditorEnhanced.UI.Interfaces;

public interface IEditorTag
{
   public string Name { get; set; }
   public GameObject Create(Transform parent);
}