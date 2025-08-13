using UnityEngine;

namespace EditorEnhanced.UI.Interfaces;

public interface IUIRect
{
   Vector2? AnchorMin { get; set; }
   Vector2? AnchorMax { get; set; }
   Vector2? OffsetMin { get; set; }
   Vector2? OffsetMax { get; set; }
   Vector2? SizeDelta { get; set; }
   Rect? Rect { get; set; }
}