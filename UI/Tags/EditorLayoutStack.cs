using HMUI;
using UnityEngine;
using UnityEngine.UI;

namespace EditorEnhanced.UI.Tags;

public class EditorLayoutStackTag : EditorLayoutTag
{
   public override string Name { get; set; } = "EEStackLayoutGroup";

   protected override void CreateAndConfigureLayoutGroup(GameObject go)
   {
      var slg = go.AddComponent<StackLayoutGroup>();
      slg.padding = Padding ?? slg.padding;
      slg.childAlignment = ChildAlignment ?? slg.childAlignment;
      slg.childForceExpandWidth = ChildForceExpandWidth ?? slg.childForceExpandWidth;
      slg.childForceExpandHeight = ChildForceExpandHeight ?? slg.childForceExpandHeight;
   }

   protected override void ConfigureContentSizeFitter(ContentSizeFitter csf)
   {
      csf.horizontalFit = HorizontalFit ?? csf.horizontalFit;
      csf.verticalFit = VerticalFit ?? ContentSizeFitter.FitMode.PreferredSize;
   }
}