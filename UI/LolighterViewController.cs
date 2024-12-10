using System;
using System.Collections.Generic;
using BeatmapEditor3D.Controller;
using BeatSaberMarkupLanguage.Tags;
using EditorEnhanced.Commands;
using HMUI;
using TMPro;
using UnityEngine;
using Zenject;
using Object = UnityEngine.Object;

namespace EditorEnhanced.UI;

internal class LolighterViewController(SignalBus signalBus) : IInitializable, IDisposable
{
    private List<GameObject> _ui;

    public void Initialize()
    {
        var btnTag = new EditorButtonTextTag();

        var uiWrapper =
            GameObject.Find(
                "/Wrapper/ViewControllers/EditBeatmapViewController/StatusBarView/StatusBarControls/BasicControlsWrapper/SlidersWrapper");

        var cameraWrapper = GameObject.Find("/Wrapper/BeatmapEditorUICameraWrapper");

        _ui = [];

        var go = btnTag.CreateObject(uiWrapper.transform);
        var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
        tmp.text = "Commit Crime";
        var ntb = go.GetComponent<NoTransitionsButton>();
        ntb.onClick.AddListener(() =>
            signalBus.Fire(new LolighterSignal())
        );
        _ui.Add(go);
    }

    public void Dispose()
    {
        foreach (var gameObject in _ui)
        {
            Object.Destroy(gameObject);
        }
    }
}