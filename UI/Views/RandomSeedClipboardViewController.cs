using System;
using System.Collections.Generic;
using BeatmapEditor3D;
using EditorEnhanced.Commands;
using EditorEnhanced.Managers;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using HMUI;
using TMPro;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

public class RandomSeedClipboardViewController : IInitializable, IDisposable
{
    private readonly EditorButtonBuilder _ebb;
    private readonly EditBeatmapNavigationViewController _ebnvc;
    private readonly EditBeatmapViewController _ebvc;
    private readonly EditorCheckboxBuilder _ecb;
    private readonly EditorLayoutHorizontalBuilder _elhb;
    private readonly EditorLayoutVerticalBuilder _elvb;
    private readonly EditorTextBuilder _etb;
    private readonly RandomSeedClipboardManager _rscm;
    private readonly SignalBus _signalBus;
    private readonly List<GameObject> _texts = [];

    public RandomSeedClipboardViewController(SignalBus signalBus,
        EditBeatmapViewController ebvc,
        EditBeatmapNavigationViewController ebnvc,
        RandomSeedClipboardManager rscm,
        EditorLayoutVerticalBuilder elvb,
        EditorLayoutHorizontalBuilder elhb,
        EditorButtonBuilder ebb,
        EditorCheckboxBuilder ecb,
        EditorTextBuilder etb)
    {
        _signalBus = signalBus;
        _ebvc = ebvc;
        _ebnvc = ebnvc;
        _rscm = rscm;
        _elvb = elvb;
        _elhb = elhb;
        _ebb = ebb;
        _ecb = ecb;
        _etb = etb;
    }

    public void Dispose()
    {
        foreach (var gameObject in _texts) Object.Destroy(gameObject);
        _texts.Clear();
    }

    public void Initialize()
    {
        var target = _ebvc._eventBoxesView._eventBoxView._indexFilterView;
        var targetNav = _ebnvc._eventBoxGroupsToolbarView;

        var verticalTag = _elvb.CreateNew();
        var horizontalTag = _elhb.CreateNew();
        var textTag = _etb.CreateNew();
        var checkboxTag = _ecb.CreateNew().SetFontSize(10f);
        var buttonTag = _ebb.CreateNew().SetFontSize(10f);

        var vt = verticalTag
            .SetChildControlWidth(true)
            .SetSpacing(2f)
            .CreateObject(targetNav.transform);
        vt.transform.SetSiblingIndex(
            _ebnvc._eventBoxGroupsToolbarView._extensionToggle.transform.parent.GetSiblingIndex());

        var ht = horizontalTag.CreateObject(vt.transform);
        textTag
            .SetText("Seeds")
            .SetFontWeight(FontWeight.Bold)
            .SetTextAlignment(TextAlignmentOptions.Center)
            .CreateObject(ht.transform);
        ht = horizontalTag.CreateObject(vt.transform);
        var text = textTag
            .SetText(_rscm.Seed.ToString())
            .SetFontSize(10f)
            .SetFontWeight(FontWeight.Regular)
            .SetTextAlignment(TextAlignmentOptions.Center)
            .CreateObject(ht.transform);
        _texts.Add(text);

        ht = horizontalTag.CreateObject(vt.transform);
        checkboxTag
            .SetBool(_rscm.RandomOnPaste)
            .SetText("Paste New")
            .SetOnValueChange(ToggleNewSeed)
            .CreateObject(ht.transform);

        ht = horizontalTag.CreateObject(vt.transform);
        checkboxTag
            .SetBool(_rscm.UseClipboard)
            .SetText("Use Copy")
            .SetOnValueChange(ToggleClipboard)
            .CreateObject(ht.transform);

        text = textTag
            .SetText(_rscm.Seed.ToString())
            .SetFontSize(14f)
            .SetTextAlignment(TextAlignmentOptions.Left)
            .CreateObject(target._randomSeedValidator.transform.parent);
        _texts.Add(text);
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
        _rscm.RandomOnPaste = value;
    }

    private void ToggleClipboard(bool value)
    {
        _rscm.UseClipboard = value;
    }

    private void CopySeed()
    {
        _rscm.Seed = _ebvc._eventBoxesView._eventBoxView._eventBox.indexFilter.seed;
        _texts.ForEach(t => t.GetComponent<CurvedTextMeshPro>().text = _rscm.Seed.ToString());
    }

    private void PasteSeed()
    {
        _signalBus.Fire(new PasteEventBoxSeedSignal(_ebvc._eventBoxesView._eventBoxView._eventBox, _rscm.Seed));
    }
}