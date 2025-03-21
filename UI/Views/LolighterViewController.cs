using System;
using EditorEnhanced.Commands;
using EditorEnhanced.UI.Extensions;
using EditorEnhanced.UI.Tags;
using UnityEngine;
using Zenject;

namespace EditorEnhanced.UI.Views;

internal class LolighterViewController(SignalBus signalBus) : IInitializable, IDisposable
{
    public void Initialize()
    {
        var target =
            GameObject.Find(
                "/Wrapper/ViewControllers/EditBeatmapNavigationViewController/EventsToolbar");

        new EditorButtonTag()
            .SetText("Commit Crime")
            .SetOnClick(() => signalBus.Fire(new LolighterSignal()))
            .CreateObject(target.transform);
    }

    public void Dispose()
    {
    }
}