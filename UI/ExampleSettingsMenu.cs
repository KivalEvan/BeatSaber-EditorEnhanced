using BeatSaberMarkupLanguage.Attributes;
using TMPro;

namespace EditorEnhanced.Menu;

internal class ExampleSettingsMenu
{
    [UIComponent("example-text")] 
    private readonly TextMeshProUGUI exampleText = null!;

    [UIAction("#post-parse")]
    private void PostParse()
    {
        Plugin.Log.Debug($"{nameof(ExampleSettingsMenu)} parsed");
    }
}