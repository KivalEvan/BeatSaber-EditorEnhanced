using System;
using System.Collections.Generic;
using System.Linq;
using BeatmapEditor3D;
using EditorEnhanced.Commands;
using EditorEnhanced.Managers;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using HMUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = System.Object;

namespace EditorEnhanced.UI.Views;

public class RandomSeedClipboardViewController(
    SignalBus signalBus,
    EditBeatmapViewController ebvc,
    EditBeatmapNavigationViewController ebnvc,
    RandomSeedClipboardManager rscm,
    EditorLayoutVerticalBuilder elvb,
    EditorLayoutHorizontalBuilder elhb,
    EditorButtonBuilder ebb,
    EditorCheckboxBuilder ecb,
    EditorTextBuilder etb) : IInitializable, IDisposable
{
    private readonly List<GameObject> _texts = [];

    public void Initialize()
    {
        var target = ebvc._eventBoxesView._eventBoxView._indexFilterView;
        var targetNav = ebnvc._eventBoxGroupsToolbarView;

        var verticalTag = elvb.CreateNew();
        var horizontalTag = elhb.CreateNew();
        var textTag = etb.CreateNew();
        var checkboxTag = ecb.CreateNew().SetFontSize(10f);
        var buttonTag = ebb.CreateNew().SetFontSize(10f);

        var vt = verticalTag
            .SetChildControlWidth(true)
            .SetSpacing(2f)
            .CreateObject(targetNav.transform);
        vt.transform.SetSiblingIndex(ebnvc._eventBoxGroupsToolbarView._extensionToggle.transform.parent.GetSiblingIndex());
        
        var ht = horizontalTag.CreateObject(vt.transform);
        textTag
            .SetText("Seeds")
            .SetFontWeight(FontWeight.Bold)
            .SetTextAlignment(TextAlignmentOptions.Center)
            .CreateObject(ht.transform);
        ht = horizontalTag.CreateObject(vt.transform);
        var text = textTag
            .SetText(rscm.Seed.ToString())
            .SetFontSize(10f)
            .SetFontWeight(FontWeight.Regular)
            .SetTextAlignment(TextAlignmentOptions.Center)
            .CreateObject(ht.transform);
        _texts.Add(text);
        
        ht = horizontalTag.CreateObject(vt.transform);
        checkboxTag
            .SetBool(rscm.RandomOnPaste)
            .SetText("Paste New")
            .SetOnValueChange(ToggleNewSeed)
            .CreateObject(ht.transform);
        
        ht = horizontalTag.CreateObject(vt.transform);
        checkboxTag
            .SetBool(rscm.UseClipboard)
            .SetText("Use Copy")
            .SetOnValueChange(ToggleClipboard)
            .CreateObject(ht.transform);

        text = textTag
            .SetText(rscm.Seed.ToString())
            .SetFontSize(14f)
            .SetTextAlignment(TextAlignmentOptions.Left)
            .CreateObject(target._randomSeedValidator.transform.parent);
        _texts.Add(text);z
        text.GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(-55f, -30f);
        buttonTag.SetText("C").SetOnClick(CopySeed).CreateObject(target._newSeedButton.transform.parent)
            .GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(-32f, -30f);
        buttonTag.SetText("P").SetOnClick(PasteSeed).CreateObject(target._newSeedButton.transform.parent)
            .GetComponent<RectTransform>()
            .anchoredPosition = new Vector2(0f, -30f);
    }

    private void ToggleNewSeed(bool value)
    {
        rscm.RandomOnPaste = value;
    }

    private void ToggleClipboard(bool value)
    {
        rscm.UseClipboard = value;
    }

    private void CopySeed()
    {
        rscm.Seed = ebvc._eventBoxesView._eventBoxView._eventBox.indexFilter.seed;
        _texts.ForEach(t => t.GetComponent<CurvedTextMeshPro>().text = rscm.Seed.ToString());
    }

    private void PasteSeed()
    {
        signalBus.Fire(new PasteEventBoxSeedSignal(ebvc._eventBoxesView._eventBoxView._eventBox, rscm.Seed));
    }

    public void Dispose()
    {
        foreach (var gameObject in _texts)
        {
            UnityEngine.Object.Destroy(gameObject);
        }
        _texts.Clear();
    }
}