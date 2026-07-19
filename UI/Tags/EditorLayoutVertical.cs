using UnityEngine;
using UnityEngine.UI;

namespace EditorEnhanced.UI.Tags;

public class EditorLayoutVerticalTag : EditorLayoutTag
{
   public override string Name { get; set; } = "EEVerticalLayoutGroup";

   protected override void CreateAndConfigureLayoutGroup(GameObject go)
   {
      var vlg = go.AddComponent<VerticalLayoutGroup>();
      vlg.spacing = Spacing ?? vlg.spacing;
      vlg.padding = Padding ?? vlg.padding;
      vlg.childAlignment = ChildAlignment ?? vlg.childAlignment;
      vlg.childControlWidth = ChildControlWidth ?? vlg.childControlWidth;
      vlg.childControlHeight = ChildControlHeight ?? vlg.childControlHeight;
      vlg.childScaleWidth = ChildScaleWidth ?? vlg.childScaleWidth;
      vlg.childScaleHeight = ChildScaleHeight ?? vlg.childScaleHeight;
      vlg.childForceExpandWidth = ChildForceExpandWidth ?? vlg.childForceExpandWidth;
      vlg.childForceExpandHeight = ChildForceExpandHeight ?? vlg.childForceExpandHeight;
   }
}
