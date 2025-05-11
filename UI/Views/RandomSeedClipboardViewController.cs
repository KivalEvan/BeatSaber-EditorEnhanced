using BeatmapEditor3D;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using HMUI;
using TMPro;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.UI.Views;

public class RandomSeedClipboardViewController(
    SignalBus signalBus,
    EditBeatmapViewController ebvc,
    EditorButtonBuilder ebb,
    EditorTextBuilder etb) : IInitializable
{
    private int _seed = 0;
    private GameObject _text;
    
    public void Initialize()
    {
        var target = ebvc._eventBoxesView._eventBoxView._indexFilterView;

        var textTag = etb.CreateNew();
        var buttonTag = ebb.CreateNew().SetFontSize(10f);

        _text = textTag.SetText(_seed.ToString()).CreateObject(target._randomSeedValidator.transform.parent);
        _text.GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(-55f, -75f);

        buttonTag.SetText("C").SetOnClick(CopySeed).CreateObject(target._newSeedButton.transform.parent).GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(-32f, -30f);
        buttonTag.SetText("P").SetOnClick(PasteSeed).CreateObject(target._newSeedButton.transform.parent).GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(0f, -30f);
    }

    private void CopySeed()
    {
        _seed = ebvc._eventBoxesView._eventBoxView._eventBox.indexFilter.seed;
        _text.GetComponent<CurvedTextMeshPro>().text = _seed.ToString();
    }

    private void PasteSeed()
    {
        signalBus.Fire(new PasteEventBoxSeedSignal(ebvc._eventBoxesView._eventBoxView._eventBox, _seed));
    }
}