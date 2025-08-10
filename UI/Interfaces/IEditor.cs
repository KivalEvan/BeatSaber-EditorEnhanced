using UnityEngine;

namespace EditorEnhanced.UI.Interfaces;

public interface IEditorBuilder<out T> where T : IEditorTag
{
    public T Instantiate();
}

public interface IEditorTag
{
    public string Name { get; set; }
    public GameObject Create(Transform parent);
}