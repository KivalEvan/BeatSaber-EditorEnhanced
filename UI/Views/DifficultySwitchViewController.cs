using System;
using BeatmapEditor3D;
using BeatmapEditor3D.DataModels;
using BeatmapEditor3D.Types;
using BeatmapEditor3D.Views;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI.Views;

public class DifficultySwitchViewController : IInitializable, IDisposable
{
    private readonly EditBeatmapViewController _ebvc;
    private readonly EditorButtonBuilder _editorBtn;
    private readonly EditorLayoutVerticalBuilder _editorLayoutVertical;
    private readonly EditorLayoutHorizontalBuilder _editorLayoutHorizontal;
    private readonly EditorLayoutStackBuilder _editorLayoutStack;
    private readonly EditorTextBuilder _editorText;
    private readonly EditorCheckboxBuilder _editorCheckbox;
    private readonly SignalBus _signalBus;
    private readonly EditBeatmapLevelViewController _eblvc;
    private readonly EditBeatmapLevelNavigationViewController _eblnvc;
    [Inject] private readonly BeatmapFlowCoordinator _bfc;
    [Inject] private readonly BeatmapLevelFlowCoordinator _blfc;
    [Inject] private readonly BeatmapProjectManager _bpm;

    public DifficultySwitchViewController(SignalBus signalBus,
        EditBeatmapViewController ebvc,
        EditBeatmapLevelViewController eblvc,
        EditBeatmapLevelNavigationViewController eblnvc,
        EditorLayoutStackBuilder editorLayoutStack,
        EditorLayoutVerticalBuilder editorLayoutVertical,
        EditorLayoutHorizontalBuilder editorLayoutHorizontal,
        EditorButtonBuilder editorBtn,
        EditorCheckboxBuilder editorCheckbox,
        EditorTextBuilder editorText)
    {
        _signalBus = signalBus;
        _ebvc = ebvc;
        _eblvc = eblvc;
        _eblnvc = eblnvc;
        _editorLayoutStack = editorLayoutStack;
        _editorLayoutVertical = editorLayoutVertical;
        _editorLayoutHorizontal = editorLayoutHorizontal;
        _editorBtn = editorBtn;
        _editorCheckbox = editorCheckbox;
        _editorText = editorText;
    }

    public void Dispose()
    {
    }

    public void Initialize()
    {
        var target = _ebvc.transform;

        var stackTag = _editorLayoutStack.CreateNew()
            .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize);
        var verticalTag = _editorLayoutVertical.CreateNew()
            .SetHorizontalFit(ContentSizeFitter.FitMode.Unconstrained)
            .SetVerticalFit(ContentSizeFitter.FitMode.PreferredSize)
            .SetPadding(new RectOffset(4, 4, 4, 4));
        var horizontalTag = _editorLayoutHorizontal.CreateNew()
            .SetChildAlignment(TextAnchor.LowerCenter)
            .SetChildControlWidth(true)
            .SetSpacing(8)
            .SetPadding(new RectOffset(4, 4, 2, 4));
        var btnTag = _editorBtn.CreateNew()
            .SetFontSize(16);
        var checkboxTag = _editorCheckbox.CreateNew()
            .SetSize(28)
            .SetFontSize(16);

        var container = verticalTag.CreateObject(target.transform);
        var layout = horizontalTag.CreateObject(container.transform);

        btnTag
            .SetText("Switch Difficulty")
            .SetOnClick(() =>
            {
                if (_bfc.anyDataModelDirty)
                {
                    _bfc._simpleMessageViewController.Init("Switching Difficulty",
                        "You have unsaved changes, do you want to save them?", "Save and switch", "No and switch",
                        "No and stay", buttonIdx =>
                        {
                            _bfc.SetDialogScreenViewController(null);
                            if (buttonIdx == 2)
                                return;
                            _blfc.HandleBeatmapFlowCoordinatorDidFinish(
                                buttonIdx == 0 ? SaveType.Save : SaveType.DontSave,
                                _bfc._beatmapCharacteristic, _bfc._beatmapDifficulty);
                            _blfc.HandleEditBeatmapViewControllerOpenBeatmapLevel(_bfc._beatmapCharacteristic,
                                BeatmapDifficulty.Easy);
                        });
                    _bfc.SetDialogScreenViewController(_bfc._simpleMessageViewController, true);
                }
                else
                {
                    _blfc.HandleBeatmapFlowCoordinatorDidFinish(SaveType.DontSave, _bfc._beatmapCharacteristic,
                        _bfc._beatmapDifficulty);
                    _blfc.HandleEditBeatmapViewControllerOpenBeatmapLevel(_bfc._beatmapCharacteristic,
                        BeatmapDifficulty.Easy);
                }
            })
            .CreateObject(layout.transform);
    }
}