using UnityEngine;
using UnityEngine.UI;

namespace EditorEnhanced.UI.Tags;

public class EditorLayoutHorizontalTag : EditorLayoutTag
{
   public override string Name { get; set; } = "EEHorizontalLayoutGroup";

   protected override void CreateAndConfigureLayoutGroup(GameObject go)
   {
      var hlg = go.AddComponent<HorizontalLayoutGroup>();
      hlg.spacing = Spacing ?? hlg.spacing;
      hlg.padding = Padding ?? hlg.padding;
      hlg.childAlignment = ChildAlignment ?? hlg.childAlignment;
      hlg.childControlWidth = ChildControlWidth ?? hlg.childControlWidth;
      hlg.childControlHeight = ChildControlHeight ?? hlg.childControlHeight;
      hlg.childScaleWidth = ChildScaleWidth ?? hlg.childScaleWidth;
      hlg.childScaleHeight = ChildScaleHeight ?? hlg.childScaleHeight;
      hlg.childForceExpandWidth = ChildForceExpandWidth ?? hlg.childForceExpandWidth;
      hlg.childForceExpandHeight = ChildForceExpandHeight ?? hlg.childForceExpandHeight;
   }
}